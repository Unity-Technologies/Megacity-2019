using Unity.Entities;
using UnityEngine;
using Unity.MegaCity.Gameplay;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Defines a spawn point on the map based on the position of a child object.
    /// </summary>
    public class SpawnPointAuthoring : MonoBehaviour
    {
        [BakingVersion("julian", 2)]
        public class SpawnPointBaker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var spawnPoints = AddBuffer<SpawnPointElement>(entity);
                var children =  FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                foreach (var child in children)
                {
                    spawnPoints.Add(new SpawnPointElement { Value = child.transform.position });
                }
            }
        }
    }
}
