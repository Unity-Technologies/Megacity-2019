using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// System to handle the player vehicle cosmetic physics.
    /// </summary>
    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerVehicleCosmeticPhysicsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerVehicleCosmeticPhysics>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var vehicleBraking = SystemAPI.GetComponentLookup<VehicleBraking>(true);
            var vehicleRoll = SystemAPI.GetComponentLookup<VehicleRoll>(true);
            var rollJob = new VehicleRollJob
            {
                VehicleBrakingLookup = vehicleBraking,
                VehicleRollLookup = vehicleRoll
            };
            state.Dependency = rollJob.ScheduleParallel(state.Dependency);
        }
    }
}