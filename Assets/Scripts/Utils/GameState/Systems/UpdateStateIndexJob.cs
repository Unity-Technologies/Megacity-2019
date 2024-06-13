namespace Unity.Entities.Simulation
{
    public partial struct UpdateStateIndexJob : IJobEntity
    {
        public DynamicBuffer<GameStateElement> Buffer;
        private void Execute(GameStateDataAspect gameState)
        {
            if (!gameState.IsCompleted)
                return;

            var currentGameStateData = Buffer[gameState.CurrentStateIndex];
            if (currentGameStateData.TargetState != GameState.Default)
            {
                for (int i = 0; i < Buffer.Length; i++)
                {
                    if (currentGameStateData.TargetState == Buffer[i].State)
                    {
                        gameState.SetNextStateBuffer(i);
                    }
                }
            }
            else
            {
                gameState.SetNextStateBuffer();
            }
        }
    }
}