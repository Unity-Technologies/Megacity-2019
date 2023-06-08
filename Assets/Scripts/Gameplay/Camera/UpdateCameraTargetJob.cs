using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Job to update the camera target position and rotation
    /// </summary>
    [BurstCompile]
    public partial struct UpdateCameraTargetJob : IJobEntity
    {
        public LocalToWorld LocalToWorld;
        public float DeltaTime;

        public void Execute(
            ref LocalTransform transform,
            in PlayerHybridCameraTarget playerHybridCameraTarget)
        {
            transform.Position = math.lerp(transform.Position, LocalToWorld.Position,
                DeltaTime * playerHybridCameraTarget.TargetFollowDamping);
            transform.Rotation = math.slerp(transform.Rotation, LocalToWorld.Rotation,
                DeltaTime * playerHybridCameraTarget.TargetFollowDamping);
        }
    }
}
