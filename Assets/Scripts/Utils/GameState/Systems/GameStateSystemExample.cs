using UnityEngine;

namespace Unity.Entities.Simulation
{
#if false
    public partial struct ROGameStateTest : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateMainMenu>();
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    public partial struct RWGameStateTest : ISystem, ISystemStartStop
    {
        private Entity m_DependencyRef;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateMainMenu>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnStartRunning(ref SystemState state)
        {
            GameStateAccess.AddDependency(state.EntityManager, out m_DependencyRef);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (Input.GetKeyDown("k")) 
            {
                GameStateAccess.CompleteDependency(state.EntityManager, ref m_DependencyRef);
            }
        }
    }


    public partial struct RWGameStateWithTargetTest : ISystem, ISystemStartStop
    {
        private Entity m_DependencyRef;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateMatch>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnStartRunning(ref SystemState state)
        {
            GameStateAccess.AddDependency(state.EntityManager, out m_DependencyRef);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (Input.GetKeyDown("k"))
            {
                GameStateAccess.CompleteDependency(state.EntityManager, ref m_DependencyRef);
            }
        }
    }
#endif
}