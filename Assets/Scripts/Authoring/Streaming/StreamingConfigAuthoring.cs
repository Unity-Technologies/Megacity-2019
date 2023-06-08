using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Unity.MegaCity.Streaming;
#if !UNITY_EDITOR_WIN
using Unity.Mathematics;
using UnityEngine.Serialization;
#endif

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Configures the streaming in/out distances based on player position in the scene
    /// </summary>
    public class StreamingConfigAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        public float StreamingInDistance = 1200f;
        public float StreamingOutDistance = 1250f;
#if !UNITY_EDITOR_WIN
        public float2 StreamingLowMinInOut = new (600f, 800f);
#endif
        public SceneAsset PlayerScene;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            var position = transform.position;
            Gizmos.DrawWireSphere(position, StreamingInDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, StreamingOutDistance);
        }

        [BakingVersion("julian", 2)]
        public class StreamingConfigAuthoringBaker : Baker<StreamingConfigAuthoring>
        {
            public override void Bake(StreamingConfigAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var config = new StreamingConfig()
                {
#if UNITY_EDITOR_WIN
                    DistanceForStreamingIn = authoring.StreamingInDistance,
                    DistanceForStreamingOut = authoring.StreamingOutDistance,
#else
                    DistanceForStreamingIn = math.min(authoring.StreamingLowMinInOut.x,authoring.StreamingInDistance),
                    DistanceForStreamingOut = math.min(authoring.StreamingLowMinInOut.y, authoring.StreamingOutDistance),
#endif
                    PlayerSectionGUID =
                        new Entities.Hash128(
                            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.PlayerScene))),
                };
                AddComponent(entity, config);
            }
        }
#endif
    }
}