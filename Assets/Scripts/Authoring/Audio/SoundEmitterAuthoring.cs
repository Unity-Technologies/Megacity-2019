using Unity.Entities;
using UnityEngine;
using Unity.MegaCity.Audio;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Takes ECSoundEmitterComponent and convert them all to ECSoundEmitter based on ECSoundEmitterDefinitionAsset
    /// Also, it can return the definition asset settings.
    /// </summary>
    public class SoundEmitterAuthoring : MonoBehaviour
    {
        public SoundEmitterDefinitionAsset definition;

        [BakingVersion("Julian", 2)]
        public class SoundEmitterBaker : Baker<SoundEmitterAuthoring>
        {
            public override void Bake(SoundEmitterAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var myTransform = authoring.transform;
                var soundEmitter = new SoundEmitter
                {
                    definitionIndex = authoring.definition.data.definitionIndex,
                    position =  myTransform.position,
                    direction = myTransform.right
                };
                AddComponent(entity, soundEmitter);
            }
        }
    }
}
