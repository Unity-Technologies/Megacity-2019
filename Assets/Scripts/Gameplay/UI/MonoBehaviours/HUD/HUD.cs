using System.Collections;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.Megacity.Gameplay;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Manages the HUD UI elements.
    /// </summary>
    [RequireComponent(typeof(Crosshair))]
    [RequireComponent(typeof(LaserBar))]
    [RequireComponent(typeof(Notification))]
    [RequireComponent(typeof(UIDocument))]
    public class HUD : MonoBehaviour
    {
        [SerializeField] private UILeaderboard m_Leaderboard;
        [SerializeField] private PlayerInfoItemSettings m_PlayerInfoSettings;
        [SerializeField] private GameObject m_VirtualJoystickPrefab;
        public VirtualJoystick JoystickLeft { get; private set; }
        public VirtualJoystick JoystickRight { get; private set; }

        private ProgressBar m_LifeBar;
        private Crosshair m_Crosshair;
        private Notification m_Notification;
        private LaserBar m_LaserBar;

        private VisualElement m_MessageScreen;
        private VisualElement m_LifeBarContainer;
        
        private Label m_BottomMessageLabel;
        private Label m_MessageLabel;
        private Label m_ControllerLabel;

        private float m_TypeDelay = 0.01f;
        private float m_DeathCooldown = 5f;
        private bool m_CompletedShowMessage;

        public static HUD Instance { get; private set; }
        public UILeaderboard Leaderboard => m_Leaderboard;
        public Crosshair Crosshair => m_Crosshair;
        public LaserBar Laser => m_LaserBar;
        public Notification Notification => m_Notification;

        private void Awake()
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

            m_Crosshair = GetComponent<Crosshair>();
            m_Notification = GetComponent<Notification>();
            m_LaserBar = GetComponent<LaserBar>();
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            m_LifeBar = root.Q<ProgressBar>("life-bar");
            m_LifeBarContainer = root.Q<VisualElement>("lifebar-container");
            m_MessageScreen = root.Q<VisualElement>("message-screen");
            m_MessageLabel = m_MessageScreen.Q<Label>("message-label");
            m_BottomMessageLabel = m_MessageScreen.Q<Label>("bottom-message-label");
            m_MessageScreen.style.display = DisplayStyle.None;
            
            CursorUtils.HideCursor();
            JoystickLeft = CreateJoystick(VirtualJoystick.Align.Left);
            JoystickRight = CreateJoystick(VirtualJoystick.Align.Right);

            if (PlayerInfoController.Instance.IsSinglePlayer)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private VirtualJoystick CreateJoystick(VirtualJoystick.Align align)
        {
            var prefab =  Instantiate(m_VirtualJoystickPrefab, transform.parent, true);
            var joystick = prefab.GetComponent<VirtualJoystick>();
            joystick.SetPosition(align);
            return joystick;
        }

        private void Hide()
        {
            m_LifeBarContainer.style.display = DisplayStyle.None;
            m_Crosshair.Hide();
            m_LaserBar.Hide();
            NetcodePanelStats.Instance.Disable();
        }

        private void Show()
        {
            m_LifeBarContainer.style.display = DisplayStyle.Flex;
            m_Crosshair.Show();
            m_LaserBar.Show();
            NetcodePanelStats.Instance.Disable();
        }

        public void UpdateLife(float life)
        {
            if (m_LifeBar.value >= m_PlayerInfoSettings.MinLifeBar &&
                life < m_PlayerInfoSettings.MinLifeBar &&
                !m_LifeBar.ClassListContains("magenta"))
            {
                m_LifeBar.AddToClassList("magenta");
            }
            else if (m_LifeBar.value <= m_PlayerInfoSettings.MinLifeBar &&
                     life > m_PlayerInfoSettings.MinLifeBar &&
                     m_LifeBar.ClassListContains("magenta"))
            {
                m_LifeBar.RemoveFromClassList("magenta");
            }

            m_LifeBar.value = life;
            m_LifeBar.title = ((int) life).ToString();
        }

        public void ShowDeathMessage(string killerName)
        {
            FadeMessageScreen(true);
            StartCoroutine(Type($"You have been destroyed by\n{killerName}!"));
            StartCoroutine(DeathCooldown());
            m_Crosshair.Hide();
        }

        public void ShowBoundsMessage()
        {
            FadeMessageScreen(true);
            StartCoroutine(Type("You still have unfinished business here..."));
            m_BottomMessageLabel.text = "";
            m_Crosshair.Hide();
        }

        private void FadeMessageScreen(bool value)
        {
            if (value)
            {
                m_MessageScreen.style.display = DisplayStyle.Flex;
                m_MessageScreen.experimental.animation
                    .Start(new StyleValues {opacity = 0f}, new StyleValues {opacity = 1f}, 1000);
            }
            else
            {
                m_MessageScreen.experimental.animation
                    .Start(new StyleValues {opacity = 1f}, new StyleValues {opacity = 0f}, 1000).OnCompleted(() =>
                    {
                        m_MessageScreen.style.display = DisplayStyle.None;
                    });
            }
        }

        private IEnumerator DeathCooldown()
        {
            var timer = m_DeathCooldown;
            while (timer >= 0)
            {
                var bottomMessage = (timer > 1)
                    ? $"Respawning in: {math.trunc(timer)}s"
                    : "Returning...";
                m_BottomMessageLabel.text = bottomMessage;
                timer -= Time.deltaTime;
                yield return null;
            }
        }

        public void HideMessageScreen()
        {
            if(m_MessageScreen.style.display == DisplayStyle.Flex)
                StartCoroutine(HideWhenCompletedMessage());
        }

        private IEnumerator HideWhenCompletedMessage()
        {
            while (!m_CompletedShowMessage)
            {
                yield return null;
            }

            FadeMessageScreen(false);
            
            if (!PlayerInfoController.Instance.IsSinglePlayer)
                m_Crosshair.Show();
        }

        private IEnumerator Type(string message)
        {
            m_CompletedShowMessage = false;
            m_MessageLabel.text = "";
            foreach (var c in message.ToCharArray())
            {
                m_MessageLabel.text += c;
                yield return new WaitForSeconds(m_TypeDelay);
            }

            yield return new WaitForSeconds(1.5f);
            m_CompletedShowMessage = true;
        }
    }
}