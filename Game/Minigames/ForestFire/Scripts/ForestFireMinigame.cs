using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase 1 of the Forest Fire minigame.
/// ~50 trees are randomly scattered on screen. Fires randomly spawn and
/// spread to adjacent trees if not clicked in time.
/// After 15 seconds, transitions to the TreeMemoryMinigame scene.
///
/// SETUP:
///   1. Create empty GameObject "ForestFireMinigame", attach this script.
///   2. Assign treePrefab (a TreeTile prefab), and UI references.
///   3. Set forestArea to match your scene's playfield size.
///   4. Add both scenes to Build Settings.
///   5. Camera: Orthographic, centered at (0,0,-10).
///   6. Add Physics2DRaycaster to camera + EventSystem in scene.
/// </summary>
public class ForestFireMinigame : MonoBehaviour
{
    public static ForestFireMinigame Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    public GameObject treePrefab;           // TreeTile prefab

    [Header("Forest Settings")]
    public int   treeCount       = 50;
    [Tooltip("Half-extents of the random scatter area in world units")]
    public Vector2 forestArea    = new Vector2(8f, 5f);
    [Tooltip("Minimum distance between trees")]
    public float minTreeSpacing  = 0.6f;

    [Header("Fire Settings")]
    [Tooltip("Seconds between random fire spawns")]
    public float fireSpawnInterval = 2f;
    [Tooltip("Seconds a fire burns before spreading to neighbors")]
    public float fireSpreadDelay   = 1.2f;

    [Header("Timer")]
    public float phaseDuration = 15f;

    [Header("UI")]
    public Image           hpBar;
    public TextMeshProUGUI hpLabel;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI phaseLabel;

    [Header("Scene Transition")]
    [Tooltip("Exact name of the Phase 2 scene in Build Settings")]
    public string phase2SceneName = "TreeMemoryMinigame";

    // ── Runtime ───────────────────────────────────────────────────────────────
    private List<TreeTile> _allTrees        = new List<TreeTile>();
    private int            _totalTrees      = 0;
    private int            _uncontainedFires = 0;
    private float          _timeRemaining;
    private bool           _phaseActive     = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnTrees();
        _totalTrees    = _allTrees.Count;
        _timeRemaining = phaseDuration;
        _phaseActive   = true;

        UpdateHPBar();
        StartCoroutine(FireSpawnLoop());
        StartCoroutine(PhaseTimer());

        if (phaseLabel != null)
            phaseLabel.text = "Phase 1: Fight the Fires!";
    }

    void Update()
    {
        if (!_phaseActive) return;

        _timeRemaining -= Time.deltaTime;
        _timeRemaining  = Mathf.Max(0f, _timeRemaining);

        if (timerLabel != null)
            timerLabel.text = Mathf.CeilToInt(_timeRemaining).ToString();
    }

    // ── Tree Spawning ─────────────────────────────────────────────────────────
    void SpawnTrees()
    {
        int   maxAttempts = treeCount * 10;
        int   attempts    = 0;
        List<Vector2> placed = new List<Vector2>();

        while (placed.Count < treeCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2 candidate = new Vector2(
                Random.Range(-forestArea.x, forestArea.x),
                Random.Range(-forestArea.y, forestArea.y)
            );

            bool tooClose = false;
            foreach (var p in placed)
            {
                if (Vector2.Distance(p, candidate) < minTreeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            placed.Add(candidate);
            GameObject go   = Instantiate(treePrefab, new Vector3(candidate.x, candidate.y, 0), Quaternion.identity);
            TreeTile   tile = go.GetComponent<TreeTile>();
            tile.Initialize(this);
            _allTrees.Add(tile);
        }
    }

    // ── Fire Loop ─────────────────────────────────────────────────────────────
    IEnumerator FireSpawnLoop()
    {
        while (_phaseActive)
        {
            yield return new WaitForSeconds(fireSpawnInterval);
            if (_phaseActive)
                SpawnRandomFire();
        }
    }

    void SpawnRandomFire()
    {
        // Pick a random healthy tree
        List<TreeTile> healthy = _allTrees.FindAll(t => t != null && t.State == TreeState.Alive);
        if (healthy.Count == 0) return;

        TreeTile target = healthy[Random.Range(0, healthy.Count)];
        target.IgniteWithSpread(fireSpreadDelay);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by TreeTile when a fire is clicked and extinguished.</summary>
    public void OnFireExtinguished()
    {
        UpdateHPBar();
    }

    /// <summary>Called by TreeTile when fire spreads and burns down trees.</summary>
    public void OnTreesBurned(int count)
    {
        _uncontainedFires++;
        UpdateHPBar();
    }

    // ── HP Bar ────────────────────────────────────────────────────────────────
    void UpdateHPBar()
    {
        int alive = _allTrees.FindAll(t => t != null && t.State == TreeState.Alive).Count;
        float ratio = _totalTrees > 0 ? (float)alive / _totalTrees : 0f;

        if (hpBar   != null) hpBar.fillAmount = ratio;
        if (hpLabel != null) hpLabel.text     = $"Forest HP: {Mathf.RoundToInt(ratio * 100)}%";
    }

    // ── Phase End ─────────────────────────────────────────────────────────────
    IEnumerator PhaseTimer()
    {
        yield return new WaitForSeconds(phaseDuration);
        EndPhase();
    }

    void EndPhase()
    {
        _phaseActive = false;

        // Pass uncontained fires count to Phase 2 via static bridge
        ForestFireBridge.UncontainedFires = _uncontainedFires;

        if (phaseLabel != null)
            phaseLabel.text = "Phase 2: Replant the Forest!";

        // Small delay so the player sees the transition message
        StartCoroutine(TransitionDelay());
    }

    IEnumerator TransitionDelay()
    {
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(phase2SceneName);
    }

    /// <summary>Returns a list of all living tree world positions (used for adjacency checks).</summary>
    public List<TreeTile> GetAllTrees() => _allTrees;
}
