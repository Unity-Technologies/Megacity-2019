using UnityEngine;

namespace Unity.Entities.Simulation.Authoring
{
    [System.Serializable]
    public struct StateNode
    {
        public GameState State;
        public GameStateElement [] Extensions;
    }

    public class GameStateAuthoring : MonoBehaviour
    {
        public GameState InitialState;
        public StateNode [] GameStateNodes; 
    }

    [BakingVersion("julian", 1)]
    public class GameStateBaking : Baker<GameStateAuthoring>
    {
        public override void Bake(GameStateAuthoring authoring)
        {
            if (authoring.GameStateNodes is null)
                return;

            if (authoring.GameStateNodes.Length <= 0)
                return;

            var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
            var gameStateBuffer = AddBuffer<GameStateElement>(entity);
            var currentIndexState = 0;

            for (int i = 0; i < authoring.GameStateNodes.Length; i++)
            {
                var nodeData = authoring.GameStateNodes[i];
                var gameStateData = new GameStateElement { State = nodeData.State };

                if(nodeData.State == authoring.InitialState)
                    currentIndexState = i;

                foreach (var extension in nodeData.Extensions)
                {
                    gameStateData.UseTimer = extension.UseTimer;
                    gameStateData.Duration = extension.Duration;
                    gameStateData.TargetState = extension.TargetState;
                }
                gameStateBuffer.Add(gameStateData);
            }

            var gameState = new GameStateData { CurrentStateIndex = currentIndexState, PrevState = GameState.Default };
            AddComponent(entity, gameState);
        }
    }
}

