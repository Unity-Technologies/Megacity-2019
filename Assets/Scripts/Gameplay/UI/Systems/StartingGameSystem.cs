#if !UNITY_SERVER
using Unity.Entities;
using Unity.Megacity.CameraManagement;
using Unity.Megacity.Streaming;
using Unity.Megacity.UI;
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// This system is responsible for showing the loading screen while the game is loading.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.Editor)]
    public partial struct StartingGameSystem : ISystem
    { 
        private EntityQuery m_MultiplayerQuery;
        private EntityQuery m_SinglePlayerQuery;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameLoadInfo>();
            m_MultiplayerQuery = state.GetEntityQuery(ComponentType.ReadOnly<GhostOwnerIsLocal>());
            m_SinglePlayerQuery = state.GetEntityQuery(ComponentType.ReadOnly<SinglePlayer>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (LoadingScreen.Instance == null)
                return;

            var gameLoadInfo = SystemAPI.GetSingleton<GameLoadInfo>();
            LoadingScreen.Instance.UpdateProgressBar(gameLoadInfo.GetProgress());

            var isSinglePlayerReady = m_SinglePlayerQuery.CalculateEntityCount() > 0;
            var isMultiplayerReady = m_MultiplayerQuery.CalculateEntityCount() > 0;
            var isCameraReady = !SceneController.IsGameScene || HybridCameraManager.Instance.IsCameraReady;
            var isPlayerReady = isSinglePlayerReady || isMultiplayerReady;
            
            if (gameLoadInfo.IsLoaded && LoadingScreen.Instance.IsVisible && isCameraReady && isPlayerReady)
            {
                LoadingScreen.Instance.Hide();
                state.Enabled = false;
            }
        }
    }
}
#endif