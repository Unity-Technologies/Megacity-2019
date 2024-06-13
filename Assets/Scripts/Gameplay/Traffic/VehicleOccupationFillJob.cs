using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Reads the road section data and fill occupancy
    /// </summary>
    [BurstCompile]
    public partial struct OccupationAliasing : IJobEntity
    {
        public NativeParallelMultiHashMap<int, VehicleSlotData>.ParallelWriter OccupancyToVehicleMap;
        public RoadSectionBlobRef RoadSectionBlobRef;

        private int CurvePositionToOccupancyIndex(int roadIndex, int laneIndex, float curvePos)
        {
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[roadIndex];

            if (curvePos >= 1.0)
            {
                if (rs.linkNext != -1)
                {
                    roadIndex = rs.linkNext;
                    rs = RoadSectionBlobRef.Data.Value.RoadSections[roadIndex];
                    curvePos = 0.0f;
                }
            }

            // This is unfortunate. Would need linkPrev.
            var slot = math.min(math.max(0, (int)math.floor(curvePos * rs.occupationLimit)), rs.occupationLimit - 1);
            return Constants.RoadIndexMultiplier * roadIndex + slot * Constants.RoadLanes + laneIndex;
        }

        private void OccupyLane(ref VehiclePathing vehicle, ref RoadSection rs, int laneIndex)
        {
            var i0 = CurvePositionToOccupancyIndex(vehicle.RoadIndex, laneIndex, vehicle.curvePos - rs.vehicleHalfLen);
            var i1 = CurvePositionToOccupancyIndex(vehicle.RoadIndex, laneIndex, vehicle.curvePos + rs.vehicleHalfLen);

            var d = new VehicleSlotData { Speed = vehicle.speed, Id = vehicle.vehicleId };

            OccupancyToVehicleMap.Add(i0, d);
            if (i0 != i1)
            {
                OccupancyToVehicleMap.Add(i1, d);
            }
        }

        public void Execute(ref VehiclePathing vehicle)
        {
            var rs = RoadSectionBlobRef.Data.Value.RoadSections[vehicle.RoadIndex];

            OccupyLane(ref vehicle, ref rs, vehicle.LaneIndex);
            if (vehicle.LaneIndex != vehicle.WantedLaneIndex)
            {
                OccupyLane(ref vehicle, ref rs, vehicle.WantedLaneIndex);
            }
        }
    }

    [BurstCompile]
    public struct OccupationFill2 : IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        [NativeDisableParallelForRestriction] public NativeArray<Occupation> Occupations;
        [ReadOnly] public NativeParallelMultiHashMap<int, VehicleSlotData> _VehicleMap;

        public void ExecuteFirst(int index)
        {
        }

        public void ExecuteNext(int firstIndex, int index)
        {
            Execute(index);
        }

        public void Execute(int index)
        {
            if (index >= 0 && index < Occupations.Length && index < _VehicleMap.Count())
            {
                var o = Occupations[index];
                _VehicleMap.TryGetFirstValue(index, out var vehicle, out var iter);
                if (o.occupied != 0)
                {
                    o.occupied = math.min(o.occupied, vehicle.Id);
                    o.speed = math.min(o.speed, vehicle.Speed);
                }
                else
                {
                    o.occupied = vehicle.Id;
                    o.speed = vehicle.Speed;
                }

                Occupations[index] = o;
            }
        }
    }

    [BurstCompile]
    public partial struct OccupationFill : IJobEntity
    {
        [ReadOnly] public NativeArray<RoadSection> RoadSections;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<Occupation> Occupations;

        private void FillOccupation(int startIndex, int endIndex, int rI, int lI, float speed, int vid, int linkNext,
            int occLimit)
        {
            var baseSlot = rI * Constants.RoadIndexMultiplier + lI;
            startIndex *= Constants.RoadLanes;
            if (linkNext >= 0 && endIndex >= occLimit)
            {
                // Fill correct occupation slot in potentially distant road
                var nStart = 0;
                var nBase = linkNext * Constants.RoadIndexMultiplier + lI;
                var end = endIndex - occLimit;
                end *= Constants.RoadLanes;
                for (var a = nStart; a <= end; a += Constants.RoadLanes)
                {
                    Occupations[nBase + a] = new Occupation { occupied = vid, speed = speed };
                }

                endIndex = RoadSections[linkNext].occupationLimit - 1;
            }

            endIndex *= Constants.RoadLanes;
            for (var a = startIndex; a <= endIndex; a += Constants.RoadLanes)
            {
                Occupations[baseSlot + a] = new Occupation { occupied = vid, speed = speed };
            }
        }

        public void Execute(in VehiclePathing vehicle)
        {
            if (vehicle.curvePos < 1.0f)
            {
                var rI = vehicle.RoadIndex;
                int lI = vehicle.LaneIndex;
                var rs = RoadSections[rI];

                var backOfVehiclePos = vehicle.curvePos - rs.vehicleHalfLen;
                var frontOfVehiclePos = vehicle.curvePos + rs.vehicleHalfLen;

                var OccupationIndexStart = math.max(0, (int)math.floor(backOfVehiclePos * rs.occupationLimit));
                int OccupationIndexEnd;

                // It is possible now that the next road link is not next to the current one in memory (e.g. merging)
                if (rs.linkNext != -1)
                {
                    OccupationIndexEnd = (int)math.floor(frontOfVehiclePos * rs.occupationLimit);
                }
                else
                {
                    OccupationIndexEnd = math.min(rs.occupationLimit - 1,
                        (int)math.floor(frontOfVehiclePos * rs.occupationLimit));
                }

                FillOccupation(OccupationIndexStart, OccupationIndexEnd, rI, lI, vehicle.speed,
                    vehicle.vehicleId, rs.linkNext, rs.occupationLimit);
                if (vehicle.LaneIndex != vehicle.WantedLaneIndex)
                {
                    FillOccupation(OccupationIndexStart, OccupationIndexEnd, rI, vehicle.WantedLaneIndex,
                        vehicle.speed, vehicle.vehicleId, rs.linkNext, rs.occupationLimit);
                }
            }
        }
    }
}
