using Unity.Entities;

namespace Unity.MegaCity.CameraManagement
{
    public struct PlayerHybridCameraTarget : IComponentData
    {
        public float TargetFollowDamping;
    }
}
