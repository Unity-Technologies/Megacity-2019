using Unity.Entities;

namespace Unity.Megacity.CameraManagement
{
    public struct PlayerHybridCameraTarget : IComponentData
    {
        public float TargetFollowDamping;
    }
}
