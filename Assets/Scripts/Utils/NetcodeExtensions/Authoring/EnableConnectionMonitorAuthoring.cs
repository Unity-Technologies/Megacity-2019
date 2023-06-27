using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Extensions
{
    [DisallowMultipleComponent]
    public class EnableConnectionMonitorAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableConnectionMonitorAuthoring>
        {
            public override void Bake(EnableConnectionMonitorAuthoring authoring)
            {
                EnableConnectionMonitor component = default(EnableConnectionMonitor);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}