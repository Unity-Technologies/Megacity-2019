using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Megacity.Gameplay;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     The system collects all entities with VehiclePrefabData attached.
    ///     Also creates a collection with All entities with RoadSection to create the traffic.
    ///     Reference the Player game object and Rigidbody to move the player's vehicle.
    ///     Move all vehicles including player's vehicle to the next position based on the occupation.
    ///     With all this data create 2 HashMaps to manage the occupation and lane slots.
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
    public partial struct TrafficSystem : ISystem
    {
        private Entity m_PlayerEntity;
        private float m_TransformRemain;
        private EntityQuery m_CarGroup;
        private EntityQuery m_VehiclePrefabQuery;
        private TrafficSettingsData m_TrafficSettings;
        private float3 m_PlayerPosition;
        private float3 m_PlayerVelocity;
        private NativeParallelMultiHashMap<int, VehicleCell> m_Cells;
        private NativeParallelMultiHashMap<int, VehicleSlotData> m_VehicleMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_CarGroup = state.GetEntityQuery(ComponentType.ReadOnly<VehiclePhysicsState>());
            m_VehiclePrefabQuery = state.GetEntityQuery(ComponentType.ReadOnly<VehiclePrefabData>());

            state.RequireForUpdate<PlayerVehicleInput>();
            state.RequireForUpdate<TrafficSettingsData>();
            state.RequireForUpdate<RoadSectionBlobRef>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (m_VehicleMap.IsCreated)
                m_VehicleMap.Dispose();
            if (m_Cells.IsCreated)
                m_Cells.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (m_PlayerEntity == Entity.Null || !Exists(m_PlayerEntity))
            {
                m_PlayerEntity = GetSingletonEntity<PlayerVehicleInput>();
                m_TrafficSettings = GetSingleton<TrafficSettingsData>();
                m_Cells = new NativeParallelMultiHashMap<int, VehicleCell>(m_TrafficSettings.PoolCellVehicleSize,
                    Allocator.Persistent);
                m_VehicleMap = new NativeParallelMultiHashMap<int, VehicleSlotData>(m_TrafficSettings.PoolCellVehicleSize,
                    Allocator.Persistent);
            }
            
            var endSimulationEntityCommandBufferSystem =
                state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var roadSectionBlobRef = GetSingleton<RoadSectionBlobRef>();
            var numSections = roadSectionBlobRef.Data.Value.RoadSections.Length;
            var vehiclePool = m_VehiclePrefabQuery.ToComponentDataArray<VehiclePrefabData>(Allocator.TempJob);

            if (vehiclePool.Length == 0)
                return;

            var queueSlots = new NativeArray<Occupation>(numSections * Constants.RoadIndexMultiplier, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            // Setup job dependencies
            var clearCombineJobHandle = ClearOccupationAndVehicleMap(queueSlots);
            var occupationFillJobHandle =
                MoveVehiclesAndSetOccupations(clearCombineJobHandle, queueSlots, roadSectionBlobRef, ref state);
            var occupationGapJobHandle = FillOccupationGaps(occupationFillJobHandle, queueSlots, roadSectionBlobRef);

            // Sample occupation ahead of each vehicle and slow down to not run into cars in front
            // Also signal if a lane change is wanted.
            var moderatorJobHandle = new VehicleSpeedModerate
            {
                Occupancy = queueSlots,
                RoadSectionBlobRef = roadSectionBlobRef,
                DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime
            }.ScheduleParallel(occupationGapJobHandle);

            // Pick concrete new lanes for cars switching lanes
            var laneSwitchJobHandle = new LaneSwitch
            {
                Occupancy = queueSlots,
                RoadSectionBlobRef = roadSectionBlobRef
            }.ScheduleParallel(moderatorJobHandle);

            // Despawn cars that have run out of road
            var despawnJobHandle = new VehicleDespawnJob
                {
                    EntityCommandBuffer =
                        endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
                }
                .ScheduleParallel(laneSwitchJobHandle);
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(despawnJobHandle);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, despawnJobHandle);

            var carCount = m_CarGroup.CalculateEntityCount();
            if (carCount < m_TrafficSettings.MaxCars)
            {
                var spawn = new VehicleSpawnJob
                {
                    VehiclePool = vehiclePool,
                    RoadSectionBlobRef = roadSectionBlobRef,
                    Occupation = queueSlots,
                    EntityCommandBuffer =
                        endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
                }.ScheduleParallel(occupationGapJobHandle);
                endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(spawn);

                state.Dependency = JobHandle.CombineDependencies(state.Dependency, spawn);
            }

            state.Dependency = MoveVehicles(state.Dependency, ref state);

            // Get rid of occupation data
            state.Dependency = new DisposeArrayJob<Occupation>
            {
                Data = queueSlots
            }.Schedule(JobHandle.CombineDependencies(state.Dependency, laneSwitchJobHandle));

            state.Dependency = new DisposeArrayJob<VehiclePrefabData>
            {
                Data = vehiclePool
            }.Schedule(JobHandle.CombineDependencies(state.Dependency, laneSwitchJobHandle));

            state.CompleteDependency();
        }

        private JobHandle ClearOccupationAndVehicleMap(NativeArray<Occupation> queueSlots)
        {
            var clearJobHandle = new ClearArrayJob<Occupation>
            {
                Data = queueSlots
            }.Schedule(queueSlots.Length, 512);
            var clearHash2Job = new ClearHashJob<VehicleSlotData> {Hash = m_VehicleMap}.Schedule();
            return JobHandle.CombineDependencies(clearJobHandle, clearHash2Job);
        }

        private JobHandle MoveVehiclesAndSetOccupations(JobHandle jobHandle, NativeArray<Occupation> queueSlots,
            RoadSectionBlobRef roadSectionBlobRef, ref SystemState state)
        {
            // Move vehicles along path, compute banking
            var pathingJobHandle = new VehiclePathUpdate
            {
                RoadSectionBlobRef = roadSectionBlobRef,
                DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime * m_TrafficSettings.GlobalSpeedFactor
            }.ScheduleParallel(jobHandle);
            // Move vehicles that have completed their curve to the next curve (or an off ramp)
            var pathLinkJobHandle =
                new VehiclePathLinkUpdate
                {
                    RoadSectionBlobRef = roadSectionBlobRef
                }.ScheduleParallel(pathingJobHandle);
            // Move from lane to lane. PERF: Opportunity to not do for every vehicle.
            var lanePositionJobHandle = new VehicleLanePosition
                {
                    RoadSectionBlobRef = roadSectionBlobRef,
                    DeltaTimeSeconds = state.WorldUnmanaged.Time.DeltaTime
                }
                .ScheduleParallel(pathLinkJobHandle);

            var laneCombineJobHandle = JobHandle.CombineDependencies(jobHandle, lanePositionJobHandle);
            // Compute what cells (of the 16 for each road section) is covered by each vehicle
            var occupationAliasingJobHandle = new OccupationAliasing
                {
                    OccupancyToVehicleMap = m_VehicleMap.AsParallelWriter(),
                    RoadSectionBlobRef = roadSectionBlobRef
                }
                .ScheduleParallel(laneCombineJobHandle);
            return new OccupationFill2
            {
                Occupations = queueSlots,
                _VehicleMap = m_VehicleMap
            }.Schedule(m_VehicleMap, 32,
                occupationAliasingJobHandle);
        }

        private JobHandle FillOccupationGaps(JobHandle occupationFillJobHandle, NativeArray<Occupation> queueSlots,
            RoadSectionBlobRef roadSectionBlobRef)
        {
            // Back-fill the information:
            // |   A      B     |
            // |AAAABBBBBBB     |
            var sections = roadSectionBlobRef.Data.Value.RoadSections.Length;
            var occupationGapJobHandle =
                new OccupationGapFill
                {
                    Occupations = queueSlots
                }.Schedule(sections, 16, occupationFillJobHandle);
            occupationGapJobHandle = new OccupationGapAdjustmentJob
                {
                    Occupations = queueSlots,
                    RoadSectionBlobRef = roadSectionBlobRef
                }
                .Schedule(sections, 32, occupationGapJobHandle);
            occupationGapJobHandle =
                new OccupationGapFill2
                {
                    Occupations = queueSlots
                }.Schedule(sections, 16, occupationGapJobHandle);
            return occupationGapJobHandle;
        }

        [BurstCompile]
        private JobHandle MoveVehicles(JobHandle spawnJobHandle, ref SystemState state)
        {
            var stepsTaken = 0;
            var timeStep = 1.0f / 60.0f;

            JobHandle finalPosition;
            m_TransformRemain += state.WorldUnmanaged.Time.DeltaTime;
            GetPlayerPosition(ref state);

            var movementJobHandle = AssignVehicleToCells(timeStep, spawnJobHandle, ref stepsTaken, ref state);

            if (stepsTaken > 0)
            {
                finalPosition = new VehicleTransformJob().ScheduleParallel(movementJobHandle);
            }
            else
            {
                finalPosition = movementJobHandle;
            }

            return finalPosition;
        }

        private void GetPlayerPosition(ref SystemState state)
        {
            //Assigns the position and velocity based on PlayerVehicleInput entity
            if (m_PlayerEntity != Entity.Null)
            {
                m_PlayerPosition = state.EntityManager.GetComponentData<LocalTransform>(m_PlayerEntity).Position;
                m_PlayerVelocity = state.EntityManager.GetComponentData<PhysicsVelocity>(m_PlayerEntity).Linear;
            }
        }

        private JobHandle AssignVehicleToCells(float timeStep, JobHandle spawnJobHandle, ref int stepsTaken,
            ref SystemState state)
        {
            var playerPosJobScheduled = false;
            var movementJobHandle = JobHandle.CombineDependencies(spawnJobHandle, state.Dependency);
            while (m_TransformRemain >= timeStep)
            {
                var clearHashJob = new ClearHashJob<VehicleCell> {Hash = m_Cells}.Schedule(movementJobHandle);
                var hashJob = new VehicleHashJob {CellMap = m_Cells.AsParallelWriter()}.ScheduleParallel(clearHashJob);


                if (!playerPosJobScheduled)
                {
                    GetPlayerPosition(ref state);
                    playerPosJobScheduled = true;
                }

                hashJob = new PlayerHashJob
                {
                    CellMap = m_Cells,
                    Pos = m_PlayerPosition,
                    Velocity = m_PlayerVelocity
                }.Schedule(hashJob);

                movementJobHandle = new VehicleMovementJob
                {
                    TimeStep = timeStep,
                    Cells = m_Cells,
                }.ScheduleParallel(hashJob);

                m_TransformRemain -= timeStep;
                ++stepsTaken;
            }

            return movementJobHandle;
        }
    }
}