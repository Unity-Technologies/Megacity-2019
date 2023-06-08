//#define ENABLE_KDTREE_VALIDATION_CHECKS
//#define ENABLE_KDTREE_ANALYTICS

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public interface IKDTreeParams {
    int MaxWorkerThreadsToUseForBuilding { get; }
    int MaxEntriesPerLeaf { get; }
    int AdditionalDepthToAllocate { get; }
    int MinLeavesPerWorkerThread { get; }
}

public struct KDTreeParams : IKDTreeParams
{
    // this value is used to restrict the amount of possible threads that can be used
    // if -1 the system will use as many as it can
    public int MaxWorkerThreadsToUseForBuilding { get; set; }

    // this value specifies the max amount of entries to store per leaf in the tree
    // a higher value will decrease the amount of tree memory required but could lead to more entry comparisons during a query
    // a lower value will increase the depth of a tree required and will increase memory use but will lead to less entry comparisons in a query
    public int MaxEntriesPerLeaf { get; set; }

    // the system calculates how many levels of depth a perfectly balanced tree would require and will not be allowed to exceed this maximum
    // however, this can lead to large leaf nodes containing many entries (since the tree can no longer split) increasing the amount of comparisons a query will need to make.
    // Optionally the tree can preallocate additional depths to allow the tree to continue to split, though this increase the amount of memory required.
    public int AdditionalDepthToAllocate { get; set; }

    // this value specifies the minimum number of leaves that each worker thread can process when building the tree
    // higher values will involve ensure each worker thread processes more leaves
    // lower values will use more jobs for each sub tree processing, potentially increasing scheduling overhead
    public int MinLeavesPerWorkerThread { get; set; }
}

[NativeContainer]
[NativeContainerSupportsMinMaxWriteRestriction]
public unsafe struct KDTree : IDisposable
{
    const float k_ZeroRadiusEpsilon = 0.00000000001f;
    const uint k_RootNodeIndex = 1;
    const uint k_IsLeafNodeBitFlag = (uint)(1) << 31;
    const uint k_CountBitMask = ~(k_IsLeafNodeBitFlag);

    public static KDTreeParams DefaultKDTreeParams = new KDTreeParams
    {
        MaxWorkerThreadsToUseForBuilding = -1,
        MaxEntriesPerLeaf = 8,
        AdditionalDepthToAllocate = 5,
        MinLeavesPerWorkerThread = 32,
    };

    // used in each tree node to specify the enclosing bounds for all the entries in the node
    internal struct Bounds
    {
        public float3 min; // 12
        public float3 max; // 12

        // 24B
    }

    public struct TreeNode
    {
        public uint count; 
        public uint beginEntryIndex;

        internal Bounds bounds; // 24;

        // 32B (2 per cacheline)

        public uint Count => count & k_CountBitMask;

        public bool IsLeaf
        {
            get { return (count & k_IsLeafNodeBitFlag) > 0; }
            set { if (value) count |= k_IsLeafNodeBitFlag; }
        }

        public byte* GetBeginPtr(byte* first)
        {
            return first + (beginEntryIndex * (uint)sizeof(Entry));
        }
    }

    internal struct Entry
    {
        internal int index; // 4
        internal float3 position; // 12

        // 16B (4 per cacheline)
    }

    public struct Neighbour : IComparable<Neighbour>
    {
        public int index; // 4
        public float distSq; // 4
        public float3 position; // 12

        // 20B (3 per cacheline)

        public int CompareTo(Neighbour other)
        {
            return distSq.CompareTo(other.distSq);
        }
    }

    // these are stored per thread
    // and combined/collapsed after querying has been performed
    // only filled out if ENABLE_KDTREE_ANALYTICS is defined
    public struct QueryStats
    {
        public int NumQueries;
        public int NumNodesVisited;
        public int NumEntriesCompared;
        public int NumEntriesFoundOverNeighbourCapacity;

        // 16B 
    }

    [BurstCompile]
    struct CopyEntriesJob : IJobParallelFor
    {
        [ReadOnly] internal NativeArray<float3> Points;
        internal KDTree This;

