using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.Audio
{
    /// <summary>
    /// Defines Sound definitions assets and the possible audio clips to play.
    /// Used in the Audio systems
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public SoundEmitterDefinitionAsset[] m_SoundDefinitions;
        public AudioClip[] m_Clips;
        public float [] m_Probabilities;
        public float2 [] m_Volumes;
    }
}
