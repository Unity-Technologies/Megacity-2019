using Unity.Entities;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Prefab to spawn for players
    /// </summary>
    public struct PlayerSpawner : IComponentData
    {
        public Entity Player;
    }
}
