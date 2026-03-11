using UnityEngine;
using UnityEngine.EventSystems;

public enum MemoryTileState { Empty, Highlighted, PlayerSelected, Correct, Wrong }

/// <summary>
/// One tile in the 5x5 memory grid for Phase 2.
/// Handles visual state and click input.
///
/// SETUP:
///   Prefab needs:
///     • SpriteRenderer
///     • BoxCollider2D
///     • This script
///   Camera needs Physics2DRaycaster + EventSystem.
/// </summary>
public class MemoryTile : MonoBehaviour, IPointerDownHandler
{
    [Header("Colors")]
    public Color emptyColor       = new Color(0.3f, 0.5f, 0.2f, 0.5f);  // dim green
    public Color highlightColor   = new Color(0.2f, 1f,   0.3f, 1f);    // bright green (shown during memorize phase)
    public Color selectedColor    = new Color(0.9f, 0.9f, 0.2f, 1f);    // yellow (player clicked)
    public Color correctColor     = new Color(0.2f, 0.9f, 0.3f, 1f);    // green (correct reveal)
    public Color wrongColor       = new Color(1f,   0.2f, 0.2f, 1f);    // red (wrong reveal)

    // ── State ─────────────────────────────────────────────────────────────────
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool IsTarget       { get; private set; }  // is this tile part of the pattern?
    public bool IsSelected     { get; private set; }  // did the player click it?

    private TreeMemoryMinigame _manager;
    private SpriteRenderer     _sr;
    private bool               _inputEnabled = false;

    // ── Init ──────────────────────────────────────────────────────────────────
    public void Initialize(int row, int col, TreeMemoryMinigame manager)
    {
        Row      = row;
        Col      = col;
        _manager = manager;
        _sr      = GetComponent<SpriteRenderer>();
        SetState(MemoryTileState.Empty);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void SetAsTarget(bool isTarget)
    {
        IsTarget = isTarget;
    }

    public void ShowHighlight(bool show)
    {
        SetState(show && IsTarget ? MemoryTileState.Highlighted : MemoryTileState.Empty);
    }

    public void EnableInput(bool enabled)
    {
        _inputEnabled = enabled;
        IsSelected    = false;
        if (!enabled) SetState(MemoryTileState.Empty);
    }

    public void RevealResult()
    {
        if (IsTarget && IsSelected)  SetState(MemoryTileState.Correct);
        else if (IsTarget)           SetState(MemoryTileState.Highlighted); // missed
        else if (IsSelected)         SetState(MemoryTileState.Wrong);       // wrong pick
        else                         SetState(MemoryTileState.Empty);
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_inputEnabled) return;
        if (IsSelected)     return; // can't deselect

        IsSelected = true;
        SetState(MemoryTileState.PlayerSelected);
        _manager.OnTileSelected(this);
    }

    // ── Visuals ───────────────────────────────────────────────────────────────
    void SetState(MemoryTileState state)
    {
        if (_sr == null) return;
        switch (state)
        {
            case MemoryTileState.Empty:          _sr.color = emptyColor;      break;
            case MemoryTileState.Highlighted:    _sr.color = highlightColor;  break;
            case MemoryTileState.PlayerSelected: _sr.color = selectedColor;   break;
            case MemoryTileState.Correct:        _sr.color = correctColor;    break;
            case MemoryTileState.Wrong:          _sr.color = wrongColor;      break;
        }
    }
}
