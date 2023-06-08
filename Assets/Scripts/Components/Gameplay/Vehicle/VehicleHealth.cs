using Unity.Entities;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Vehicle health
    /// </summary>
    public struct VehicleHealth : IComponentData
    {
        [GhostField(Quantization = 1000)] public float Value;
        [GhostField] public NetworkTick AliveStateChangeTick;
        [GhostField] public NetworkTick ReviveStateChangeTick;
    }
}
