using System;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Megacity.CameraManagement
{
    /// <summary>
    /// Create camera target authoring component in order to
    /// allow the game object camera to follow the player camera target entity
    /// </summary>
    public class HybridCameraManager : MonoBehaviour
    {
        private enum CameraTargetMode
        {
            None,
            FollowPlayer,
            DollyTrack
        }
        
        [SerializeField]
        private float m_TargetFollowDamping = 5.0f;
        [SerializeField]
        private Transform m_PlayerCameraTarget;
        [SerializeField]
        private Transform m_DollyCameraTarget;
        [SerializeField] 
        private GameObject m_AutopilotCamera;
        private CameraTargetMode m_CameraTargetMode;
        public bool IsDollyCamera => m_CameraTargetMode == CameraTargetMode.DollyTrack;
        public bool IsFollowCamera => m_CameraTargetMode == CameraTargetMode.FollowPlayer;
        
        private PostProcessingBloomModifier m_Bloom;
        private DeadScreenFX m_DeadScreenFX;
        private CinemachineImpulseSource m_ImpulseSource;
        public bool IsCameraReady { private set; get; }

        public static HybridCameraManager Instance;
    
        private void Awake()
        {
            if (Instance != null)
            {
                IsCameraReady = false;
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            m_Bloom = FindObjectOfType<PostProcessingBloomModifier>();
            m_DeadScreenFX = FindObjectOfType<DeadScreenFX>();
            m_ImpulseSource = FindObjectOfType<CinemachineImpulseSource>();
        }

        public void SetDollyCamera()
        {
            m_CameraTargetMode = CameraTargetMode.DollyTrack;
            m_AutopilotCamera.gameObject.SetActive(true);
        }
        
        public void SetFollowCamera()
        {
            m_CameraTargetMode = CameraTargetMode.FollowPlayer;
            m_AutopilotCamera.gameObject.SetActive(false);
        }

        public void SetPlayerCameraPosition(float3 position, float deltaTime)
        {
            m_PlayerCameraTarget.position = math.lerp(m_PlayerCameraTarget.position, position, deltaTime * m_TargetFollowDamping);
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
        
        public void Reset()
        {
            IsCameraReady = false;
            m_AutopilotCamera.gameObject.SetActive(false);
            m_CameraTargetMode = CameraTargetMode.None;
        }

        public void PlaceCamera(float3 position)
        {
            m_PlayerCameraTarget.position = position;
            IsCameraReady = true;
        }
    }
}
