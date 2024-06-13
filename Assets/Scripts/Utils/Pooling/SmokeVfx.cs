using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

namespace Unity.Pool
{
    public class SmokeVfx : MonoBehaviour
    {
        private IObjectPool<SmokeVfx> m_objectPool;
        private VisualEffect m_VisualEffect;
        private const int MaxSpawnRate = 50;

        private void OnEnable()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
        }

        public IObjectPool<SmokeVfx> ObjectPool
        {
            set => m_objectPool = value;
        }

        public void Deactivate()
        {
            m_objectPool.Release(this);
        }

        public void UpdateSmokeRate(int health)
        {
            if (m_VisualEffect == null || !m_VisualEffect.HasInt("SpawnRate"))
                return;
            
            m_VisualEffect.SetInt("SpawnRate", MaxSpawnRate - health);
        }
    }
}
