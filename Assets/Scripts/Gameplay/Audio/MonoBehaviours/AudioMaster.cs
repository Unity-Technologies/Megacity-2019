using UnityEngine;
using UnityEngine.Audio;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Saves the constant parameters for Audio Traffic Pool,
    /// This Includes access to the AudioMixer groups.
    /// </summary>
    public class AudioMaster : MonoBehaviour
    {
        public bool showDebugLines = false;
        public float maxDistance = 90f;
        public int closestEmitterPerClipCount = 3;
        public AudioMixerGroup soundFX;
        public AudioMixerGroup volume;
        public AudioMixerGroup music;

        public static AudioMaster Instance
        {
            private set;
            get;
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }
    }
}