        public void Execute(int index)
        {
            This.AddEntry(index, Points[index]);
        }
    }

    [BurstCompile]
    struct PreprocessJob : IJobParallelFor
    {
        internal int Depth;
        internal KDTree This;

        public void Execute(int index)
        {
            uint nodeIndex = (uint)math.pow(2, Depth) + (uint)index;
            This.BuildSubTree(nodeIndex, Depth, true);
        }
    }

    [BurstCompile]
    struct BuildSubTreeJob : IJobParallelFor
    {
        internal int Depth;
        internal KDTree This;

        public void Execute(int index)
        {
            uint nodeIndex = (uint)math.pow(2, Depth) + (uint)index;
            This.BuildSubTree(nodeIndex, Depth, false);
        }
    }

    [NativeDisableUnsafePtrRestriction] TreeNode* m_NodesPtr;

    int m_Capacity;
    Allocator m_AllocatorLabel;

    internal int m_NumEntries;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal int m_Length;
    internal int m_MinIndex;
    internal int m_MaxIndex;
    AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

    [NativeDisableUnsafePtrRestriction] byte* m_EntriesPtr;

    internal int m_NumWorkers;
    internal int m_MaxDepth;
    internal int m_MaxLeafSize;
    internal int m_MinLeavesPerWorker;

#if ENABLE_KDTREE_ANALYTICS
    [NativeDisableUnsafePtrRestriction] QueryStats* m_QueryStats;
    NativeArray<int> m_NodeUsage;
#endif

    public bool IsCreated { get { return m_Capacity > 0; } }

    public static int CalculateNumNodes(int numEntries, IKDTreeParams treeParams, out int maxDepth, out int balancedLeafNodes)
    {
        balancedLeafNodes = math.max(1, (numEntries / treeParams.MaxEntriesPerLeaf));

        // need to ensure we have enough nodes for an eytzinger layout (much more cache performant)
        maxDepth = treeParams.AdditionalDepthToAllocate + (int)math.ceil(math.log2(balancedLeafNodes));
        return numEntries > treeParams.MaxEntriesPerLeaf ? (int)math.pow(2f, 1 + maxDepth) : 1;
    }

    public static int CalculateNumWorkers(int numEntries, int maxWorkersAvailable, int maxLeafSize, int minLeavesPerWorker)
    {
        int maxWorkers = (int)math.pow(2f, math.floor(math.log2(maxWorkersAvailable)));

        int minEntriesPerWorker = maxLeafSize * minLeavesPerWorker;

        int numWorkers = 1;
        while ((numWorkers * 2) <= maxWorkers
            && numEntries >= (numWorkers * 2 * minEntriesPerWorker))
        {
            numWorkers *= 2;
        }

        return numWorkers;
    }

    public KDTree(int capacity, Allocator allocator, IKDTreeParams treeParams = null)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Native allocation is only valid for Temp, Job and Persistent
        if (allocator <= Allocator.None)
            throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity", "Capacity must be >= 0");
#endif

        if(treeParams == null)
            treeParams = DefaultKDTreeParams;

        int maxDepth, balancedLeafNodes;
        int numNodes = CalculateNumNodes(capacity, treeParams, out maxDepth, out balancedLeafNodes);

        // because the nodes are stored together in LR pairs, a single node still needs the memory allocated for two nodes
        // since the root index starts at 1 rather than 0 (to make the index calculates easier)
        if (numNodes == 1) numNodes = 2;

        m_NumWorkers = CalculateNumWorkers(capacity, JobsUtility.JobWorkerCount, treeParams.MaxEntriesPerLeaf, treeParams.MinLeavesPerWorkerThread);
        if(treeParams.MaxWorkerThreadsToUseForBuilding > 0) { 
            m_NumWorkers = math.min(treeParams.MaxWorkerThreadsToUseForBuilding, m_NumWorkers);
        }

        m_MaxLeafSize = treeParams.MaxEntriesPerLeaf;
        m_MinLeavesPerWorker = treeParams.MinLeavesPerWorkerThread;

        //Debug.Log($"Allocating {numNodes} KDTree nodes for {capacity} entries, using {m_NumWorkers} workers with {maxDepth} maxDepth\n");

