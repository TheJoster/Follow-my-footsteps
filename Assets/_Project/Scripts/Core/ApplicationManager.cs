using UnityEngine;

namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Manages application-level functionality like quit, pause, etc.
    /// </summary>
    public class ApplicationManager : MonoBehaviour
    {
        private void Update()
        {
            // Quit application when Escape is pressed
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
            }
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        private void QuitApplication()
        {
#if UNITY_EDITOR
            // Stop playing in editor
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("Application quit (Editor mode)");
#else
            // Quit application in build
            Application.Quit();
            Debug.Log("Application quit");
#endif
        }
    }
}
