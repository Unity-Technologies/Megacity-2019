using Unity.Entities;
using UnityEngine;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Bake all the components required for the VehicleTraffic
    /// </summary>
    public class VehicleTrafficAuthoring : MonoBehaviour
    {
    }

    [BakingVersion("Julian", 2)]
    public class VehicleTrafficBaker : Baker<VehicleTrafficAuthoring>
    {
        public override void Bake(VehicleTrafficAuthoring authoring)
        {
            var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent<VehiclePathing>(entity);
            AddComponent<VehicleTargetPosition>(entity);
            AddComponent<VehiclePhysicsState>(entity);
        }
    }
}
