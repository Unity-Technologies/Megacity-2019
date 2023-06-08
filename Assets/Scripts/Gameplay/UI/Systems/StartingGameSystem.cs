#if !UNITY_SERVER
using Unity.Entities;
using Unity.MegaCity.Streaming;
using Unity.MegaCity.UI;
using UnityEngine;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// This system is responsible for showing the loading screen while the game is loading.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial struct StartingGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameLoadInfo>();
            state.Enabled = !Application.isEditor;
        }

        public void OnUpdate(ref SystemState state)
        {
            if(LoadingScreen.Instance == null)
                return;

            var gameLoadInfo = SystemAPI.GetSingleton<GameLoadInfo>();
            LoadingScreen.Instance.UpdateProgressBar(gameLoadInfo.GetProgress());

            if (gameLoadInfo.IsLoaded)
            {
                LoadingScreen.Instance.Hide();
                state.Enabled = false;
            }
            else
            {
                LoadingScreen.Instance.Show();
            }
        }
    }
}
#endif
