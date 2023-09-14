using Unity.Entities;

namespace Unity.Megacity.Audio
{
    public struct AudioBlobRef : IComponentData
    {
        public BlobAssetReference<AllEmitterData> Data;
    }
}