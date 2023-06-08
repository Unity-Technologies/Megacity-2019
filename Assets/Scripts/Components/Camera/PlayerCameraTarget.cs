using Unity.Entities;
using Unity.NetCode;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Update the hybrid camera target with player position camera target in order for the virtual camera to follow it
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    public struct PlayerCameraTarget : IComponentData { }
}
