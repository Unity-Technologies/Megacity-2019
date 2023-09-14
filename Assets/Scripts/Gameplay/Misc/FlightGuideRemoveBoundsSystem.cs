using Unity.Entities;
using Unity.Megacity.CameraManagement;
using Unity.Transforms;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Remove level bounds when the camera is in dolly mode
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct FlightGuideRemoveBoundsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelBounds>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (HybridCameraManager.Instance != null && HybridCameraManager.Instance.IsDollyCamera)
            {
                var levelBoundsEntity = SystemAPI.GetSingletonEntity<LevelBounds>();
                state.EntityManager.DestroyEntity(levelBoundsEntity);
            }
        }
    }
}