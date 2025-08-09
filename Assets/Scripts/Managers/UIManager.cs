/*
 * UIManager.cs
 * ----------------------------------------------------------------
 * Central UI controller for score/progress text and win/lose overlays.
 *
 * PURPOSE:
 * - Provide a simple API for updating score, crate progress, and quick messages.
 * - Show/hide game over and win screens.
 * - Expose button hooks for restart/menu navigation via GameManager.
 *
 * FEATURES:
 * - TextMeshPro fields for score, crates, messages, and win text.
 * - Timed message display with fade-out coroutine.
 * - Safe null-guarding on optional panels/text.
 */

using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text scoreText;   // "Score: N"
    public TMP_Text cratesText;  // "Crates: A / B"
    public TMP_Text messageText; // transient message with fade
    public TMP_Text winText;     // "YOU WON!" banner text
    
    [Header("Screens")]
    public GameObject gameOverPanel; // Game Over overlay
    public GameObject winPanel;      // Win overlay

    #region Public UI API
    /// <summary>
    /// Updates the score label.
    /// </summary>
    public void SetScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    /// <summary>
    /// Updates the crates progress label.
    /// </summary>
    public void SetCrates(int delivered, int total)
    {
        if (cratesText != null) cratesText.text = $"Crates: {delivered} / {total}";
    }

    /// <summary>
    /// Shows a transient message for a duration, then fades it out.
    /// </summary>
    public void ShowMessage(string text, float seconds = 1.2f)
    {
        if (messageText == null) return;

        StopAllCoroutines();                 // cancel any running fades
        messageText.gameObject.SetActive(true);
        messageText.text = text;
        StartCoroutine(FadeOutMessage(seconds));
    }

    /// <summary>
    /// Shows or hides the Game Over overlay.
    /// </summary>
    public void ShowGameOver(bool show)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(show);
    }

    /// <summary>
    /// Shows or hides the Win overlay and updates the banner text.
    /// </summary>
    public void ShowWin(bool show, int score)
    {
        if (winPanel != null) winPanel.SetActive(show);
        if (winText != null)  winText.SetText($"YOU WON!\nHighest Score: {score}");
    }
    #endregion

    #region Coroutines
    /// <summary>
    /// Waits for 't' seconds, then fades out the message quickly and hides it.
    /// </summary>
    private IEnumerator FadeOutMessage(float t)
    {
        float timer = 0f;

        // Ensure fully visible at start
        var color = messageText.color;
        color.a = 1f;
        messageText.color = color;

        // Hold message for duration
        while (timer < t)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Quick fade-out
        float fade = 0.3f;
        while (fade > 0f)
        {
            fade -= Time.deltaTime;
            color.a = Mathf.Clamp01(fade / 0.3f);
            messageText.color = color;
            yield return null;
        }

        messageText.gameObject.SetActive(false);
    }
    #endregion

    #region Button Hooks
    /// <summary>
    /// UI button: restart the game via GameManager.
    /// </summary>
    public void OnRestart() => GameManager.Instance.RestartGame();

    /// <summary>
    /// UI button: return to main menu via GameManager.
    /// </summary>
    public void OnBackToMenu() => GameManager.Instance.BackToMenu();
    #endregion
}
