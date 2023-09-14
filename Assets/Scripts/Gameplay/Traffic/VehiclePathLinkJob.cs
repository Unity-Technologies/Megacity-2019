using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Update the position of the vehicle inside of the path
    /// </summary>
    [BurstCompile]
    public partial struct VehiclePathLinkUpdate : IJobEntity
    {
        [ReadOnly] public RoadSectionBlobRef RoadSectionBlobRef;

        public void Execute(ref VehiclePathing p)
        {
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[p.RoadIndex];
            if (p.curvePos >= 1.0f)
            {
                float chanceForExtra = p.random.NextFloat(0.0f, 1.0f);
                if (rs.linkExtra >= 0 && chanceForExtra < rs.linkExtraChance)
                {
                    p.RoadIndex = rs.linkExtra;
                    p.curvePos = 0.0f;
                }
                else
                {
                    if (rs.linkNext >= 0)
                    {
                        p.RoadIndex = rs.linkNext;
                        p.curvePos = 0.0f;
                    }
                }
            }
        }
    }
}
