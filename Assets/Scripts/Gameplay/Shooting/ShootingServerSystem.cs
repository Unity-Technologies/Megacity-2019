using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Handles shooting on the server
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ShootingServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var netTime = SystemAPI.GetSingleton<NetworkTime>();
            var shootingJob = new ApplyingDamageAndPointsJob
            {
                DamagePerSecond = 50f,
                DeltaTime = SystemAPI.Time.DeltaTime,
                PredictingTick = netTime.ServerTick,
                healthLookup = SystemAPI.GetComponentLookup<VehicleHealth>(),
                playerPointsLookUp = SystemAPI.GetComponentLookup<PlayerScore>(),
                playerNameLookup = SystemAPI.GetComponentLookup<PlayerName>(),
                immunityLookup = SystemAPI.GetComponentLookup<Immunity>(),
            };

            state.Dependency = shootingJob.Schedule(state.Dependency);
        }
    }
}