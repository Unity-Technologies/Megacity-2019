using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Megacity.CameraManagement
{
    /// <summary>
    /// Updates player camera target position and rotation
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerCameraTargetUpdater : ISystem
    {
        public EntityQuery m_CameraTarget;
        public void OnCreate(ref SystemState state)
        {
            m_CameraTarget = state.GetEntityQuery(ComponentType.ReadOnly<PlayerCameraTarget>(),ComponentType.ReadOnly<LocalToWorld>());
            state.RequireForUpdate(m_CameraTarget);
        }

        public void OnUpdate(ref SystemState state)
        {
            var cameraTarget = SystemAPI.GetSingleton<PlayerCameraTarget>();
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            
            if (!HybridCameraManager.Instance.IsCameraReady)
                HybridCameraManager.Instance.PlaceCamera(cameraTarget.Position);
            
            HybridCameraManager.Instance.SetPlayerCameraPosition(cameraTarget.Position, deltaTime);
            HybridCameraManager.Instance.SetPlayerCameraRotation(cameraTarget.Rotation, deltaTime);
        }
    }
}
