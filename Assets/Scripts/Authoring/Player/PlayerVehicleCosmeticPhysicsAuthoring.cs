using Unity.Entities;
using UnityEngine;
using Unity.MegaCity.Gameplay;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Add required tag components for cosmetic physics
    /// </summary>
    public class PlayerVehicleCosmeticPhysicsAuthoring : MonoBehaviour
    {
        public GameObject VehicleController;

        [BakingVersion("julian", 2)]
        public class PlayerVehicleCosmeticPhysicsBaker : Baker<PlayerVehicleCosmeticPhysicsAuthoring>
        {
            public override void Bake(PlayerVehicleCosmeticPhysicsAuthoring authoring)
            {
                var vehicle = GetEntity(authoring.VehicleController, TransformUsageFlags.Dynamic);
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerVehicleCosmeticPhysics { VehicleEntity = vehicle });
            }
        }
    }
}
