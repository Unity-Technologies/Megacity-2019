using Unity.Entities;
using Unity.Megacity.Gameplay;
using Unity.Megacity.UI;
using Unity.NetCode;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Update the laser UI
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdateLaser : ISystem
    {
        private EntityQuery m_LocalPlayerQuery;
        public void OnCreate(ref SystemState state)
        {
            m_LocalPlayerQuery = state.GetEntityQuery(ComponentType.ReadOnly<GhostOwnerIsLocal>());
            state.RequireForUpdate(m_LocalPlayerQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HUD.Instance == null)
                return;

            foreach (var laser in Query<RefRO<VehicleLaser>>().WithAll<GhostOwnerIsLocal>())
            {
                HUD.Instance.Laser.UpdateBar(laser.ValueRO.Energy);
            }
        }
    }
}
