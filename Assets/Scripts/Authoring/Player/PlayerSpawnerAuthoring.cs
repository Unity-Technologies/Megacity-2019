using Unity.Entities;
using Unity.Megacity.Gameplay;
using UnityEngine;

namespace Unity.Megacity.Authoring
{
    /// <summary>
    /// Create tag component for the player camera target
    /// </summary>
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Player;
        public GameObject SinglePlayer;

        [BakingVersion("julian", 2)]
        public class PlayerSpawnerBaker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                var player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
                var singlePlayer = GetEntity(authoring.SinglePlayer, TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new PlayerSpawner { Player = player, SinglePlayer = singlePlayer});
            }
        }
    }   
}
