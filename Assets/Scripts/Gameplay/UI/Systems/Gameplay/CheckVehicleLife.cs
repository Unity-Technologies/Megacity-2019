using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using Unity.MegaCity.Gameplay;
using Unity.MegaCity.UI;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Check if the vehicle has died and show the death message
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct CheckVehicleLife : ISystem
    {
        private float m_PreviousLife;
        private float m_Cooldown;
        private bool m_HasDied;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GhostOwnerIsLocal>();
            state.RequireForUpdate<VehicleHealth>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HybridCameraManager.Instance == null)
                return;

            foreach (var (health, playerScore) in
                SystemAPI.Query<RefRO<VehicleHealth>,RefRO<PlayerScore>>()
                .WithAll<GhostOwnerIsLocal>())
            {
                if (m_PreviousLife > 0 && health.ValueRO.Value <= 0)
                {
                    m_Cooldown = 5f;
                    m_HasDied = true;

                    // Start showing death message in UI
                    HUD.Instance.ShowDeathMessage(playerScore.ValueRO.KillerName.ToString());
                }

                m_PreviousLife = health.ValueRO.Value;
            }

            if (!m_HasDied)
                return;

            if (m_Cooldown > 0)
            {
                HybridCameraManager.Instance.StartDeadFX();
                m_Cooldown -= SystemAPI.Time.DeltaTime;
            }
            else if(m_PreviousLife > 0)
            {
                m_HasDied = false;
                HUD.Instance.HideMessageScreen();
                HybridCameraManager.Instance.StopDeadFX();
            }
        }
    }
}
