using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// NPC vehicles and traffic specific components
    /// </summary>
    public static class Constants
    {
        public const float NodePositionRounding = 100.0f; // 100 = 2dp, 1 = 0dp (a meter), .1 = (10 meters) etc.

        public const int
            RoadOccupationSlotsMax =
                16; // Number of slots in a road segment (for a lane length) (so VehicleLength * this = longest possible road section)

        public const int RoadLanes = 3; // Number of lanes (in a line)
        public const int RoadIndexMultiplier = RoadLanes * RoadOccupationSlotsMax;
        public const float VehicleLength = 30.0f;
        public const float VehicleWidth = 10.0f;

        public const float brakeFactor = 1.0f / (5.0f * 60.0f);
        public const float accelFactor = 1.0f / (20.0f * 60.0f);

        public const float VehicleSpeedMin = 0.2f;
        public const float VehicleSpeedMax = 1.0f;
        public const float VehicleSpeedFudge = 100.0f; // Compensate for dt-based updates
        public const byte LaneSwitchDelay = 99;

        public const float AvoidanceRadius = 4.0f;
        public const float AvoidanceRadiusPlayer = 24.0f;
        public const float MaxTetherSquared = 2500f;

        public const float SlowingDistanceMeters = 45.0f;
        public const float MaxSpeedMetersPerSecond = 10.0f;
    }

    public struct VehiclePrefabData : IComponentData
    {
        public Entity VehiclePrefab;
        public float VehicleSpeed;
    }

    public struct TrafficSettingsData : IComponentData
    {
        public float GlobalSpeedFactor;
        public float PathSegments;
        public float MaxCars;
        public int PoolCellVehicleSize;
    }

    public struct VehicleSlotData
    {
        public float Speed;
        public int Id;
    }

    public struct RoadSection : IComponentData
    {
        public int sortIndex;

        // Add lane widths
        public float width;
        public float height;

        // Add lane speeds
        public float minSpeed;
        public float maxSpeed;

        // Cubic segment
        public float3 p0;
        public float3 p1;
        public float3 p2;
        public float3 p3;

        // Arc length between p1 - p2
        public float arcLength;

        public float vehicleHalfLen;
        public float linkExtraChance;

        public int occupationLimit;
        public int linkExtra;
        public int linkNext;
    }

    public struct Spawner : IComponentData
    {
        public float3 Direction;
        public float3 Position;
        public float Time;

        public float minSpeed;
        public float maxSpeed;

        public int delaySpawn;
        public int RoadIndex;
        public int LaneIndex; // see VehiclePathing
        public uint poolSpawn;

        public Random random;
    }

    public struct Occupation
    {
        //some sort of indication of speed of the object at this slot (if any)
        public float speed;

        //uid of vehicle in this slot (or 0 for empty)
        public int occupied;
    }

    public struct VehiclePathing : IComponentData
    {
        public float curvePos; // Center
        public float speedMult;
        public float speedRangeSelected;
        public float targetSpeed;
        public float speed;
        public float LaneTween;

        public Random random;

        public int vehicleId;

        public int VehicleType;

        public int RoadIndex;

        public byte LaneIndex; // 0-2 indicates the road offset (think LANE) of the road segment we are travelling on
        public byte WantedLaneIndex;
        public byte WantNewLane;
        public byte LaneSwitchDelay; // try to avoid hysteresis in lane switching
    }

    public struct VehicleTargetPosition : IComponentData
    {
        public float3 IdealPosition;
        public float IdealSpeed;
    }

    public struct VehiclePhysicsState : IComponentData
    {
        public float3 Position;
        public float3 Heading;
        public float3 Velocity;
        public float BankRadians;
        public float SpeedMult;
    }

    public struct RoadSectionBlobRef : IComponentData
    {
        public BlobAssetReference<RoadSectionBlob> Data;
    }

    public struct RoadSectionBlob
    {
        public BlobArray<RoadSection> RoadSections;
    }

    public struct VehicleCell
    {
        public float3 Position;
        public float3 Velocity;
        public float Radius;
    }
}
