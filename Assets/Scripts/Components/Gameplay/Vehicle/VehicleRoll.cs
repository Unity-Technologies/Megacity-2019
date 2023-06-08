using Unity.Entities;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Vehicle roll settings and state
    /// </summary>
    public struct VehicleRoll : IComponentData
    {
        [GhostField(Quantization = 1000)] public float BankAmount;
        [GhostField(Quantization = 1000)] public float ManualRollValue;
        [GhostField(Quantization = 1000)] public float ManualRollSpeed;
    }
}
