using Unity.Megacity.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    public abstract class MainMenuGameMode : MonoBehaviour
    {
        protected abstract GameMode GameMode { get; }
        protected abstract VisualElement m_MenuOptions { get; }

        protected abstract Button m_AutomaticFocusButton { get; }

        // Reference to main menu
        protected MainMenu m_MainMenu;
        private void Start()
        {
#if UNITY_SERVER && !UNITY_EDITOR
            gameObject.SetActive(false);
#endif
            InitUI();
        }

        public virtual void InitUI()
        {
            m_MainMenu = GetComponent<MainMenu>();
            m_MainMenu.OnGameModeSelected += OnSelectedMode;
        }

        public void ToggleVisibility()
        {
            if (m_MenuOptions.style.display == DisplayStyle.None)
            {
                m_MenuOptions.style.display = DisplayStyle.Flex;
                m_AutomaticFocusButton.Focus();
            }
            else
            {
                m_MenuOptions.style.display = DisplayStyle.None;
            }
        }
        
        private void OnDestroy()
        {
            if(m_MainMenu != null)
                m_MainMenu.OnGameModeSelected -= OnSelectedMode;
        }
        
        private void OnSelectedMode(GameMode gameMode)
        {
            if(gameMode == GameMode)
                ToggleVisibility();
        }

        protected void BackToTheMenu()
        {
            MatchMakingUI.Instance.SetUIConnectionStatusEnable(false);
            m_MainMenu.ToggleBaseMenuOptions();
            ToggleVisibility();
        }
    }
}