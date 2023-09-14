using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    ///  Vehicle laser settings
    /// </summary>
    public struct VehicleLaser : IComponentData
    {
        [GhostField(Quantization = 1000)] public float Energy;
        [GhostField(Quantization = 1000)] public float3 HitPoint;
        [GhostField] public Entity Target;
        
        public float ChargerSpeed;
        public float ForwardOffset;
        public float Length;
    }
    
    /// <summary>
    /// Immunity settings
    /// </summary>
    public struct Immunity : IComponentData
    {
        public float TickAmount;
        [GhostField] public NetworkTick StateChangeTick;
    }
}