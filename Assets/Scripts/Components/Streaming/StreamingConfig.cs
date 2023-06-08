using System;
using Unity.Entities;
using Unity.NetCode;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.MegaCity.Streaming
{
    /// <summary>
    /// Creates two lists of Entities from the ones that got inside of camera range and the entities that got outside.
    /// Using the camera position and StreamingLogicConfig parameters create the lists using 2 different jobs to fill 2 different arrays.
    /// By using BuildCommandBufferJob adds to each entity (SceneSectionData) a component to add or remove it from the Scene.
    /// </summary>
    [Serializable]
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    public struct StreamingConfig : IComponentData
    {
        public float DistanceForStreamingIn;
        public float DistanceForStreamingOut;
        public Hash128 PlayerSectionGUID;
    }
}
