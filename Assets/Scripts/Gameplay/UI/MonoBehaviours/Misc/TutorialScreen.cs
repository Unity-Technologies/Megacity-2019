using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Tutorial Screen UI element
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TutorialScreen : MonoBehaviour
    {
        public static TutorialScreen Instance { get; private set; }
        
        private VisualElement m_TutorialScreen;
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
        }
        
        public void ShowTutorial()
        {
            if (m_InTutorialScreen) 
                return;
            
            m_TutorialScreen.style.display = DisplayStyle.Flex;
            m_InTutorialScreen = true;
            
            CursorUtils.ShowCursor(false);

            StartCoroutine(WaitForAnyInput());
        }
        
        private IEnumerator WaitForAnyInput()
        {
            while (m_InTutorialScreen)
            {
                if (Input.anyKey)
                {
                    m_TutorialScreen.style.display = DisplayStyle.None;
                    m_InTutorialScreen = false;
                    yield break;
                }
                yield return null;
            }
        }
    }
}