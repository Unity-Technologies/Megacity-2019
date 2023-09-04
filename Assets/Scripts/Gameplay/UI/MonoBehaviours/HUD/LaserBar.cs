using MyUILibrary;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// LaserBar UI element
    /// </summary>
    public class LaserBar : MonoBehaviour
    {
        private RadialProgress m_LaserBar;
        private VisualElement m_Container;
        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var parent = root.Q<VisualElement>("crosshair");
            m_Container = parent.Q<VisualElement>("laser-bar");
            m_LaserBar = m_Container.Q<RadialProgress>("radial-progress");
            m_LaserBar.HideLabel();
        }

        public void Hide()
        {
            m_Container.style.display = DisplayStyle.None;
        }
        
        public void Show()
        {
            m_Container.style.display = DisplayStyle.Flex;
        }

        public void UpdateBar(float energy)
        {
            m_LaserBar.progress = energy;
        }
    }
}
