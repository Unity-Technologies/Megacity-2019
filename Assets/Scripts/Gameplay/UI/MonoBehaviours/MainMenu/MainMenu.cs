//  Add Platforms here that exclude Quit Menu option

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.MegaCity.Audio;
using UnityEngine;
using Unity.MegaCity.CameraManagement;
using Unity.MegaCity.Gameplay;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Random = UnityEngine.Random;

namespace Unity.MegaCity.UI
{
    public enum MultiplayerMode
    {
        Matchmaker = 0,
        Connect = 1
    }

    /// <summary>
    /// Manages the UI for the main menu.
    /// This sets the audio settings for the city.
    /// Defines if the player should be manual or automatic.
    /// Allows the execution to exiting by pressing Escape
    /// Has access to the UI game settings
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public HybridCameraManager m_HybridCameraManager;
        [SerializeField] private AudioMaster m_AudioMaster;
        [SerializeField] private UIGameSettings m_GameSettings;
        [SerializeField] private MultiplayerServerSettings m_ServerSettings;

        [field: SerializeField]
        public MultiplayerMode SelectedMultiplayerMode { get; private set; } = MultiplayerMode.Matchmaker;

        [SerializeField] private MatchMakingConnector m_MatchMakingConnector;
        public AudioMaster AudioMaster => m_AudioMaster;
        private int m_CurrentMenuItem;
        private int m_PrevMenuItem;

        private TextField m_NameTextField;
        private RadioButtonGroup m_MultiplayerModeGroup;

        private Button m_PlayerControllerButton;
        private Button m_QuitButton;
        private Button m_GameSettingsButton;
        private VisualElement m_VisualMenu;
        private VisualElement m_OverlayMenu;
        private VisualElement m_MainMenuContainer;
        private List<Button> m_Options;

        public MatchMakingConnector MatchMakingConnector
        {
            get => m_MatchMakingConnector;
            private set => m_MatchMakingConnector = value;
        }

        public static MainMenu Instance { get; private set; }

