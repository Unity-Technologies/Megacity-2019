using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// System to handle shooting presentation
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct ShootingPresentationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!LaserPool.Instance)
                return;

            LaserPool.Instance.BeginUpdate();
            foreach (var (laser, localTrans, velocity) in
                Query<RefRO<VehicleLaser>, RefRO<LocalTransform>, RefRO<PhysicsVelocity>>())
            {
                if (math.all(laser.ValueRO.HitPoint == default) || laser.ValueRO.Energy <= 0) 
                    continue;

                var forward = math.mul(localTrans.ValueRO.Rotation, math.normalize(new float3(0, 2, 25)));

                var startPoint = localTrans.ValueRO.Position + forward * 5;
                var currentSpeed = math.length(velocity.ValueRO.Linear);
                LaserPool.Instance.AddLine(startPoint, laser.ValueRO.HitPoint, currentSpeed);
            }

            LaserPool.Instance.EndUpdate();
        }
    }
}
