using Unity.Collections;
using Unity.Entities;
using Unity.Megacity.Audio;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Authoring
{
    /// <summary>
    /// The system gets all ECSoundEmitterComponent which belongs to the buildings.
    /// Then builds a hashmap based on the [definitionIndex] as a key and insert a new SingleEmitterData as a value.
    /// By using the hashmap build an AudioBlobRef data and add that to the Scene.
    /// Each Scene has a AudioBlobRef with a map of every single emitter.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [BakingVersion("julian", 2)]
    public partial struct SoundEmitterBlobBakingSystem : ISystem
    {
        private EntityQuery m_SoundEmitterQuery;
        
        public void OnCreate(ref SystemState state)
        {
            m_SoundEmitterQuery = state.GetEntityQuery(typeof(SoundEmitter));
        }
        
        public void OnUpdate(ref SystemState state) 
        {
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            var entityCount = m_SoundEmitterQuery.CalculateEntityCount();
            var highestIndex = 0;
            var rootSceneEntity = Entity.Null;
            
            var hashMap = new NativeParallelMultiHashMap<int, SingleEmitterData>(entityCount, Allocator.Temp);
            foreach (var (soundEmitter, entity) in Query<SoundEmitter>().WithEntityAccess())
            {
                if (rootSceneEntity == Entity.Null)
                {
                    //The first entity of the scene is used to store the AudioBlobRef [Blob Asset]
                    //This is necessary per scene to keep one asset per scene since each scene is streamed (load/unload)
                    rootSceneEntity = entity;
                }
                var definitionIndex = soundEmitter.definitionIndex < 0 ? 0 : soundEmitter.definitionIndex;
                var emitterData = new SingleEmitterData
                {
                    Position = soundEmitter.position,
                    Direction = soundEmitter.direction,
                };

                hashMap.Add(definitionIndex, emitterData);
                if (definitionIndex > highestIndex) 
                    highestIndex = definitionIndex;
            }
            
            // Don't create audio blob assets for sub-scenes with no ECSoundEmitterComponent
            if (rootSceneEntity == Entity.Null)
                return;

            ref var allEmitterData = ref blobBuilder.ConstructRoot<AllEmitterData>();
            var defInxBegBuilder = blobBuilder.Allocate(ref allEmitterData.DefIndexBeg, highestIndex + 1);
            var defInxEndBuilder = blobBuilder.Allocate(ref allEmitterData.DefIndexEnd, highestIndex + 1);
            var emitterBuilder = blobBuilder.Allocate(ref allEmitterData.Emitters, entityCount);

            int emitterIndex = 0;
            for (int definitionIndex = 0; definitionIndex < highestIndex; definitionIndex++)
            {
                defInxBegBuilder[definitionIndex] = emitterIndex;
                var enumerator = hashMap.GetValuesForKey(definitionIndex);
                while (enumerator.MoveNext())
                {
                    emitterBuilder[emitterIndex] = enumerator.Current;
                    emitterIndex++;
                }
                defInxEndBuilder[definitionIndex] = emitterIndex;
            }

            var blobRef = blobBuilder.CreateBlobAssetReference<AllEmitterData>(Allocator.Persistent);
            state.EntityManager.AddComponentData(rootSceneEntity, new AudioBlobRef { Data = blobRef });
        }
    }
}
