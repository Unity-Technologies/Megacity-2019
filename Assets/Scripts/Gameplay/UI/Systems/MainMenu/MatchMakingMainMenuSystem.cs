﻿using Unity.Entities;
using Unity.Megacity.UGS;
using Unity.Megacity.UI;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// This system is responsible for connecting to the server from the Main Menu.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct MatchMakingMainMenuSystem : ISystem, ISystemStartStop
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
                MatchMakingConnector.Instance.ConnectToServer();
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            if (MainMenu.Instance == null)
                return;

            if (MatchMakingConnector.Instance.IsTryingToConnect)
            {
                var isConnected = IsConnectingOrConnected();
                Debug.Log($"Is trying to connect via Button? : '{MatchMakingConnector.Instance.IsTryingToConnect}' \nIs the connection currently active? : {isConnected}'");
                if (!isConnected)
                {
                    ref var netStream = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                    if (NetworkEndpoint.TryParse(MatchMakingConnector.Instance.IP, MatchMakingConnector.Instance.Port, out var networkEndpoint) && networkEndpoint.IsValid)
                    {
                        Debug.Log($"Connecting Manually ClientWorld '{state.World.Name}' to '{networkEndpoint}'...");
                        netStream.Connect(state.EntityManager, networkEndpoint);
                        isConnected = true;
                    }
                    else
                    {
                        Debug.LogError($"Play button press is not actionable, as invalid IP '{MatchMakingConnector.Instance.IP}' and/or Port '{MatchMakingConnector.Instance.Port}'!");
                    }
                }

                Debug.Log($"Has the connection been established successfully? : '{isConnected}'");
                if (isConnected)
                {
                    MainMenu.Instance.ConnectionSucceeded();
                }
                else
                {
                    MainMenu.Instance.ConnectionFailed();
                }
            }
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

            if (!NetworkEndpoint.TryParse(MatchMakingConnector.Instance.IP, MatchMakingConnector.Instance.Port, out var networkEndpoint))
            {
                Debug.LogError($"Unable to parse NetworkEndpoint from '{MatchMakingConnector.Instance.IP}:{MatchMakingConnector.Instance.Port}' Cannot connect!");
                return false;
            }

            state.EntityManager.CreateSingleton(new NetworkStreamRequestConnect { Endpoint = networkEndpoint });
            Debug.Log($"Connecting ClientWorld '{state.World.Name}' to '{networkEndpoint}'...");

            return true;
        }
    }
}