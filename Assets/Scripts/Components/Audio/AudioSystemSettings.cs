using Unity.Entities;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Audio system settings component authored based on the constant Audio Master class settings.
    /// </summary>
    public struct AudioSystemSettings : IComponentData
    {
        public bool DebugMode;
        public float MaxDistance;
        public int ClosestEmitterPerClipCount;
    }
}
