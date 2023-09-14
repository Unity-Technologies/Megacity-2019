using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     For each Occupancy, fill from end to front the speeds if not occupied
    /// </summary>
    [BurstCompile]
    public struct OccupationGapAdjustmentJob : IJobParallelFor
    {
        public RoadSectionBlobRef RoadSectionBlobRef;

        [NativeDisableParallelForRestriction]
        public NativeArray<Occupation> Occupations;

        public void Execute(int roadIndex)
        {
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[roadIndex];

            if (rs.linkNext < 0)
            {
                return;
            }

            for (var lane = 0; lane < Constants.RoadLanes; lane++)
            {
                var dstSlot = roadIndex * Constants.RoadIndexMultiplier +
                              (rs.occupationLimit - 1) * Constants.RoadLanes + lane;
                var srcSlot = rs.linkNext * Constants.RoadIndexMultiplier + lane;

                var src = Occupations[srcSlot];
                var dst = Occupations[dstSlot];

                if (dst.occupied == 0)
                {
                    dst.speed = src.speed;
                    dst.occupied = src.occupied;
                }
                else
                {
                    dst.speed = math.min(src.speed, dst.speed);
                    dst.occupied = math.min(src.occupied, dst.occupied);
                }

                Occupations[dstSlot] = dst;
            }
        }
    }

    [BurstCompile]
    public struct OccupationGapFill : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Occupation> Occupations;

        public void Execute(int index)
        {
            for (var lane = 0; lane < Constants.RoadLanes; lane++)
            {
                var baseSlot = index * Constants.RoadIndexMultiplier + lane;
                var lastSpeed = float.MaxValue;
                for (var occ = (Constants.RoadOccupationSlotsMax - 1) * Constants.RoadLanes;
                     occ >= 0;
                     occ -= Constants.RoadLanes)
                {
                    var occupation = Occupations[baseSlot + occ];
                    if (occupation.occupied != 0)
                    {
                        lastSpeed = occupation.speed;
                    }
                    else
                    {
                        occupation.speed = lastSpeed;
                        lastSpeed += 0.1f;
                        Occupations[baseSlot + occ] = occupation;
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct OccupationGapFill2 : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Occupation> Occupations;

        public void Execute(int index)
        {
            for (var lane = 0; lane < Constants.RoadLanes; lane++)
            {
                var baseSlot = index * Constants.RoadIndexMultiplier + lane;
                for (var occ = (Constants.RoadOccupationSlotsMax - 2) * Constants.RoadLanes;
                     occ >= 0;
                     occ -= Constants.RoadLanes)
                {
                    var src = Occupations[baseSlot + occ + 1];
                    var dst = Occupations[baseSlot + occ];
                    dst.speed = math.min(src.speed, dst.speed);
                    Occupations[baseSlot + occ] = dst;
                }
            }
        }
    }
}
