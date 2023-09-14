using Unity.Entities;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Vehicle thrust settings
    /// </summary>
    public struct VehicleThrust : IComponentData
    {
        [GhostField(Quantization = 1000)] public float Thrust;
    }
}
