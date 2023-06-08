using Unity.Entities;
using Unity.Mathematics;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Instantiate AudioSources GameObjects.
    /// Collects all kind of emitters in the Scene, and creates one KDTree per Emitter type.
    /// Every frame iterates the KDTree to search for the nearest positions from the camera.
    /// Later if set the positions for the Audio Sources and plays them.
    /// If a building is added or removed, updates the KDTree data.
    /// </summary>
    public struct SoundEmitter : IComponentData
    {
        public int definitionIndex;
        public float3 position;
        public float3 direction;
    }

    public struct AllEmitterData
    {
        public BlobArray<int> DefIndexBeg;
        public BlobArray<int> DefIndexEnd;
        public BlobArray<SingleEmitterData> Emitters;
    }

    public struct SingleEmitterData
    {
        public float3 Position;
        public float3 Direction;
    }
}
