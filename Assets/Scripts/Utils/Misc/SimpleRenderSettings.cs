using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.MegaCity.Utils
{
    /// <summary>
    /// Alter the HD Renderpipeline Settings on the first run based on the quality settings.
    /// </summary>
    public class SimpleRenderSettings : MonoBehaviour
    {
        private struct RenderSettings
        {
            public bool volumetrics;
            public bool ssr;
            public bool contactShadows;
        }

        private Dictionary<string, RenderSettings> m_RenderSettings;
        private int m_LastQualityLevel = -1;

        private void CreateRenderSettings()
        {
            m_RenderSettings = new Dictionary<string, RenderSettings>();
            m_RenderSettings["Low"] = new RenderSettings { volumetrics = false, ssr = false, contactShadows = false };
            // Don't override settings that we don't want to change to allow easier editing in the editor.
            //m_RenderSettings["Ultra"] = new RenderSettings {volumetrics = true, ssr = true, contactShadows = true};
            //m_RenderSettings["Insane"] = new RenderSettings {volumetrics = true, ssr = true, contactShadows = true};
        }

        private void ApplyRenderSettings()
        {
            // Consoles use their own HD Render Pipeline Settings to allow for build time changes.
            int qualityLevel = QualitySettings.GetQualityLevel();
#if UNITY_STANDALONE || UNITY_EDITOR
            string qualityName = QualitySettings.names[qualityLevel];

            var hdCam = GetComponent<HDAdditionalCameraData>();
            if (hdCam)
            {
                RenderSettings currentSettings;
                if (m_RenderSettings.TryGetValue(qualityName, out currentSettings))
                {
                    // Add custom rendering settings to this camera.
                    hdCam.customRenderingSettings =
                        true; // Apply our changes above otherwise we use the default settings.

                    // Set the mask of the features we want to override.
                    hdCam.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.SSR] = true;
                    hdCam.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.ContactShadows] =
                        true;
                    hdCam.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Volumetrics] =
                        true;
                    hdCam.renderingPathCustomFrameSettingsOverrideMask.mask[
                        (uint)FrameSettingsField.ReprojectionForVolumetrics] = true;

                    // Set the values we want to override.
                    hdCam.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.SSR, currentSettings.ssr);
                    hdCam.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ContactShadows,
                        currentSettings.contactShadows);
                    hdCam.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Volumetrics,
                        currentSettings.volumetrics);
                    hdCam.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ReprojectionForVolumetrics,
                        currentSettings.volumetrics);
                }
                else
                {
                    hdCam.customRenderingSettings = false; // Unknown quality setting so no overrides for this camera.
                }
            }

#endif
            m_LastQualityLevel = qualityLevel;
        }

        // Start is called before the first frame update
        void Start()
        {
            CreateRenderSettings();
            ApplyRenderSettings();
        }

        // Update is called once per frame
        void Update()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            if (qualityLevel != m_LastQualityLevel)
            {
                ApplyRenderSettings();
            }
        }
    }
}
