namespace Unity.Entities.Simulation
{
    [System.Serializable]
    public struct GameStateElement : IBufferElementData
    {
        public GameState State;
        public GameState TargetState;
        public bool UseTimer;
        public float Duration;
    }
}