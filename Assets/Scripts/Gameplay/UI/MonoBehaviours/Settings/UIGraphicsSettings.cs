using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Access to GameObjects in the Scene and graphics settings that allows change the quality of the game.
    /// Uses the Graphic Settings View to modify by Toggle and DropdownField that should be Modify in the UI,
    /// These controls modify:
    ///  - Postprocessing
    ///  - Quality Settings
    ///  - Texture Detail
    ///  - Shadow Quality
    ///  - Level of Detail
    ///  - Reflections
    ///  - Fog
    ///  - VSync
    ///  - ScreenMode
    /// </summary>
    public class UIGraphicsSettings : UISettingsTab
    {
        [SerializeField] private Volume m_PostProcessing;
        [SerializeField] private GameObject FogVolume;
        [SerializeField] private GameObject ReflectionVolume;
        [SerializeField] private UIScreenResolution ResolutionScreen;

        private Toggle m_PostprocesingValue;
        private Toggle m_VolumetricFogValue;
        private Toggle m_ReflectionsValue;
        private Toggle m_VerticalSyncValue;

        private DropdownField m_QualityValue;
        private DropdownField m_ScreenmodeValue;
        private DropdownField m_ScreenResolution;
        private DropdownField m_TextureDetailsValue;
        private DropdownField m_ShadowQualityValue;
        private DropdownField m_LevelOfDetailValue;

        public override string TabName => "graphics";
        private bool m_CanSetAsCustom = true;

        private void Start()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("advance");
            m_PostprocesingValue = root.Q<GroupBox>().Q<Toggle>("postprocessing");
            m_VolumetricFogValue = root.Q<GroupBox>().Q<Toggle>("volumetric-fog");
            m_ReflectionsValue = root.Q<GroupBox>().Q<Toggle>("reflections");
            m_VerticalSyncValue = root.Q<GroupBox>().Q<Toggle>("vertical-sync");

            m_QualityValue = root.Q<DropdownField>("quality-settings");
            m_ScreenResolution = root.Q<DropdownField>("screen-resolution");
            m_ScreenResolution.choices = ResolutionScreen.GetResolutionOptions();
            m_ScreenResolution.value = m_ScreenResolution.choices[ResolutionScreen.CurrentResolutionIndex];
            m_ScreenmodeValue = root.Q<DropdownField>("screen-mode");
            m_ScreenmodeValue.choices = ResolutionScreen.GetResolutionModes();
            m_TextureDetailsValue = root.Q<DropdownField>("texture-details");
            m_ShadowQualityValue = root.Q<DropdownField>("shadow-quality");
            m_LevelOfDetailValue = root.Q<DropdownField>("level-of-detail");

            m_ScreenResolution.RegisterValueChangedCallback(OnScreenResolutionChanged);
            m_PostprocesingValue.RegisterValueChangedCallback(OnPostprocessingChanged);
            m_VolumetricFogValue.RegisterValueChangedCallback(OnVolumetricFogChanged);
            m_ReflectionsValue.RegisterValueChangedCallback(OnReflectionsChanged);
            m_VerticalSyncValue.RegisterValueChangedCallback(OnVsyncChanged);

            m_QualityValue.RegisterValueChangedCallback(OnGraphicsQualityChanged);
            m_ScreenmodeValue.RegisterValueChangedCallback(OnScreenModeChanged);
            m_TextureDetailsValue.RegisterValueChangedCallback(OnTextureDetailsChanged);
            m_ShadowQualityValue.RegisterValueChangedCallback(OnShadowQualityChanged);
            m_LevelOfDetailValue.RegisterValueChangedCallback(LevelOfDetailChanged);

            switch (QualitySettings.GetQualityLevel())
            {
                case 0:
                    m_QualityValue.value = m_QualityValue.choices[0];
                    OnLowButtonOnClicked();
                    break;
                case 1:
                    m_QualityValue.value = m_QualityValue.choices[1];
                    OnMediumButtonOnClicked();
                    break;
                case 2:
                    m_QualityValue.value = m_QualityValue.choices[2];
                    OnHighButtonOnClicked();
                    break;
            }

            if (Screen.fullScreenMode == FullScreenMode.Windowed && !Screen.fullScreen)
            {
                m_ScreenmodeValue.value = FullScreenMode.Windowed.ToString();
            }

            SaveCurrentState();
        }

        private void OnScreenResolutionChanged(ChangeEvent<string> value)
        {
#if !(UNITY_ANDROID || UNITY_IPHONE)
            ResolutionScreen.SetResolution(value.newValue.ToLower(), out var isFullscreen);
            var screenMode = isFullscreen ? 1 : 0;
            m_ScreenmodeValue.value = m_ScreenmodeValue.choices[screenMode];
#endif
        }

        protected override void SaveCurrentState()
        {
            base.SaveCurrentState();

            UpdateCurrentToggleState(m_PostprocesingValue);
            UpdateCurrentToggleState(m_VolumetricFogValue);
            UpdateCurrentToggleState(m_ReflectionsValue);
            UpdateCurrentToggleState(m_VerticalSyncValue);

            UpdateCurrentDropdownFieldState(m_QualityValue);
            UpdateCurrentDropdownFieldState(m_ScreenmodeValue);
            UpdateCurrentDropdownFieldState(m_TextureDetailsValue);
            UpdateCurrentDropdownFieldState(m_ShadowQualityValue);
            UpdateCurrentDropdownFieldState(m_LevelOfDetailValue);
        }

        public override void Reset()
        {
            base.Reset();

            ResetCurrentToggleState(m_PostprocesingValue);
            ResetCurrentToggleState(m_VolumetricFogValue);
            ResetCurrentToggleState(m_ReflectionsValue);
            ResetCurrentToggleState(m_VerticalSyncValue);

            ResetCurrentDropdownFieldState(m_QualityValue);
            ResetCurrentDropdownFieldState(m_ScreenmodeValue);
            ResetCurrentDropdownFieldState(m_TextureDetailsValue);
            ResetCurrentDropdownFieldState(m_ShadowQualityValue);
            ResetCurrentDropdownFieldState(m_LevelOfDetailValue);
            
        }

        private void OnHighButtonOnClicked()
        {
            m_CanSetAsCustom = false;
            QualitySettings.SetQualityLevel(2);
            m_VerticalSyncValue.value = true;
            m_VolumetricFogValue.value = true;
            m_PostprocesingValue.value = true;
            m_ReflectionsValue.value = true;

            const string detail = "High";
            m_ShadowQualityValue.value = detail;
            m_TextureDetailsValue.value = detail;
            m_LevelOfDetailValue.value = detail;
        }

        private void OnMediumButtonOnClicked()
        {
            m_CanSetAsCustom = false;
            QualitySettings.SetQualityLevel(1);
            m_VerticalSyncValue.value = false;
            m_VolumetricFogValue.value = true;
            m_PostprocesingValue.value = true;
            m_ReflectionsValue.value = true;

            const string detail = "Medium";
            m_ShadowQualityValue.value = detail;
            m_TextureDetailsValue.value = detail;
            m_LevelOfDetailValue.value = detail;
        }

        private void OnLowButtonOnClicked()
        {
            m_CanSetAsCustom = false;
            QualitySettings.SetQualityLevel(0);
            m_VerticalSyncValue.value = false;
            m_VolumetricFogValue.value = false;
            m_PostprocesingValue.value = false;
            m_ReflectionsValue.value = false;

            const string detail = "Low";
            m_ShadowQualityValue.value = detail;
            m_TextureDetailsValue.value = detail;
            m_LevelOfDetailValue.value = detail;
        }

        private void OnDestroy()
        {
            if (IsInitialized)
            {
                m_PostprocesingValue.UnregisterValueChangedCallback(OnPostprocessingChanged);
                m_VolumetricFogValue.UnregisterValueChangedCallback(OnVolumetricFogChanged);
                m_ReflectionsValue.UnregisterValueChangedCallback(OnReflectionsChanged);
                m_VerticalSyncValue.UnregisterValueChangedCallback(OnVsyncChanged);

                m_ScreenmodeValue.UnregisterValueChangedCallback(OnScreenModeChanged);
                m_TextureDetailsValue.UnregisterValueChangedCallback(OnTextureDetailsChanged);
                m_ShadowQualityValue.UnregisterValueChangedCallback(OnShadowQualityChanged);
                m_LevelOfDetailValue.UnregisterValueChangedCallback(LevelOfDetailChanged);
            }
        }

        private void OnVsyncChanged(ChangeEvent<bool> value)
        {
            QualitySettings.vSyncCount = value.newValue ? 1 : 0;
            SetCustom();
        }

        private void OnVolumetricFogChanged(ChangeEvent<bool> value)
        {
            RenderSettings.fog = value.newValue;
            FogVolume.SetActive(value.newValue);
            SetCustom();
        }

        private void OnPostprocessingChanged(ChangeEvent<bool> value)
        {
            m_PostProcessing.enabled = value.newValue;
            SetCustom();
        }

        private void OnReflectionsChanged(ChangeEvent<bool> value)
        {
            ReflectionVolume.SetActive(value.newValue);
            QualitySettings.realtimeReflectionProbes = value.newValue;
            SetCustom();
        }

        private void OnShadowQualityChanged(ChangeEvent<string> value)
        {
            SetShadowQuality(value.newValue.ToLower());
            SetCustom();
        }

        private void OnTextureDetailsChanged(ChangeEvent<string> value)
        {
            SetTextureDetail(value.newValue.ToLower());
            SetCustom();
        }

        private void LevelOfDetailChanged(ChangeEvent<string> value)
        {
            SetLOD(value.newValue.ToLower());
            SetCustom();
        }

        private void OnGraphicsQualityChanged(ChangeEvent<string> value)
        {
            switch (value.newValue)
            {
                case "High":
                    OnHighButtonOnClicked();
                    break;
                case "Medium":
                    OnMediumButtonOnClicked();
                    break;
                case "Low":
                    OnLowButtonOnClicked();
                    break;
            }
        }

        private void OnScreenModeChanged(ChangeEvent<string> value)
        {
            SetFullscreen(value.newValue.ToLower());
            SetCustom();
        }

        private void SetShadowQuality(string value)
        {
            switch (value)
            {
                case "low":
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                case "medium":
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    break;
                case "high":
                    QualitySettings.shadows = ShadowQuality.All;
                    break;
            }
        }

        private void SetTextureDetail(string value)
        {
            switch (value)
            {
                case "low":
                    QualitySettings.globalTextureMipmapLimit = 2;
                    break;
                case "medium":
                    QualitySettings.globalTextureMipmapLimit = 1;
                    break;
                case "high":
                    QualitySettings.globalTextureMipmapLimit = 0;
                    break;
            }
        }

        private void SetLOD(string value)
        {
            switch (value)
            {
                case "low":
                    QualitySettings.maximumLODLevel = 2;
                    break;
                case "medium":
                    QualitySettings.maximumLODLevel = 1;
                    break;
                case "high":
                    QualitySettings.maximumLODLevel = 0;
                    break;
            }
        }

        private void SetFullscreen(string value)
        {
            ResolutionScreen.SetScreenMode(value);
        }

        private void SetCustom()
        {
            if (m_CanSetAsCustom)
                m_QualityValue.value = m_QualityValue.choices[3];
        }
    }
}