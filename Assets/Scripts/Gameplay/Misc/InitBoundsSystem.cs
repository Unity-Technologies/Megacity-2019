using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Initialize bounds system
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct InitBoundsSystem : ISystem
    {
        private EntityQuery m_LocalPlayerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelBounds>();
            m_LocalPlayerQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
                    .WithAll<GhostOwnerIsLocal>()
                    .WithNone<PlayerLocationBounds>()
                    .Build(ref state);
            state.RequireForUpdate(m_LocalPlayerQuery);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (m_LocalPlayerQuery.CalculateEntityCount() < 1)
                return;
            
            var singlePlayerEntity = m_LocalPlayerQuery.ToEntityArray(Allocator.Temp)[0];
            state.EntityManager.AddComponentData(singlePlayerEntity,new PlayerLocationBounds { IsInside = true });
            state.Enabled = false;
        }
    }
}