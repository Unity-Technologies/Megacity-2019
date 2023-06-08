#if !UNITY_EDITOR && (UNITY_PS4)
#define PLATFORM_EXCLUDES_QUIT_MENU
#endif
#if !UNITY_EDITOR
using UnityEngine;
#endif
using System.Collections.Generic;
using Unity.Entities;
using Unity.MegaCity.UI;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// System that handles quitting the game
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct QuitSystem : ISystem
    {
        public static bool WantsToQuit;
        private static bool ShouldDisconnectPlayers;

        public void OnUpdate(ref SystemState state)
        {
            if (ShouldDisconnectPlayers)
            {
                ShouldDisconnectPlayers = false;
                if (SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(out var singletonEntity))
                {
                    MainMenu.Instance.MatchMakingConnector.ClientIsInGame = false;
                    UnityEngine.Debug.Log($"[{state.WorldUnmanaged.Name}] User has requested to disconnect from the server.");
                    var requestDisconnect = new NetworkStreamRequestDisconnect { Reason = NetworkStreamDisconnectReason.ConnectionClose };
                    state.EntityManager.AddComponentData(singletonEntity, requestDisconnect);
                }
            }

#if !PLATFORM_EXCLUDES_QUIT_MENU
            if (WantsToQuit)
            {
                WantsToQuit = false;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_SERVER
                Application.Quit();
#endif
            }
#endif
        }

        public static void DisconnectAllPlayers()
        {
            if(!ShouldDisconnectPlayers)
                ShouldDisconnectPlayers = true;
        }

        public static bool IsPlayerConnected()
        {
            return GetAllClientConnected().Count > 0;
        }

        private static List<World> GetAllClientConnected()
        {
            var clientWorlds = new List<World>();
            foreach (var world in World.All)
            {
                if (world.IsClient() || world.IsThinClient())
                    clientWorlds.Add(world);
            }
            return clientWorlds;
        }
    }
}
