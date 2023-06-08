using Unity.Entities;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Set of components required for player vehicle movement and control
    /// </summary>
    public struct VehicleBraking : IComponentData
    {
        [GhostField(Quantization = 1000)] public float YawBreakRotation;
        [GhostField(Quantization = 1000)] public float PitchPseudoBraking;
    }
}
