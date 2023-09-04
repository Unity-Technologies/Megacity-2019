using Unity.Megacity.Gameplay;
using Unity.Megacity.UGS;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    public class MatchMakingUI : MonoBehaviour
    {
        [SerializeField] private MultiplayerServerSettings m_ServerSettings;
        
        // UI Elements
        private TextField m_MultiplayerTextField;
        private DropdownField m_MultiplayerServerDropdownMenu;
        private Label m_ConnectionStatusLabel;
        private MatchMakingLoadingBar m_MatchMakingLoadingBar;
        
        public static MatchMakingUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Init()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            m_ConnectionStatusLabel = root.Q<Label>("connection-label");
            m_MatchMakingLoadingBar = new MatchMakingLoadingBar(root);
            m_MultiplayerTextField = root.Q<TextField>("multiplayer-server-textfield");
            m_MultiplayerServerDropdownMenu = root.Q<DropdownField>("multiplayer-server-location");
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

            var ip = ipSplit[0];
            var portString = ipSplit[1];
            if (!ushort.TryParse(portString, out var portShort))
                return;
            MatchMakingConnector.Instance.SetIPAndPort(ip, portShort);
        }
        
        public void UpdateConnectionStatus(string status)
        {
            m_ConnectionStatusLabel.text = status;
        }
        
        public void SetUIConnectionStatusEnable(bool matchmaking)
        {
            m_MatchMakingLoadingBar.Enable(matchmaking);
            m_ConnectionStatusLabel.style.display = matchmaking ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void SetConnectionMode(bool isMatchMaking)
        {
            m_MultiplayerTextField.style.display = isMatchMaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerServerDropdownMenu.style.display = isMatchMaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerServerDropdownMenu.value = m_MultiplayerServerDropdownMenu.choices[0] ?? "Local";
            UpdatePortAndIP(m_ServerSettings.GetIPByName(m_MultiplayerServerDropdownMenu.value));
        }
    }
}