using Gameplay;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using Unity.Megacity.UI;
using Unity.Services.Samples.GameServerHosting;
using UnityEngine.SceneManagement;
using Unity.Megacity.Gameplay;
using Unity.Megacity.CameraManagement;

namespace Unity.Megacity
{
    /// <summary>
    /// The bootstrap needs to extend `ClientServerBootstrap`, there can only be one class extending it in the project 
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class NetCodeBootstrap : ClientServerBootstrap
    {
        const string k_ServicesDataResourceLocation = "GameServiceData";
        const string k_MultiplayerDataResourceLocation = "MultiplayerServerSettings";
        public static NetworkEndpoint MegacityServerIp => NetworkEndpoint.Parse("128.14.159.58", 7979);

        /// <summary>
        /// Limitation imposed to ensure UTP send/receiveQueueSize's are set appropriately.
        /// <see cref="MegacityDriverConstructor"/>.
        /// </summary>
        public const int MaxPlayerCount = 200;

        // The initialize method is what entities calls to create the default worlds
        public override bool Initialize(string defaultWorldName)
        {
            // Handle max player count globally.
            NetworkStreamReceiveSystem.DriverConstructor = new MegacityDriverConstructor();

#if UNITY_SERVER && !UNITY_EDITOR
            var isFrontEnd = SceneController.IsFrontEnd; 
            if(isFrontEnd)
            {
                SceneController.LoadGame();
            }
            
            Application.targetFrameRate = 60; 
            CreateDefaultClientServerWorlds();
            TryStartUgs();

            // Niki.Walker: Disabled as UNITY_SERVER does not support creating thin client worlds.
            // // On the server, also create thin clients (if requested).
            // TryCreateThinClientsIfRequested();

            return true;
#else

            // Try and auto-connect.
#if UNITY_EDITOR
            if (IsConnectedFromTheMainScene(defaultWorldName)) 
            {
                return true;
            }
            
            if (RequestedPlayType == PlayType.Client)
            {
                if (MultiplayerPlayModePreferences.IsEditorInputtedAddressValidForConnect(out var editorSpecifiedEndpoint))
                {
                    AutoConnectPort = editorSpecifiedEndpoint.Port;
                    DefaultConnectAddress = editorSpecifiedEndpoint;
                    UnityEngine.Debug.Log($"Detected auto-connection preference in 'Multiplayer PlayMode Tool' targeting '{editorSpecifiedEndpoint}' (Port: '{AutoConnectPort}')!");
                }
            }
#else
                // We always set the DefaultConnectAddress in a player, because it's unlikely you'll want to test locally here.
                DefaultConnectAddress = ModeBootstrap.Options.UserSpecifiedEndpoint;

                if (TryCreateThinClientsIfRequested())
                    return true;
#endif
            ServerConnectionUtils.CreateDefaultWorld();
            return true;
#endif
        }

        private bool IsConnectedFromTheMainScene(string defaultWorldName)
        {
            var isMainScene = SceneController.IsGameScene; 
#if UNITY_EDITOR
            var gameInfo = Resources.Load<MultiplayerServerSettings>(k_MultiplayerDataResourceLocation);
            if (!gameInfo)
            {
                Debug.LogError($"[EditorSettings] No Game Info Object at 'Assets/Resources/{k_MultiplayerDataResourceLocation}'");
                return false;
            }

            gameInfo.IsAutoloading = isMainScene;

            if (isMainScene)
            {
                var isMultiplayerMode = gameInfo.AutoRunGameModeInEditorMain == GameMode.Multiplayer || gameInfo.AutoRunGameModeInEditorMain == GameMode.None;
                if (isMultiplayerMode)
                {
                    Debug.Log("Auto creating server and client worlds");
                    AutoConnectPort = 7979;
                    CreateDefaultClientServerWorlds();
                    return true;
                }
                else 
                {
                    Debug.Log("Auto creating Local world");
                    CreateLocalWorld(defaultWorldName);
                }
            }
#endif
            return isMainScene;
        }

        [UsedImplicitly]
        private static bool TryCreateThinClientsIfRequested()
        {
            if (ModeBootstrap.Options.IsThinClient)
            {
                var requestedNumThinClients = ModeBootstrap.Options.TargetThinClientWorldCount;
                if (requestedNumThinClients > 0)
                {
                    // Hardcoded DefaultConnectAddress for the Megacity demo.
                    AutoConnectPort = ModeBootstrap.Options.UserSpecifiedEndpoint.Port;

                    for (var i = 0; i < requestedNumThinClients; i++)
                    {
                        Debug.Log($"Creating a Thin Client World! {(i + 1)} of {requestedNumThinClients}...");
                        var world = CreateThinClientWorld();
                        if (i == 0 || World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
                        {
                            World.DefaultGameObjectInjectionWorld = world;
                            Debug.Log($"Setting DefaultGameObjectInjectionWorld to world '{world.Name}'.");
                        }
                    }

                    Debug.Log($"Detected headless client! Automatically creating {requestedNumThinClients} ThinClients, and connecting them to the hardcoded endpoint '{DefaultConnectAddress}' (Port: '{AutoConnectPort}')!");
                    return true;
                }

                Debug.LogError($"Detected headless client, but TargetThinClientWorldCount is {requestedNumThinClients}! Cannot initialize!");
            }

            return false;
        }

        /// <summary>
        /// Runs parallel to the initialization thread from here.
        /// </summary>
        private async void TryStartUgs()
        {
            var gameInfo = Resources.Load<GameServerInfo_Data>(k_ServicesDataResourceLocation);
            if (!gameInfo)
            {
                Debug.LogError($"[GSH] No Game Server Info Object at 'Assets/Resources/{k_ServicesDataResourceLocation}'");
                return;
            }

            var multiplayGameServerManager = new GameObject("MultiplayServer").AddComponent<GameServerManager>();
            multiplayGameServerManager.Init(gameInfo);
            Debug.LogError($"[GSH] Multiplay GameServer Manager {gameInfo.Data}'");
            
            if (!await multiplayGameServerManager.InitServices())
                return;
            Debug.LogError($"[GSH] Try Start Server {ModeBootstrap.Options.UserSpecifiedEndpoint.Address}");
            await multiplayGameServerManager.TryStartServer(ModeBootstrap.Options.UserSpecifiedEndpoint.Address);
        }
    }
}