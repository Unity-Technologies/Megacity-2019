using Unity.MegaCity.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Game Settings Options Menu UI element
    /// </summary>
    [RequireComponent(typeof(UIGameSettings))]
    public class GameSettingsOptionsMenu : MonoBehaviour
    {
        private UIGameSettings m_UIGameSettings;
        private VisualElement m_SettingsOptions;
        
        private Button m_TutorialButton;
        private Button m_SettingButton;
        private Button m_BackToGameButton;
        private Button m_BackToMenuButton;
        private Button m_QuitButton;
        
        private bool m_InSettingOptions;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_SettingsOptions = root.Q<VisualElement>("settings-options-panel");
            m_TutorialButton = root.Q<Button>("tutorial-btn");
            m_SettingButton = root.Q<Button>("settings-btn");
            m_BackToGameButton = root.Q<Button>("back-to-game-btn");
            m_BackToMenuButton = root.Q<Button>("back-to-menu-btn");
            m_QuitButton = root.Q<Button>("quit-button");

            m_TutorialButton.RegisterCallback<ClickEvent>(_ =>
            {
                TutorialScreen.Instance.ShowTutorial();
                ShowSettingsOptions(false);
            });
            
            m_BackToGameButton.RegisterCallback<ClickEvent>(_ =>
            {
                ShowSettingsOptions(false);
            });
            
            m_BackToMenuButton.RegisterCallback<ClickEvent>(_ =>
            {
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
            
            m_QuitButton.clicked += () => { QuitSystem.WantsToQuit = true; };
            m_UIGameSettings = GetComponent<UIGameSettings>();
        }

        private void DisconnectPlayerAndShowMenu()
        {
            ShowSettingsOptions(false);
            MainMenu.Instance.Show();
            QuitSystem.DisconnectAllPlayers();
            if (VivoxManager.Instance != null)
                VivoxManager.Instance.Logout();
        }

        private void ShowSettingsOptions(bool display)
        {
            if (display == m_InSettingOptions)
                return;
            
            m_InSettingOptions = display;
            m_SettingsOptions.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
            
            CursorUtils.ShowCursor(display);
        }

        private void Update()
        {
            // Show the GameSettings In Game
            if (Input.GetKeyDown(KeyCode.Tab) && QuitSystem.IsPlayerConnected() && 
                MainMenu.Instance.MatchMakingConnector.ClientIsInGame && 
                !MainMenu.Instance.IsVisible)
            {
                if (m_UIGameSettings.IsVisible)
                {
                    m_UIGameSettings.Hide();
                    return;
                }

                ShowSettingsOptions(!m_InSettingOptions);
            }
        }
    }
}
