using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Job to apply damage and points to the target.
    /// </summary>
    [BurstCompile]
    public partial struct ApplyingDamageAndPointsJob : IJobEntity
    {
        public float DamagePerSecond;
        public float DeltaTime;
        public NetworkTick PredictingTick;

        public ComponentLookup<VehicleHealth> healthLookup;
        public ComponentLookup<PlayerScore> playerPointsLookUp;
        [ReadOnly]
        public ComponentLookup<PlayerName> playerNameLookup;
        [ReadOnly]
        public ComponentLookup<Immunity> immunityLookup;

        [BurstCompile]
        private void Execute(Entity entity, in VehicleLaser laser, in Simulate simulate)
        {
            if (laser.Target != Entity.Null && laser.Target != entity)
            {
                var health = healthLookup[laser.Target];
                var immunity = immunityLookup[laser.Target];
                var damage = DeltaTime * DamagePerSecond;

                if (health.Value > 0 && immunity.StateChangeTick == NetworkTick.Invalid)
                {
                    var ownerScore = playerPointsLookUp[entity];
                    ownerScore.Value += damage;
                    if (health.Value - damage > 0)
                    {
                        health.Value -= damage;
                    }
                    else if (health.Value - damage <= 0)
                    {
                        if (playerPointsLookUp.HasComponent(laser.Target))
                        {
                            var playerScore = playerPointsLookUp[laser.Target];
                            playerScore.KillerName = playerNameLookup[entity].Name;
                            playerPointsLookUp[laser.Target] = playerScore;
                        }
                        ownerScore.KilledPlayer = playerNameLookup[laser.Target].Name;
                        ownerScore.Kills += 1;
                        health.Value = 0;
                        health.AliveStateChangeTick = PredictingTick;
                    }

                    healthLookup[laser.Target] = health;
                    playerPointsLookUp[entity] = ownerScore;
                }
            }
        }
    }
}
