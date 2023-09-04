using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// System that updates the player location bounds.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct UpdateBoundsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelBounds>();
            state.RequireForUpdate<PlayerLocationBounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var levelBounds = GetSingleton<LevelBounds>();
            var evaluateIsInsideBoundsJob = new EvaluateIsInsideBoundsJob { LevelBounds = levelBounds };
            evaluateIsInsideBoundsJob.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct EvaluateIsInsideBoundsJob : IJobEntity
    {
        [ReadOnly] public LevelBounds LevelBounds; 
        public void Execute(in LocalToWorld localToWorld, ref PlayerLocationBounds locationBounds)
        {
            var position = localToWorld.Position;

            // evaluate top cylinder radius
            if (position.y > LevelBounds.Top.y)
            {
                locationBounds.IsInside = math.distancesq(position, LevelBounds.Top) < LevelBounds.SafeAreaSq;
            }
            // evaluate center cylinder radius
            else if (position.y > LevelBounds.Bottom.y && position.y < LevelBounds.Top.y)
            {
                var point = new float3(LevelBounds.Center.x, position.y, LevelBounds.Center.z);
                locationBounds.IsInside = math.distancesq(position, point) < LevelBounds.SafeAreaSq;
            }
            // evaluate bottom cylinder radius
            else if (position.y < LevelBounds.Bottom.y)
                locationBounds.IsInside =  math.distancesq(position, LevelBounds.Bottom) > LevelBounds.SafeAreaSq;
        }
    }
}
