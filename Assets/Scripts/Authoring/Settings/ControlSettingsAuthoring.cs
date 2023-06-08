using Unity.Entities;
using Unity.MegaCity.Gameplay;
using UnityEngine;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Authoring component for ControlSettings
    /// </summary>
    public class ControlSettingsAuthoring : MonoBehaviour
    {
        [BakingVersion("diego", 1)]
        public class ControlSettingsBaker : Baker<ControlSettingsAuthoring>
        {
            public override void Bake(ControlSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ControlSettings
                {
                    MouseSensitivity = 1f,
                    InverseLookHorizontal = false,
                    InverseLookVertical = false
                });
            }
        }
    }
}