using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Unity.MegaCity.Utils
{
    [MaterialProperty("_BaseColor3")]
    public struct LayeredLitBaseColor3 : IComponentData
    {
        public float4 Value;
    }
}
