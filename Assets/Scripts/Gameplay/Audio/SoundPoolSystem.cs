using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace Unity.Megacity.Audio
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation, WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class AudioFrame : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(AudioFrame))]
    public partial class SoundPoolSystem : SystemBase
    {
        static ProfilerMarker ProfilerMarkerCollectData = new ProfilerMarker("SoundPoolSystem.CollectData");
        static ProfilerMarker ProfilerMarkerQueryTrees = new ProfilerMarker("SoundPoolSystem.QueryTrees");

        static ProfilerMarker ProfilerMarkerA = new ProfilerMarker("SoundPoolSystem.A");
        static ProfilerMarker ProfilerMarkerB = new ProfilerMarker("SoundPoolSystem.B");
        static ProfilerMarker ProfilerMarkerC = new ProfilerMarker("SoundPoolSystem.C");
        static ProfilerMarker ProfilerMarkerD = new ProfilerMarker("SoundPoolSystem.D");

        private Scene m_AdditiveScene;
        private SoundManager m_SoundManager;
        private AudioTree [] m_AudioTrees;
        private AudioSource [] m_AudioSources;
        private NativeArray<ECSoundEmitterDefinition> m_Definitions;

        private EntityQuery m_AllEmitterBlobsQuery;
        private EntityQuery m_AddedEmitterBlobsQuery;
        private EntityQuery m_RemovedEmitterBlobsQuery;

        private Random m_Random;
        private int m_DirtyTrees;
        private int m_UpdateRoundRobin;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<AudioSystemSettings>();
            var entityQueryDescAll = new EntityQueryDesc { All = new[] {ComponentType.ReadOnly<AudioBlobRef>()}};
            RequireForUpdate(GetEntityQuery(entityQueryDescAll));

            m_AllEmitterBlobsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<AudioBlobRef>() }
            });

            m_AddedEmitterBlobsQuery = GetEntityQuery(new EntityQueryDesc
            {
                None = new [] { ComponentType.ReadOnly<TreeDataCollected>() },
                All = new[] { ComponentType.ReadOnly<AudioBlobRef>() }
            });

            m_RemovedEmitterBlobsQuery = GetEntityQuery(new EntityQueryDesc
            {
                None = new[] { ComponentType.ReadOnly<AudioBlobRef>() },
                All = new[] { ComponentType.ReadOnly<TreeDataCollected>() },
            });

            m_Random = new Random((uint)SystemAPI.Time.ElapsedTime + (uint)DateTime.Now.Ticks);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            var audioSettings = SystemAPI.GetSingleton<AudioSystemSettings>();
            if (!audioSettings.HasInitialized)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    m_AdditiveScene = SceneManager.GetSceneByName("AdditiveBuildingAudioPoolScene");
                }
                else
