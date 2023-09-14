using Unity.Entities;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    public struct PlayerVehicleCameraSettings : IComponentData
    {
        public float FollowCameraZBreakZoom;
        public float FollowCameraZBreakSpeed;
        public float FollowCameraZFollow;
        [GhostField(Quantization = 1000)] public float FollowCameraZOffset;
    }
}
