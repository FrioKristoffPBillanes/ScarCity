using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The single point of truth for minigame transitions.
///
/// Flow:
///   CrisisMarker.OnMouseDown()
///       → MinigameBridge.BeginMinigame()   [saves state, loads minigame scene]
///
///   MinigameResult.FinishMinigame(bool success)
///       → MinigameBridge.ReturnToCity()    [loads city scene, notifies marker]
///
/// SETUP:
///   Place on the same persistent GameObject as GameManager (or its own
///   DontDestroyOnLoad object). One instance, lives forever.
/// </summary>
public class MinigameBridge : MonoBehaviour
{
    public static MinigameBridge Instance { get; private set; }

    // ── State preserved across the scene boundary ─────────────────────────────
    private CrisisMarker _pendingMarker;       // the marker that launched this minigame
    private string       _citySceneName;       // so we know where to come back

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by CrisisMarker to launch a minigame.
    /// Pauses the global timer while the player is in the minigame scene.
    /// </summary>
    public void BeginMinigame(string minigameSceneName, CrisisMarker marker)
    {
        _pendingMarker = marker;
        _citySceneName = GameManager.Instance.citySceneName;

        // Pause the global game timer while in minigame
        GameManager.Instance.gameActive = false;

        SceneManager.LoadScene(minigameSceneName);
    }

    /// <summary>
    /// Called by the minigame's result script when the player finishes.
    ///
    /// Usage inside any minigame scene:
    ///     MinigameBridge.Instance.ReturnToCity(true);   // success
    ///     MinigameBridge.Instance.ReturnToCity(false);  // failure
    /// </summary>
    public void ReturnToCity(bool success)
    {
        // Resume the global timer
        GameManager.Instance.gameActive = true;

        // Load city scene, then notify the marker once it's loaded
        SceneManager.LoadScene(_citySceneName);
        StartCoroutine(NotifyMarkerAfterLoad(success));
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    System.Collections.IEnumerator NotifyMarkerAfterLoad(bool success)
    {
        // Wait one frame for the city scene to initialize
        yield return null;

        if (_pendingMarker != null)
        {
            _pendingMarker.OnMinigameReturned(success);
            _pendingMarker = null;
        }
    }
}
