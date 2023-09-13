using System;
using System.Text.RegularExpressions;
using Unity.Megacity.Gameplay;
using Unity.Megacity.UGS;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    public class MultiplayerMenu : MainMenuGameMode
    {
        public enum MultiplayerMode
        {
            Matchmaker = 0,
            Connect = 1
        }
        
        [field: SerializeField]
        public MultiplayerMode SelectedMultiplayerMode { get; private set; } = MultiplayerMode.Matchmaker;

        [SerializeField] 
        private PlayerInfoItemSettings m_PlayerSettings;
        
        // UI Elements
        private VisualElement m_MultiplayerMenuOptions;
        private Button m_MultiplayerPlayButton;
        private TextField m_NameTextField;
        private RadioButtonGroup m_MultiplayerModeGroup;
        private Button m_MultiplayerReturnButton;
        protected override GameMode GameMode => GameMode.Multiplayer;
        protected override VisualElement m_MenuOptions => m_MultiplayerMenuOptions;
        protected override Button m_AutomaticFocusButton => m_MultiplayerPlayButton;

        public override void InitUI()
        {
            base.InitUI();
            
            // Initialize MatchMakingConnector
            MatchMakingUI.Instance.Init();

            // Get references to UI elements
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_MultiplayerMenuOptions = root.Q<VisualElement>("multiplayer-menu-options");
            m_MultiplayerModeGroup = m_MultiplayerMenuOptions.Q<RadioButtonGroup>("multiplayer-mode");
            m_NameTextField = m_MultiplayerMenuOptions.Q<TextField>("name-textfield");
            m_MultiplayerPlayButton = m_MultiplayerMenuOptions.Q<Button>("multiplayer-play-button");
            m_MultiplayerReturnButton = m_MultiplayerMenuOptions.Q<Button>("multiplayer-return-button");

            m_MultiplayerMenuOptions.style.display = DisplayStyle.None;
            m_MultiplayerModeGroup.RegisterValueChangedCallback(evt =>
            {
                SetConnectionMode((MultiplayerMode) evt.newValue);
            });
            
            m_NameTextField.value = m_PlayerSettings.PlayerName;
            m_NameTextField.RegisterValueChangedCallback(evt =>
            {
                var filteredText = FilterNonAlphanumeric(evt.newValue);
                if (filteredText != evt.newValue)
                    m_NameTextField.SetValueWithoutNotify(filteredText);
            });

            m_MultiplayerPlayButton.clicked += () =>
            {
                if (!m_MultiplayerPlayButton.enabledSelf)
                    return;
                OnPlaySelected();
            };

            m_MultiplayerReturnButton.clicked += BackToTheMenu;
            m_MultiplayerPlayButton.RegisterCallback<MouseOverEvent>(_ => { m_MultiplayerPlayButton.Focus(); });
            m_MultiplayerReturnButton.RegisterCallback<MouseOverEvent>(_ => { m_MultiplayerReturnButton.Focus(); });
            
            SetConnectionMode(SelectedMultiplayerMode);
        }

        /// <summary>
        /// Filter out non-alphanumeric characters from the input string
        /// </summary>
        /// <param name="input"> Input string to filter</param>
        /// <returns> Filtered string</returns>
        private string FilterNonAlphanumeric(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9-_]", string.Empty);
        }

        private void SetConnectionMode(MultiplayerMode mode)
        {
            SelectedMultiplayerMode = mode;
            var connectButtonText = SelectedMultiplayerMode == MultiplayerMode.Matchmaker
                ? "Find Match"
                : SelectedMultiplayerMode.ToString();
            m_MultiplayerPlayButton.text = connectButtonText;
            var isMatchMaking = mode == MultiplayerMode.Matchmaker;
            MatchMakingUI.Instance.SetConnectionMode(isMatchMaking);
        }

        private void OnPlaySelected()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
            {
                MatchMakingUI.Instance.UpdateConnectionStatus("Error: No Internet Connection!");
                return;
            }
            
            if (MatchMakingConnector.Instance.ClientIsInGame)
            {
                Debug.LogWarning("Cant hit play while already in-game!");
                return;
            }

            switch (SelectedMultiplayerMode)
            {
                case MultiplayerMode.Matchmaker:
                    Matchmake();
                    break;
                case MultiplayerMode.Connect:
                    MatchMakingConnector.Instance.ConnectToServer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void Matchmake()
        {
            MatchMakingConnector.Instance.SetProfileServiceName(m_NameTextField.text);
            SetUIMatchmaking(true);
            await MatchMakingConnector.Instance.Matchmake();
            SetUIMatchmaking(false);
        }

        private void SetUIMatchmaking(bool matchmaking)
        {
            m_NameTextField.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerModeGroup.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerPlayButton.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_MultiplayerReturnButton.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            //show when is doing matchmaking
            MatchMakingUI.Instance.SetUIConnectionStatusEnable(matchmaking);
            m_MultiplayerPlayButton.SetEnabled(!matchmaking);
            m_MultiplayerReturnButton.SetEnabled(!matchmaking);
        }
    }
}