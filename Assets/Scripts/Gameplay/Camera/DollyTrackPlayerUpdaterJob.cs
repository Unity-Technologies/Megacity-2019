using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.Gameplay;
using Unity.Transforms;

namespace Unity.Megacity.CameraManagement
{
    /// <summary>
    /// Reads the transform data copied from the dolly cart game object and moves the player accordingly
    /// </summary>

    [BurstCompile]
    internal partial struct DollyTrackPlayerUpdaterJob : IJobEntity
    {
        public float3 DollyTrackPosition;
        public quaternion DollyTrackRotation;
        public float DeltaTime;

        public void Execute(
            ref LocalTransform localTransform,
            in PlayerVehicleSettings playerVehicleSettings)
        {
            if (math.distancesq(localTransform.Position, DollyTrackPosition) > playerVehicleSettings.TargetSqLerpThreshold)
            {
                localTransform.Position = DollyTrackPosition;
            }

            localTransform.Position = math.lerp(localTransform.Position, DollyTrackPosition,
                DeltaTime * playerVehicleSettings.TargetFollowDamping);
            localTransform.Rotation = math.slerp(localTransform.Rotation, DollyTrackRotation,
                DeltaTime * playerVehicleSettings.TargetFollowDamping);
        }
    }
}
