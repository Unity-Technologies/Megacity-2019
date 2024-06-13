using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Megacity.Audio
{
    /// <summary>
    /// Initialize and Dispose the KDTree Data for SoundPoolSystem
    /// </summary>
    public struct AudioTree : IDisposable
    {
        public KDTree Tree;
        public NativeArray<KDTree.Neighbour> Results;
        public NativeReference<int> ResultsCount;
        public NativeList<float3> EmittersPosition;
        public NativeList<int> DefinitionIndices;
        public Color DebugLineColor;

        public void Initialize(int maxResults)
        {
            DebugLineColor = UnityEngine.Random.ColorHSV();
            EmittersPosition = new NativeList<float3>(Allocator.Persistent);
            DefinitionIndices = new NativeList<int>(Allocator.Persistent);
            Results = new NativeArray<KDTree.Neighbour>(maxResults, Allocator.Persistent);
            ResultsCount = new NativeReference<int>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if(EmittersPosition.IsCreated)
                EmittersPosition.Dispose();

            if(Results.IsCreated)
                Results.Dispose();

            if(ResultsCount.IsCreated)
                ResultsCount.Dispose();

            if(DefinitionIndices.IsCreated)
                DefinitionIndices.Dispose();

            if(Tree.IsCreated)
                Tree.Dispose();
        }
    }
}
