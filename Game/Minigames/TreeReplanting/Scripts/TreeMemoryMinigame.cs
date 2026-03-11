using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase 2 of the Forest Fire minigame — memory replanting.
///
/// Gameplay:
///   - 5x5 grid of tile slots
///   - Each round, N tiles are highlighted (the pattern to memorize)
///   - After a short display window, tiles go dark
///   - Player must click exactly the correct tiles from memory
///   - 100% correct → next round (N+1 tiles)
///   - Any mistake → Fail()
///   - Survive until target tile count is reached → Succeed()
///
/// Target tile count = Mathf.Clamp(4 + ForestFireBridge.UncontainedFires * 2, 4, 12)
/// Rounds start at 1 tile, increase by 1 per round.
///
/// SETUP:
///   1. Create empty GameObject "TreeMemoryMinigame", attach this script.
///   2. Assign memoryTilePrefab and UI references.
///   3. Set tileSpacing to match your prefab size.
///   4. Camera: Orthographic, centered (0,0,-10).
///   5. Physics2DRaycaster on camera + EventSystem in scene.
///   6. This inherits MinigameResult — Succeed()/Fail() auto-return to city.
/// </summary>
public class TreeMemoryMinigame : MinigameResult
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grid")]
    public GameObject memoryTilePrefab;
    public int   gridSize    = 5;
    public float tileSpacing = 1.2f;

    [Header("Timing")]
    [Tooltip("Seconds the pattern is shown before hiding")]
    public float showPatternDuration = 2.5f;
    [Tooltip("Seconds to show result before moving to next round")]
    public float resultDisplayDuration = 1.2f;

    [Header("UI")]
    public TextMeshProUGUI roundLabel;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI phaseLabel;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private MemoryTile[,] _grid;
    private List<MemoryTile> _targetTiles  = new List<MemoryTile>();
    private List<MemoryTile> _selectedTiles = new List<MemoryTile>();
    private int  _currentRoundTiles = 1;   // starts at 1, increases each round
    private int  _targetTileCount;          // calculated from Phase 1 performance
    private bool _waitingForInput  = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        // Calculate difficulty from Phase 1
        _targetTileCount   = Mathf.Clamp(4 + ForestFireBridge.UncontainedFires * 2, 4, 12);
        _currentRoundTiles = 1;

        BuildGrid();
        UpdateRoundLabel();

        if (phaseLabel != null)
            phaseLabel.text = "Phase 2: Remember Where to Plant!";

        StartCoroutine(RunRound());
    }

    // ── Grid Building ─────────────────────────────────────────────────────────
    void BuildGrid()
    {
        _grid = new MemoryTile[gridSize, gridSize];

        float offset = (gridSize - 1) * tileSpacing * 0.5f;

        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                float x = c * tileSpacing - offset;
                float y = r * tileSpacing - offset;

                GameObject go   = Instantiate(memoryTilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                MemoryTile tile = go.GetComponent<MemoryTile>();
                tile.Initialize(r, c, this);
                _grid[r, c] = tile;
            }
        }
    }

    // ── Round Flow ────────────────────────────────────────────────────────────
    IEnumerator RunRound()
    {
        _waitingForInput = false;
        _selectedTiles.Clear();

        SetAllInputEnabled(false);
        PickNewTargets(_currentRoundTiles);
        ShowPattern(true);

        if (instructionText != null)
            instructionText.text = $"Memorize {_currentRoundTiles} tile{(_currentRoundTiles > 1 ? "s" : "")}!";
        if (statusText != null)
            statusText.text = "Remember the highlighted tiles...";

        yield return new WaitForSeconds(showPatternDuration);

        // Hide pattern
        ShowPattern(false);

        if (instructionText != null)
            instructionText.text = "Now click the correct tiles!";
        if (statusText != null)
            statusText.text = $"Select {_currentRoundTiles} tile{(_currentRoundTiles > 1 ? "s" : "")}";

        SetAllInputEnabled(true);
        _waitingForInput = true;
    }

    void PickNewTargets(int count)
    {
        // Reset all targets
        foreach (var tile in _grid)
            tile.SetAsTarget(false);

        _targetTiles.Clear();

        // Pick 'count' random unique tiles
        List<MemoryTile> pool = new List<MemoryTile>();
        foreach (var tile in _grid) pool.Add(tile);

        // Shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < count && i < pool.Count; i++)
        {
            pool[i].SetAsTarget(true);
            _targetTiles.Add(pool[i]);
        }
    }

    void ShowPattern(bool show)
    {
        foreach (var tile in _grid)
            tile.ShowHighlight(show);
    }

    void SetAllInputEnabled(bool enabled)
    {
        foreach (var tile in _grid)
            tile.EnableInput(enabled);
    }

    // ── Input Handling ────────────────────────────────────────────────────────

    /// <summary>Called by MemoryTile when the player clicks it.</summary>
    public void OnTileSelected(MemoryTile tile)
    {
        if (!_waitingForInput) return;

        _selectedTiles.Add(tile);

        // Wrong tile immediately — fail
        if (!tile.IsTarget)
        {
            _waitingForInput = false;
            StartCoroutine(HandleRoundResult(false));
            return;
        }

        // Check if player has selected all correct tiles
        if (_selectedTiles.Count == _targetTiles.Count)
        {
            _waitingForInput = false;
            StartCoroutine(HandleRoundResult(true));
        }
        else
        {
            // Update status
            int remaining = _targetTiles.Count - _selectedTiles.Count;
            if (statusText != null)
                statusText.text = $"{remaining} more tile{(remaining > 1 ? "s" : "")} to go...";
        }
    }

    IEnumerator HandleRoundResult(bool success)
    {
        SetAllInputEnabled(false);

        // Reveal correct/wrong tiles
        foreach (var tile in _grid)
            tile.RevealResult();

        if (success)
        {
            if (statusText != null)
                statusText.text = "✓ Correct!";

            yield return new WaitForSeconds(resultDisplayDuration);

            // Check win condition
            if (_currentRoundTiles >= _targetTileCount)
            {
                // Player survived all rounds
                Succeed();
            }
            else
            {
                _currentRoundTiles++;
                UpdateRoundLabel();
                StartCoroutine(RunRound());
            }
        }
        else
        {
            if (statusText != null)
                statusText.text = "✗ Wrong tile — mission failed!";

            yield return new WaitForSeconds(resultDisplayDuration);
            Fail();
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    void UpdateRoundLabel()
    {
        if (roundLabel != null)
            roundLabel.text = $"Round {_currentRoundTiles} / {_targetTileCount}";
    }
}
