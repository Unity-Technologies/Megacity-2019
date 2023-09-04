using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Contains the shared and global properties and Methods for the UI tabs Views in GameSettings.
    /// Manages how to show and hide the states should be controlled.
    /// </summary>
    public abstract class UISettingsTab : MonoBehaviour
    {
        protected bool IsSet;
        private string ViewName => "game-settings";
        public VisualElement GameSettingsView { get; set; }
        protected bool IsVisible { get; private set; }

        public abstract string TabName { get; }
        protected Dictionary<Slider, float> m_CurrentSliderData = new();
        protected Dictionary<Toggle, bool> m_CurrentToggleData = new();
        protected Dictionary<DropdownField, string> m_CurrentDropdownFieldData = new();

        public void Show()
        {
            if (!IsSet)
            {
                var root = GetComponent<UIDocument>().rootVisualElement;
                if (root.Q<VisualElement>(ViewName).style.display == DisplayStyle.Flex)
                    Initialization();
            }

            IsVisible = true;
            SaveCurrentState();
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public void Apply()
        {
            SaveCurrentState();
        }

        protected virtual void SaveCurrentState()
        {
        }

        protected virtual void Initialization()
        {
            IsSet = true;
        }

        public virtual void Reset()
        {
        }

        protected void UpdateSliderCurrentState(Slider slider)
        {
            m_CurrentSliderData[slider] = slider.value;
        }

        protected void ResetSliderCurrentState(Slider slider)
        {
            if (m_CurrentSliderData.TryGetValue(slider, out var value))
            {
                slider.value = value;
            }
        }

        protected void UpdateCurrentToggleState(Toggle toggle)
        {
            m_CurrentToggleData[toggle] = toggle.value;
        }

        protected void ResetCurrentToggleState(Toggle toggle)
        {
            if (m_CurrentToggleData.TryGetValue(toggle, out var value))
            {
                toggle.value = value;
            }
        }

        protected void UpdateCurrentDropdownFieldState(DropdownField dropdownField)
        {
            m_CurrentDropdownFieldData[dropdownField] = dropdownField.value;
        }

        protected void ResetCurrentDropdownFieldState(DropdownField dropdownField)
        {
            if (m_CurrentDropdownFieldData.TryGetValue(dropdownField, out var value))
            {
                dropdownField.value = value;
            }
        }
    }
}
