using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Evaluates if a vehicle is inside of an intersection and Updates the road section.
    /// </summary>
    [BurstCompile]
    internal struct RoadSectionIntersectionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RoadSection> RoadSections;
        [WriteOnly] public NativeArray<int> RoadSectionsToCheck;
        [ReadOnly] public float4 IntersectionPositionRadiusSq;

        private float SqDistance(float3 sphere, float3 minBounds, float3 maxBounds)
        {
            var lessMin = sphere < minBounds;
            var grtMax = sphere > maxBounds;

            var lessSqDist = (minBounds - sphere) * (minBounds - sphere);
            var grtSqDist = (sphere - maxBounds) * (sphere - maxBounds);

            var sqDist = math.select(new float3(0, 0, 0), lessSqDist, lessMin);
            sqDist = math.select(sqDist, grtSqDist, grtMax);

            return math.csum(sqDist);
        }

        public void Execute(int index)
        {
            var rs = RoadSections[index];

            // Compute RoadSection Bounds
            var minBounds = math.min(rs.p1, rs.p2);
            var maxBounds = math.max(rs.p1, rs.p2);

            // Enqueue road sections that overlap
            var sqDistance = SqDistance(IntersectionPositionRadiusSq.xyz, minBounds, maxBounds);

            var overlap = sqDistance <= IntersectionPositionRadiusSq.w;

            RoadSectionsToCheck[index] = overlap ? index : -1;
        }
    }
}
