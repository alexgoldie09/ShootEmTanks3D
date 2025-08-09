/*
 * GameManager.cs
 * ----------------------------------------------------------------
 * Central game state controller (singleton) for scoring, win/lose flow, and scene transitions.
 *
 * PURPOSE:
 * - Track score, crate progress, and game-over state.
 * - Notify UI of score/progress updates and show win/lose overlays.
 * - Provide simple scene controls for restart and main menu navigation.
 *
 * FEATURES:
 * - Lightweight singleton with DontDestroyOnLoad.
 * - Score updates with crate goal handling.
 * - Delayed win/lose events using Invoke().
 * - Simple API used by gameplay systems (CollisionEngine, Enemy, etc.).
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Global singleton instance
    public static GameManager Instance { get; private set; }

    [Header("Game Stats")]
    public int score = 0;           // Current player score
    public int totalCrates = 0;     // Total crates required to win
    private int currentCrates = 0;  // Crates collected so far

    [Header("Game State")]
    public bool gameOver = false;   // True once a win/lose condition has fired

    [Header("UI")]
    public UIManager uiManager;     // Hook to UI layer for updates/overlays

    #region Unity Lifecycle
    /// <summary>
    /// Singleton initialization and initial UI sync.
    /// </summary>
    void Awake()
    {
        // Enforce single instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initial UI push
        if (uiManager != null)
        {
            uiManager.SetScore(score);
            uiManager.SetCrates(currentCrates, totalCrates);
        }
    }
    #endregion

    #region Scoring & Progression
    /// <summary>
    /// Adds to the score and, if not an enemy kill, counts toward crate progress.
    /// Triggers win once crate goal is reached.
    /// </summary>
    /// <param name="amount">Score delta to add.</param>
    /// <param name="isEnemy">True if the points came from defeating an enemy.</param>
    public void AddScore(int amount, bool isEnemy)
    {
        if (gameOver) return;

        score += amount;
        if (!isEnemy)
            currentCrates += amount;

        // UI feedback
        if (uiManager != null)
        {
            uiManager.SetScore(score);
            uiManager.SetCrates(currentCrates, totalCrates);
            uiManager.ShowMessage(isEnemy ? "ENEMY DOWN!!" : "GOAL!!");
        }

        // Win condition: collected enough crates
        if (currentCrates >= totalCrates)
        {
            gameOver = true;
            Invoke(nameof(WinGame), 3f); // small celebration delay
        }
    }
    #endregion

    #region Game Flow
    /// <summary>
    /// External trigger to start the game-over sequence (e.g., player death).
    /// </summary>
    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        Invoke(nameof(GameOver), 3f); // short delay for FX
    }

    /// <summary>
    /// Shows the game-over overlay.
    /// </summary>
    private void GameOver()
    {
        if (uiManager != null)
            uiManager.ShowGameOver(true);
    }

    /// <summary>
    /// Shows the win overlay (with final score).
    /// </summary>
    private void WinGame()
    {
        if (uiManager != null)
            uiManager.ShowWin(true, score);
    }
    #endregion

    #region Scene Management
    /// <summary>
    /// Reloads the current scene and resets basic game state + UI.
    /// </summary>
    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);

        // Reset core state (project-specific defaults)
        score = 0;
        currentCrates = 0;
        totalCrates = 3;
        gameOver = false;

        // Reset UI
        if (uiManager != null)
        {
            uiManager.ShowGameOver(false);
            uiManager.ShowWin(false, score);
            uiManager.SetScore(score);
            uiManager.SetCrates(currentCrates, totalCrates);
        }
    }

    /// <summary>
    /// Loads the main menu and tears down the persistent manager.
    /// </summary>
    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Destroy(gameObject);
    }
    #endregion
}