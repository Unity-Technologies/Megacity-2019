using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.Gameplay;
using Unity.Megacity.UI;
using Unity.NetCode;
using Unity.Physics;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Update the possible target UI
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdatePossibleTarget : ISystem
    {
        private EntityQuery m_LocalPlayerQuery;
        public void OnCreate(ref SystemState state)
        {
            m_LocalPlayerQuery = state.GetEntityQuery(ComponentType.ReadOnly<GhostOwnerIsLocal>(),
                                                      ComponentType.ReadOnly<VehicleLaser>());
            state.RequireForUpdate(m_LocalPlayerQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HUD.Instance == null && !HUD.Instance.Crosshair.IsVisible)
                return;

            HUD.Instance.Crosshair.Normal();
            foreach (var laser in Query<PlayerLaserAspect>().WithAll<GhostOwnerIsLocal>())
            {
                var physicsWorld = GetSingleton<PhysicsWorldSingleton>();
                var collisionWorld = physicsWorld.CollisionWorld;

                var rayInput = new RaycastInput
                {
                    Start = laser.StartPoint,
                    End = laser.HitPoint,
                    Filter = CollisionFilter.Default
                };

                var distance = laser.Length;
                if (collisionWorld.CastRay(rayInput, out var closestHit))
                {
                    if (state.EntityManager.HasComponent<PlayerScore>(closestHit.Entity))
                        HUD.Instance.Crosshair.Highlight();

                    distance = math.length(closestHit.Position - laser.StartPoint);
                }
                    
                HUD.Instance.Crosshair.SetOffset(distance);
            }
        }
    }
}