        float treesizeInMb = (sizeof(TreeNode) * numNodes) / (1024f * 1024f);
        float treesizeInKb = (sizeof(TreeNode) * numNodes) / (1024f);
        //Debug.Log($"Total tree memory = {(treesizeInMb > 1f ? treesizeInMb : treesizeInKb):0.0}{(treesizeInMb > 1f ? "Mb" : "Kb")}");

        m_NodesPtr = (TreeNode*)UnsafeUtility.Malloc(sizeof(TreeNode) * numNodes, JobsUtility.CacheLineSize, allocator);

        int entryPadding = m_NumWorkers * JobsUtility.CacheLineSize;
        m_EntriesPtr = (byte*)UnsafeUtility.Malloc((sizeof(Entry) * capacity) + entryPadding, JobsUtility.CacheLineSize, allocator);

        m_Capacity = capacity;
        m_NumEntries = 0;
        m_AllocatorLabel = allocator;

        m_MaxDepth = maxDepth - 2;

#if ENABLE_KDTREE_ANALYTICS
        // we know our stats fit in a cacheline, so we can edit them in parallel without needing to worry about false sharing
        var statsSize = JobsUtility.CacheLineSize * JobsUtility.MaxJobThreadCount;
        m_QueryStats = (QueryStats*)UnsafeUtility.Malloc(statsSize, JobsUtility.CacheLineSize, allocator);

        m_NodeUsage = new NativeArray<int>(m_MaxLeafSize + 2, Allocator.Persistent);
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        m_Length = m_Capacity;
        m_MinIndex = 0;
        m_MaxIndex = -1;
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
    }

    [WriteAccessRequired]
    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_EntriesPtr, m_AllocatorLabel);
        m_EntriesPtr = null;

        UnsafeUtility.Free(m_NodesPtr, m_AllocatorLabel);
        m_NodesPtr = null;

#if ENABLE_KDTREE_ANALYTICS
        UnsafeUtility.Free(m_QueryStats, m_AllocatorLabel);
        m_QueryStats = null;

        m_NodeUsage.Dispose();
