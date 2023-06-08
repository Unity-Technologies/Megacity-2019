using Unity.Entities;
using UnityEngine;
using Unity.MegaCity.Gameplay;

namespace Unity.MegaCity.Authoring
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
                AddComponent(entity, new LevelBounds
                {
                    Center = authoring.transform.position,
                    Top = authoring.transform.position + (Vector3.up * authoring.SafeArea),
                    Bottom = authoring.transform.position - (Vector3.up * authoring.SafeArea),
                    SafeAreaSq = authoring.SafeArea * authoring.SafeArea,
                    IsInside = true
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
