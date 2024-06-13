using System;
using Unity.Megacity.CameraManagement;
using UnityEngine;
using Unity.Megacity.Gameplay;
using Unity.Megacity.UGS;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Manages the UI for the main menu.
    /// This sets the audio settings for the city.
    /// Defines if the player should be manual or automatic.
    /// Allows the execution to exiting by pressing Escape
    /// Has access to the UI game settings
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public event Action<GameMode> OnGameModeSelected;  
        private UIGameSettings m_GameSettings;

        [SerializeField] private PlayerInfoItemSettings m_PlayerSettings;
        // Base Menu Options
        private VisualElement m_BaseMenuOptions;
        private Button m_SinglePlayerButton;
        private Button m_MultiplayerButton;
        private Button m_QuitButton;
        private Button m_GameSettingsButton;
        private VisualElement m_MainMenuContainer;

        public static MainMenu Instance { get; private set; }

        public bool IsVisible => m_MainMenuContainer.style.display == DisplayStyle.Flex;
        
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                m_GameSettingsButton.clicked -= ShowGameSettings;
                m_QuitButton.clicked -= QuitDemo;
            }
        }

        private void Start()
        {
            if (SceneController.IsReturningToMainMenu)
            {
                SceneController.IsReturningToMainMenu = false;
                ServerConnectionUtils.CreateDefaultWorld();
            }
#if UNITY_SERVER && !UNITY_EDITOR
            Debug.Log("Beginning server mode");
            gameObject.SetActive(false);
#else
            InitUI();
#endif
        }

        private void InitUI()
        { 
            m_PlayerSettings.GameMode = GameMode.None;
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_MainMenuContainer = root.Q<VisualElement>("main-menu-container");
            
            // Base Menu Options
            m_BaseMenuOptions = m_MainMenuContainer.Q<VisualElement>("base-menu-options");
            m_GameSettingsButton = m_MainMenuContainer.Q<Button>("settings-button");
            m_QuitButton = m_MainMenuContainer.Q<Button>("quit-button");
            m_SinglePlayerButton = m_MainMenuContainer.Q<Button>("single-player-button");
            m_MultiplayerButton = m_MainMenuContainer.Q<Button>("multiplayer-button");

            m_GameSettingsButton.clicked += ShowGameSettings;
            m_QuitButton.clicked += QuitDemo;

            m_SinglePlayerButton.clicked += () =>
            {
                ToggleBaseMenuOptions();
                //m_SinglePlayerMenu.ToggleVisibility();
                m_PlayerSettings.GameMode = GameMode.SinglePlayer;
                if (OnGameModeSelected != null)
                    OnGameModeSelected(m_PlayerSettings.GameMode);
            };
            
            m_MultiplayerButton.clicked += () =>
            {
                ToggleBaseMenuOptions();
                //m_MultiplayerMenu.ToggleVisibility();
                m_PlayerSettings.GameMode = GameMode.Multiplayer;
                if (OnGameModeSelected != null)
                    OnGameModeSelected(m_PlayerSettings.GameMode);
            };
            
            m_SinglePlayerButton.RegisterCallback<MouseOverEvent>(_ => { m_SinglePlayerButton.Focus(); });
            m_MultiplayerButton.RegisterCallback<MouseOverEvent>(_ => {m_MultiplayerButton.Focus(); });
            m_GameSettingsButton.RegisterCallback<MouseOverEvent>(_ => { m_GameSettingsButton.Focus(); });
            m_QuitButton.RegisterCallback<MouseOverEvent>(_ => { m_QuitButton.Focus(); });

            m_SinglePlayerButton.Focus();
            
            SetupCosmeticFlickering(m_MainMenuContainer);
            Show();
        }

        public void ToggleBaseMenuOptions()
        {
            if (m_BaseMenuOptions.style.display == DisplayStyle.None)
            {
                m_BaseMenuOptions.style.display = DisplayStyle.Flex;
                m_SinglePlayerButton.Focus();
                m_PlayerSettings.GameMode = GameMode.None;
            }
            else
            {
                m_BaseMenuOptions.style.display = DisplayStyle.None;
            }
        }

        public void ConnectionSucceeded()
        {
            HybridCameraManager.Instance.SetFollowCamera();
            Hide();
            MatchMakingConnector.Instance.ConnectionSucceeded();
        }

        public void ConnectionFailed()
        {
            MatchMakingConnector.Instance.ConnectionFailed();
            SceneController.LoadMenu();
        }

        private void Show()
        {
            m_MainMenuContainer.style.display = DisplayStyle.Flex;
            MatchMakingConnector.Instance.ClientIsInGame = false;
            CursorUtils.ShowCursor();
        }

        public void Hide()
        {
            LoadingScreen.Instance.Show();
            m_MainMenuContainer.style.display = DisplayStyle.None;
        }
        
        private void QuitDemo()
        {
            // If we want to do something before quiting game
            QuitSystem.WantsToQuit = true;
        }

        private void ShowGameSettings()
        {
            if (m_GameSettings == null)
            {
                m_GameSettings = FindObjectOfType<UIGameSettings>();
            }
            
            m_GameSettings.Show(m_MainMenuContainer);
            m_MainMenuContainer.style.display = DisplayStyle.None;
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