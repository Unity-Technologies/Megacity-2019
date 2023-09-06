using System.Collections;
using Unity.Megacity.CameraManagement;
using Unity.Megacity.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Tutorial Screen UI element
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TutorialScreen : MonoBehaviour
    {
        public static TutorialScreen Instance { get; private set; }
        
        private VisualElement m_TutorialScreen;
        private VisualElement m_SinglePlayerTutorial;
        private VisualElement m_MultiplayerTutorial;
        private bool m_InTutorialScreen;

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

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_TutorialScreen = root.Q<VisualElement>("tutorial-screen");
            m_SinglePlayerTutorial = root.Q<VisualElement>("tutorial-single-player");
            m_MultiplayerTutorial = root.Q<VisualElement>("tutorial-multiplayer");
        }

        private void Start()
        {
            if (PlayerInfoController.Instance == null)
                return;
            
            ShowTutorial();
            
            if (PlayerInfoController.Instance.IsSinglePlayer)
            {
                if(HybridCameraManager.Instance.IsDollyCamera)
                    HideTutorial();
                
                m_SinglePlayerTutorial.style.display = DisplayStyle.Flex;
                m_MultiplayerTutorial.style.display = DisplayStyle.None;
            }
            else
            {
                m_MultiplayerTutorial.style.display = DisplayStyle.Flex;
                m_SinglePlayerTutorial.style.display = DisplayStyle.None;
            }
        }

        public void ShowTutorial()
        {
            if (m_InTutorialScreen) 
                return;
            
            m_TutorialScreen.style.display = DisplayStyle.Flex;
            m_InTutorialScreen = true;
            
            StartCoroutine(WaitForAnyInput());
        }

        private void HideTutorial()
        {
            m_TutorialScreen.style.display = DisplayStyle.None;
            m_InTutorialScreen = false;
        }

        private IEnumerator WaitForAnyInput()
        {
            while (m_InTutorialScreen)
            {
                InputSystem.onAnyButtonPress.CallOnce(_ =>
                {
                    HideTutorial();
                });
                
                yield return null;
            }
        }
    }
}