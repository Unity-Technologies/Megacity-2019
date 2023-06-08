using System.Collections;
using UnityEngine;
using Kino.PostProcessing;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Handles the Post Processing Glitch
    /// </summary>
    public class PostProcessingGlitch : MonoBehaviour
    {
        [FormerlySerializedAs("volume")] [SerializeField]
        private Volume m_Volume;

        private Glitch m_CurrentGlitch;

        [SerializeField] private float block = 0.2f;
        [SerializeField] private float drift = 0.1f;
        [SerializeField] private float jitter = 0.3f;
        [SerializeField] private float jump = 0.05f;
        [SerializeField] private float shake = 0.05f;

        private IEnumerator m_EnableGlitchCoroutine;

        private void Awake()
        {
            var profile = m_Volume.sharedProfile;
            if (!profile.TryGet(out m_CurrentGlitch))
                m_CurrentGlitch = profile.Add<Glitch>();
        }

        private void OnDisable()
        {
            SetGlitchEnabled(false);
        }

        public void SetGlitchEnabled(bool value)
        {
            if (value)
            {
                m_EnableGlitchCoroutine = EnableGlitch();
                StartCoroutine(m_EnableGlitchCoroutine);
            }
            else
            {
                if (m_EnableGlitchCoroutine != null)
                    StopCoroutine(m_EnableGlitchCoroutine);

                m_CurrentGlitch.block.value = 0;
                m_CurrentGlitch.drift.value = 0;
                m_CurrentGlitch.jitter.value = 0;
                m_CurrentGlitch.jump.value = 0;
                m_CurrentGlitch.shake.value = 0;
            }
        }

        private IEnumerator EnableGlitch()
        {
            var elapsedTime = 0f;
            var targetBlock = block;
            var targetDrift = drift;
            var targetJitter = jitter;
            var targetJump = jump;
            var targetShake = shake;
            while (elapsedTime < 1f)
            {
                m_CurrentGlitch.block.value = math.lerp(0, targetBlock, elapsedTime / 1f);
                m_CurrentGlitch.drift.value = math.lerp(0, targetDrift, elapsedTime / 1f);
                m_CurrentGlitch.jitter.value = math.lerp(0, targetJitter, elapsedTime / 1f);
                m_CurrentGlitch.jump.value = math.lerp(0, targetJump, elapsedTime / 1f);
                m_CurrentGlitch.shake.value = math.lerp(0, targetShake, elapsedTime / 1f);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}