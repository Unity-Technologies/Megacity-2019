using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// System to handle the player vehicle collision.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    partial struct PlayerVehicleCollisionSystem : ISystem
    {
        private ComponentLookup<PhysicsVelocity> _physicsVelocities;
        private ComponentLookup<VehicleThrust> _vehicleThrust;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<SimulationSingleton>();
            _physicsVelocities = state.GetComponentLookup<PhysicsVelocity>(true);
            _vehicleThrust = state.GetComponentLookup<VehicleThrust>();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorldSingleton = GetSingleton<PhysicsWorldSingleton>();
            var simulationSingleton = GetSingleton<SimulationSingleton>();
            _physicsVelocities.Update(ref state);
            _vehicleThrust.Update(ref state);
            state.Dependency = new PlayerVehicleCollisionJob
            {
                SimulateLookup = SystemAPI.GetComponentLookup<Simulate>(true),
                Bodies = physicsWorldSingleton.Bodies,
                PhysicsVelocities = _physicsVelocities,
                VehicleThrusts = _vehicleThrust
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }
}
