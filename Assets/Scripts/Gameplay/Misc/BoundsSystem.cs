using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Updates the glitch effect when the player is near the bounds.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdateBoundsSystem : ISystem
    {
        private bool m_GlitchActive;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var levelBounds = GetSingletonRW<LevelBounds>().ValueRW;

            foreach (var localToWorld in Query<RefRW<LocalToWorld>>().WithAll<GhostOwnerIsLocal>())
            {
                var insideBounds = true;
                var position = localToWorld.ValueRO.Position;
                
                if (position.y > levelBounds.Top.y && math.distancesq(position, levelBounds.Top) > levelBounds.SafeAreaSq)
                    insideBounds = false;
                
                if (position.y < levelBounds.Bottom.y && math.distancesq(position, levelBounds.Bottom) > levelBounds.SafeAreaSq)
                    insideBounds =  false;
            
                var point = new float3(levelBounds.Center.x, position.y, levelBounds.Center.z);
                if(math.distancesq(position, point) > levelBounds.SafeAreaSq)
                    insideBounds = false;

                levelBounds.IsInside = insideBounds;
            }
            
            SetSingleton(levelBounds);
        }
    }
}
