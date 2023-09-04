using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     Calculate if the NPC cars can switch lanes and where their next spot will be at
    /// </summary>
    [BurstCompile]
    public partial struct LaneSwitch : IJobEntity
    {
        [ReadOnly] public NativeArray<Occupation> Occupancy;
        [ReadOnly] public RoadSectionBlobRef RoadSectionBlobRef;

        private bool LaneChangeNoLongerSafe(int start, int end, int rI, int lI, int vid)
        {
            var baseOffset = rI * Constants.RoadIndexMultiplier + lI;
            start *= Constants.RoadLanes;
            end *= Constants.RoadLanes;

            for (var a = start; a <= end; a += Constants.RoadLanes)
            {
                if (Occupancy[baseOffset + a].occupied != 0 && Occupancy[baseOffset + a].occupied != vid)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LaneChangeSafe(int start, int end, int rI, int lI)
        {
            var baseOffset = rI * Constants.RoadIndexMultiplier + lI;
            start *= Constants.RoadLanes;
            end *= Constants.RoadLanes;

            for (var a = start; a <= end; a += Constants.RoadLanes)
            {
                if (Occupancy[baseOffset + a].occupied != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private float LaneChangeSpeed(int start, int rI, int lI)
        {
            var baseOffset = rI * Constants.RoadIndexMultiplier + lI;
            start *= Constants.RoadLanes;

            var speed = Occupancy[baseOffset + start].speed;
            if (speed <= 0.0f)
            {
                return float.MaxValue;
            }

            return speed;
        }

        public void Execute(ref VehiclePathing vehicle)
        {
            var rI = vehicle.RoadIndex;
            int lI = vehicle.LaneIndex;
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[rI];

            var occupationIndexStart = math.max(0, (int)math.floor(vehicle.curvePos * rs.occupationLimit) - 1);
            var occupationIndexEnd = math.min(rs.occupationLimit - 1,
                (int)math.floor(vehicle.curvePos * rs.occupationLimit) + 1);

            if (vehicle.LaneIndex != vehicle.WantedLaneIndex)
            {
                if (vehicle.LaneSwitchDelay > 0)
                {
                    vehicle.LaneSwitchDelay--;
                }
                else
                {
                    if (LaneChangeNoLongerSafe(occupationIndexStart, occupationIndexEnd, rI, vehicle.WantedLaneIndex,
                            vehicle.vehicleId))
                    {
                        vehicle.LaneTween = 1 - vehicle.LaneTween;
                        var tempIdx = vehicle.LaneIndex;
                        vehicle.LaneIndex = vehicle.WantedLaneIndex;
                        vehicle.WantedLaneIndex = tempIdx;
                        vehicle.LaneSwitchDelay = Constants.LaneSwitchDelay;
                    }
                }
            }
            else if (vehicle.WantNewLane != 0)
            {
                int4 laneOptions;
                switch (lI)
                {
                    default:
                        laneOptions = new int4(lI, lI, lI, lI);
                        break;
                    case 0:
                        laneOptions = new int4(lI, lI + 1, lI, lI);
                        break;
                    case 1:
                        laneOptions = new int4(lI - 1, lI + 1, lI, lI);
                        break;
                    case 2:
                        laneOptions = new int4(lI - 1, lI, lI, lI);
                        break;
                }

                var neighbourSpeeds = new float4(
                    LaneChangeSpeed(occupationIndexStart, rI, laneOptions.x),
                    LaneChangeSpeed(occupationIndexStart, rI, laneOptions.y),
                    LaneChangeSpeed(occupationIndexStart, rI, laneOptions.z),
                    LaneChangeSpeed(occupationIndexStart, rI, laneOptions.w));
                var unoccupied = new bool4(
                    LaneChangeSafe(occupationIndexStart, occupationIndexEnd, rI, laneOptions.x),
                    LaneChangeSafe(occupationIndexStart, occupationIndexEnd, rI, laneOptions.y),
                    LaneChangeSafe(occupationIndexStart, occupationIndexEnd, rI, laneOptions.z),
                    LaneChangeSafe(occupationIndexStart, occupationIndexEnd, rI, laneOptions.w));
                var mask = neighbourSpeeds > vehicle.speed;
                mask = mask & unoccupied;

                if (mask.x)
                {
                    vehicle.WantedLaneIndex = (byte)laneOptions.x;
                    vehicle.LaneTween = 0.0f;
                }
                else if (mask.y)
                {
                    vehicle.WantedLaneIndex = (byte)laneOptions.y;
                    vehicle.LaneTween = 0.0f;
                }
            }
        }
    }
}
