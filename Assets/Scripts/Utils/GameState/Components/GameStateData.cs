namespace Unity.Entities.Simulation
{
    public struct GameStateData : IComponentData
    {
        public int CurrentStateIndex;
        public GameState State;
        public GameState PrevState;
        public bool IsCompleted;
    }
}