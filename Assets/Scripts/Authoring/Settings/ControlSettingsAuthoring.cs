using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.Gameplay;
using UnityEngine;

namespace Unity.Megacity.Authoring
{
    /// <summary>
    /// Authoring component for ControlSettings
    /// </summary>
    public class ControlSettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private float MouseSensitivity = 1f;
        [SerializeField]
        private float2 AccelerationRange;
        [SerializeField]
        private float2 DirectionRange;
        
        [BakingVersion("julian", 2)] 
        public class ControlSettingsBaker : Baker<ControlSettingsAuthoring>
        {
            public override void Bake(ControlSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ControlSettings
                {
                    MouseSensitivity = authoring.MouseSensitivity,
                    InverseLookHorizontal = false,
                    InverseLookVertical = false,
                    AccelerationRange = authoring.AccelerationRange,
                    DirectionRange = authoring.DirectionRange
                });
            }
        }
    }
}