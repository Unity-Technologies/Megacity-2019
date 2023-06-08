using Unity.Entities;
using Unity.NetCode;
using static Unity.Entities.SystemAPI;
using Unity.Burst;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Handles immunity state changes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ImmunitySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var netTime = GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick)
                return;

            var predictingTick = netTime.ServerTick;
            var isServer = state.WorldUnmanaged.IsServer();

            foreach (var (imminuty, health) in
                Query<RefRW<Immunity>, RefRW<VehicleHealth>>().
                WithAll<Simulate>())
            {
                if (health.ValueRO.ReviveStateChangeTick == NetworkTick.Invalid)
                    continue;

                if (imminuty.ValueRO.StateChangeTick == NetworkTick.Invalid && isServer)
                {
                    imminuty.ValueRW.StateChangeTick = predictingTick;
                    health.ValueRW.ReviveStateChangeTick = NetworkTick.Invalid;
                }
            }

            foreach (var imminuty in Query<RefRW<Immunity>>().WithAll<Simulate>())
            {
                if (imminuty.ValueRO.StateChangeTick == NetworkTick.Invalid)
                    continue;

                var tickSince = predictingTick.TicksSince(imminuty.ValueRO.StateChangeTick);
                if (tickSince > imminuty.ValueRO.TickAmount && isServer)
                {
                    imminuty.ValueRW.StateChangeTick = NetworkTick.Invalid;
                }
            }
        }
    }
}
