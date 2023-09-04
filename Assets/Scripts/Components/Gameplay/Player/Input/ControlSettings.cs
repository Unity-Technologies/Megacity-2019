using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Control settings values
    /// </summary>
    public struct ControlSettings : IComponentData
    {
        public float MouseSensitivity;
        public float2 AccelerationRange;
        public float2 DirectionRange;
        public bool InverseLookHorizontal;
        public bool InverseLookVertical;
    }
}