using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.Traffic;
using Unity.Megacity.UI;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Megacity.Audio
{
    public partial class AudioFrame : ComponentSystemGroup
    {
    }
    /// <summary>
    /// Instantiate AudioSources GameObjects.
    /// Then system collects all vehicles in the Scene that have VehiclePathing attached.
    /// Creates different lists to store, position, entities and distance, that are near to the camera position.
    /// Then based on that lists, it plays the audio sources in the entities positions.
    /// </summary>
    [UpdateInGroup(typeof(AudioFrame))]
    public partial class SoundVehiclePoolSystem : SystemBase
    {
        private AudioSystemSettings m_AudioSettings;
        private TrafficAudioSettings m_TrafficAudioSettings;
        private AudioSource[] m_AudioSources;
        private Transform m_MainCameraTransform;
        private Scene AdditiveScene;

        private NativeArray<float> m_ClosestVehiclesSqDistance;
        private NativeArray<Entity> m_ClosestVehiclesEntities;
        private NativeArray<float3> m_ClosestVehiclesPositions;
        private (Entity entity, AudioSource source)[] m_VehiclesWithActiveSounds;

        protected override void OnCreate()
        {
            RequireForUpdate<AudioSystemSettings>();
        }

        protected override void OnDestroy()
        {
            if (m_ClosestVehiclesSqDistance.IsCreated)
                m_ClosestVehiclesSqDistance.Dispose();
            if (m_ClosestVehiclesEntities.IsCreated)
                m_ClosestVehiclesEntities.Dispose();
            if (m_ClosestVehiclesPositions.IsCreated)
                m_ClosestVehiclesPositions.Dispose();
        }

        protected override void OnUpdate()
        {
            if (SceneController.IsGameScene)
                return;
            
            Initialize();
            
            if(m_MainCameraTransform == null)
                m_MainCameraTransform = Camera.main.transform;
            // Step 1 - Find the MaxVehicles closest vehicles. This is done by keeping the
            // squared distance of the furthest vehicle away from the current set, and every
            // other vehicle is then compared against it. When a vehicle is found to be closer,
            // we record its entity and position, and we find the new furthest vehicle away from
            // the set. This produces an unordered array with the right amount of closest vehicles.

            var closestVehiclesSqDistance = m_ClosestVehiclesSqDistance;
            var closestVehiclesEntities = m_ClosestVehiclesEntities;
            var closestVehiclesPositions = m_ClosestVehiclesPositions;

            float3 cameraPosition = m_MainCameraTransform.position;
            for (int i = 0; i < m_AudioSettings.MaxVehicles; i++)
            {
                m_ClosestVehiclesSqDistance[i] = float.MaxValue;
            }

            float maxSqDistanceInClosestSet = float.MaxValue;
            int maxSqDistanceInClosestSetIndex = 0;
            var maxDistanceSq = m_AudioSettings.MaxSqDistance;
            Entities
                .WithAll<VehiclePathing>()
                .ForEach((Entity entity, in LocalToWorld transform) =>
            {
                var sqDist = math.distancesq(transform.Position, cameraPosition);
                if (sqDist < maxDistanceSq && sqDist < maxSqDistanceInClosestSet)
                {
                    // Vehicle in range and closer than the furthest in the current set.
                    closestVehiclesSqDistance[maxSqDistanceInClosestSetIndex] = sqDist;
                    closestVehiclesEntities[maxSqDistanceInClosestSetIndex] = entity;
                    closestVehiclesPositions[maxSqDistanceInClosestSetIndex] = transform.Position;
                    maxSqDistanceInClosestSet = sqDist;

                    // Find the new furthest vehicle in the current set, since we just replaced it.
                    for (int i = 0; i < closestVehiclesSqDistance.Length; i++)
                    {
                        if (closestVehiclesSqDistance[i] > maxSqDistanceInClosestSet)
                        {
                            maxSqDistanceInClosestSet = closestVehiclesSqDistance[i];
                            maxSqDistanceInClosestSetIndex = i;
                        }
                    }
                }
            }).Run();

            // Step 2 - Reorder currently playing sound sources so they are in the same
            // order as the closest vehicles returned by the current search.

            for (int i = 0; i < m_AudioSettings.MaxVehicles; i++)
            {
                if (m_ClosestVehiclesSqDistance[i] == float.MaxValue)
                {
                    // If we have less vehicles in range than the array length, skip the end of the array.
                    break;
                }

                if (m_ClosestVehiclesEntities[i] == m_VehiclesWithActiveSounds[i].entity)
                {
                    // If the current entity is already at the correct index, keep going.
                    // This is expected to be the most frequent case.
                    continue;
                }

                // If the current entity is different from the one in the array of currently playing sources,
                // find it (if it exists) and swap the array elements.
                for (int j = 0; j < m_AudioSettings.MaxVehicles; j++)
                {
                    if (m_ClosestVehiclesEntities[i] == m_VehiclesWithActiveSounds[j].entity)
                    {
                        (m_VehiclesWithActiveSounds[i], m_VehiclesWithActiveSounds[j]) = (m_VehiclesWithActiveSounds[j], m_VehiclesWithActiveSounds[i]);
                    }
                }
            }

            // Step 3 - Update the sound sources positions (always) and clips (when the entity changed).

            for (int i = 0; i < m_AudioSettings.MaxVehicles; i++)
            {
                if (m_ClosestVehiclesSqDistance[i] == float.MaxValue)
                {
                    // Less vehicles in range that potential sources, so this source isn't used, stop it.
                    if(m_VehiclesWithActiveSounds[i].source != null)
                        m_VehiclesWithActiveSounds[i].source.Stop();
                }
                else
                {
                    if (m_ClosestVehiclesEntities[i] != m_VehiclesWithActiveSounds[i].entity)
                    {
                        // The entity changed, this means that vehicle that was playing the current source is not
                        // amongst the closest ones anymore. So we recycle the source and change its clip.
                        var vehicleType = SystemAPI.GetComponent<VehiclePathing>(m_ClosestVehiclesEntities[i]).VehicleType;
                        m_VehiclesWithActiveSounds[i].entity = m_ClosestVehiclesEntities[i];
                        m_VehiclesWithActiveSounds[i].source.clip = m_TrafficAudioSettings.audioClips[vehicleType];
                        m_VehiclesWithActiveSounds[i].source.Play();
                    }

                    // Update the position of the source to reflect the movement of the vehicle.
                    m_VehiclesWithActiveSounds[i].source.transform.position = m_ClosestVehiclesPositions[i];
                }
            }

            if (m_AudioSettings.DebugMode)
            {
                for (int i = 0; i < m_AudioSettings.MaxVehicles; i++)
                {
                    var source = m_VehiclesWithActiveSounds[i].source;
                    if (source.isPlaying)
                    {
                        Debug.DrawLine(cameraPosition, source.transform.position, Color.green);
                    }
                }
            }
        }

        private void Initialize()
        {
            if (m_TrafficAudioSettings == null)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    AdditiveScene = SceneManager.GetSceneByName("AdditiveVehicleAudioPoolScene");
                }
                else
#endif
                {
                    AdditiveScene = SceneManager.CreateScene("AdditiveVehicleAudioPoolScenePlaymode");
                }
                
                m_AudioSettings = SystemAPI.GetSingleton<AudioSystemSettings>();
                m_ClosestVehiclesSqDistance = new NativeArray<float>(m_AudioSettings.MaxVehicles, Allocator.Persistent);
                m_ClosestVehiclesEntities = new NativeArray<Entity>(m_AudioSettings.MaxVehicles, Allocator.Persistent);
                m_ClosestVehiclesPositions = new NativeArray<float3>(m_AudioSettings.MaxVehicles, Allocator.Persistent);
                m_VehiclesWithActiveSounds = new (Entity entity, AudioSource source)[m_AudioSettings.MaxVehicles];

                m_TrafficAudioSettings = Object.FindObjectOfType<TrafficAudioSettings>();

                for (int i = 0; i < m_AudioSettings.MaxVehicles; i++)
                {
                    var gameObject = new GameObject($"AudioSource {i}");
                    var source = gameObject.AddComponent<AudioSource>();
                    source.loop = true;
                    source.spatialBlend = 1f;
                    source.dopplerLevel = 0f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.maxDistance = m_AudioSettings.MaxDistance;
                    source.outputAudioMixerGroup = AudioMaster.Instance.soundFX;

                    m_VehiclesWithActiveSounds[i].source = source;

                    SceneManager.MoveGameObjectToScene(gameObject, AdditiveScene);
                }
            }
        }
    }
}
