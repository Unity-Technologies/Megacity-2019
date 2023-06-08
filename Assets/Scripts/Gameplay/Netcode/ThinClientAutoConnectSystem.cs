using Unity.Entities;
using Unity.MegaCity.Traffic;
using Unity.MegaCity.UI;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Auto-connects thin clients to the main client.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ThinClientAutoConnectSystem : ISystem
    {
        private EntityQuery m_NetworkStreamConnectionQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            m_NetworkStreamConnectionQuery = state.GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (ModeBootstrap.Options.IsThinClient)
                return; // Auto-connection is already handled by NetcodeBootstrap.

            // Only auto-connect thin clients once the main client is connecting.
            var clientIsInGame = MainMenu.Instance.MatchMakingConnector.ClientIsInGame;
            var weAreInGame = m_NetworkStreamConnectionQuery.CalculateChunkCountWithoutFiltering() != 0;

            // Trying to match states...
            if (weAreInGame == clientIsInGame) return;

            if (weAreInGame)
            {
                // Disconnect so that we match...
                var networkIdEntity = SystemAPI.GetSingletonEntity<NetworkId>();
                state.EntityManager.AddComponentData(networkIdEntity, new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ConnectionClose });
                Debug.Log($"[{state.WorldUnmanaged.Name}] Auto-disconnecting Thin Client as detected that the main client is not in game!");
            }
            else
            {
                // Connect so that we match...
                if (!NetworkEndpoint.TryParse(MainMenu.Instance.MatchMakingConnector.IP, NetCodeBootstrap.MegaCityServerIp.Port, out var networkEndpoint))
                {
                    UnityEngine.Debug.LogError($"[{state.WorldUnmanaged.Name}] Thin Client cannot connect as cannot parse endpoint!");
                    return;
                }

                ref var netStream = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                netStream.Connect(state.EntityManager, networkEndpoint);
                Debug.Log($"[{state.WorldUnmanaged.Name}] Auto-connecting Thin Client to '{networkEndpoint}'...");
            }
        }
    }
}
