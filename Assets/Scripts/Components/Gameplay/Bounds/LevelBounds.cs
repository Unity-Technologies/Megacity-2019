using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Megacity.Gameplay
{
    public struct LevelBounds : IComponentData
    {
        public float3 Top;
        public float3 Bottom;
        public float3 Center;
        public float SafeAreaSq;
    }
}
