using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.MegaCity.EditorTools
{
    [InitializeOnLoad]
    public class OpenSceneByDefault
    {
        private const string LastOpenedKey = "LastOpened";
        public static float LastOpened { get; private set; }
        static OpenSceneByDefault()
        {
            LastOpened = EditorPrefs.GetFloat(LastOpenedKey, float.MaxValue);
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            var openedTime = LastOpened;
            var timeSinceStartup = (float) EditorApplication.timeSinceStartup;
        
            if (timeSinceStartup < openedTime)
            {
                LastOpened = timeSinceStartup;
                EditorSceneManager.OpenScene("Assets/Scenes/Megacity.unity");   
            }

            EditorPrefs.SetFloat(LastOpenedKey, LastOpened);
            EditorApplication.delayCall -= OnEditorLoaded;
        }
    }
}