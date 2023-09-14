using Unity.Collections;
using static Unity.Entities.SystemAPI;

namespace Unity.Entities.Simulation
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UpdateGameState : ISystem
    {
        enum Modification 
        {
            Add = 0, 
            Remove = 1
        }
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<GameStateElement>();
        }

        public void OnDestroy(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameStateData = GetSingleton<GameStateData>();
            var gameStateBuffer = GetSingletonBuffer<GameStateElement>();

            if (gameStateData.CurrentStateIndex >= gameStateBuffer.Length)
                return;

            var currentElement = gameStateBuffer[gameStateData.CurrentStateIndex];

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var gameState in Query<GameStateDataAspect>()) 
            {
                if (gameState.State != currentElement.State)
                {
                    TryRemoveOrAddByState(ref ecb, gameState, gameState.State, Modification.Remove);
                    gameState.UpdateState(currentElement.State);
                    TryRemoveOrAddByState(ref ecb, gameState, gameState.State, Modification.Add);
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void TryRemoveOrAddByState(ref EntityCommandBuffer ecb, GameStateDataAspect gameState, GameState validate, Modification modification)
        {
            switch (validate)
            {
                case GameState.Default: break;
                case GameState.MainMenu:
                    if(modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateMainMenu>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateMainMenu>(gameState.Self);
                    break;
                case GameState.Loading:
                    if (modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateLoading>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateLoading>(gameState.Self);
                    break;
                case GameState.InitialCinematic:
                    if(modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateInitialCinematic>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateInitialCinematic>(gameState.Self);
                    break;
                case GameState.PreparingMatch:
                    if (modification == Modification.Remove)
                        ecb.RemoveComponent<GameStatePreparingMatch>(gameState.Self);
                    else
                        ecb.AddComponent<GameStatePreparingMatch>(gameState.Self);
                    break;
                case GameState.Match:
                    if (modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateMatch>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateMatch>(gameState.Self);
                    break;
                case GameState.EndMatch:
                    if (modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateEndMatch>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateEndMatch>(gameState.Self);
                    break;
                case GameState.ResetMatch:
                    if (modification == Modification.Remove)
                        ecb.RemoveComponent<GameStateResetMatch>(gameState.Self);
                    else
                        ecb.AddComponent<GameStateResetMatch>(gameState.Self);
                    break;
            }
        }
    }
}