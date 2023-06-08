using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Handles immunity state changes on the client and updates the shield visuals
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UpdateImmunityVisualsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if(ShieldPool.Instance == null || state.World.IsServer())
                return;

            foreach (var (immunity, localToWorld, entity) in
                    Query<RefRO<Immunity>, RefRO<LocalToWorld>>().
                    WithEntityAccess())
            {
                if (immunity.ValueRO.StateChangeTick == NetworkTick.Invalid)
                {
                    ShieldPool.Instance.RemoveShieldVfx(entity);
                    continue;
                }

                var localPosition = localToWorld.ValueRO.Position;
                ShieldPool.Instance.UpdateShieldVfx(localPosition, localToWorld.ValueRO.Rotation, entity);
            }

            ShieldPool.Instance.ClearMissingEntities(state.EntityManager);
        }
    }
}
