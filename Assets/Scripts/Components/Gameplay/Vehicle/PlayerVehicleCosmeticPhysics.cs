using Unity.Entities;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Move the player mesh to allow nice cosmetic effects like rolling and breaking the car
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    public struct PlayerVehicleCosmeticPhysics : IComponentData
    {
        public Entity VehicleEntity;
    }
}
