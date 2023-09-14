using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Player laser Aspect 
    /// </summary>
    public readonly partial struct PlayerLaserAspect : IAspect
    {
        public readonly Entity Self;
        public readonly RefRO<LocalTransform> Transform;
        private readonly RefRW<VehicleLaser> VehicleLaser;

        private float3 Forward => math.mul(Transform.ValueRO.Rotation, math.normalize(new float3(0, 2, 25)));

        public float3 StartPoint => Transform.ValueRO.Position + Forward * VehicleLaser.ValueRO.ForwardOffset;

        public float3 HitPoint
        {
            get
            {
                var length = VehicleLaser.ValueRO.ForwardOffset + VehicleLaser.ValueRO.Length;
                return Transform.ValueRO.Position + (Forward * length);
            }
        }

        public float Length => math.length(HitPoint - StartPoint);
    }
}
