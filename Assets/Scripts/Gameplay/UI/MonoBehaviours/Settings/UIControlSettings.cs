using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// This class manages the Controls View in UI Game Settings view.
    /// </summary>
    public class UIControlSettings : UISettingsTab
    {
        public static UIControlSettings Instance { get; private set; }
        
        public override string TabName => "controls";

        private Slider m_MouseSensitivitySlider;
        private Toggle m_InverseLookHorizontalToggle;
        private Toggle m_InverseLookVerticalToggle;

        public float MouseSensitivity = 1f;
        public bool InverseLookHorizontal;
        public bool InverseLookVertical;
        public bool ShouldUpdate;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected override void Initialization()
        {
            var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("controls-sliders");
            m_MouseSensitivitySlider = root.Q<Slider>("mouse-sensitivity");
            m_InverseLookHorizontalToggle = root.Q<Toggle>("invert-look-horizontal");
            m_InverseLookVerticalToggle = root.Q<Toggle>("invert-look-vertical");

            m_MouseSensitivitySlider.RegisterValueChangedCallback(OnMouseSensitivityUpdated);
            m_InverseLookHorizontalToggle.RegisterValueChangedCallback(OnInverseLookHorizontalChanged);
            m_InverseLookVerticalToggle.RegisterValueChangedCallback(OnInverseLookVerticalChanged);
            base.Initialization();
        }

        private void OnInverseLookHorizontalChanged(ChangeEvent<bool> evt)
        {
            InverseLookHorizontal = evt.newValue;
        }

        private void OnInverseLookVerticalChanged(ChangeEvent<bool> evt)
        {
            InverseLookVertical = evt.newValue;
        }

        private void OnMouseSensitivityUpdated(ChangeEvent<float> evt)
        {
            MouseSensitivity = evt.newValue;
        }

        protected override void SaveCurrentState()
        {
            base.SaveCurrentState();
            UpdateSliderCurrentState(m_MouseSensitivitySlider);
            UpdateCurrentToggleState(m_InverseLookHorizontalToggle);
            UpdateCurrentToggleState(m_InverseLookVerticalToggle);
            
            // Tell system to update values
            ShouldUpdate = true;
        }

        private void OnDestroy()
        {
            if (IsSet)
            {
                m_MouseSensitivitySlider.UnregisterValueChangedCallback(OnMouseSensitivityUpdated);
                m_InverseLookHorizontalToggle.UnregisterValueChangedCallback(OnInverseLookHorizontalChanged);
                m_InverseLookVerticalToggle.UnregisterValueChangedCallback(OnInverseLookVerticalChanged);
            }
        }

        public override void Reset()
        {
            base.Reset();
            ResetSliderCurrentState(m_MouseSensitivitySlider);
            ResetCurrentToggleState(m_InverseLookHorizontalToggle);
            ResetCurrentToggleState(m_InverseLookVerticalToggle);
        }
    }
}