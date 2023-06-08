using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Updates player camera target position and rotation
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerCameraTargetUpdater : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerCameraTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var player = GetSingletonEntity<PlayerCameraTarget>();
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var localToWorld = state.EntityManager.GetComponentData<LocalToWorld>(player);
            if (HybridCameraManager.Instance == null)
                return;

            HybridCameraManager.Instance.SetPlayerCameraPosition(localToWorld.Position, deltaTime);
            HybridCameraManager.Instance.SetPlayerCameraRotation(localToWorld.Rotation, deltaTime);
        }
    }
}
