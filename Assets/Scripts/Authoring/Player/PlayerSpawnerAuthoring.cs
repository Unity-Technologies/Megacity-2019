using Unity.Entities;
using UnityEngine;
using Unity.MegaCity.Gameplay;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Create tag component for the player camera target
    /// </summary>
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Player;

        [BakingVersion("julian", 2)]
        public class PlayerSpawnerBaker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerSpawner { Player = player });
            }
        }
    }   
}
