using UnityEngine;
using UnityEngine.Pool;

namespace Unity.Pool
{
    public class ShieldVfx : MonoBehaviour
    {
        private IObjectPool<ShieldVfx> m_objectPool;
        private Transform m_Transform;
        private void OnEnable()
        {
            m_Transform = transform;
        }

        public IObjectPool<ShieldVfx> ObjectPool
        {
            set => m_objectPool = value;
        }

        public void Deactivate()
        {
            m_objectPool.Release(this);
        }

        public void UpdatePosition(Vector3 position, Quaternion rotation)
        {
            m_Transform.SetPositionAndRotation(position, rotation);
        }
    }
}
