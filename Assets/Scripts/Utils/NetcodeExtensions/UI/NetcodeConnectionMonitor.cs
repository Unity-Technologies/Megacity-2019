using UnityEngine;
using System.Collections;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Unity.NetCode
{
    public class NetcodeConnectionMonitor : MonoBehaviour
    {
        private Label m_ConsoleLabel;
        private ScrollView m_ConsoleContainer;
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_ConsoleLabel = root.Q<Label>("monitor-console-label");
            m_ConsoleContainer = root.Q<ScrollView>("content-scroll-view");
            m_ConsoleLabel.RegisterValueChangedCallback(OnTextUpdated);
        }

        private void OnTextUpdated(ChangeEvent<string> evt)
        {
            StartCoroutine(ScrollToTheEnd());
        }

        private IEnumerator ScrollToTheEnd()
        {
            yield return new WaitForSeconds(0.1f);
            m_ConsoleContainer.verticalScroller.value = m_ConsoleContainer.verticalScroller.highValue;
        }

        public void AddEntry(FixedString64Bytes playerName, ConnectionState.State connectionState)
        {
            var color = connectionState == ConnectionState.State.Connected ? "#76D36FFF" : "#FF80BFFF";
            m_ConsoleLabel.text += $"\n{playerName} has <color={color}>{connectionState}</color>";
        }
    }
}