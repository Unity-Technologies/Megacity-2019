using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Pool;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Handles updating the health VFX on the client
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UpdateHealthVfxSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (vehicleHealth, localToWorld, entity) in Query<RefRW<VehicleHealth>, RefRO<LocalToWorld>>()
                         .WithEntityAccess().WithAll<PlayerVehicleSettings>())
            {
                // Resetting Health / Remove Smoke VFX
                if (vehicleHealth.ValueRO.AliveStateChangeTick ==  NetworkTick.Invalid)
                {
                    SmokeVfxPool.Instance.RemoveSmokeVfx(entity);
                    continue;
                }

                if(vehicleHealth.ValueRO.Value > 50f)
                    continue;

                var localPosition = localToWorld.ValueRO.Position;
                var offset = localToWorld.ValueRO.Up - localToWorld.ValueRO.Forward * 2f;

                SmokeVfxPool.Instance.UpdateSmokeVfx(
                    localPosition + offset,
                    localToWorld.ValueRO.Rotation,
                    entity, vehicleHealth.ValueRO.Value);
            }

            SmokeVfxPool.Instance.ClearMissingEntities(state.EntityManager);
        }
    }
}
