using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Spawns new vehicle based on the available occupation
    /// </summary>
    public partial struct VehicleSpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;
        public RoadSectionBlobRef RoadSectionBlobRef;
        [ReadOnly] public NativeArray<Occupation> Occupation;
        [ReadOnly] public NativeArray<VehiclePrefabData> VehiclePool;

        public static int vehicleUID;

        private bool Occupied(int start, int end, int rI, int lI)
        {
            var baseOffset = rI * Constants.RoadIndexMultiplier + lI;
            start *= Constants.RoadLanes;
            end *= Constants.RoadLanes;

            for (var a = start; a <= end; a += Constants.RoadLanes)
            {
                if (Occupation[baseOffset + a].occupied != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetSpawnVehicleIndex(ref Random random, uint poolSpawn)
        {
            if (poolSpawn == 0)
            {
                return random.NextInt(0, VehiclePool.Length);
            }

            // Otherwise we need to figure out which vehicle to assign
            // Todo: could bake the num set bits out!
            var availabilityMask = (uint)~(~0 << VehiclePool.Length);
            var pool = poolSpawn & availabilityMask;
            var numSetBits = pool - ((pool >> 1) & 0x55555555);
            numSetBits = (numSetBits & 0x33333333) + ((numSetBits >> 2) & 0x33333333);
            numSetBits = (((numSetBits + (numSetBits >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;

            // we now have a number between 0 & 32,
            var chosenBitIdx = random.NextInt(0, (int)numSetBits) + 1;
            var poolTemp = pool;
            var lsb = poolTemp;
            //TODO: make the below better?
            while (chosenBitIdx > 0)
            {
                lsb = poolTemp;
                poolTemp &= poolTemp - 1; // clear least significant set bit
                lsb ^= poolTemp; // lsb contains the index (1<<index) of the pool for this position
                chosenBitIdx--;
            }

            var fidx = math.log2(lsb);

            return (int)fidx;
        }

        public void Execute([EntityIndexInQuery] int index, ref Spawner thisSpawner)
        {
            if (thisSpawner.delaySpawn > 0)
            {
                thisSpawner.delaySpawn--;
            }
            else
            {
                var rs = RoadSectionBlobRef.Data.Value.RoadSections[thisSpawner.RoadIndex];
                Interlocked.Increment(ref vehicleUID);

                var backOfVehiclePos = thisSpawner.Time - rs.vehicleHalfLen;
                var frontOfVehiclePos = thisSpawner.Time + rs.vehicleHalfLen;

                var occupationIndexStart = math.max(0, (int)math.floor(backOfVehiclePos * rs.occupationLimit));
                var occupationIndexEnd = math.min(rs.occupationLimit - 1,
                    (int)math.floor(frontOfVehiclePos * rs.occupationLimit));

                if (!Occupied(occupationIndexStart, occupationIndexEnd, thisSpawner.RoadIndex, thisSpawner.LaneIndex))
                {
                    var vehiclePoolIndex = GetSpawnVehicleIndex(ref thisSpawner.random, thisSpawner.poolSpawn);
                    var speedMult = VehiclePool[vehiclePoolIndex].VehicleSpeed;
                    var speedRangeSelected = thisSpawner.random.NextFloat(0.0f, 1.0f);
                    var initialSpeed = 0.0f;

                    var vehicleEntity =
                        EntityCommandBuffer.Instantiate(index, VehiclePool[vehiclePoolIndex].VehiclePrefab);
                    EntityCommandBuffer.SetComponent(index, vehicleEntity, new VehiclePathing
                    {
                        VehicleType = vehiclePoolIndex,
                        vehicleId = vehicleUID,
                        RoadIndex = thisSpawner.RoadIndex, LaneIndex = (byte)thisSpawner.LaneIndex,
                        WantedLaneIndex = (byte)thisSpawner.LaneIndex, speed = initialSpeed,
                        speedRangeSelected = speedRangeSelected, speedMult = speedMult,
                        targetSpeed = initialSpeed, curvePos = thisSpawner.Time,
                        random = new Random(thisSpawner.random.NextUInt(1, uint.MaxValue))
                    });
                    var heading = CatmullRom.GetTangent(rs.p0, rs.p1, rs.p2, rs.p3, 0.0f);
                    EntityCommandBuffer.SetComponent(index, vehicleEntity,
                        new VehicleTargetPosition { IdealPosition = thisSpawner.Position });
                    EntityCommandBuffer.SetComponent(index, vehicleEntity,
                        new VehiclePhysicsState
                            { Position = thisSpawner.Position, Heading = heading, SpeedMult = speedMult });
                }

                var speedInverse = 1.0f / thisSpawner.minSpeed;
                thisSpawner.delaySpawn = (int)Constants.VehicleLength +
                                         thisSpawner.random.NextInt((int)(speedInverse * 10.0f),
                                             (int)(speedInverse * 120.0f));
            }
        }
    }
}
