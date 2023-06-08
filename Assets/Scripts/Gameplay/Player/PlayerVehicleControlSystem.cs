using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics.Systems;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Schedule the necessary job to process the user inputs and move the player accordingly
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial struct PlayerVehicleControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            var thrustJob = new ThrustJob {DeltaTime = deltaTime};
            var bankingJob = new VehicleBankingJob();
            var breakingJob = new VehicleBreakingPseudoPhysicsJob {DeltaTime = deltaTime};
            var moveJob = new MoveJob {DeltaTime = deltaTime, Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick};
            var autoLevelJob = new AutoLevelJob {DeltaTime = deltaTime};

            state.Dependency = thrustJob.ScheduleParallel(state.Dependency);
            state.Dependency = bankingJob.ScheduleParallel(state.Dependency);
            state.Dependency = breakingJob.ScheduleParallel(state.Dependency);
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
            state.Dependency = autoLevelJob.ScheduleParallel(state.Dependency);
        }
    }
}