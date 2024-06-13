using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Creates a hash for each vehicle and stores the vehicles in a hashmap.
    /// </summary>
    public struct GridHash
    {
        public static readonly float3[] cellOffsets =
        {
            new(0, 0, 0),
            new(-1, 0, 0),
            new(0, -1, 0),
            new(0, 0, -1),
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1)
        };

        public static readonly int2[] cell2DOffsets =
        {
            new(0, 0),
            new(-1, 0),
            new(0, -1),
            new(1, 0),
            new(0, 1)
        };

        public static int Hash(float3 v, float cellSize)
        {
            return Hash(Quantize(v, cellSize));
        }

        public static int3 Quantize(float3 v, float cellSize)
        {
            return new int3(math.floor(v / cellSize));
        }

        public static int Hash(float2 v, float cellSize)
        {
            return Hash(Quantize(v, cellSize));
        }

        public static int2 Quantize(float2 v, float cellSize)
        {
            return new int2(math.floor(v / cellSize));
        }

        public static int Hash(int3 grid)
        {
            unchecked
            {
                // Simple int3 hash based on a pseudo mix of :
                // 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
                // 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
                var hash = grid.x;
                hash = (hash * 397) ^ grid.y;
                hash = (hash * 397) ^ grid.z;
                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
                return hash;
            }
        }

        public static int Hash(int2 grid)
        {
            unchecked
            {
                // Simple int3 hash based on a pseudo mix of :
                // 1) https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
                // 2) https://en.wikipedia.org/wiki/Jenkins_hash_function
                var hash = grid.x;
                hash = (hash * 397) ^ grid.y;
                hash += hash << 3;
                hash ^= hash >> 11;
                hash += hash << 15;
                return hash;
            }
        }

        public static ulong Hash(ulong hash, ulong key)
        {
            const ulong m = 0xc6a4a7935bd1e995UL;
            const int r = 47;

            var h = hash;
            var k = key;

            k *= m;
            k ^= k >> r;
            k *= m;

            h ^= k;
            h *= m;

            h ^= h >> r;
            h *= m;
            h ^= h >> r;

            return h;
        }
    }


    [BurstCompile]
    public partial struct VehicleHashJob : IJobEntity
    {
        public const float kCellSize = 64.0f;

        public NativeParallelMultiHashMap<int, VehicleCell>.ParallelWriter CellMap;

        public void Execute(in VehiclePhysicsState physicsState)
        {
            var radius = Constants.AvoidanceRadius;
            var hash = GridHash.Hash(physicsState.Position, kCellSize);

            CellMap.Add(
                hash,
                new VehicleCell
                {
                    Position = physicsState.Position,
                    Velocity = physicsState.Velocity,
                    Radius = radius
                });
        }
    }

    [BurstCompile]
    public struct PlayerHashJob : IJob
    {
        public float3 Pos;
        public float3 Velocity;
        public NativeParallelMultiHashMap<int, VehicleCell> CellMap;

        public void Execute()
        {
            var radius = Constants.AvoidanceRadiusPlayer;
            var hash = GridHash.Hash(Pos, VehicleHashJob.kCellSize);

            CellMap.Add(
                hash,
                new VehicleCell
                {
                    Position = Pos,
                    Velocity = Velocity,
                    Radius = radius
                });
        }
    }
}
