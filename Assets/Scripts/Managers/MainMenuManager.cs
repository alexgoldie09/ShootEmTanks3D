/*
 * MainMenuManager.cs
 * ----------------------------------------------------------------
 * Handles main menu button actions for starting the game or quitting.
 *
 * PURPOSE:
 * - Serve as a bridge between UI buttons and scene/game lifecycle actions.
 *
 * FEATURES:
 * - Loads the primary gameplay scene.
 * - Quits the application (or stops play mode in the Unity editor).
 */

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor; // Needed to stop play mode in the editor
#endif

public class MainMenuManager : MonoBehaviour
{
    #region Button Hooks
    /// <summary>
    /// Loads the main gameplay scene.
    /// Scene name must match exactly in the Build Settings.
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Quits the game.
    /// If in Unity editor, stops play mode instead.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop play mode when running in the editor
        EditorApplication.isPlaying = false;
#else
        // Close application in a built game
        Application.Quit();
#endif
    }
    #endregion
}