using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     Calculate and set NPC vehicles speed
    /// </summary>
    [BurstCompile]
    public partial struct VehicleSpeedModerate : IJobEntity
    {
        [ReadOnly] public RoadSectionBlobRef RoadSectionBlobRef;
        [ReadOnly] public NativeArray<Occupation> Occupancy;
        public float DeltaTimeSeconds;

        public void Execute(ref VehiclePathing vehicle)
        {
            var rI = vehicle.RoadIndex;
            int lI = vehicle.LaneIndex;
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[rI];

            var frontOfVehiclePos = vehicle.curvePos + rs.vehicleHalfLen;

            // Look ahead one slot
            var slot = (int)math.floor(frontOfVehiclePos * rs.occupationLimit) + 1;

            if (slot >= rs.occupationLimit)
            {
                if (rs.linkNext != -1)
                {
                    rI = rs.linkNext;
                    rs = RoadSectionBlobRef.Data.Value.RoadSections[rI];
                    slot = 0;
                }
                else
                {
                    --slot;
                }
            }

            var sampleIndex = rI * Constants.RoadIndexMultiplier + slot * Constants.RoadLanes + lI;

            var wantedSpeed = vehicle.speedMult * math.lerp(rs.minSpeed, rs.maxSpeed, vehicle.speedRangeSelected);

            vehicle.WantNewLane = 0;

            vehicle.targetSpeed = math.min(wantedSpeed, Occupancy[sampleIndex].speed);

            var lerpAmount = DeltaTimeSeconds < 1.0f ? DeltaTimeSeconds : 1.0f;
            vehicle.speed = math.lerp(vehicle.speed, vehicle.targetSpeed, lerpAmount);

            if (math.abs(vehicle.targetSpeed - wantedSpeed) > 0.10f * wantedSpeed)
            {
                vehicle.WantNewLane = 1;
            }
        }
    }
}
