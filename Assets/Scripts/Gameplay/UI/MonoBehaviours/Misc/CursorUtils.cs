using UnityEngine;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Utility class for managing the cursor.
    /// </summary>
    public static class CursorUtils
    {
        public static void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public static void HideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}