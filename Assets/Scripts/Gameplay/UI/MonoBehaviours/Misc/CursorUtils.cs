using UnityEngine;

namespace Unity.MegaCity.UI
{
    /// <summary>
    /// Utility class for managing the cursor.
    /// </summary>
    public static class CursorUtils
    {
        public static void ShowCursor(bool visible)
        {
            if (visible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}