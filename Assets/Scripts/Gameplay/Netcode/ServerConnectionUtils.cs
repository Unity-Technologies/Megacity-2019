using System.Collections.Generic;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using Unity.Entities;

namespace Unity.Megacity
{
    public class ServerConnectionUtils
    {
        private static bool IsTryingToConnect = false;

        public static void RequestConnection(string ip, ushort port)
        {
            if (IsTryingToConnect)
                return;

            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer)
            {
                Debug.Log($"Requesting connection to [Client and Server] {ip} {port}");
                StartClientServer(port);
            }
            else if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Client)
            {
                Debug.Log($"Requesting connection to [Server] {ip} {port}");
                ConnectToServer(ip, port);
            }
        }

        public static void CreateDefaultWorld()
        {
            DestroyActiveSimulationWorld();
            ClientServerBootstrap.AutoConnectPort = 0;
            var local = ClientServerBootstrap.CreateLocalWorld("Default World");
            World.DefaultGameObjectInjectionWorld ??= local;
        }
        
        /// <summary>
        ///     Start a Client and Server in your local IP
        /// </summary>
        private static void StartClientServer(ushort port)
        {
            IsTryingToConnect = true;
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
            {
                Debug.LogError(
                    $"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
                return;
            }

            //Destroy the local simulation world to avoid the game scene to be loaded into it
            //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
            //and other issues.
            DestroyActiveSimulationWorld();

            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            World.DefaultGameObjectInjectionWorld ??= server;
            
            SceneController.LoadGame();

            var networkEndpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
            {
                using var drvQuery =
                    server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(networkEndpoint);
            }

            networkEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(port);
            {
                using var drvQuery =
                    client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, networkEndpoint);
            }
            IsTryingToConnect = false;
        }

        /// <summary>
        ///     Connect to the server in the IP address and port
        /// </summary>
        /// <param name="ip">Server IP Address</param>
        /// <param name="port">Port</param>
        private static void ConnectToServer(string ip, ushort port)
        {
            IsTryingToConnect = true;
            DestroyActiveSimulationWorld();
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            
            SceneController.LoadGame();

            World.DefaultGameObjectInjectionWorld ??= client;

            var networkEndpoint = NetworkEndpoint.Parse(ip, port);
            {
                using var drvQuery =
                    client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, networkEndpoint);
            }
            IsTryingToConnect = false;
        }

        /// <summary>
        /// Destroying a world should happen in a GameObjectLoop instead of ECS systems loop.
        /// </summary>
        private static void DestroyActiveSimulationWorld()
        {
            var worlds = new List<World>();
            foreach (var world in World.All)
            {
                if ((world.Flags & WorldFlags.Game) != WorldFlags.None)
                {
                    worlds.Add(world);
                }
            }

            foreach (var world in worlds)
            {
                if(world.IsCreated)
                    world.Dispose();
            }
        }
    }
}