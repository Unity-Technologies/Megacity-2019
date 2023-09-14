using Unity.Megacity.CameraManagement;
using Unity.Megacity.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    public class SinglePlayerMenu : MainMenuGameMode
    {
        // Single Player Menu
        private VisualElement m_SinglePlayerMenuOptions;
        private Button m_GuidedFlightButton;
        private Button m_FreeFlightButton;
        private Button m_ReturnButton;

        protected override GameMode GameMode => GameMode.SinglePlayer;
        protected override VisualElement m_MenuOptions => m_SinglePlayerMenuOptions;
        protected override Button m_AutomaticFocusButton => m_GuidedFlightButton;

        public override void InitUI()
        {
            base.InitUI();
            // Get references to UI elements
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_SinglePlayerMenuOptions = root.Q<VisualElement>("single-player-menu-options");
            m_GuidedFlightButton = m_SinglePlayerMenuOptions.Q<Button>("guided-flight-button");
            m_FreeFlightButton = m_SinglePlayerMenuOptions.Q<Button>("free-flight-button");
            m_ReturnButton = m_SinglePlayerMenuOptions.Q<Button>("single-player-return-button");
            
            m_SinglePlayerMenuOptions.style.display = DisplayStyle.None;

            // Subscribe to UI events
            m_GuidedFlightButton.clicked += OnRailsFlyoverRoutine;
            m_FreeFlightButton.clicked += PlayerControllerRoutine;

            m_ReturnButton.clicked += BackToTheMenu;
            m_GuidedFlightButton.RegisterCallback<MouseOverEvent>(_ => { m_GuidedFlightButton.Focus(); });
            m_FreeFlightButton.RegisterCallback<MouseOverEvent>(_ => { m_FreeFlightButton.Focus(); });
            m_ReturnButton.RegisterCallback<MouseOverEvent>(_ => { m_ReturnButton.Focus(); });
        }
        
        private void OnRailsFlyoverRoutine()
        {
            Debug.Log("Beginning on-rails flyover mode");
            HybridCameraManager.Instance.SetDollyCamera();
            m_MainMenu.Hide();
            SceneController.LoadGame();
        }
        
        private void PlayerControllerRoutine()
        {
            Debug.Log("Beginning player-controlled mode");
            HybridCameraManager.Instance.SetFollowCamera();
            m_MainMenu.Hide();
            SceneController.LoadGame();
        }
    }
}