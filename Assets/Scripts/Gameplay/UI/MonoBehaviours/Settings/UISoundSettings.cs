using Unity.MegaCity.Audio;
using Unity.MegaCity.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Modifies the AudioMixer Groups via AudioMaster
    /// Reads the data to set the Sliders in the AudioView
    /// </summary>
    public class UISoundSettings : UISettingsTab
    {
        private Slider m_VolumeSlider;
        private Slider m_SoundFXSlider;
        private Slider m_VivoxVolumeSlider;
        private Slider m_VivoxMicrophoneSlider;

        public override string TabName => "audio";

        protected override void Initialization()
        {
            var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("sliders");
            m_VolumeSlider = root.Q<Slider>("volume");
            m_SoundFXSlider = root.Q<Slider>("sound-fx");
            m_VivoxVolumeSlider = root.Q<Slider>("vivox-volume");
            m_VivoxMicrophoneSlider = root.Q<Slider>("vivox-microphone-volume");

            m_VivoxVolumeSlider.RegisterValueChangedCallback(OnVivoxVolumeUpdated);
            m_VivoxMicrophoneSlider.RegisterValueChangedCallback(OnVivoxMicrophoneVolumeUpdated);
            m_SoundFXSlider.RegisterValueChangedCallback(OnSoundFXUpdated);
            m_VolumeSlider.RegisterValueChangedCallback(OnVolumeUpdated);
            base.Initialization();
        }

        protected override void SaveCurrentState()
        {
            base.SaveCurrentState();
            UpdateSliderCurrentState(m_VolumeSlider);
            UpdateSliderCurrentState(m_SoundFXSlider);
            UpdateSliderCurrentState(m_VivoxVolumeSlider);
            UpdateSliderCurrentState(m_VivoxMicrophoneSlider);
        }

        private void OnDestroy()
        {
            if (IsSet)
            {
                m_SoundFXSlider.UnregisterValueChangedCallback(OnSoundFXUpdated);
                m_VolumeSlider.UnregisterValueChangedCallback(OnVolumeUpdated);
            }
        }

        private void OnVolumeUpdated(ChangeEvent<float> value)
        {
            AudioMaster.Instance.volume.audioMixer.SetFloat("volume", Mathf.Log(value.newValue) * 20);
        }

        private void OnSoundFXUpdated(ChangeEvent<float> value)
        {
            AudioMaster.Instance.volume.audioMixer.SetFloat("sound-fx", Mathf.Log(value.newValue) * 20);
        }

        private void OnVivoxMicrophoneVolumeUpdated(ChangeEvent<float> value)
        {
            VivoxManager.Instance?.Devices.SetMicrophoneVolume((int)value.newValue);
        }

        private void OnVivoxVolumeUpdated(ChangeEvent<float> value)
        {
            VivoxManager.Instance?.Devices.SetSpeakerVolume((int)value.newValue);
        }


        public override void Reset()
        {
            base.Reset();
            ResetSliderCurrentState(m_VolumeSlider);
            ResetSliderCurrentState(m_SoundFXSlider);
        }
    }
}
