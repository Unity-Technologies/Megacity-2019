using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerInfoItemSettings", menuName = "Gameplay/Settings/HUDPlayerInfoSettings", order = 1)]
    public class PlayerInfoItemSettings : ScriptableObject
    {
        
        [SerializeField]
        private float m_RayOffsetFromCamera = 10f;
        [SerializeField]
        private float m_MinDistanceBetweenCameraRayAndPlayer = 5f;
        [SerializeField]
        private float m_MinLifeBar = 30f;
        [SerializeField]
        private float3 m_Offset;
        [SerializeField]
        private float3 m_MinOffset;
        [SerializeField]
        private float2 m_MinMaxScale;
        [SerializeField]
        private float2 m_MinMaxDistance;

        public float3 Offset => m_Offset;

        public float3 MinOffset => m_MinOffset;

        public float MinDistanceBetweenCameraRayAndPlayer  => m_MinDistanceBetweenCameraRayAndPlayer;

        public float RayOffsetFromCamera => m_RayOffsetFromCamera;

        public float MinLifeBar => m_MinLifeBar;

        public float MinScale => m_MinMaxScale.x;

        public float MaxScale => m_MinMaxScale.y;

        public float MinDistanceSq => m_MinMaxDistance.x * m_MinMaxDistance.x;

        public float MaxDistanceSq => m_MinMaxDistance.y * m_MinMaxDistance.y;
    }
}
