using Unity.Megacity.Audio;
using Unity.Megacity.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Modifies the AudioMixer Groups via AudioMaster
    /// Reads the data to set the Sliders in the AudioView
    /// </summary>
    public class UISoundSettings : UISettingsTab
    {
        private Slider m_VolumeSlider;
        private Slider m_SoundFXSlider;
        private Slider m_MusicSlider;
        private Slider m_VivoxVolumeSlider;
        private Slider m_VivoxMicrophoneSlider;

        private float m_MaxVolume;
        private float m_MaxSoundFX;
        private float m_MaxMusic;

        public override string TabName => "audio";

        protected override void Initialize()
        {
            base.Initialize();
            
            var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("sliders");
            m_VolumeSlider = root.Q<Slider>("volume");
            m_SoundFXSlider = root.Q<Slider>("sound-fx");
            m_MusicSlider = root.Q<Slider>("music");
            m_VivoxVolumeSlider = root.Q<Slider>("vivox-volume");
            m_VivoxMicrophoneSlider = root.Q<Slider>("vivox-microphone-volume");
            
            // Set the max values for the sliders
            AudioMaster.Instance.volume.audioMixer.GetFloat("volume", out m_MaxVolume);
            AudioMaster.Instance.soundFX.audioMixer.GetFloat("sound-fx", out m_MaxSoundFX);
            AudioMaster.Instance.music.audioMixer.GetFloat("music", out m_MaxMusic);

            m_VivoxVolumeSlider.RegisterValueChangedCallback(OnVivoxVolumeUpdated);
            m_VivoxMicrophoneSlider.RegisterValueChangedCallback(OnVivoxMicrophoneVolumeUpdated);
            m_SoundFXSlider.RegisterValueChangedCallback(OnSoundFXUpdated);
            m_VolumeSlider.RegisterValueChangedCallback(OnVolumeUpdated);
            m_MusicSlider.RegisterValueChangedCallback(OnMusicUpdated);
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
            if (IsInitialized)
            {
                m_SoundFXSlider.UnregisterValueChangedCallback(OnSoundFXUpdated);
                m_VolumeSlider.UnregisterValueChangedCallback(OnVolumeUpdated);
                m_MusicSlider.UnregisterValueChangedCallback(OnMusicUpdated);
            }
        }

        private void OnVolumeUpdated(ChangeEvent<float> value)
        {
            AudioMaster.Instance.volume.audioMixer.SetFloat("volume", Mathf.Log(value.newValue) * 20f + m_MaxVolume);
        }

        private void OnSoundFXUpdated(ChangeEvent<float> value)
        {
            AudioMaster.Instance.soundFX.audioMixer.SetFloat("sound-fx", Mathf.Log(value.newValue) * 20 + m_MaxSoundFX);
        }
        
        private void OnMusicUpdated(ChangeEvent<float> value)
        {
            AudioMaster.Instance.music.audioMixer.SetFloat("music", Mathf.Log(value.newValue) * 20 + m_MaxMusic);
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
