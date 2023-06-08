using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using VivoxUnity;
using Random = Unity.Mathematics.Random;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Detects the talker and updates UI Vivox player's name. 
    /// </summary>
    public class VivoxPlayerList : MonoBehaviour
    {
        [SerializeField]
        public Sprite m_MicrophoneOn;
        [SerializeField]
        public Sprite m_MicrophoneOff;
        [SerializeField]
        public Vector2 m_MinMaxAlphaIndicatorSpeaker;
        [SerializeField]
        public Vector2 m_MinMaxScaleIndicatorSpeaker;
        [SerializeField]
        private Color m_SignalColor;

        private IParticipant lastParticipant;

        private Button m_MicrophoneButton;
        private VisualElement m_MicrophoneIcon;
        private Random m_Random;
        private Label m_SpeakerName;
        private VisualElement m_SpeakerSignal;
        private bool IsMicrophoneMuted = false;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var container = root.Q<VisualElement>("vivox-container");
            m_SpeakerName = container.Q<Label>("vivox-name");
            m_MicrophoneButton = container.Q<Button>("vivox-button");
            m_SpeakerSignal = m_MicrophoneButton.Q<VisualElement>("vivox-signal");
            m_MicrophoneIcon = m_MicrophoneButton.Q<VisualElement>("vivox-icon");
            m_Random = new Random((uint)Time.realtimeSinceStartup + (uint)DateTime.Now.Ticks);
        }

        private void Update()
        {
            if (VivoxManager.Instance == null || VivoxManager.Instance.Devices == null)
                return;
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                OnMuteToggle();
            }

            if (IsMicrophoneMuted != VivoxManager.Instance.Devices.Muted)
            {
                IsMicrophoneMuted = VivoxManager.Instance.Devices.Muted;
                var style = new StyleBackground(IsMicrophoneMuted ? m_MicrophoneOff : m_MicrophoneOn);
                m_MicrophoneIcon.style.backgroundImage = style;
                m_MicrophoneIcon.EnableInClassList("icon-magenta", IsMicrophoneMuted);
                m_MicrophoneButton.EnableInClassList("vivox-button-magenta", IsMicrophoneMuted);
            }
        }

        private void Start()
        {
            if (VivoxManager.Instance != null && VivoxManager.Instance.Channel != null)
            {
                VivoxManager.Instance.Channel.OnParticipantAddedEvent += OnParticipantAdded;
                VivoxManager.Instance.Channel.OnParticipantRemovedEvent += OnParticipantRemoved;    
            }
        }

        private void OnDestroy()
        {
            if (VivoxManager.Instance != null && VivoxManager.Instance.Channel != null)
            {
                VivoxManager.Instance.Channel.OnParticipantAddedEvent -= OnParticipantAdded;
                VivoxManager.Instance.Channel.OnParticipantRemovedEvent -= OnParticipantRemoved;    
            }
        }
        
        private void OnParticipantAdded(string username, ChannelId channel, IParticipant participant)
        {
            participant.PropertyChanged += OnParticipantOnPropertyChanged;
        }

        private void OnParticipantRemoved(string username, ChannelId channel, IParticipant participant)
        {
            participant.PropertyChanged -= OnParticipantOnPropertyChanged;
        }

        private void OnMuteToggle()
        {
            VivoxManager.Instance.Devices.SetMicrophoneMute(!VivoxManager.Instance.Devices.Muted);
        }

        private void OnParticipantOnPropertyChanged(object obj, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "SpeechDetected")
            {
                var participant = obj as IParticipant;

                if (participant != null && lastParticipant == null && participant.SpeechDetected)
                {
                    lastParticipant = participant;
                    m_SpeakerName.text = participant.Account.DisplayName;
                    AnimateIcon();
                }
                else if (participant != null && lastParticipant == participant && !participant.SpeechDetected)
                {
                    lastParticipant = null;
                    m_SpeakerSignal.style.scale = new StyleScale(Vector2.one);
                    m_SpeakerName.text = "";
                    AssignIndicatorAlpha(m_MinMaxAlphaIndicatorSpeaker.x);
                }
            }
        }

        private void AnimateIcon()
        {
            if (lastParticipant == null)
            {
                return;
            }

            var nextScale = m_Random.NextFloat(m_MinMaxScaleIndicatorSpeaker.x, m_MinMaxScaleIndicatorSpeaker.y);
            AssignIndicatorAlpha(m_MinMaxAlphaIndicatorSpeaker.y);
            m_SpeakerSignal.experimental.animation.Scale(nextScale, 100).OnCompleted(AnimateIcon);
        }

        private void AssignIndicatorAlpha(float alpha)
        {
            var currentColor = m_SignalColor;
            currentColor.a = alpha;
            m_SpeakerSignal.style.backgroundColor = new StyleColor(currentColor);
        }
    }
}
