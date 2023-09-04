namespace Unity.Entities.Simulation
{
    public readonly partial struct GameStateDataAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRW<GameStateData> m_GameState;

        public readonly GameState State => m_GameState.ValueRO.State;
        public readonly GameState PreviousState => m_GameState.ValueRO.State;

        public readonly int CurrentStateIndex => m_GameState.ValueRO.CurrentStateIndex;

        public readonly bool IsCompleted => m_GameState.ValueRO.IsCompleted;

        public void UpdateState(GameState targetState)
        {
            m_GameState.ValueRW.PrevState = State;
            m_GameState.ValueRW.State = targetState;
            m_GameState.ValueRW.IsCompleted = false;
        }

        public void SetComplete() 
        {
            m_GameState.ValueRW.IsCompleted = true;
        }

        public void SetNextStateBuffer()
        {
            SetNextStateBuffer(CurrentStateIndex + 1);
        }
        public void SetNextStateBuffer(int index)
        {
            m_GameState.ValueRW.CurrentStateIndex = index;
        }
    }
}