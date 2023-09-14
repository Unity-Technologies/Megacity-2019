#if (UNITY_ANDROID || UNITY_IPHONE || ENABLED_VIRTUAL_JOYSTICK)
using Unity.Mathematics;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Megacity.UI
{
    public class VirtualJoystick : MonoBehaviour
    {
        public enum Align
        {
            Left = 1,
            Right = 2,
        }
        
        private VisualElement m_JoystickBackground;
        private VisualElement m_Handle;
        private Vector2 m_Position;
        [SerializeField] private Align m_Align = Align.Left;
        public Vector2 Delta { get; private set; }

#if (UNITY_ANDROID || UNITY_IPHONE || ENABLED_VIRTUAL_JOYSTICK)
        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_JoystickBackground = root.Q<VisualElement>("background");
            m_Handle = root.Q<VisualElement>("handle");
            m_Handle.RegisterCallback<PointerDownEvent>(OnPressed);
            m_Handle.RegisterCallback<PointerUpEvent>(OnReleased);
            m_Handle.RegisterCallback<PointerMoveEvent>(OnMoved);
            
            var joystick = root.Q<VisualElement>("joystick");
            joystick.style.display = DisplayStyle.Flex;
        }
#if UNITY_EDITOR 
        private void Update()
        {
            if(!UnityEngine.Cursor.visible)
                CursorUtils.ShowCursor();
        }
#endif
        private void OnPressed(PointerDownEvent e)
        {
            m_Handle.CapturePointer(e.pointerId);
            m_Position = e.position;
        }

        private void OnReleased(PointerUpEvent e)
        {
            m_Handle.ReleasePointer(e.pointerId);
            m_Handle.transform.position = Vector3.zero;
            Delta = Vector2.zero;
        }

        private void OnMoved(PointerMoveEvent e)
        {
            if (!m_Handle.HasPointerCapture(e.pointerId))
                return;

            var currentPosition = (Vector2)e.position;
            var maxDelta = (m_JoystickBackground.worldBound.size - m_Handle.worldBound.size) / 2;
            var mappedPosition = ClampPosition(currentPosition - m_Position, -maxDelta, maxDelta);
            m_Handle.transform.position = mappedPosition;
            Delta = mappedPosition / maxDelta;
        }

        private Vector2 ClampPosition(Vector2 vector, Vector2 min, Vector2 max)
        {
            var x = math.clamp(vector.x, min.x, max.x);
            var y = math.clamp(vector.y, min.y, max.y);
            return new(x, y);
        }
#else
        private void Start()
        {
            Destroy(gameObject);
        }
#endif
        public void SetPosition(Align align)
        {
            m_Align = align;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var joystick = root.Q<VisualElement>("joystick");
            joystick.AddToClassList(m_Align == Align.Left? "left" : "right");
        }
    }
}