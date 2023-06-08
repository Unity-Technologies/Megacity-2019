using System;
using System.Threading.Tasks;
using Unity.MegaCity.Gameplay;
using Unity.MegaCity.Traffic;
using Unity.Services.Samples;
using Unity.Services.Samples.GameServerHosting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Matchmaking UI element
    /// </summary>
    [Serializable]
    public class MatchMakingConnector
    {
        private MultiplayerServerSettings m_ServerSettings;
        private DropdownField m_MultiplayerServerDropdownMenu;
        private Label m_ConnectionStatusLabel;
        private string m_DefaultName;
        private MatchMakingLoadingBar m_MatchMakingLoadingBar;
        public bool IsTryingToConnect { get; private set; } = false;
        [field: SerializeField] public string IP { get; private set; } = "127.0.0.1";
        [field: SerializeField] public ushort Port { get; private set; } = NetCodeBootstrap.MegaCityServerIp.Port;

        private TextField m_MultiplayerTextField;
        public bool ClientIsInGame { get; set; }
        private ClientMatchmaker m_Matchmaker;
        private PlayerAuthentication m_ProfileService;

        public string DefaultName => m_DefaultName;

        public MatchMakingConnector(MultiplayerServerSettings ServerSettings)
        {
            m_ServerSettings = ServerSettings;
            m_DefaultName = Environment.UserName.ToUpper();
        }

        public async Task Init()
        {
            IsTryingToConnect = false;
            ClientIsInGame = false;
#if UNITY_SERVER && !UNITY_EDITOR
            return;
#endif
            #region ServiceSignin

            m_ProfileService = new PlayerAuthentication();
            await m_ProfileService.SignIn(m_DefaultName);
            m_Matchmaker = new ClientMatchmaker();

            #endregion
        }

        public async Task Matchmake()
        {
            Debug.Log("Beginning Matchmaking.");
            m_ConnectionStatusLabel.text = "Beginning Matchmaking.";

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Debug.LogWarning($"To use Unity's dashboard services, " +
                                 "you need to link your Unity project to a project ID. " +
                                 "To do this, go to Project Settings to select your organization, " +
                                 "select your project and then link a project ID. " +
                                 "You also need to make sure your organization has access to the required products. " +
                                 "Visit https://dashboard.unity3d.com to sign up.");
                return;
            }

            try
            {
                var matchmakingResult = await m_Matchmaker.Matchmake(m_ProfileService.LocalPlayer);
                if (matchmakingResult.result == MatchmakerPollingResult.Success)
                {
                    IP = matchmakingResult.ip;
                    Port = (ushort) matchmakingResult.port;
                    m_ConnectionStatusLabel.text = "[Matchmaker] Matchmaking Success!";
                    Debug.Log($"[Matchmaker] Matchmaking Success! Connecting to {IP} : {Port}");
                    await Task.Delay(5000); // Give the server a second to process before connecting
                    ConnectToServer();
                }
                else
                {
                    m_ConnectionStatusLabel.text =
                        $"[Matchmaker] {matchmakingResult.result}] - {matchmakingResult.resultMessage}";
                    Debug.LogError($"[Matchmaker] {matchmakingResult.result}] - {matchmakingResult.resultMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Matchmaker] Error Matchmaking: {ex}");
                m_ConnectionStatusLabel.text = $"[Matchmaker] Error Matchmaking: {ex}";
            }
        }

        public void Dispose()
        {
            m_Matchmaker?.Dispose();
        }

        public void InitUI(VisualElement container)
        {
            m_ConnectionStatusLabel = container.Q<Label>("connection-label");
            m_MatchMakingLoadingBar = new MatchMakingLoadingBar(container);
            m_MultiplayerTextField = container.Q<TextField>("multiplayer-server-textfield");
            m_MultiplayerServerDropdownMenu = container.Q<DropdownField>("multiplayer-server-location");
            m_MultiplayerServerDropdownMenu.choices = m_ServerSettings.GetUIChoices();
            m_MultiplayerServerDropdownMenu.RegisterValueChangedCallback(OnServerDropDownChanged);

            m_MultiplayerTextField.RegisterValueChangedCallback(newStringEvent =>
            {
                UpdatePortAndIP(newStringEvent.newValue);
            });
        }

        private void OnServerDropDownChanged(ChangeEvent<string> value)
        {
            m_MultiplayerTextField.value = m_ServerSettings.GetIPByName(value.newValue);
            UpdatePortAndIP(m_MultiplayerTextField.value);
        }

        private void UpdatePortAndIP(string newStringEvent)
        {
            var ipSplit = newStringEvent.Split(":");
            if (ipSplit.Length < 2)
                return;

            IP = ipSplit[0];
            var portString = ipSplit[1];
            if (!ushort.TryParse(portString, out var portShort))
                return;
            Port = portShort;
        }

        public void SetUIConnectionStatusEnable(bool matchmaking)
        {
            m_MatchMakingLoadingBar.Enable(matchmaking);
            m_ConnectionStatusLabel.style.display = matchmaking ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetProfileServeName(string newValue)
        {
            m_ProfileService.LocalPlayer.SetName(newValue);
        }

        public void UpdateConnectionStatusLabel()
        {
            m_ConnectionStatusLabel.text = "Attempting to Connect...";
            Debug.Log($"Attempting to Connect to {IP}:{Port}.");
            IsTryingToConnect = true;
        }

        public void ConnectionSucceeded()
        {
            ClientIsInGame = true;
            m_ConnectionStatusLabel.text = "Connected to Server...";
            Debug.Log("Connected to Server...");
            IsTryingToConnect = false;
        }

        public void ConnectionFailed()
        {
            IsTryingToConnect = false;
            ClientIsInGame = false;
        }

        public void ConnectToServer()
        {
            if (ClientIsInGame)
                return;
            UpdateConnectionStatusLabel();
        }

        public void SetConnectionMode(bool isMatchMaking)
        {
            m_MultiplayerTextField.style.display = isMatchMaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerServerDropdownMenu.style.display = isMatchMaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerServerDropdownMenu.value = m_MultiplayerServerDropdownMenu.choices[0];
            UpdatePortAndIP(m_ServerSettings.GetIPByName(m_MultiplayerServerDropdownMenu.value));
        }
    }
}