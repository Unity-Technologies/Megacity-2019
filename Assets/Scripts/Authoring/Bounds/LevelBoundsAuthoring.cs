using Unity.Entities;
using Unity.Megacity.Gameplay;
using UnityEngine;

namespace Unity.Megacity.Authoring
{
    /// <summary>
    /// examines the object's scale and position to generate a cube and
    /// establishes the map's limits according to the cube.
    /// </summary>
    public class LevelBoundsAuthoring : MonoBehaviour
    {
        [SerializeField] 
        private float offset;
        private float Scale => transform.localScale.y / 2;
        private float SafeArea => Scale - offset;
        
        [BakingVersion("julian", 3)]
        public class LevelBoundsBaker : Baker<LevelBoundsAuthoring>
        {
            public override void Bake(LevelBoundsAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var position = authoring.transform.position;
                AddComponent(entity, new LevelBounds
                {
                    Center = position,
                    Top = position + Vector3.up * authoring.SafeArea,
                    Bottom = position - Vector3.up * authoring.SafeArea,
                    SafeAreaSq = authoring.SafeArea * authoring.SafeArea,
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            var thisTransform = transform;
            Gizmos.color = Color.red;
            var position = thisTransform.position;
            Gizmos.DrawWireSphere(position + (Vector3.up * SafeArea), SafeArea);
            Gizmos.DrawWireSphere(position - (Vector3.up * SafeArea), SafeArea);
            Gizmos.DrawWireSphere(position, SafeArea);
        }
    }
}
