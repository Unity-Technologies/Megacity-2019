using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.MegaCity.Audio
{

    /// <summary>
    /// Gets the nearest neighbours based on a position and range from the incoming tree
    /// </summary>
    [BurstCompile]
    public struct GetEntriesInRangeJob : IJob
    {
        [ReadOnly]
        public KDTree Tree;
        public float3 QueryPosition;
        public float Range;

        public NativeReference<int> ResultsCount;
        [NativeSetThreadIndex] private int ThreadIndex;

        // output
        public NativeArray<KDTree.Neighbour> Neighbours;

        public void Execute()
        {
            ResultsCount.Value = Tree.GetEntriesInRange(QueryPosition, Range, ref Neighbours, ThreadIndex);
        }
    }

    /// <summary>
    /// Adds new Entries to the KDTree, based on the BlobAsset (Building)
    /// </summary>
    [BurstCompile]
    struct CopyDataToTree : IJob
    {
        [ReadOnly] public NativeArray<AudioBlobRef> blobs;
        public KDTree tree;
        public int defIdx;

        public void Execute()
        {
            int treeIdx = 0;

            for (int blobIdx = 0; blobIdx < blobs.Length; blobIdx++)
            {
                ref var indexBeg = ref blobs[blobIdx].Data.Value.DefIndexBeg;

                if (defIdx < indexBeg.Length)
                {
                    ref var indexEnd = ref blobs[blobIdx].Data.Value.DefIndexEnd;
                    ref var emitters = ref blobs[blobIdx].Data.Value.Emitters;

                    var beg = indexBeg[defIdx];
                    var end = indexEnd[defIdx];

                    for (int emitterIdx = beg; emitterIdx < end; emitterIdx++)
                    {
                        tree.AddEntry(treeIdx, emitters[emitterIdx].Position);
                        treeIdx += 1;
                    }
                }
            }
        }
    }
}
