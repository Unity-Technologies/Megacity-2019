using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Megacity.CameraManagement
{
    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(PlayerCameraTargetUpdater))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PreparePlayerCameraTarget : ISystem
    {
        [BurstCompile]
        partial struct UpdatePlayerCameraTargetDataJob : IJobEntity
        {
            [BurstCompile]
            public void Execute(ref PlayerCameraTarget playerCameraTarget, in LocalToWorld localToWorld)
            {
                playerCameraTarget.Position = localToWorld.Position;
                playerCameraTarget.Rotation = localToWorld.Rotation;
            }
        }
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerCameraTarget>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var updatePlayerTargetDataJob = new UpdatePlayerCameraTargetDataJob();
            state.Dependency = updatePlayerTargetDataJob.ScheduleParallel(state.Dependency);
            state.CompleteDependency();
        }
    }
}