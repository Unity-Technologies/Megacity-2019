using Unity.Entities;
using UnityEngine;

namespace Unity.MegaCity.Utils
{
    /// <summary>
    /// Adds the MultiMaterialObjectTag which identifies an entity that is missing a material definition.
    /// </summary>
    [DisallowMultipleComponent]
    public class MultiMaterialObjectTagAuthoring : MonoBehaviour
    {
        [BakingVersion("julian", 2)]
        private class MultiMaterialObjectTagBaker : Baker<MultiMaterialObjectTagAuthoring>
        {
            public override void Bake(MultiMaterialObjectTagAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var component = default(MultiMaterialObjectTag);
                AddComponent(entity, component);
            }
        }
    }
}
