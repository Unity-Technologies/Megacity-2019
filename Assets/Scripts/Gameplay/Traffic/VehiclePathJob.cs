using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     Move vehicles along path, compute banking
    /// </summary>

    [BurstCompile]
    public partial struct VehiclePathUpdate : IJobEntity
    {
        [ReadOnly] public RoadSectionBlobRef RoadSectionBlobRef;
        public float DeltaTimeSeconds;

        public void Execute(ref VehiclePathing p, ref VehicleTargetPosition pos, in VehiclePhysicsState physicsState)
        {
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[p.RoadIndex];

            float3 c0 = CatmullRom.GetPosition(rs.p0, rs.p1, rs.p2, rs.p3, p.curvePos);
            float3 c1 = CatmullRom.GetTangent(rs.p0, rs.p1, rs.p2, rs.p3, p.curvePos);
            float3 c2 = CatmullRom.GetConcavity(rs.p0, rs.p1, rs.p2, rs.p3, p.curvePos);

            float curveSpeed = length(c1);

            pos.IdealPosition = c0;
            pos.IdealSpeed = p.speed;

            if (lengthsq(physicsState.Position - c0) < Constants.MaxTetherSquared)
            {
                p.curvePos += Constants.VehicleSpeedFudge / rs.arcLength * p.speed / curveSpeed * DeltaTimeSeconds;
            }
        }
    }
}
