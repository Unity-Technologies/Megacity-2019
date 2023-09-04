using Unity.Entities;
using Unity.Megacity.CameraManagement;
using Unity.Megacity.UI;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Update the message screen when the player is near the bounds.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdateMessageNearBoundsSystem : ISystem
    {
        private bool m_MessageActive;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerLocationBounds>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if(HybridCameraManager.Instance == null)
                return;
            
            var playerLocationBounds = SystemAPI.GetSingleton<PlayerLocationBounds>();
            
            if (!playerLocationBounds.IsInside && !m_MessageActive)
            {
                HUD.Instance.ShowBoundsMessage();
                m_MessageActive = true;
            }
            else if (playerLocationBounds.IsInside && m_MessageActive)
            {
                HUD.Instance.HideMessageScreen();
                m_MessageActive = false;
            }
        }
    }
}