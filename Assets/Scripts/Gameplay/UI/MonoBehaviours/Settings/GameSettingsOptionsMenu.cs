using Unity.Megacity.CameraManagement;
using Unity.Megacity.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Game Settings Options
    /// Game Settings Options
    /// </summary>
    [RequireComponent(typeof(UIGameSettings))]
    public class GameSettingsOptionsMenu : MonoBehaviour
    {
        public static GameSettingsOptionsMenu Instance { get; private set; }
        
        private UIGameSettings m_UIGameSettings;
        private VisualElement m_SettingsOptions;

        private Button m_TutorialButton;
        private Button m_SettingButton;
        private Button m_ResumeButton;
        private Button m_BackToMenuButton;
        private Button m_QuitButton;

        private bool m_InSettingOptions;
        private InputAction m_OpenOptionsAction;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                m_UIGameSettings = GetComponent<UIGameSettings>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_SettingsOptions = root.Q<VisualElement>("settings-options-panel");
            m_TutorialButton = root.Q<Button>("tutorial-btn");
            m_SettingButton = root.Q<Button>("settings-btn");
            m_ResumeButton = root.Q<Button>("back-to-game-btn");
            m_BackToMenuButton = root.Q<Button>("back-to-menu-btn");
            m_QuitButton = root.Q<Button>("quit-button");

            m_TutorialButton.RegisterCallback<ClickEvent>(_ =>
            {
                TutorialScreen.Instance.ShowTutorial();
                ShowSettingsOptions(false);
            });

            m_ResumeButton.RegisterCallback<ClickEvent>(_ => { ShowSettingsOptions(false); });

            m_BackToMenuButton.RegisterCallback<ClickEvent>(_ =>
            {
                HybridCameraManager.Instance.Reset();
                DisconnectPlayerAndShowMenu();
            });

            m_SettingButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (!m_UIGameSettings.IsVisible)
                {
                    m_UIGameSettings.Show();
                }
                else
                {
                    m_UIGameSettings.Hide();
                }
            });

#if !(UNITY_ANDROID || UNITY_IPHONE)
            m_OpenOptionsAction = new InputAction("OpenOptions", InputActionType.Button, binding: "<Keyboard>/tab");
            m_OpenOptionsAction.Enable();
#endif

            m_QuitButton.clicked += () => { QuitSystem.WantsToQuit = true; };
        }

        private void DisconnectPlayerAndShowMenu()
        {
            ShowSettingsOptions(false);

            QuitSystem.DisconnectAllPlayers();
            if (VivoxManager.Instance != null)
                VivoxManager.Instance.Logout();

            if (PlayerInfoController.Instance.IsSinglePlayer)
                SceneController.LoadMenu();
        }

        public void ShowSettingsOptions(bool display)
        {
            if (SceneController.IsFrontEnd || display == m_InSettingOptions)
                return;

            m_InSettingOptions = display;
            m_SettingsOptions.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;

            m_TutorialButton.SetEnabled(!HybridCameraManager.Instance.IsDollyCamera);

            if (display)
                CursorUtils.ShowCursor();
            else
                CursorUtils.HideCursor();
        }

#if !(UNITY_ANDROID || UNITY_IPHONE)
        private void Update()
        {
            if (m_OpenOptionsAction.triggered)
            {
                ShowSettingsOptions(!m_InSettingOptions);
            }
        }
#endif
    }
}