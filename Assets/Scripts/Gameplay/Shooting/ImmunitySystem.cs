using Unity.Entities;
using Unity.NetCode;
using static Unity.Entities.SystemAPI;
using Unity.Burst;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Handles immunity state changes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ImmunitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var netTime = GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick)
                return;

            var predictingTick = netTime.ServerTick;

            foreach (var (immunity, health) in
                Query<RefRW<Immunity>, RefRW<VehicleHealth>>().
                WithAll<Simulate>())
            {
                if (health.ValueRO.ReviveStateChangeTick == NetworkTick.Invalid)
                    continue;

                if (immunity.ValueRO.StateChangeTick == NetworkTick.Invalid)
                {
                    immunity.ValueRW.StateChangeTick = predictingTick;
                    health.ValueRW.ReviveStateChangeTick = NetworkTick.Invalid;
                }
            }

            foreach (var immunity in Query<RefRW<Immunity>>().WithAll<Simulate>())
            {
                if (immunity.ValueRO.StateChangeTick == NetworkTick.Invalid)
                    continue;

                var tickSince = predictingTick.TicksSince(immunity.ValueRO.StateChangeTick);
                if (tickSince > immunity.ValueRO.TickAmount)
                {
                    immunity.ValueRW.StateChangeTick = NetworkTick.Invalid;
                }
            }
        }
    }
}
