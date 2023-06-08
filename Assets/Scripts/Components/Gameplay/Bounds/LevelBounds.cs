using Unity.Entities;
using Unity.Mathematics;

namespace Unity.MegaCity.Gameplay
{
    public struct LevelBounds : IComponentData
    {
        public bool IsInside;
        public float3 Top;
        public float3 Bottom;
        public float3 Center;
        public float SafeAreaSq;
    }
}
