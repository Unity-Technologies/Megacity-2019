using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Megacity.Streaming
{
    /// <summary>
    /// Set of jobs to handle streaming in/out of sub scenes
    /// </summary>
    [BurstCompile]
    public partial struct StreamSubScenesIn : IJobEntity
    {
        public NativeList<Entity> AddRequestList;
        public float3 CameraPosition;
        public float MaxDistanceSquared;

        public void Execute(Entity entity, in SceneSectionData sceneData)
        {
            AABB boundingVolume = sceneData.BoundingVolume;
            var distanceSq = boundingVolume.DistanceSq(CameraPosition);
            if (distanceSq < MaxDistanceSquared)
                AddRequestList.Add(entity);
        }
    }

    [BurstCompile]
    public partial struct StreamSubScenesOut : IJobEntity
    {
        public NativeList<Entity> RemoveRequestList;
        public float3 CameraPosition;
        public float MaxDistanceSquared;
        public Hash128 PlayerSectionGUID;
        public Hash128 TrafficSectionGUID;
        
        public void Execute(Entity entity, in SceneSectionData sceneData)
        {
            if (sceneData.SceneGUID == PlayerSectionGUID)
                return;
            
            if (sceneData.SceneGUID == TrafficSectionGUID)
                return;

            AABB boundingVolume = sceneData.BoundingVolume;
            var distanceSq = boundingVolume.DistanceSq(CameraPosition);
            if (distanceSq > MaxDistanceSquared)
                RemoveRequestList.Add(entity);
        }
    }

    [BurstCompile]
    public struct BuildCommandBufferJob : IJob
    {
        public EntityCommandBuffer CommandBuffer;
        public NativeArray<Entity> AddRequestArray;
        public NativeArray<Entity> RemoveRequestArray;

        public void Execute()
        {
            foreach (var entity in AddRequestArray)
            {
                CommandBuffer.AddComponent(entity, new RequestSceneLoaded { LoadFlags = SceneLoadFlags.LoadAdditive });
            }

            foreach (var entity in RemoveRequestArray)
            {
                CommandBuffer.RemoveComponent<RequestSceneLoaded>(entity);
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct StreamingLogicSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StreamingConfig>();
            _query = state.GetEntityQuery(ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<StreamingConfig>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if(SceneController.IsReturningToMainMenu)
                return;
            
            state.CompleteDependency();
            var entityCommandBufferSystem =
                state.World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();

            var streamingLogicConfig = _query.GetSingleton<StreamingConfig>();
            var cameraPosition = _query.GetSingleton<LocalToWorld>().Position;

            var addRequestList = new NativeList<Entity>(Allocator.TempJob);
            var removeRequestList = new NativeList<Entity>(Allocator.TempJob);

            var streamIn = new StreamSubScenesIn
            {
                AddRequestList = addRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingIn *
                                     streamingLogicConfig.DistanceForStreamingIn
            };

            state.Dependency = streamIn.Schedule(state.Dependency);

            var streamOut = new StreamSubScenesOut
            {
                RemoveRequestList = removeRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingOut *
                                     streamingLogicConfig.DistanceForStreamingOut,
                PlayerSectionGUID = streamingLogicConfig.PlayerSectionGUID,
                TrafficSectionGUID = streamingLogicConfig.TrafficSectionGUID,
            };
            state.Dependency = streamOut.Schedule(state.Dependency);
            state.Dependency = new BuildCommandBufferJob
            {
                CommandBuffer = entityCommandBufferSystem.CreateCommandBuffer(),
                AddRequestArray = addRequestList.AsDeferredJobArray(),
                RemoveRequestArray = removeRequestList.AsDeferredJobArray()
            }.Schedule(state.Dependency);
            entityCommandBufferSystem.AddJobHandleForProducer(state.Dependency);
            addRequestList.Dispose(state.Dependency);
            removeRequestList.Dispose(state.Dependency);
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    public partial struct ServerStreamingLogicSystem : ISystem
    {
        private EntityQuery m_Query;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StreamingConfig>();
            m_Query = state.GetEntityQuery(ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<StreamingConfig>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if(SceneController.IsReturningToMainMenu)
                return;
            
            var entityCommandBufferSystem =
                state.World.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>();

            var streamingLogicConfig = m_Query.GetSingleton<StreamingConfig>();
            var cameraPosition = m_Query.GetSingleton<LocalToWorld>().Position;

            var addRequestList = new NativeList<Entity>(Allocator.TempJob);
            var removeRequestList = new NativeList<Entity>(Allocator.TempJob);

            var streamIn = new StreamSubScenesIn
            {
                AddRequestList = addRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingIn *
                                     streamingLogicConfig.DistanceForStreamingIn
            };

            state.Dependency = streamIn.Schedule(state.Dependency);

            var streamOut = new StreamSubScenesOut
            {
                RemoveRequestList = removeRequestList,
                CameraPosition = cameraPosition,
                MaxDistanceSquared = streamingLogicConfig.DistanceForStreamingOut *
                                     streamingLogicConfig.DistanceForStreamingOut,
                PlayerSectionGUID = streamingLogicConfig.PlayerSectionGUID,
                TrafficSectionGUID = streamingLogicConfig.TrafficSectionGUID,
            };
            state.Dependency = streamOut.Schedule(state.Dependency);
            state.Dependency = new BuildCommandBufferJob
            {
                CommandBuffer = entityCommandBufferSystem.CreateCommandBuffer(),
                AddRequestArray = addRequestList.AsDeferredJobArray(),
                RemoveRequestArray = removeRequestList.AsDeferredJobArray()
            }.Schedule(state.Dependency);
            entityCommandBufferSystem.AddJobHandleForProducer(state.Dependency);
            addRequestList.Dispose(state.Dependency);
            removeRequestList.Dispose(state.Dependency);
        }
    }
}
