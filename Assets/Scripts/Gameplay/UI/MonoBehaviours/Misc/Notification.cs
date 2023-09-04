using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Notification UI element
    /// </summary>
    public class Notification : MonoBehaviour
    {
        [SerializeField]
        private float m_TimeOnScreen = 1.5f;
        private Label m_Notification;

        public bool IsVisible => m_Notification.style.display == DisplayStyle.Flex;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Notification = root.Q<Label>("notification-label");
        }

        public void Show()
        {
            m_Notification.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_Notification.style.display = DisplayStyle.None;
        }

        public void Message(string message)
        {
            m_Notification.text = message;
            Show();
            m_Notification.style.scale = new StyleScale(new Vector2(0,0));
            m_Notification.experimental.animation.Scale(1, 500).Ease(Easing.OutCubic).OnCompleted(() =>
            {
                StartCoroutine(HideMessage());
            });
        }

        private IEnumerator HideMessage()
        {
            yield return new WaitForSeconds(m_TimeOnScreen);
            m_Notification.experimental.animation.Scale(0, 500).Ease(Easing.OutCubic).OnCompleted(()=>
            {
                Hide();
            });
        }
    }

}
