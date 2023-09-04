using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.NetCode
{
    /// <summary>
    /// Shows diagnostics of the performance, and Ping.
    /// </summary>
    public class NetcodePanelStats : MonoBehaviour
    {
        private NetcodeConnectionMonitor m_Monitor;
        private Button m_BackButton;
        private VisualElement m_BackIcon;
        private VisualElement m_InfoPanel;
        private VisualElement m_Main;
        private bool m_PanelHidden;

        private Label m_PlayersLabel;
        private Label m_PingLabel;
        private Label m_EntitiesLabel;
        private Label m_GhostsLabel;
        private Label m_FPSLabel;
        private Label m_QualityLabel;
        private Label m_SystemsLabel;
        private bool m_IsRunning;

        public static NetcodePanelStats Instance { private set; get; }
        public NetcodeConnectionMonitor Monitor => m_Monitor;
        public bool IsMonitorEnable => m_Monitor != null;
        public bool IsRunning => m_IsRunning;
        private float PanelHeight => m_InfoPanel.layout.height + 10f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void ToggleNetcodePanel()
        {
            if (m_PanelHidden)
                ShowPanel();
            else
                HidePanel();
        }

        private void OnEnable()
        {
            m_Monitor = FindObjectOfType<NetcodeConnectionMonitor>();
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Main = root.Q<VisualElement>("info-panel");
            m_InfoPanel = root.Q<VisualElement>("info-panel-body");
            m_BackButton = root.Q<Button>("info-panel-back-button");
            m_FPSLabel = root.Q<Label>("fps-value");
            m_QualityLabel = root.Q<Label>("quality-value");
            m_PlayersLabel = root.Q<Label>("players-value");
            m_PingLabel = root.Q<Label>("ping-value");
            m_EntitiesLabel = root.Q<Label>("entities-value");
            m_GhostsLabel = root.Q<Label>("ghosts-value");
            m_SystemsLabel = root.Q<Label>("systems-value");
            m_BackIcon = root.Q<VisualElement>("back-icon");
            m_BackButton.clicked += ToggleNetcodePanel;
        }

        private void HidePanel()
        {
            m_InfoPanel.experimental.animation.Position(new Vector3(0, -PanelHeight), 500).Ease(Easing.OutCubic)
                    .OnCompleted(() => { m_PanelHidden = true; });
            m_BackIcon.experimental.animation.Rotation(Quaternion.Euler(0, 0, 180f), 300).Ease(Easing.OutQuad);
        }

        private void ShowPanel()
        {
            m_InfoPanel.experimental.animation.Position(new Vector3(0, 0), 500).Ease(Easing.OutCubic)
                    .OnCompleted(() => { m_PanelHidden = false; });
            m_BackIcon.experimental.animation.Rotation(Quaternion.Euler(0, 0, 0), 300).Ease(Easing.OutQuad);
        }

        public void Enable()
        {
            if(m_Main.style.display != DisplayStyle.Flex)
                m_Main.style.display = DisplayStyle.Flex;
            m_IsRunning = true;
            if(!Application.isEditor)
                HidePanel();
        }

        public void Disable()
        {
            if (m_Main.style.display != DisplayStyle.None)
                m_Main.style.display = DisplayStyle.None;
            m_IsRunning = false;
        }

        public void SetNumberOfPlayers(int number)
        {
            m_PlayersLabel.text = number.ToString();
        }

        public void SetFPSLabel(float fps)
        {
            m_FPSLabel.text = $"{((int)fps).ToString()} FPS";
        }

        public void SetPingLabel(int estimatedRTT, int deviationRTT)
        {
            m_PingLabel.text = estimatedRTT < 1000
                ? $"{estimatedRTT}±{deviationRTT}ms"
                : $"~{estimatedRTT + deviationRTT / 2:0}ms";
        }

        public void SetSystemsLabel(uint value)
        {
            m_SystemsLabel.text = value.ToString("n0");
        }

        public void SetEntitiesLabel(int value)
        {
            m_EntitiesLabel.text = value.ToString("n0");
        }

        public void SetGhostsLabel(GhostCount value)
        {
            m_GhostsLabel.text = value.GhostCountOnServer.ToString("n0");
        }

        public void UpdateQualityLabel(string quality)
        {
            m_QualityLabel.text = quality;
        }
    }
}
