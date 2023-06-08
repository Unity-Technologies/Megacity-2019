using Unity.Collections;
using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using Unity.MegaCity.Gameplay;
using Unity.MegaCity.UI;
using Unity.NetCode;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Update the life bar UI
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdateLifeBar : ISystem
    {
        private float m_PreviousLife;
        private EntityQuery m_LocalPlayerQuery;
        private bool m_CanShakeTheCamera;
        private double m_ReceivingDamageTime;

        public void OnCreate(ref SystemState state)
        {
            m_LocalPlayerQuery = state.GetEntityQuery(ComponentType.ReadWrite<VehicleHealth>(),
                                                      ComponentType.ReadOnly<GhostOwnerIsLocal>());
            state.RequireForUpdate(m_LocalPlayerQuery);
            m_CanShakeTheCamera = true;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HUD.Instance == null)
                return;

            var delayForShaking = 0.15;
            var vehicleHealth = m_LocalPlayerQuery.ToComponentDataArray<VehicleHealth>(Allocator.Temp);

            for (int i = 0; i < vehicleHealth.Length; i++)
            {
                var currentLife = vehicleHealth[i].Value;
                if (m_PreviousLife != currentLife)
                {
                    // force the start receiving damage to be called only once when receiving the first hit.
                    if (m_CanShakeTheCamera && currentLife < m_PreviousLife)
                    {
                        HybridCameraManager.Instance.StartShaking();
                        m_CanShakeTheCamera = false;
                    }

                    HUD.Instance.UpdateLife(currentLife);
                    m_PreviousLife = currentLife;
                    m_ReceivingDamageTime = state.World.Time.ElapsedTime;
                }
                else if(m_ReceivingDamageTime + delayForShaking < state.World.Time.ElapsedTime && !m_CanShakeTheCamera)
                {
                    m_CanShakeTheCamera = true;
                    HybridCameraManager.Instance.StopRedOverlay();
                }
            }
        }
    }
}
