using System.Collections.Generic;
using UnityEngine;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// UI element for switching between screen resolutions
    /// </summary>
    public class UIScreenResolution : MonoBehaviour
    {
        // Define multiple resolutions to switch between
        [System.Serializable]
        public struct Resolution
        {
            public int width;
            public int height;
            public bool fullscreen;

            public override string ToString()
            {
                var mode = fullscreen ? "fullscreen" : "windowed";
                return $"{width}x{height} {mode}";
            }
        }

        [SerializeField] private List<Resolution> resolutions;

        private int currentResolutionIndex = 0;

        public int CurrentResolutionIndex => currentResolutionIndex;

        private void Awake()
        {
            // Get the screen's aspect ratio
            var screenAspectRatio = (float) Screen.width / (float) Screen.height;

            // Find the best resolution based on the screen's aspect ratio
            var bestAspectRatioDifference = float.MaxValue;

            for (int i = 0; i < resolutions.Count; i++)
            {
                var resolution = resolutions[i];
                var resolutionAspectRatio = (float) resolution.width / (float) resolution.height;
                var aspectRatioDifference = Mathf.Abs(screenAspectRatio - resolutionAspectRatio);

                if (aspectRatioDifference < bestAspectRatioDifference)
                {
                    bestAspectRatioDifference = aspectRatioDifference;
                    currentResolutionIndex = i;
                }
            }

            SetResolution(CurrentResolutionIndex);
        }

        public void NextResolution()
        {
            // Move to the next resolution in the list
            currentResolutionIndex++;
            if (CurrentResolutionIndex >= resolutions.Count)
            {
                currentResolutionIndex = 0;
            }

            SetResolution(CurrentResolutionIndex);
        }

        public void SetResolution(string value, out bool isFullscreen)
        {
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].ToString().Contains(value))
                    SetResolution(i);
            }

            isFullscreen = resolutions[CurrentResolutionIndex].fullscreen;
        }

        private void SetResolution(int index)
        {
            // Update the current resolution index
            currentResolutionIndex = index;

            // Get the current resolution
            Resolution currentResolution = resolutions[CurrentResolutionIndex];

            // Set the screen resolution and fullscreen mode
            Screen.SetResolution(currentResolution.width, currentResolution.height, currentResolution.fullscreen);
        }

        public void SetScreenMode(string value)
        {
            if (value == FullScreenMode.Windowed.ToString().ToLower())
            {
                Screen.fullScreen = false;
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
            else
            {
                Screen.fullScreen = true;
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
        }

        public List<string> GetResolutionOptions()
        {
            var value = new List<string>();
            for (int i = 0; i < resolutions.Count; i++)
            {
                var resolution = resolutions[i];
                value.Add($"{resolution}");
            }

            return value;
        }

        public List<string> GetResolutionModes()
        {
            var value = new List<string>();
            value.Add($"{FullScreenMode.Windowed}");
            value.Add($"{FullScreenMode.FullScreenWindow}");

            return value;
        }
    }
}