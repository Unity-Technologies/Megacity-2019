using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.Pool
{
    public class GameObjectPool : MonoBehaviour
    {
        public static GameObjectPool Instance;
        [SerializeField]
        private GameObject[] Prefabs;
        public int MaxPoolSize = 64;

        private Dictionary<string, IObjectPool<GameObject>> m_PoolDictionary;

        public void ReleaseObject(string value, GameObject obj)
        {
            var pool = m_PoolDictionary[value];
            pool.Release(obj);
        }

        public GameObject PlaceObject(string prefabName, Vector3 position)
        {
            var pool = m_PoolDictionary[prefabName];
            var obj = pool.Get();
            obj.transform.position = position;
            return obj;
        }

        private void Awake()
        {
            Instance = this;

            m_PoolDictionary = new Dictionary<string, IObjectPool<GameObject>>();

            foreach (var prefab in Prefabs)
            {
                m_PoolDictionary.Add(
                    prefab.name,
                    new ObjectPool<GameObject>(
                        () => Instantiate(prefab),
                        OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, 10, MaxPoolSize));
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnReturnedToPool(GameObject obj)
        {
            obj.gameObject.SetActive(false);
        }

        private void OnTakeFromPool(GameObject obj)
        {
            obj.gameObject.SetActive(true);
        }

        private void OnDestroyPoolObject(GameObject obj)
        {
            Destroy(obj.gameObject);
        }
    }
}
