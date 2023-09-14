using Unity.Entities;
using Unity.NetCode;

namespace Unity.Megacity.Utils
{
    public struct MultiMaterialObjectTag : IComponentData
    {
    }

    public struct MultiMaterialUpdated : IComponentData
    {
    }
    
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    public struct SetPlayerColorTag : IComponentData
    {
    }
}
