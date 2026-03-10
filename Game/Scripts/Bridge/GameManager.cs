using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Core singleton that owns all game state: timer, HP, score.
/// Persists across scene loads (DontDestroyOnLoad).
/// Place on an empty GameObject in your MainMenu or CityScene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Settings (tweak in Inspector) ────────────────────────────────────────
    [Header("Game Settings")]
    [Tooltip("Total game duration in seconds (180 = 3 minutes)")]
    public float totalTime = 180f;

    [Tooltip("How many crises the city can fail before game over")]
    public int maxHP = 5;

    [Tooltip("Maximum possible score on a perfect, instant run")]
    public int maxScore = 10000;

    [Header("Scene Names (must match Build Settings exactly)")]
    public string citySceneName    = "CityScene";
    public string gameOverSceneName = "GameOverScene";

    // ── Runtime State (read from other scripts, don't set externally) ─────────
    [Header("Runtime — Read Only")]
    public float timeRemaining;
    public int   currentHP;
    public bool  gameActive = false;

    // Tracks whether the player has had a perfect run so far this session
    public bool IsPerfectRun => currentHP == maxHP;

    // ── Score ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Live score, recalculated every frame.
    ///
    /// Formula:  MAX_SCORE × (hp/maxHP) × (0.5 + 0.5 × timeLeft/totalTime)
    ///
    /// Effect:
    ///   • HP = 0            → score = 0 (game over condition)
    ///   • Timer hits 0      → score = MAX_SCORE × (hp/maxHP) × 0.5  (never zeros)
    ///   • Perfect + fast    → score approaches MAX_SCORE
    ///   • Time component only contributes 50% of the weight, so surviving
    ///     matters more than pure speed.
    /// </summary>
    public int LiveScore
    {
        get
        {
            if (currentHP <= 0) return 0;
            float hpRatio   = (float)currentHP / maxHP;
            float timeRatio = 0.5f + 0.5f * (timeRemaining / totalTime);
            return Mathf.RoundToInt(maxScore * hpRatio * timeRatio);
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // Classic singleton with scene-persistence
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!gameActive) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame(GameEndReason.TimerExpired);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Call this from your Main Menu "Play" button.</summary>
    public void StartGame()
    {
        timeRemaining = totalTime;
        currentHP     = maxHP;
        gameActive    = true;
        SceneManager.LoadScene(citySceneName);
    }

    /// <summary>Called by CrisisMarker when a crisis times out unresolved.</summary>
    public void TakeDamage(int amount = 1)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        // Notify the HUD
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            timeRemaining = 0f;
            EndGame(GameEndReason.CityDestroyed);
        }
    }

    // ── Events (subscribe from HUD scripts) ───────────────────────────────────
    public event System.Action<int, int>   OnHPChanged;    // (current, max)
    public event System.Action<GameEndReason> OnGameEnded;

    // ── Internal ──────────────────────────────────────────────────────────────
    void EndGame(GameEndReason reason)
    {
        if (!gameActive) return; // guard against double-call
        gameActive = false;

        int finalScore = LiveScore;

        // Persist to PlayerPrefs so GameOverScene can read it
        PlayerPrefs.SetInt("FinalScore",   finalScore);
        PlayerPrefs.SetInt("FinalHP",      currentHP);
        PlayerPrefs.SetInt("WasPerfect",   IsPerfectRun ? 1 : 0);
        PlayerPrefs.SetFloat("TimeLeft",   timeRemaining);
        PlayerPrefs.Save();

        OnGameEnded?.Invoke(reason);
        SceneManager.LoadScene(gameOverSceneName);
    }
}

public enum GameEndReason { TimerExpired, CityDestroyed }
