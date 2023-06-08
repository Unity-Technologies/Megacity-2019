using Unity.Burst;
using Unity.Entities;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Updates the player position and rotation to match the dolly track
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(PlayerCameraTargetUpdater))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct DollyTrackPlayerUpdater : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            if (HybridCameraManager.Instance == null)
                return;

            if (HybridCameraManager.Instance.m_CameraTargetMode != HybridCameraManager.CameraTargetMode.DollyTrack)
                return;

            var dollyTrackRotation = HybridCameraManager.Instance.GetDollyCameraRotation();
            var dollyTrackPosition = HybridCameraManager.Instance.GetDollyCameraPosition();
            var dollyTrackPlayerUpdaterJob = new DollyTrackPlayerUpdaterJob
            {
                DollyTrackPosition = dollyTrackPosition,
                DollyTrackRotation = dollyTrackRotation,
                DeltaTime = deltaTime
            };

            state.Dependency = dollyTrackPlayerUpdaterJob.ScheduleParallel(state.Dependency);
        }
    }
}
