using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Drives the city-view HUD: timer, HP hearts/bar, and live score.
///
/// SETUP:
///   In your CityScene, create a Canvas and assign the UI element references below.
/// </summary>
public class CityHUD : MonoBehaviour
{
    [Header("Timer")]
    public TextMeshProUGUI timerLabel;      // e.g. "2:45"
    public Image           timerBar;        // optional — fillAmount driven by time

    [Header("HP")]
    public TextMeshProUGUI hpLabel;         // e.g. "HP: 4/5"
    // Optional: swap these for heart icons if you prefer the visual approach
    public Image[]         hpHearts;        // array of heart icons, disabled as HP drops

    [Header("Score")]
    public TextMeshProUGUI scoreLabel;      // live score display

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHPChanged += RefreshHP;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHPChanged -= RefreshHP;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Timer
        float t     = GameManager.Instance.timeRemaining;
        int   mins  = Mathf.FloorToInt(t / 60f);
        int   secs  = Mathf.FloorToInt(t % 60f);

        if (timerLabel != null)
            timerLabel.text = $"{mins}:{secs:D2}";

        if (timerBar != null)
            timerBar.fillAmount = t / GameManager.Instance.totalTime;

        // Score
        if (scoreLabel != null)
            scoreLabel.text = GameManager.Instance.LiveScore.ToString("N0");
    }

    // Called via GameManager.OnHPChanged event
    void RefreshHP(int current, int max)
    {
        if (hpLabel != null)
            hpLabel.text = $"HP: {current}/{max}";

        // Drive heart icons
        if (hpHearts != null)
        {
            for (int i = 0; i < hpHearts.Length; i++)
            {
                if (hpHearts[i] != null)
                    hpHearts[i].enabled = (i < current);
            }
        }
    }
}