#endif
                {
                    m_AdditiveScene = SceneManager.CreateScene("AdditiveVehicleBuildingPoolScenePlaymode");
                }
                
                
                m_SoundManager = Object.FindObjectOfType<SoundManager>();
                var poolSize = m_SoundManager.m_Clips.Length * audioSettings.ClosestEmitterPerClipCount;
                m_AudioSources = new AudioSource[poolSize];
                for (int i = 0; i < poolSize; i++)
                {
                    var clipIdx = i / audioSettings.ClosestEmitterPerClipCount;
                    var instance = i % audioSettings.ClosestEmitterPerClipCount;
                    var gameObject = new GameObject($"AudioSource (clip {clipIdx} / instance {instance}");
                    m_AudioSources[i] = gameObject.AddComponent<AudioSource>();
                    m_AudioSources[i].clip = m_SoundManager.m_Clips[clipIdx];
                    m_AudioSources[i].loop = true;
                    m_AudioSources[i].spatialBlend = 1f;
                    m_AudioSources[i].dopplerLevel = 0f;
                    m_AudioSources[i].rolloffMode = AudioRolloffMode.Linear;
                    m_AudioSources[i].pitch = 1f + instance / 100f;
                    m_AudioSources[i].maxDistance = audioSettings.MaxDistance;
                    m_AudioSources[i].playOnAwake = false;
                    m_AudioSources[i].outputAudioMixerGroup = AudioMaster.Instance.soundFX;
                    SceneManager.MoveGameObjectToScene(gameObject, m_AdditiveScene);
                }

                m_Definitions = new NativeArray<ECSoundEmitterDefinition>(m_SoundManager.m_SoundDefinitions.Length, Allocator.Persistent);
                m_AudioTrees = new AudioTree[m_Definitions.Length];

                for (int i = 0; i < m_Definitions.Length; i++)
                {
                    m_Definitions[i] = m_SoundManager.m_SoundDefinitions[i].data;
                    var maxResultsPerAudioTree = m_AudioSources.Length / m_Definitions.Length;
                    m_AudioTrees[i].Initialize(maxResultsPerAudioTree);
                }

                audioSettings.HasInitialized = true;
                SystemAPI.SetSingleton(audioSettings);
            }
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            if (m_Definitions.IsCreated)
                m_Definitions.Dispose();
            if (m_AudioTrees != null)
            {
                for (int i=0;i < m_AudioTrees.Length; i++)
                {
                    m_AudioTrees[i].Dispose();
                }
            }
            
            if (m_AudioSources != null)
            {
                foreach (var audioSource in m_AudioSources)
                {
                    if(audioSource != null)
                        audioSource.Stop();
                }

                m_AudioSources = Array.Empty<AudioSource>();
            }
        }

        protected override void OnUpdate()
        {
            var audioSettings = SystemAPI.GetSingleton<AudioSystemSettings>();
            if (UnityEngine.Camera.main == null || !audioSettings.HasInitialized || !m_Definitions.IsCreated)
            {
                return;
            }
            
            var camPos = UnityEngine.Camera.main.transform.position;

            using (ProfilerMarkerQueryTrees.Auto())
            {
                // Do the queries before updating the trees, so we don't have to complete
                // the dependency on the kd-trees during the same frame.
                // This introduces a frame of latency but it doesn't matter because the
                // sounds being loaded and unloaded are far from the camera anyway.
                Dependency = GetTreeResults(camPos, Dependency);
            }

            using (ProfilerMarkerCollectData.Auto())
            {
                UpdateTreeData();
            }

            var currentAudioSourceIndex = 0;

            for (int i = 0; i < m_AudioTrees.Length; i++)
            {
                if (!m_AudioTrees[i].Results.IsCreated)
                    continue;

                var points = m_AudioTrees[i].Results;
                var pointsCount = m_AudioTrees[i].ResultsCount.Value;

                for (int j = 0; j < pointsCount; j++)
                {
                    var position = points[j].position;

                    var definition = m_Definitions[i];
                    // Only assign a clip and recycle the Audio Source if the current position is out of the camera range, otherwise keep playing this current clip.
                    if (math.distance(m_AudioSources[currentAudioSourceIndex].transform.position, camPos) > definition.maxDist)
                    {
                        var clipIndex = GetRandomIndex(definition.soundPlayerIndexMin, definition.soundPlayerIndexMax, m_SoundManager.m_Probabilities);
                        var minMaxVolume = m_SoundManager.m_Volumes[clipIndex];
                        var clip = m_SoundManager.m_Clips[clipIndex];
                        var volume = m_Random.NextFloat(minMaxVolume.x, minMaxVolume.y);
                        var randomStartTime = m_Random.NextFloat(0f, clip.length);

                        m_AudioSources[currentAudioSourceIndex].transform.position = m_AudioTrees[i].Results[j].position;
                        m_AudioSources[currentAudioSourceIndex].maxDistance = definition.maxDist;
                        m_AudioSources[currentAudioSourceIndex].volume = definition.volume + volume;
                        m_AudioSources[currentAudioSourceIndex].clip = clip;
                        m_AudioSources[currentAudioSourceIndex].time = randomStartTime;
                        if (!m_AudioSources[currentAudioSourceIndex].isPlaying)
                        {
                            m_AudioSources[currentAudioSourceIndex].Play();
                        }
                    }
                    
                    if(audioSettings.DebugMode)
                        Debug.DrawLine(camPos, position, m_AudioTrees[i].DebugLineColor);

                    currentAudioSourceIndex++;
                }
            }
        }

        public int GetRandomIndex(int min, int max, float[] priorities)
        {
            // Create a list of available clip indices within the given range
            var availableIndices = new List<int>();
            for (int i = min; i <= max; i++)
            {
                availableIndices.Add(i);
            }

            // Calculate the total priority score
            var totalPriority = 0f;
            for (int i = 0; i < priorities.Length; i++)
            {
                totalPriority += priorities[i];
            }

            // Create a dictionary of clip index and probability pairs
            var clipProbabilities = new Dictionary<int, float>();
            foreach (int index in availableIndices)
            {
                float clipPriority = priorities[index];
                float clipProbability = clipPriority / totalPriority;
                clipProbabilities.Add(index, clipProbability);
            }

            // Use the probability distribution to randomly select a clip index
            var randomValue = m_Random.NextFloat();
            var cumulativeProbability = 0f;
            foreach (KeyValuePair<int, float> pair in clipProbabilities)
            {
                cumulativeProbability += pair.Value;
                if (randomValue < cumulativeProbability)
                {
                    return pair.Key;
                }
            }

            // If no clip index was selected, return the minimum index
            return min;
        }

        private void UpdateTreeData()
        {
            // The kd-trees are updated in a round-robin fashion. If anything changed, the amount of trees to be
            // updated is set the count of trees. And every frame, one three (one sound definition) will be updated.
            var defCount = m_Definitions.Length;

            if (!m_AddedEmitterBlobsQuery.IsEmptyIgnoreFilter)
            {
                // Tag with system state component.
                EntityManager.AddComponent<TreeDataCollected>(m_AddedEmitterBlobsQuery);
                m_DirtyTrees = defCount;
            }

            if (!m_RemovedEmitterBlobsQuery.IsEmptyIgnoreFilter)
            {
                // System state component cleanup.
                EntityManager.RemoveComponent<TreeDataCollected>(m_RemovedEmitterBlobsQuery);
                m_DirtyTrees = defCount;
            }

            if (m_DirtyTrees == 0)
            {
                return;
            }

            ProfilerMarkerA.Begin();
            var blobs = m_AllEmitterBlobsQuery.ToComponentDataArray<AudioBlobRef>(Allocator.TempJob);
            var totalEmitterCountPerDefinition = new NativeArray<int>(defCount, Allocator.Temp);

            Job.WithCode(() =>
            {
                for (int blobIdx = 0; blobIdx < blobs.Length; blobIdx++)
                {
                    ref var indexBeg = ref blobs[blobIdx].Data.Value.DefIndexBeg;
                    ref var indexEnd = ref blobs[blobIdx].Data.Value.DefIndexEnd;
                    for (int i = 0; i < indexBeg.Length; i++)
                    {
                        // NB - the index arrays can be smaller than the definition array, in cases where
                        // scenes do not contain emitters matching the definitions at the end of the array.
                        totalEmitterCountPerDefinition[i] += indexEnd[i] - indexBeg[i];
                    }
                }
            }).Run();
            ProfilerMarkerA.End();

            var treeParams = KDTree.DefaultKDTreeParams;
            treeParams.AdditionalDepthToAllocate = 2;

            int defIdx = m_UpdateRoundRobin;
            ref var tree = ref m_AudioTrees[defIdx].Tree;
            if (tree.IsCreated)
            {
                ProfilerMarkerD.Begin();
                tree.Dispose();
                ProfilerMarkerD.End();
            }

            var emitterCount = totalEmitterCountPerDefinition[defIdx];
            if (emitterCount != 0)
            {
                ProfilerMarkerB.Begin();
                tree = new KDTree(emitterCount, Allocator.Persistent, treeParams);
                ProfilerMarkerB.End();

                var dependency = new CopyDataToTree
                {
                    blobs = blobs,
                    defIdx = defIdx,
                    tree = tree
                }.Schedule();

                ProfilerMarkerC.Begin();
                dependency = tree.BuildTree(emitterCount, dependency);
                dependency = blobs.Dispose(dependency);
                Dependency = JobHandle.CombineDependencies(Dependency, dependency);
                ProfilerMarkerC.End();
            }
            else
            {
                blobs.Dispose();
            }

            m_DirtyTrees -= 1;
            m_UpdateRoundRobin = (m_UpdateRoundRobin + 1) % defCount;
        }

        private JobHandle GetTreeResults(Vector3 camPos, JobHandle jobHandle)
        {
            for (int i = 0; i < m_AudioTrees.Length; i++)
            {
                if (!m_AudioTrees[i].Tree.IsCreated)
                    continue;

                var definition = m_Definitions[i];
                var searchJob = new GetEntriesInRangeJob
                {
                    QueryPosition = camPos,
                    Range = definition.maxDist,
                    Tree = m_AudioTrees[i].Tree,
                    Neighbours = m_AudioTrees[i].Results,
                    ResultsCount = m_AudioTrees[i].ResultsCount,
                };
                jobHandle = searchJob.Schedule(jobHandle);
                jobHandle.Complete();

            }
            return jobHandle;
        }
    }
}
