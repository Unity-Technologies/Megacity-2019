using UnityEngine.SceneManagement;

namespace Unity.Megacity
{
    public static class SceneController
    {
        public static bool IsFrontEnd => SceneManager.GetActiveScene().name == "Menu";
        public static bool IsGameScene => SceneManager.GetActiveScene().name == "Main";

        public static bool IsReturningToMainMenu;
        
        public static void LoadGame()
        {
            SceneManager.LoadSceneAsync("Main");
        }
        
        public static void LoadMenu()
        {
            SceneManager.LoadScene("Menu");
            IsReturningToMainMenu = true;
        }
    }
}