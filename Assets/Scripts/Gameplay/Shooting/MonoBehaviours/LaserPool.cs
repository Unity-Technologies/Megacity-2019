using UnityEngine;
using Unity.Mathematics;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Manages the laser pool
    /// </summary>
    public class LaserPool : MonoBehaviour
    {
        public static LaserPool Instance;

        [Header("Laser Renderer")] 
        [SerializeField] private GameObject Renderer;
        [Header("Particle Systems")] 
        [SerializeField] private GameObject LaserBeam;

        [SerializeField] private GameObject LaserHit;
        [SerializeField] private float m_Speed = 25f;
        [SerializeField] private float m_MaxDistanceFactorPerSpeed = 25f;
        [SerializeField] private int m_MaxPlayers = 256;

        private LineRenderer[] m_LaserPool;
        private ParticleSystem[] m_LaserBeamPool;
        private ParticleSystem[] m_LaserHitPool;
        private AudioSource[] m_LaserBeamAudioPool;

        private int m_ActiveLines;
        private int m_PrevActiveLines;

        // TODO: Instantiate initial pool size and grow the pool on demand
        //private int m_InitialPoolSize = 4;

        private Scene AdditiveScene;

        public void BeginUpdate()
        {
            m_PrevActiveLines = m_ActiveLines;
            m_ActiveLines = 0;
        }

        public void EndUpdate()
        {
            for (var i = m_PrevActiveLines; i < m_ActiveLines; ++i)
            {
                m_LaserPool[i].enabled = true;
                m_LaserBeamPool[i].Play(true);
                m_LaserHitPool[i].Play(true);
                if(!m_LaserBeamAudioPool[i].isPlaying)
                    m_LaserBeamAudioPool[i].Play();
            }

            for (var i = m_ActiveLines; i < m_PrevActiveLines; ++i)
            {
                m_LaserPool[i].enabled = false;
                m_LaserBeamPool[i].Stop(true);
                m_LaserHitPool[i].Stop(true);
                if(m_LaserBeamAudioPool[i].isPlaying)
                    m_LaserBeamAudioPool[i].Stop();
            }
        }


        public void AddLine(float3 start, float3 end, float currentSpeed)
        {
            var distance = math.distance(m_LaserPool[m_ActiveLines].GetPosition(0), start);
            var shouldPlace = currentSpeed <= 0 && distance > 1;
            // if the distance is long should place it quickly otherwise it should only smooth the position
            if (distance > (currentSpeed * m_MaxDistanceFactorPerSpeed) || shouldPlace)
                Place(start, end);

            // Set Line Renderer
            var speed = Time.deltaTime * m_Speed;
            var offset = (end - start) / 4;

            var pos0 = math.lerp(m_LaserPool[m_ActiveLines].GetPosition(0), start, speed);
            var pos1 = math.lerp(m_LaserPool[m_ActiveLines].GetPosition(1), start + offset, speed);
            var pos2 = math.lerp(m_LaserPool[m_ActiveLines].GetPosition(2), end - offset, speed);
            var pos3 = math.lerp(m_LaserPool[m_ActiveLines].GetPosition(3), end, speed);

            m_LaserPool[m_ActiveLines].SetPosition(0, pos0);
            m_LaserPool[m_ActiveLines].SetPosition(1, pos1);
            m_LaserPool[m_ActiveLines].SetPosition(2, pos2);
            m_LaserPool[m_ActiveLines].SetPosition(3, pos3);

            // Set LaserBeam position

            var newPos = math.lerp(m_LaserBeamPool[m_ActiveLines].transform.position, start, speed);
            var rotation = Quaternion.Lerp(m_LaserBeamPool[m_ActiveLines].transform.rotation, Quaternion.LookRotation(end - start), speed);
            m_LaserBeamPool[m_ActiveLines].transform.SetPositionAndRotation(newPos, rotation);
            
            var endPos = math.lerp(m_LaserHitPool[m_ActiveLines].transform.position, end, speed);
            m_LaserHitPool[m_ActiveLines].transform.position = endPos;
            m_ActiveLines++;
        }

        private void Place(float3 start, float3 end)
        {
            var offset = (end - start) / 4;
            m_LaserPool[m_ActiveLines].SetPosition(0, start);
            m_LaserPool[m_ActiveLines].SetPosition(1, start + offset);
            m_LaserPool[m_ActiveLines].SetPosition(2, end - offset);
            m_LaserPool[m_ActiveLines].SetPosition(3, end);

            m_LaserBeamPool[m_ActiveLines].transform
                .SetPositionAndRotation(start, Quaternion.LookRotation(end - start));
            m_LaserHitPool[m_ActiveLines].transform.position = end;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                AdditiveScene = SceneManager.GetSceneByName("AdditiveLaserPoolScene");
            else
#endif
                AdditiveScene = SceneManager.CreateScene("AdditiveLaserPoolScenePlaymode");

            // Initialize pools
            m_LaserPool = new LineRenderer[m_MaxPlayers];
            m_LaserBeamPool = new ParticleSystem[m_MaxPlayers];
            m_LaserHitPool = new ParticleSystem[m_MaxPlayers];
            m_LaserBeamAudioPool = new AudioSource[m_MaxPlayers];
            
            // Allocate initial pools
            for (var i = 0; i < m_MaxPlayers; ++i)
            {
                AllocateLaser(i);
            }
        }

        private void AllocateLaser(int index)
        {
            var laser = Instantiate(Renderer);
            m_LaserPool[index] = laser.GetComponent<LineRenderer>();
            m_LaserPool[index].enabled = false;
            laser.SetActive(true);

            SceneManager.MoveGameObjectToScene(laser, AdditiveScene);

            var laserBeam = Instantiate(LaserBeam);
            m_LaserBeamPool[index] = laserBeam.GetComponent<ParticleSystem>();
            m_LaserBeamPool[index].Pause(true);
            laserBeam.SetActive(true);

            var laserAudioSource = laserBeam.GetComponent<AudioSource>();
            m_LaserBeamAudioPool[index] = laserAudioSource;

            SceneManager.MoveGameObjectToScene(laserBeam, AdditiveScene);

            var laserHit = Instantiate(LaserHit);
            m_LaserHitPool[index] = laserHit.GetComponent<ParticleSystem>();
            m_LaserHitPool[index].Pause(true);
            laserHit.SetActive(true);

            SceneManager.MoveGameObjectToScene(laserHit, AdditiveScene);
        }


        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
