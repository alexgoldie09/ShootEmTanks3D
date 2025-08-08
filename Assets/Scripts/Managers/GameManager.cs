using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Stats")]
    public int score = 0;
    public int totalCrates = 0;

    [Header("Game State")]
    public bool gameOver = false;

    void Awake()
    {
        // Enforce singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int amount)
    {
        if (gameOver) return;

        score += amount;
        Debug.Log($"[GameManager] Score: {score}");

        if (score >= totalCrates)
        {
            WinGame();
        }
    }

    public void TriggerGameOver()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("[GameManager] Game Over!");
        // Restart scene after 2 seconds
        Invoke(nameof(RestartGame), 2f);
    }

    private void WinGame()
    {
        gameOver = true;
        Debug.Log("[GameManager] All crates delivered! You win!");
        // Restart scene or load victory screen
        Invoke(nameof(RestartGame), 3f);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameOver = false;
    }
}
