using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using Unity.MegaCity.UI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Updates the glitch effect when the player is near the bounds.
    /// </summary>
    [UpdateAfter(typeof(UpdateBoundsSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdateGlitchNearBoundsSystem : ISystem
    {
        private bool m_GlitchActive;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelBounds>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var levelBounds = SystemAPI.GetSingleton<LevelBounds>();
            
            if (!levelBounds.IsInside && !m_GlitchActive)
            {
                HybridCameraManager.Instance.EnableGlitch(true);
                HUD.Instance.ShowBoundsMessage();
                m_GlitchActive = true;
            }
            else if (levelBounds.IsInside && m_GlitchActive)
            {
                HybridCameraManager.Instance.EnableGlitch(false);
                HUD.Instance.HideMessageScreen();
                m_GlitchActive = false;
            }
        }
    }
}