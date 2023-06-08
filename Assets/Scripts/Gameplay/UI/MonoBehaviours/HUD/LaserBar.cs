using MyUILibrary;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// LaserBar UI element
    /// </summary>
    public class LaserBar : MonoBehaviour
    {
        private RadialProgress m_LaserBar;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var parent = root.Q<VisualElement>("crosshair");
            var container = parent.Q<VisualElement>("laser-bar");
            m_LaserBar = container.Q<RadialProgress>("radial-progress");
            m_LaserBar.HideLabel();
        }
        public void UpdateBar(float energy)
        {
            m_LaserBar.progress = energy;
        }
    }
}