        public bool IsVisible => m_VisualMenu.style.display == DisplayStyle.Flex;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            MatchMakingConnector = new MatchMakingConnector(m_ServerSettings);
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Debug.LogWarning($"To use Unity's dashboard services, " +
                                 "you need to link your Unity project to a project ID. " +
                                 "To do this, go to Project Settings to select your organization, " +
                                 "select your project and then link a project ID. " +
                                 "You also need to make sure your organization has access to the required products. " +
                                 "Visit https://dashboard.unity3d.com to sign up.");
            }
            else
            {
                await MatchMakingConnector.Init();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // too strongly coupled to the ui so there we go :(
            // In Awake() the AudioManager is not present (before OnEnable()) so we would run into a null-ref exception.
#if UNITY_SERVER && !UNITY_EDITOR
            Debug.Log("Beginning server mode");
            gameObject.SetActive(false);
            return;
#else
            InitUI();
#endif
        }

        private void InitUI()
        {
            m_Options = new List<Button>();
            m_MainMenuContainer = GetComponent<UIDocument>().rootVisualElement;
            m_MultiplayerModeGroup = m_MainMenuContainer.Q<RadioButtonGroup>("multiplayer-mode");
            m_NameTextField = m_MainMenuContainer.Q<TextField>("name-textfield");
            m_PlayerControllerButton = m_MainMenuContainer.Q<Button>("player-controller-button");
            m_GameSettingsButton = m_MainMenuContainer.Q<Button>("settings-button");
            m_QuitButton = m_MainMenuContainer.Q<Button>("quit-button");
            m_VisualMenu = m_MainMenuContainer.Q<VisualElement>("visual-menu");
            m_OverlayMenu = m_MainMenuContainer.Q<VisualElement>("overlay");

            MatchMakingConnector.InitUI(m_MainMenuContainer);

            m_VisualMenu.style.display = DisplayStyle.Flex;

            m_MultiplayerModeGroup.RegisterValueChangedCallback(selectedGroup =>
            {
                SetConnectionMode((MultiplayerMode) selectedGroup.newValue);
            });

            var userName = MatchMakingConnector.DefaultName;
            Debug.Log($"Initializing UI as {userName}");
            if (!string.IsNullOrEmpty(userName))
            {
                m_NameTextField.value = userName;
                PlayerInfoController.Instance.Name = userName;
            }

            m_NameTextField.RegisterValueChangedCallback(evt =>
            {
                var filteredText = FilterNonAlphanumeric(evt.newValue);
                if (filteredText != evt.newValue)
                {
                    m_NameTextField.SetValueWithoutNotify(filteredText);
                }

                if (PlayerInfoController.Instance != null)
                    PlayerInfoController.Instance.Name = filteredText;

                MatchMakingConnector.SetProfileServeName(filteredText);
            });

            m_PlayerControllerButton.clicked += () =>
            {
                if (!m_PlayerControllerButton.enabledSelf)
                    return;
                m_CurrentMenuItem = 0;
                SelectItem();
            };
            m_GameSettingsButton.clicked += () =>
            {
                m_CurrentMenuItem = 1;
                SelectItem();
            };
            m_QuitButton.clicked += () =>
            {
                m_CurrentMenuItem = 2;
                SelectItem();
            };

            m_PlayerControllerButton.RegisterCallback<MouseOverEvent>(e => { m_CurrentMenuItem = 0; });
            m_GameSettingsButton.RegisterCallback<MouseOverEvent>(e => { m_CurrentMenuItem = 1; });
            m_QuitButton.RegisterCallback<MouseOverEvent>(e => { m_CurrentMenuItem = 2; });

            m_Options.Add(m_PlayerControllerButton);
            m_Options.Add(m_GameSettingsButton);
            m_Options.Add(m_QuitButton);
            SetMenuOptionUIElements(m_CurrentMenuItem);
            SetConnectionMode(SelectedMultiplayerMode);
            SetupCosmeticFlickering(m_MainMenuContainer);
        }

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
            m_PlayerControllerButton.text = connectButtonText;
            var isMatchMaking = mode == MultiplayerMode.Matchmaker;
            MatchMakingConnector.SetConnectionMode(isMatchMaking);
        }

        private void OnPlaySelected()
        {
            if (MatchMakingConnector.ClientIsInGame)
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
                    MatchMakingConnector.ConnectToServer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ConnectionSucceeded()
        {
            m_HybridCameraManager.m_CameraTargetMode = HybridCameraManager.CameraTargetMode.FollowPlayer;
            AnimateOut();
            MatchMakingConnector.ConnectionSucceeded();
        }

        public void ConnectionFailed()
        {
            MatchMakingConnector.ConnectionFailed();
            SceneManager.LoadScene("MegaCity");
        }

        public void Show()
        {
            m_VisualMenu.style.display = DisplayStyle.Flex;
            m_VisualMenu.visible = true;
            MatchMakingConnector.ClientIsInGame = false;
            CursorUtils.ShowCursor(true);
        }

        private void AnimateOut()
        {
            //LoadAudioSettings();
            m_OverlayMenu.style.display = DisplayStyle.Flex;

            m_OverlayMenu.experimental.animation
                .Start(new StyleValues {opacity = 1}, 1000)
                .Ease(Easing.Linear)
                .OnCompleted(() =>
                {
                    m_VisualMenu.style.display = DisplayStyle.None;
                    m_VisualMenu.visible = false;
                    m_OverlayMenu.experimental.animation
                        .Start(new StyleValues {opacity = 0f}, 2000)
                        .Ease(Easing.Linear)
                        .OnCompleted(() => { m_OverlayMenu.style.display = DisplayStyle.None; });

                    // Show HUD
                    TutorialScreen.Instance.ShowTutorial();
                });
        }

        private async void Matchmake()
        {
            SetUIMatchmaking(true);
            await MatchMakingConnector.Matchmake();
            SetUIMatchmaking(false);
        }

        private void SetUIMatchmaking(bool matchmaking)
        {
            m_PlayerControllerButton.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_QuitButton.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            m_GameSettingsButton.style.display = matchmaking ? DisplayStyle.None : DisplayStyle.Flex;
            //show when is doing matchmaking
            MatchMakingConnector.SetUIConnectionStatusEnable(matchmaking);
            m_PlayerControllerButton.SetEnabled(!matchmaking);
        }

        private void Update()
        {
            if (!IsVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && !m_GameSettings.IsVisible)
                QuitSystem.WantsToQuit = true;

            //	Only restrict update interval with respect to moving, not selecting
            if (Input.GetKeyDown(KeyCode.DownArrow))
                ++m_CurrentMenuItem;
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                --m_CurrentMenuItem;
            else if (Input.GetKeyDown(KeyCode.Return))
                SelectItem();

            m_CurrentMenuItem =
                Mathf.Clamp(m_CurrentMenuItem, 0, m_Options.Count > 0 ? m_Options.Count - 1 : 0);

            if (m_CurrentMenuItem != m_PrevMenuItem)
            {
                SetMenuOptionUIElements(m_CurrentMenuItem);
                m_PrevMenuItem = m_CurrentMenuItem;
            }
        }

        private void SetMenuOptionUIElements(int optionActive)
        {
            foreach (var buttonOption in m_Options)
                buttonOption.RemoveFromClassList("button-menu-active");

            m_Options[optionActive].AddToClassList("button-menu-active");
        }

        private void SelectItem()
        {
            switch (m_CurrentMenuItem)
            {
                case 0:
                    OnPlaySelected();
                    break;
                case 1:
                    ShowGameSettings();
                    break;
                case 2:
                    QuitDemo();
                    break;
            }
        }

        private void QuitDemo()
        {
            MatchMakingConnector?.Dispose();
            // If we want to do something before quiting game
            QuitSystem.WantsToQuit = true;
        }

        private void ShowGameSettings()
        {
            m_GameSettings.Show(m_VisualMenu);
            m_VisualMenu.style.display = DisplayStyle.None;
        }

        private void SetupCosmeticFlickering(VisualElement root)
        {
            var flickeringElements = root.Query(className: "flicker").ToList();
            foreach (var flickeringElement in flickeringElements)
            {
                var randomDelay = Random.Range(1, 4);
                flickeringElement.AddToClassList($"transition-{randomDelay}");
                flickeringElement.RegisterCallback<TransitionEndEvent>(_ =>
                    flickeringElement.ToggleInClassList("flicker-loop"));
                root.schedule.Execute(() => flickeringElement.ToggleInClassList("flicker-loop")).StartingIn(100);
            }
        }
    }
}