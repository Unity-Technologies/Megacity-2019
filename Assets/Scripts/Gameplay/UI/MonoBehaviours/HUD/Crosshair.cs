using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Crosshair UI element
    /// </summary>
    public class Crosshair : MonoBehaviour
    {
        [SerializeField]
        private float m_Speed = 10f;
        [SerializeField]
        private float m_MinDistance = 2f;
        [SerializeField]
        private float m_MaxDistance = 40f;
        [SerializeField]
        private float m_MinOffset = 28f;
        [SerializeField]
        private float m_MaxOffset = 36f;

        private VisualElement m_Crosshair;

        public bool IsVisible => m_Crosshair.style.display == DisplayStyle.Flex;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Crosshair = root.Q<VisualElement>("crosshair");
        }

        public void Show()
        {
            m_Crosshair.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_Crosshair.style.display = DisplayStyle.None;
        }

        public void SetOffset(float distance)
        {
            var currentDistance = math.min(m_MaxDistance, math.max(m_MinDistance, distance));
            var t = (currentDistance - m_MinDistance) / (m_MaxDistance - m_MinDistance);
            var value = math.lerp(m_MinOffset, m_MaxOffset, t);
            var newOffset = math.lerp(m_Crosshair.style.bottom.value.value, math.round(value), Time.deltaTime * m_Speed);
            m_Crosshair.style.bottom = Length.Percent(newOffset);
        }

        public void Highlight()
        {
            if (!m_Crosshair.ClassListContains("highlight"))
            {
                m_Crosshair.AddToClassList("highlight");
            }

            if (m_Crosshair.ClassListContains("highlight"))
            {
                m_Crosshair.EnableInClassList("highlight", true);
            }
        }

        public void Normal()
        {
            if (m_Crosshair.ClassListContains("highlight"))
            {
                m_Crosshair.EnableInClassList("highlight", false);
            }
        }
    }
}
