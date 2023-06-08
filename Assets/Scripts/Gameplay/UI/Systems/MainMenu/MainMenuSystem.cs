using Unity.Entities;
using Unity.MegaCity.Audio;
using Unity.MegaCity.UI;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// This system is responsible for connecting to the server from the Main Menu.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct MainMenuSystem : ISystem, ISystemStartStop
    {
        private EntityQuery m_NetworkStreamConnectionQuery;

        public void OnCreate(ref SystemState state)
        {
            m_NetworkStreamConnectionQuery = state.GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (MainMenu.Instance == null)
                return;

            if (ModeBootstrap.Options.SkipMenu && TryConnect(ref state))
                MainMenu.Instance.MatchMakingConnector.ConnectToServer();
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (MainMenu.Instance == null)
                return;

            if (MainMenu.Instance.MatchMakingConnector.IsTryingToConnect)
            {
                var isConnected = IsConnectingOrConnected();
                Debug.Log($"Is trying to connect via Button? : '{MainMenu.Instance.MatchMakingConnector.IsTryingToConnect}' \nIs the connection currently active? : {isConnected}'");
                if (!isConnected)
                {
                    ref var netStream = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                    isConnected = TryConnectingViaNetStream(state, netStream);
                }

                Debug.Log($"Has the connection been established successfully? : '{isConnected}'");
                if (isConnected)
                {
                    Enter(ref state);
                }
                else
                {
                    MainMenu.Instance.ConnectionFailed();
                }
            }
        }

        private bool TryConnectingViaNetStream(SystemState state, NetworkStreamDriver netStream)
        {
            if (NetworkEndpoint.TryParse(MainMenu.Instance.MatchMakingConnector.IP, MainMenu.Instance.MatchMakingConnector.Port, out var networkEndpoint) && networkEndpoint.IsValid)
            {
                Debug.Log($"Connecting Manually ClientWorld '{state.World.Name}' to '{networkEndpoint}'...");
                netStream.Connect(state.EntityManager, networkEndpoint);
                return true;
            }
            else
            {
                Debug.LogError($"Play button press is not actionable, as invalid IP '{MainMenu.Instance.MatchMakingConnector.IP}' and/or Port '{MainMenu.Instance.MatchMakingConnector.Port}'!");
            }

            return false;
        }

        private void Enter(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<AudioSystemSettings>(out var audioManager))
                LoadAudioSettings(state.EntityManager);
            MainMenu.Instance.ConnectionSucceeded();
        }

        private bool IsConnectingOrConnected()
        {
            // NetcodeBootstrap may cause you to auto-connect
            return m_NetworkStreamConnectionQuery.CalculateChunkCountWithoutFiltering() > 0;
        }

        private bool TryConnect(ref SystemState state)
        {
            if (IsConnectingOrConnected())
                return true;

            if (!NetworkEndpoint.TryParse(MainMenu.Instance.MatchMakingConnector.IP, MainMenu.Instance.MatchMakingConnector.Port, out var networkEndpoint))
            {
                Debug.LogError($"Unable to parse NetworkEndpoint from '{MainMenu.Instance.MatchMakingConnector.IP}:{MainMenu.Instance.MatchMakingConnector.Port}' Cannot connect!");
                return false;
            }

            state.EntityManager.CreateSingleton(new NetworkStreamRequestConnect { Endpoint = networkEndpoint });
            Debug.Log($"Connecting ClientWorld '{state.World.Name}' to '{networkEndpoint}'...");

            return true;
        }

        private void LoadAudioSettings(EntityManager entityManager)
        {
            var audioSystemEntity = entityManager.CreateEntity(typeof(AudioSystemSettings));
            var audioMaster = MainMenu.Instance.AudioMaster;
            var systemSettings = new AudioSystemSettings
            {
                DebugMode = audioMaster.showDebugLines,
                MaxDistance = audioMaster.maxDistance,
                ClosestEmitterPerClipCount = audioMaster.closestEmitterPerClipCount,
            };
            entityManager.SetComponentData(audioSystemEntity, systemSettings);
        }
    }
}