#endif 

        m_Capacity = 0;
    }

    [WriteAccessRequired]
    public void AddEntry(int index, in float3 pos)
    {
        Entry* entryPtr = (Entry*)(m_EntriesPtr + (index * sizeof(Entry)));
        *entryPtr = new Entry { index = index, position = pos };
    }

    public JobHandle BuildTree(NativeArray<float3> positions, JobHandle inputDeps = new JobHandle())
    {
        return BuildTree(positions, positions.Length, inputDeps);
    }

    public JobHandle BuildTree(NativeArray<float3> positions, int numEntries, JobHandle inputDeps = new JobHandle())
    {
        // first we copy
        var copyJob = new CopyEntriesJob
        {
            This = this,
            Points = positions,
        };

        // lets split amongst thsi workers
        var dep = copyJob.Schedule(numEntries, numEntries/m_NumWorkers, inputDeps);

        return BuildTree(numEntries, dep);
    }

    public JobHandle BuildTree(int numEntries, JobHandle inputDeps = new JobHandle())
    {
        var dep = inputDeps;

        m_NumEntries = numEntries;

        if (m_NumEntries == 0)
        {
            return dep;
        }

        SetNode(k_RootNodeIndex, m_EntriesPtr, m_EntriesPtr + ((m_NumEntries - 1) * sizeof(Entry)));

        if (m_NumEntries > m_MaxLeafSize)
        {
            // calculate how many workers we need based on entries
            var entryNumWorkers = CalculateNumWorkers(m_NumEntries, JobsUtility.JobWorkerCount, m_MaxLeafSize, m_MinLeavesPerWorker);
            entryNumWorkers = math.min(m_NumWorkers, entryNumWorkers);
            var maxDepthOnPreProcess = (int)math.log2(entryNumWorkers);

            // preprocess tree for parallel work
            for (int depth = 0; depth < maxDepthOnPreProcess; depth++)
            {
                int numNodesToProcess = (int)math.pow(2, depth);

                var preProcessJob = new PreprocessJob
                {
                    This = this,
                    Depth = depth,
                };
                dep = preProcessJob.Schedule(numNodesToProcess, 1, dep);
            }

            //build tree on workers
            var buildSubTreeJob = new BuildSubTreeJob
            {
                This = this,
                Depth = maxDepthOnPreProcess,
            };
            dep = buildSubTreeJob.Schedule(entryNumWorkers, 1, dep);
        }
        else
        {
            // our rootnode is a leaf
            TreeNode* nodePtr = m_NodesPtr + k_RootNodeIndex;
            nodePtr->count |= k_IsLeafNodeBitFlag;
        }

        return dep;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe Bounds FindNodeBounds(TreeNode* nodePtr, byte* beginPtr, byte* endPtr, out float3 mean, out float varianceSq)
    {
        var bounds = new Bounds
        {
            min = new float3(float.MaxValue),
            max = new float3(float.MinValue)
        };
        mean = new float3(0f);

        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry))
        {
            var ptr = (Entry*)entryPtr;
            ExpandBounds(ref bounds, ptr->position);
            mean += ptr->position;
        }

        int count = ((int)(endPtr - beginPtr) / sizeof(Entry)) + 1;

        mean /= count;

        var centre = new float3((bounds.max.x + bounds.min.x) / 2f, (bounds.max.y + bounds.min.y) / 2f, (bounds.max.z + bounds.min.z) / 2f);
        varianceSq = math.distancesq(bounds.max, centre);

        return bounds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CalculateSplitDimension(in Bounds bounds, in float3 meanPos, out float splitValue)
    {
        float lengthX = bounds.max.x - bounds.min.x;
        float lengthY = bounds.max.y - bounds.min.y;
        float lengthZ = bounds.max.z - bounds.min.z;

        if (lengthX >= lengthY && lengthX >= lengthZ)
        {
            splitValue = meanPos.x;
            return 0;
        }
        else if (lengthY >= lengthX && lengthY >= lengthZ)
        {
            splitValue = meanPos.y;
            return 1;
        }
        else
        {
            splitValue = meanPos.z;
            return 2;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float GetDimensionComponent(int dimension, byte* entry)
    {
        return *(float*)(entry + 4 + (4 * dimension));
    }

    void BuildSubTree(uint nodeIndex, int depth, bool preProcess = false)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

        uint count = nodePtr->Count;
        byte* beginPtr = nodePtr->GetBeginPtr(m_EntriesPtr);
        byte* endPtr = beginPtr + (count - 1) * sizeof(Entry);

        if (count == 0)
        {
            // preprocessing resulted in an unbalanced tree
            // so if this is a leaf node, we should drop out now
            nodePtr->count |= k_IsLeafNodeBitFlag;

            if (preProcess)
            {
                uint leftNode = 2 * nodeIndex;
                uint rightNode = leftNode + 1;

                // fill in left/right node details
                SetEmptyLeafNode(leftNode);
                SetEmptyLeafNode(rightNode);
            }
            return;
        }

        float3 mean;
        float varianceSq;
        nodePtr->bounds = FindNodeBounds(nodePtr, beginPtr, endPtr, out mean, out varianceSq);

        if (depth < m_MaxDepth
            && varianceSq >= k_ZeroRadiusEpsilon
            && count > m_MaxLeafSize)
        {
            // as we are preprocessing, we know we are not a leaf node, so we can split
            depth++;

            // split into left/right
            float splitValue;
            int splitDimension = CalculateSplitDimension(nodePtr->bounds, mean, out splitValue);

            byte* leftPtr = beginPtr;
            byte* rightPtr = endPtr;

            int rightPtrOffset = preProcess ? (int)(m_NumWorkers / math.pow(2f, depth)) * JobsUtility.CacheLineSize : 0;

            while (leftPtr < rightPtr)
            {
                // while left positions are on the left, skip to next
                while (leftPtr < rightPtr && GetDimensionComponent(splitDimension, leftPtr) < splitValue)
                {
                    leftPtr += sizeof(Entry);
                }

                // while right positions are on the right, skip to next
                while (rightPtr > leftPtr && GetDimensionComponent(splitDimension, rightPtr) >= splitValue)
                {
                    // copy item to dest ptr
                    // (to ensure the split is cacheline aligned)
                    *(Entry*)(rightPtr + rightPtrOffset) = *(Entry*)rightPtr;

                    rightPtr -= sizeof(Entry);
                }

                // if entries are on the wrong side, swap them over
                if (leftPtr < rightPtr)
                {
                    Entry temp = *(Entry*)rightPtr;
                    *(Entry*)rightPtr = *(Entry*)leftPtr;
                    *(Entry*)(rightPtr + rightPtrOffset) = *(Entry*)leftPtr;
                    *(Entry*)leftPtr = temp;

                    leftPtr += sizeof(Entry);
                    rightPtr -= sizeof(Entry);
                }
            }

            *(Entry*)(rightPtr + rightPtrOffset) = *(Entry*)rightPtr;

            // find pivot
            if (leftPtr > beginPtr && GetDimensionComponent(splitDimension, leftPtr) >= splitValue)
            {
                leftPtr -= sizeof(Entry);
            }

            uint leftNode = 2 * nodeIndex;
            uint rightNode = leftNode + 1;

            SetNode(leftNode, beginPtr, leftPtr);
            if (!preProcess)
                BuildSubTree(leftNode, depth);

            SetNode(rightNode, leftPtr + sizeof(Entry) + rightPtrOffset, endPtr + rightPtrOffset);
            if (!preProcess)
                BuildSubTree(rightNode, depth);

#if ENABLE_UNITY_COLLECTIONS_CHECKS && ENABLE_KDTREE_VALIDATION_CHECKS
            CheckNode(leftNode, depth);
            CheckNode(rightNode, depth);

            CheckChildNodes(leftNode, rightNode, count);
#endif
        }
        else
        {
            // this is a leaf node
            nodePtr->count |= k_IsLeafNodeBitFlag;

            if (preProcess)
            {
                uint leftNode = 2 * nodeIndex;
                uint rightNode = leftNode + 1;

                // fill in left/right node details
                SetEmptyLeafNode(leftNode);
                SetEmptyLeafNode(rightNode);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetNode(uint nodeIndex, byte* beginPtr, byte* endPtr)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        ulong entryIndex = ((ulong)beginPtr - (ulong)m_EntriesPtr) / (uint)sizeof(Entry);

        * nodePtr = new TreeNode
        {
            beginEntryIndex = (uint)entryIndex,
            count = (uint)((endPtr - beginPtr) / sizeof(Entry)) + 1
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetEmptyLeafNode(uint nodeIndex)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        *nodePtr = new TreeNode
        {
            count = k_IsLeafNodeBitFlag,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ExpandBounds(ref Bounds bounds, in float3 pos)
    {
        bounds.min = math.min(bounds.min, pos);
        bounds.max = math.max(bounds.max, pos);
    }

    // ******************************************************************
    // querying

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float GetClosestBoundsIntersection(in float3 p, in Bounds b)
    {
        // Find the closest point to the circle within the rectangle
        var closest = math.clamp(p, b.min, b.max);

        return math.distancesq(p, closest);
    }

    public int GetEntriesInRange(in float3 position, float range, ref NativeArray<Neighbour> neighbours, int threadIndex = 0)
    {
        return GetEntriesInRange(-1, position, range, ref neighbours, threadIndex);
    }

    public int GetEntriesInRange(in float3 position, float range, ref NativePriorityHeap<Neighbour> neighbours, int threadIndex = 0)
    {
        return GetEntriesInRange(-1, position, range, ref neighbours, threadIndex);
    }

    // we can optionally specify a querying index if one of the entries is checking for neighbours
    // eg. so that it can filter itself out
    public int GetEntriesInRange(int queryingIndex, in float3 position, float range, ref NativePriorityHeap<Neighbour> neighbours, int threadIndex = 0)
    {
        if (m_NumEntries == 0)
            return 0;

#if ENABLE_KDTREE_ANALYTICS
        QueryStats* stats = (QueryStats*)((byte*)m_QueryStats + (JobsUtility.CacheLineSize * threadIndex));
        stats->NumQueries++;
#endif

        var rangeSq = range * range;
        QueryTreeRecursive(queryingIndex, position, ref rangeSq, k_RootNodeIndex, ref neighbours, 0, threadIndex);

        return neighbours.Count;
    }

    public int GetEntriesInRange(int queryingIndex, in float3 position, float range, ref NativeArray<Neighbour> neighbours, int threadIndex = 0)
    {
        if (m_NumEntries == 0)
            return 0;

#if ENABLE_KDTREE_ANALYTICS
        QueryStats* stats = (QueryStats*)((byte*)m_QueryStats + (JobsUtility.CacheLineSize * threadIndex));
        stats->NumQueries++;
#endif

        var neighboursAsPriorityHeap = NativePriorityHeap<Neighbour>.FromArray(neighbours, 0, NativePriorityHeap.Comparison.Max);

        var rangeSq = range * range;
        QueryTreeRecursive(queryingIndex, position, ref rangeSq, k_RootNodeIndex, ref neighboursAsPriorityHeap, 0, threadIndex);

        neighbours = neighboursAsPriorityHeap.AsArray();
        return neighboursAsPriorityHeap.Count;
    }

    void QueryTreeRecursive(int queryingIndex, in float3 position, ref float rangeSq, uint nodeIndex, ref NativePriorityHeap<Neighbour> neighboursAsPriorityHeap, int numNeighbours, int threadIndex)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

#if ENABLE_KDTREE_ANALYTICS
        QueryStats* stats = (QueryStats*)((byte*)m_QueryStats + (JobsUtility.CacheLineSize * threadIndex));
        stats->NumNodesVisited++;
#endif

        // is this a leaf node
        // do dist check
        if (nodePtr->IsLeaf)
        {
            SearchEntriesInRange(nodePtr, queryingIndex, position, ref rangeSq, ref neighboursAsPriorityHeap, threadIndex);
        }
        else
        {
            uint leftNodeIndex = 2 * nodeIndex;
            TreeNode* leftNodePtr = m_NodesPtr + leftNodeIndex;

            uint rightNodeIndex = leftNodeIndex + 1;
            TreeNode* rightNodePtr = m_NodesPtr + rightNodeIndex;

            float leftIntersectDistSq = GetClosestBoundsIntersection(position, leftNodePtr->bounds);
            float rightIntersectDistSq = GetClosestBoundsIntersection(position, rightNodePtr->bounds);

            if (leftIntersectDistSq <= rightIntersectDistSq)
            {
                if (leftIntersectDistSq <= rangeSq)
                    QueryTreeRecursive(queryingIndex, position, ref rangeSq, leftNodeIndex, ref neighboursAsPriorityHeap, numNeighbours, threadIndex);

                if (rightIntersectDistSq <= rangeSq)
                    QueryTreeRecursive(queryingIndex, position, ref rangeSq, rightNodeIndex, ref neighboursAsPriorityHeap, numNeighbours, threadIndex);
            }
            else
            {
                if (rightIntersectDistSq <= rangeSq)
                    QueryTreeRecursive(queryingIndex, position, ref rangeSq, rightNodeIndex, ref neighboursAsPriorityHeap, numNeighbours, threadIndex);

                if (leftIntersectDistSq <= rangeSq)
                    QueryTreeRecursive(queryingIndex, position, ref rangeSq, leftNodeIndex, ref neighboursAsPriorityHeap, numNeighbours, threadIndex);
            }
        }
    }

    public void SearchEntriesInRange(TreeNode* nodePtr, int queryingIndex, in float3 position, ref float rangeSq, ref NativePriorityHeap<Neighbour> neighbours, int threadIndex)
    {
        uint count = nodePtr->Count;
        byte* beginPtr = nodePtr->GetBeginPtr(m_EntriesPtr);
        byte* endPtr = beginPtr + (count - 1) * sizeof(Entry);

        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry))
        {
            var ptr = (Entry*)entryPtr;
      
#if ENABLE_KDTREE_ANALYTICS
            QueryStats* stats = (QueryStats*)((byte*)m_QueryStats + (JobsUtility.CacheLineSize * threadIndex));
            stats->NumEntriesCompared++;
#endif
            float distSq = math.distancesq(position, ptr->position);

            if (distSq <= rangeSq && queryingIndex != ptr->index)
            {
                if (neighbours.Count < neighbours.Capacity)
                {
                    neighbours.Push(new Neighbour { index = ptr->index, distSq = distSq, position = ptr->position });

                    if (neighbours.Count == neighbours.Capacity)
                        rangeSq = neighbours.Peek().distSq;
                }
                else
                {
#if ENABLE_KDTREE_ANALYTICS
                    stats->NumEntriesFoundOverNeighbourCapacity++;
#endif
                    // pop furthest off heap
                    neighbours.Pop();
                    neighbours.Push(new Neighbour { index = ptr->index, distSq = distSq, position = ptr->position });

                    rangeSq = neighbours.Peek().distSq;
                }
            }
        }
    }


    // *******************************************************
    // analytics

    // useful for profiling how many comparisons are required for a given
    // tree setup.
    public JobHandle BeginAnalyticsCapture(JobHandle inputDeps)
    {
#if ENABLE_KDTREE_ANALYTICS
        var initJob = new InitialiseQueryStatsJob
        {
            This = this,
        };
        return initJob.Schedule(inputDeps);
#else
        return inputDeps;
#endif
    }

    [BurstCompile]
    struct InitialiseQueryStatsJob : IJob
    {
        internal KDTree This;

        public void Execute()
        {
#if ENABLE_KDTREE_ANALYTICS
            for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
            {
                QueryStats* stats = (QueryStats*)((byte*)This.m_QueryStats + (JobsUtility.CacheLineSize * i));
                stats->NumEntriesCompared = 0;
                stats->NumQueries = 0;
                stats->NumEntriesFoundOverNeighbourCapacity = 0;
                stats->NumNodesVisited = 0;
            }
#endif
        }
    }

    [BurstCompile]
    struct CombineQueryStatsJob : IJob
    {
        internal KDTree This;

        public void Execute()
        {
#if ENABLE_KDTREE_ANALYTICS
            QueryStats* combinedStats = This.m_QueryStats;

            for (int i = 1; i < JobsUtility.MaxJobThreadCount; i++)
            {
                QueryStats* stats = (QueryStats*)((byte*)This.m_QueryStats + (JobsUtility.CacheLineSize * i));

                combinedStats->NumEntriesCompared += stats->NumEntriesCompared;
                combinedStats->NumQueries += stats->NumQueries;
                combinedStats->NumEntriesFoundOverNeighbourCapacity += stats->NumEntriesFoundOverNeighbourCapacity;
                combinedStats->NumNodesVisited += stats->NumNodesVisited;
            }
#endif
        }
    }

    public JobHandle EndAnalyticsCapture(JobHandle inputDeps, Texture2D nodeUsageTexture = null)
    {
#if ENABLE_KDTREE_ANALYTICS
        var dep = inputDeps;

        var analyseNodesJob = new AnalyseNodeUsageJob
        {
            This = this,
        };
        dep = analyseNodesJob.Schedule(dep);

        if (nodeUsageTexture != null)
        {
            var buildNodeUsageTextureJob = new BuildNodeUsageTextureJob
            {
                Texture = nodeUsageTexture.GetRawTextureData<Color32>(),
                Height = nodeUsageTexture.height,
                Width = nodeUsageTexture.width,
                This = this,
            };
            dep = buildNodeUsageTextureJob.Schedule(dep);
        }

        var combineJob = new CombineQueryStatsJob
        {
            This = this,
        };
        dep = combineJob.Schedule(dep);

        return dep;
#else
        return inputDeps;
#endif
    }

    [BurstCompile]
    struct AnalyseNodeUsageJob : IJob
    {
        internal KDTree This;

        public void Execute()
        {
#if ENABLE_KDTREE_ANALYTICS
            for (int i = 0; i < This.m_NodeUsage.Length; i++)
            {
                This.m_NodeUsage[i] = 0;
            }
            This.PopulateNodeSizeBucketsRecursive(k_RootNodeIndex, ref This.m_NodeUsage);
#endif
        }
    }

    [BurstCompile]
    unsafe struct BuildNodeUsageTextureJob : IJob
    {
        public NativeArray<Color32> Texture;
        public int Height;
        public int Width;

        internal KDTree This;

        public void Execute()
        {
#if ENABLE_KDTREE_ANALYTICS
            var nodeUsage = This.m_NodeUsage;
            var pixelsPerNode = Width / nodeUsage.Length;

            int maxUsage = 0;
            for (int i = 0; i < nodeUsage.Length; i++)
            {
                if (nodeUsage[i] > maxUsage)
                    maxUsage = nodeUsage[i];
            }

            Color32* texturePtr = (Color32*)Texture.GetUnsafePtr();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var nodeIndex = x / pixelsPerNode;

                    var barHeight = 0;
                    if (nodeIndex < nodeUsage.Length)
                    {
                        barHeight = (nodeUsage[nodeIndex] * Height) / maxUsage;

                        if (y <= barHeight)
                        {
                            *texturePtr = new Color32(255, 255, 255, 255);
                        }
                        else
                        {
                            *texturePtr = new Color32(0, 0, 0, 50);
                        }
                    }
                    else
                    {
                        *texturePtr = new Color32(0, 0, 0, 0);
                    }

                    texturePtr++;
                }
            }
#endif
        }
    }

    public QueryStats GetQueryStats()
    {
#if ENABLE_KDTREE_ANALYTICS
        return *m_QueryStats;
#else
        return new QueryStats();
#endif
    }

#if ENABLE_KDTREE_ANALYTICS
    void PopulateNodeSizeBucketsRecursive(uint nodeIndex, ref NativeArray<int> nodeSizeBuckets)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        if (nodePtr->IsLeaf)
        {
            // leaf
            uint count = nodePtr->Count;
            int bucketCount = math.min((int)count, m_MaxLeafSize + 1);
            nodeSizeBuckets[bucketCount]++;
        }
        else
        {
            uint leftNodeIndex = 2 * nodeIndex;
            uint rightNodeIndex = leftNodeIndex + 1;
            PopulateNodeSizeBucketsRecursive(leftNodeIndex, ref nodeSizeBuckets);
            PopulateNodeSizeBucketsRecursive(rightNodeIndex, ref nodeSizeBuckets);
        }
    }
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS && ENABLE_KDTREE_VALIDATION_CHECKS
    internal void CheckNode(uint nodeIndex, int depth)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

        // check that each entry matches up
        uint count = nodePtr->Count;

        if (count > m_Capacity || count == 0)
            throw new InvalidOperationException($"Node at index {nodeIndex} has corrupt count {count} at depth {depth}");

        byte* beginPtr = nodePtr->GetBeginPtr(m_EntriesPtr);
        if (beginPtr == null)
            throw new InvalidOperationException($"Node at index {nodeIndex} has null begin ptr (count {count}, depth {depth}");

        byte* endPtr = beginPtr + (count - 1) * sizeof(Entry);

        int i = 0;
        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry), i++)
        {
            var entry = *(Entry*)entryPtr;

            int j = 0;
            for (byte* innerEntryPtr = beginPtr; innerEntryPtr <= endPtr; innerEntryPtr += sizeof(Entry), j++)
            {
                if (i != j)
                {
                    var compare = *(Entry*)innerEntryPtr;
                    if (compare.index == entry.index)
                        throw new InvalidOperationException($"Node at index {nodeIndex} has duplicated entities at {i} and {j} (count {count})");
                }
            }
        }
    }

    internal void CheckChildNodes(uint leftNode, uint rightNode, uint parentCount)
    {
        var leftNodePtr = m_NodesPtr + leftNode;
        var rightNodePtr = m_NodesPtr + rightNode;

        uint totalChildCount = (leftNodePtr->Count) + (rightNodePtr->Count);
        if (totalChildCount != parentCount)
            throw new InvalidOperationException($"Total left/right count {totalChildCount} does not match parent count {parentCount}");
    }
#endif

}