using UnityEngine;
using UnityEngine.UI;
using TMPro; // requires TextMeshPro package

/// <summary>
/// The clickable marker that appears on the city map when a crisis spawns.
/// Shows a countdown timer. Clicking it launches the minigame.
/// If the timer hits zero, the city takes damage.
///
/// SETUP:
///   Make a prefab with:
///     • A SpriteRenderer or UI Image for the crisis icon
///     • A child Canvas (World Space) with:
///         - crisis name TMP label
///         - countdown TMP label
///         - a progress bar Image (filled, horizontal)
///     • A Collider2D (or 3D with camera raycast) for click detection
///   Then assign the references below in the Inspector.
/// </summary>
public class CrisisMarker : MonoBehaviour
{
    [Header("UI References")]
    public SpriteRenderer iconRenderer;
    public TextMeshPro    crisisNameLabel;
    public TextMeshPro    countdownLabel;
    public Image          timerBar;          // Image Type: Filled, Fill Method: Horizontal

    [Header("Visual Pulse")]
    [Tooltip("The marker will pulse red when this many seconds remain")]
    public float urgentThreshold = 10f;
    public Color normalColor  = Color.white;
    public Color urgentColor  = Color.red;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private CrisisType    _type;
    private int           _spawnIndex;
    private CrisisSpawner _spawner;
    private float         _timeRemaining;
    private bool          _resolved = false;

    // ── Public Init ───────────────────────────────────────────────────────────

    /// <summary>Called by CrisisSpawner immediately after instantiation.</summary>
    public void Initialize(CrisisType type, int spawnIndex, CrisisSpawner spawner)
    {
        _type          = type;
        _spawnIndex    = spawnIndex;
        _spawner       = spawner;
        _timeRemaining = type.timeLimit;

        if (iconRenderer    != null) iconRenderer.sprite = type.mapIcon;
        if (crisisNameLabel != null) crisisNameLabel.text = type.displayName;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (_resolved || !GameManager.Instance.gameActive) return;

        _timeRemaining -= Time.deltaTime;
        UpdateUI();

        if (_timeRemaining <= 0f)
            FailCrisis();
    }

    void OnMouseDown()
    {
        if (_resolved || !GameManager.Instance.gameActive) return;
        LaunchMinigame();
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    void UpdateUI()
    {
        float ratio = Mathf.Clamp01(_timeRemaining / _type.timeLimit);

        if (countdownLabel != null)
            countdownLabel.text = Mathf.CeilToInt(_timeRemaining).ToString();

        if (timerBar != null)
            timerBar.fillAmount = ratio;

        // Pulse red when urgent
        bool isUrgent = _timeRemaining <= urgentThreshold;
        Color target  = isUrgent ? urgentColor : normalColor;

        if (iconRenderer    != null) iconRenderer.color  = target;
        if (crisisNameLabel != null) crisisNameLabel.color = target;
    }

    void LaunchMinigame()
    {
        // Hand off state to MinigameBridge before scene load
        MinigameBridge.Instance.BeginMinigame(_type.minigameSceneName, this);
    }

    void FailCrisis()
    {
        if (_resolved) return;
        _resolved = true;

        GameManager.Instance.TakeDamage(_type.hpDamageOnFail);
        _spawner.OnCrisisRemoved(_spawnIndex);

        // Play a failure animation/sound here if desired
        Destroy(gameObject);
    }

    /// <summary>
    /// Called by MinigameBridge when the player returns from the minigame.
    /// </summary>
    /// <param name="success">True = player solved it; False = player failed it</param>
    public void OnMinigameReturned(bool success)
    {
        _resolved = true;
        _spawner.OnCrisisRemoved(_spawnIndex);

        if (!success)
            GameManager.Instance.TakeDamage(_type.hpDamageOnFail);

        // Play success/fail feedback here
        Destroy(gameObject);
    }

    /// <summary>Returns the current time remaining (used by MinigameBridge to pause it).</summary>
    public float GetTimeRemaining() => _timeRemaining;
}
