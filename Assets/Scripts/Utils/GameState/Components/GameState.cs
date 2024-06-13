namespace Unity.Entities.Simulation
{
    public enum GameState
    {
        Default = 0,
        MainMenu = 1,
        Loading = 2,
        InitialCinematic = 3,
        PreparingMatch = 4,
        Match = 5,
        EndMatch = 6,
        ResetMatch = 7,
    }

    public struct GameStateMainMenu : IComponentData, IEnableableComponent { }
    public struct GameStateLoading : IComponentData, IEnableableComponent { }
    public struct GameStateInitialCinematic : IComponentData, IEnableableComponent { }
    public struct GameStatePreparingMatch : IComponentData, IEnableableComponent { }
    public struct GameStateMatch : IComponentData, IEnableableComponent { }
    public struct GameStateEndMatch : IComponentData, IEnableableComponent { }
    public struct GameStateResetMatch : IComponentData, IEnableableComponent { }
}