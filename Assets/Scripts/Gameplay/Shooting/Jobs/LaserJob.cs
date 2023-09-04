using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Job to simulate the laser.
    /// </summary>
    [BurstCompile]
    public partial struct LaserJob : IJobEntity
    {
        [ReadOnly]
        public NetworkTick PredictingTick;
        [ReadOnly]
        public float DeltaTime;
        [ReadOnly]
        public PhysicsWorldHistorySingleton CollisionHistory;
        [ReadOnly]
        public PhysicsWorld PhysicsWorld;
        [ReadOnly]
        public ComponentLookup<VehicleHealth> healthLookup;
        
        [BurstCompile]
        private void Execute(in Entity entity, 
            ref VehicleLaser laser, 
            in PlayerVehicleInput input,
            in LocalTransform localTrans,
            in VehicleHealth health,
            in CommandDataInterpolationDelay interpolationDelay,
            in Simulate simulate)
        {
            laser.HitPoint = default;
            laser.Target = Entity.Null;
            if (!input.Shoot)
            {
                laser.Energy = math.min(100, laser.Energy + (DeltaTime * laser.ChargerSpeed));
                return;
            }
            
            if (laser.Energy <= 0 || health.Value <= 0)
                return;

            var forward = math.mul(localTrans.Rotation, math.normalize(new float3(0, 2, 25)));
            var startPoint = localTrans.Position + forward * laser.ForwardOffset;
            var hitPoint = localTrans.Position + forward * (laser.ForwardOffset + laser.Length);

            var drainBatterySpeed = laser.ChargerSpeed * 2f;
            laser.Energy = math.max(0, laser.Energy - (DeltaTime * drainBatterySpeed));
            
            var rayInput = new RaycastInput
            {
                Start = startPoint,
                End = hitPoint,
                Filter = CollisionFilter.Default
            };
            
            uint delay = interpolationDelay.Delay;
            CollisionHistory.GetCollisionWorldFromTick(PredictingTick, delay, ref PhysicsWorld, out var collisionWorld);

            if (collisionWorld.CastRay(rayInput, out var closestHit))
            {
                hitPoint = closestHit.Position;
                if (healthLookup.HasComponent(closestHit.Entity) && entity != closestHit.Entity)
                    laser.Target = closestHit.Entity;
            }

            laser.HitPoint = hitPoint;
        }
    }
}