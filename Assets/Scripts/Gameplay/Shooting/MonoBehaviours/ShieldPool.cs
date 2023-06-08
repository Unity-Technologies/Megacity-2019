using System.Collections.Generic;
using Unity.Entities;
using Unity.Pool;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Manages the shield pool
    /// </summary>
    public class ShieldPool : MonoBehaviour
    {
        public static ShieldPool Instance;

        private Dictionary<Entity, ShieldVfx> m_ShiedlDictionary = new();

        [SerializeField] private ShieldVfx shieldPrefab;

        private IObjectPool<ShieldVfx> m_ObjectPool;

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

            m_ObjectPool = new ObjectPool<ShieldVfx>(CreateVfx,
                OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
                collectionCheck, defaultCapacity, maxSize);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                AdditiveScene = SceneManager.GetSceneByName("AdditiveShieldPoolScene");
            else
#endif
                AdditiveScene = SceneManager.CreateScene("AdditiveShieldScenePlaymode");
        }

        private ShieldVfx CreateVfx()
        {
            var shield = Instantiate(shieldPrefab);
            shield.ObjectPool = m_ObjectPool;
            SceneManager.MoveGameObjectToScene(shield.gameObject, AdditiveScene);
            return shield;
        }

        private void OnReleaseToPool(ShieldVfx shieldObject)
        {
            shieldObject.gameObject.SetActive(false);
        }

        private void OnGetFromPool(ShieldVfx shieldObject)
        {
            shieldObject.gameObject.SetActive(true);
        }

        private void OnDestroyPooledObject(ShieldVfx shieldObject)
        {
            Destroy(shieldObject.gameObject);
        }

        public void RemoveShieldVfx(Entity entity)
        {
            if (m_ShiedlDictionary.TryGetValue(entity, out var shieldVFX))
            {
                ClearVFX(entity, shieldVFX);
            }
        }

        private void ClearVFX(Entity entity, ShieldVfx shieldVFX)
        {
            shieldVFX.Deactivate();
            m_ShiedlDictionary.Remove(entity);
        }

        public void UpdateShieldVfx(Vector3 position, Quaternion rotation, Entity entity)
        {
            // Check if shieldVFX already exists for this car
            if (m_ShiedlDictionary.TryGetValue(entity, out var shieldVFX))
            {
                shieldVFX.UpdatePosition(position, rotation);
            }
            else
            {
                var vfxObject = m_ObjectPool.Get();
                vfxObject.transform.SetPositionAndRotation(position, rotation);
                m_ShiedlDictionary.Add(entity, vfxObject);
            }
        }

        public void ClearMissingEntities(EntityManager entityManager)
        {
            var nullEntities = new List<Entity>();
            foreach (KeyValuePair<Entity, ShieldVfx> item in m_ShiedlDictionary)
            {
                if (!entityManager.Exists(item.Key))
                {
                    item.Value.Deactivate();
                    nullEntities.Add(item.Key);
                }
            }

            foreach (var key in nullEntities)
            {
                m_ShiedlDictionary.Remove(key);
            }
        }
    }
}
