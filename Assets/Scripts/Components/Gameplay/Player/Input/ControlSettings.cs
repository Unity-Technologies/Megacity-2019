using Unity.Entities;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Control settings values
    /// </summary>
    public struct ControlSettings : IComponentData
    {
        public float MouseSensitivity;
        public bool InverseLookHorizontal;
        public bool InverseLookVertical;
    }
}