using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Unity.Pool
{
    public class SmokeVfxPool : MonoBehaviour
    {
        public static SmokeVfxPool Instance;

        private Dictionary<Entity, SmokeVfx> m_SmokeDictionary = new();

        [SerializeField] private SmokeVfx smokePrefab;

        private IObjectPool<SmokeVfx> m_ObjectPool;

        [SerializeField] private bool collectionCheck = true;

        // extra options to control the pool capacity and maximum size
        [SerializeField] private int defaultCapacity = 20;
        [SerializeField] private int maxSize = 100;
        
        private Scene AdditiveScene;
        private void Awake()
        {
            if (Instance != null)
                Destroy(Instance);
            else
                Instance = this;

            m_ObjectPool = new ObjectPool<SmokeVfx>(CreateVfx,
                OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
                collectionCheck, defaultCapacity, maxSize);
        }
        
        private void Start()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                AdditiveScene = SceneManager.GetSceneByName("AdditiveSmokePoolScene");
            else
#endif
                AdditiveScene = SceneManager.CreateScene("AdditiveSmokeScenePlaymode");
        }

        private SmokeVfx CreateVfx()
        {
            var projectileInstance = Instantiate(smokePrefab);
            projectileInstance.ObjectPool = m_ObjectPool;
            return projectileInstance;
        }

        private void OnReleaseToPool(SmokeVfx smokeObject)
        {
            smokeObject.gameObject.SetActive(false);
        }

        private void OnGetFromPool(SmokeVfx smokeObject)
        {
            smokeObject.gameObject.SetActive(true);
        }

        private void OnDestroyPooledObject(SmokeVfx smokeObject)
        {
            Destroy(smokeObject.gameObject);
        }

        public void RemoveSmokeVfx(Entity entity)
        {
            if (m_SmokeDictionary.TryGetValue(entity, out var smokeVfx))
            {
                smokeVfx.Deactivate();
                m_SmokeDictionary.Remove(entity);
            }
        }

        public void UpdateSmokeVfx(Vector3 position, Quaternion rotation, Entity entity, float health)
        {
            // Check if SmokeVfx already exists for this car
            if (m_SmokeDictionary.TryGetValue(entity, out var smokeVfx))
            {
                smokeVfx.UpdateSmokeRate((int)health);
                smokeVfx.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                var vfxObject = m_ObjectPool.Get();
                vfxObject.transform.SetPositionAndRotation(position, rotation);
                m_SmokeDictionary.Add(entity, vfxObject);
            }
        }

        public void ClearMissingEntities(EntityManager entityManager)
        {
            var nullEntities = new List<Entity>();
            foreach (KeyValuePair<Entity, SmokeVfx> item in m_SmokeDictionary)
            {
                if (!entityManager.Exists(item.Key))
                {
                    item.Value.Deactivate();
                    nullEntities.Add(item.Key);
                }
            }

            foreach (var key in nullEntities)
            {
                m_SmokeDictionary.Remove(key);
            }
        }
    }
}
