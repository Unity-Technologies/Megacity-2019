using Unity.Entities;

namespace Unity.MegaCity.Audio
{
    public struct AudioBlobRef : IComponentData
    {
        public BlobAssetReference<AllEmitterData> Data;
    }
}