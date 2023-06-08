using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Reads the progress value from GameLoadInfo singleton and update the loading progress bar accordingly
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private VisualElement m_MainMenu;
        private VisualElement m_LoadingScreen;
        private ProgressBar m_ProgressBar;

        public static LoadingScreen Instance
        {
            get;
            private set;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SetUpUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetUpUI()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_MainMenu = root.Q<VisualElement>("visual-menu");
            m_LoadingScreen = root.Q<VisualElement>("loading-screen");
            m_ProgressBar = root.Q<ProgressBar>("progressbar");
        }

        public void UpdateProgressBar(float progress)
        {
            m_ProgressBar.value = math.lerp(m_ProgressBar.value, progress, Time.deltaTime);
        }

        public void Show()
        {
            m_MainMenu.style.display = DisplayStyle.None;
            m_LoadingScreen.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_MainMenu.style.display = DisplayStyle.Flex;
            m_LoadingScreen.style.display = DisplayStyle.None;
        }
    }
}
