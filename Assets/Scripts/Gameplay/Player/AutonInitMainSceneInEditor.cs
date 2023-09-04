#if UNITY_EDITOR
using Unity.Entities;
using Unity.Megacity.CameraManagement;
using UnityEngine;

namespace Unity.Megacity.Gameplay
{
    public partial struct AutonInitMainSceneInEditor : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawner>();
            state.RequireForUpdate<SpawnPointElement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameInfo = Resources.Load<MultiplayerServerSettings>("MultiplayerServerSettings");
            if (!gameInfo)
            {
                state.Enabled = false;
                Debug.LogError($"[EditorSettings] No Game Info Object at 'Assets/Resources/MultiplayerServerSettings'");
                return;
            }

            if (gameInfo.IsAutoloading && HybridCameraManager.Instance != null && PlayerInfoController.Instance != null)
            {
                gameInfo.IsAutoloading = false;
                PlayerInfoController.Instance.SetMode(gameInfo.AutoRunGameModeInEditorMain);
                HybridCameraManager.Instance.SetFollowCamera();
                var isMultiplayerMode = gameInfo.AutoRunGameModeInEditorMain == GameMode.Multiplayer;
                if (!isMultiplayerMode && gameInfo.AutoRunSinglePlayerGameModeInEditorMain == SinglePlayerGameMode.GuidedFlight)
                    HybridCameraManager.Instance.SetDollyCamera();

            }
            state.Enabled = false;
        }
    }
}
#endif