using Cinemachine;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Create camera target authoring component in order to
    /// allow the game object camera to follow the player camera target entity
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
        public enum CameraTargetMode
        {
            None,
            FollowPlayer,
            DollyTrack
        }
        public CameraTargetMode m_CameraTargetMode;

        [SerializeField]
        private float m_TargetFollowDamping = 5.0f;
        [SerializeField]
        private Transform m_PlayerCameraTarget;
        [SerializeField]
        private Transform m_DollyCameraTarget;
        [SerializeField]
        private CinemachineImpulseSource m_ImpulseSource;
        [SerializeField]
        private PostProcessingBloomModifier m_Bloom;
        [SerializeField]
        private PostProcessingVignetteModifier m_Vignette;
        [SerializeField]
        private DeadScreenFX m_DeadScreenFX;
        [SerializeField]
        private PostProcessingGlitch m_Glitch;

        public static HybridCameraManager Instance;

        private void Awake()
        {
            if (Instance != null)
                Destroy(Instance);
            else
                Instance = this;
        }

        public void SetPlayerCameraPosition(float3 position, float deltaTime)
        {
            m_PlayerCameraTarget.position =
                math.lerp(m_PlayerCameraTarget.position, position, deltaTime * m_TargetFollowDamping);
        }

        public void SetPlayerCameraRotation(quaternion rotation, float deltaTime)
        {
            m_PlayerCameraTarget.rotation =
                math.slerp(m_PlayerCameraTarget.rotation, rotation, deltaTime * m_TargetFollowDamping);
        }

        public float3 GetDollyCameraPosition()
        {
            return m_DollyCameraTarget.position;
        }

        public quaternion GetDollyCameraRotation()
        {
            return m_DollyCameraTarget.rotation;
        }

        public void StartShaking()
        {
            m_ImpulseSource.GenerateImpulse();
            m_Bloom.GenerateColorTransition();
        }

        public void StopRedOverlay()
        {
            m_Bloom.StopColorTransition();
        }

        public void StartDeadFX()
        {
            if(!m_DeadScreenFX.IsRunning)
                m_DeadScreenFX.GeneratingEffectTransition();
        }

        public void StopDeadFX()
        {
            if (m_DeadScreenFX.IsRunning)
                m_DeadScreenFX.StopGeneratingEffect();
        }

        public void IncreaseVignette(bool value)
        {
            m_Vignette.IncreaseVignette(value);
        }

        public void EnableGlitch(bool value)
        {
            m_Glitch.SetGlitchEnabled(value);
        }
    }
}
