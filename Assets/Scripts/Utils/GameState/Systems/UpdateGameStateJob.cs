namespace Unity.Entities.Simulation
{
    public partial struct UpdateGameStateJob : IJobEntity
    {
        private void Execute(GameStateDataAspect gameStateAspect)
        {
            gameStateAspect.SetComplete();
        }
    }
}