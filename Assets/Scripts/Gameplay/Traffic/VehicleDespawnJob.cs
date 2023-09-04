using Unity.Burst;
using Unity.Entities;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    ///     Despawn the vehicles when they reach the end of the path
    /// </summary>
    [BurstCompile]
    public partial struct VehicleDespawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in VehiclePathing vehicle)
        {
            if (vehicle.curvePos >= 1.0f)
            {
                EntityCommandBuffer.DestroyEntity(index, entity);
            }
        }
    }
}
