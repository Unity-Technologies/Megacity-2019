using UnityEngine;

namespace Unity.Megacity.Audio
{
    /// <summary>
    /// References audio clips and sounds that are applied to the emitters by the sound Pool system
    /// </summary>
    public class TrafficAudioSettings : MonoBehaviour
    {
        public AudioClip[] audioClips;
        public AudioClip[] vehicleLowIntensities;
        public AudioClip[] vehicleHighIntensities;
    }
}
