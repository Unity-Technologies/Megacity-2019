using Unity.Services.Vivox;
using UnityEngine;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Manages Vivox devices volume
    /// </summary>
    public class VivoxDevicesVolume : MonoBehaviour
    {
        public bool Muted
        {
            get
            {
                if (VivoxService.Instance != null && VivoxService.Instance.Client != null)
                    return VivoxService.Instance.Client.AudioInputDevices.Muted;
                return true;
            }
        }

        public void SetMicrophoneVolume(int volume)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.Client == null)
                return;

            VivoxService.Instance.Client.AudioInputDevices.VolumeAdjustment = volume;
        }

        public void SetSpeakerVolume(int volume)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.Client == null)
                return;

            VivoxService.Instance.Client.AudioOutputDevices.VolumeAdjustment = volume;
        }

        public void SetMicrophoneMute(bool value)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.Client == null)
                return;

            VivoxService.Instance.Client.AudioInputDevices.Muted = value;
        }

        public void SetMuteSpeaker(bool value)
        {
            if (VivoxService.Instance == null || VivoxService.Instance.Client == null)
                return;

            VivoxService.Instance.Client.AudioOutputDevices.Muted = value;
        }
    }
}