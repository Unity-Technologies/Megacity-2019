using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Author the Traffic settings components. Used later by the traffic system to manage the vehicles
    /// </summary>
    public class TrafficSettings : MonoBehaviour
    {
        public float pathSegments = 100;
        public float globalSpeedFactor = 1.0f;
        public int maxCars = 2000;
        public int poolVehicleCellSize = 30000;
        public float[] speedMultipliers;
        public List<GameObject> vehiclePrefabs;

        [BakingVersion("Julian", 2)]
        public class TrafficSettingsBaker : Baker<TrafficSettings>
        {
            public override void Bake(TrafficSettings authoring)
            {
                for (int j = 0; j < authoring.vehiclePrefabs.Count; j++)
                {
                    // A primary entity needs to be called before additional entities can be used
                    Entity vehiclePrefab = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    var prefabEntity = GetEntity(authoring.vehiclePrefabs[j], TransformUsageFlags.Dynamic);
                    var prefabData = new VehiclePrefabData
                    {
                        VehiclePrefab = prefabEntity,
                        VehicleSpeed = j < authoring.speedMultipliers.Length ? authoring.speedMultipliers[j] : 3.0f
                    };

                    AddComponent(vehiclePrefab, prefabData);
                }
                var trafficSettings = new TrafficSettingsData
                {
                    GlobalSpeedFactor = authoring.globalSpeedFactor,
                    PathSegments = authoring.pathSegments,
                    MaxCars = authoring.maxCars,
                    PoolCellVehicleSize = authoring.poolVehicleCellSize
                };
                var entityData = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entityData, trafficSettings);
            }
        }
    }
}