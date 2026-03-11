using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public enum TreeState { Alive, OnFire, Burned }

/// <summary>
/// Individual tree tile for the Forest Fire minigame (Phase 1).
/// Can be Alive, OnFire, or Burned.
/// Click a tree that's OnFire to extinguish it before it spreads.
///
/// SETUP:
///   Make a prefab with:
///     • A SpriteRenderer (assign alive/fire/burned sprites)
///     • A CircleCollider2D
///     • This script attached
///   Camera needs Physics2DRaycaster + EventSystem in scene.
/// </summary>
public class TreeTile : MonoBehaviour, IPointerDownHandler
{
    [Header("Sprites")]
    public Sprite aliveSprite;
    public Sprite fireSprite;
    public Sprite burnedSprite;

    [Header("Spread Settings")]
    [Tooltip("Radius in world units to search for neighboring trees to spread to")]
    public float spreadRadius = 1.2f;
    [Tooltip("Max neighbors a single fire spreads to")]
    public int   maxSpreadTargets = 4;

    // ── State ─────────────────────────────────────────────────────────────────
    public TreeState State { get; private set; } = TreeState.Alive;

    private ForestFireMinigame _manager;
    private SpriteRenderer     _sr;
    private Coroutine          _spreadCoroutine;

    // ── Init ──────────────────────────────────────────────────────────────────
    public void Initialize(ForestFireMinigame manager)
    {
        _manager = manager;
        _sr      = GetComponent<SpriteRenderer>();
        SetVisual(TreeState.Alive);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Sets tree on fire. After spreadDelay seconds, spreads to neighbors if not clicked.</summary>
    public void IgniteWithSpread(float spreadDelay)
    {
        if (State != TreeState.Alive) return;

        State = TreeState.OnFire;
        SetVisual(TreeState.OnFire);

        _spreadCoroutine = StartCoroutine(SpreadAfterDelay(spreadDelay));
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (State != TreeState.OnFire) return;

        // Cancel the spread
        if (_spreadCoroutine != null)
        {
            StopCoroutine(_spreadCoroutine);
            _spreadCoroutine = null;
        }

        State = TreeState.Alive;
        SetVisual(TreeState.Alive);
        _manager.OnFireExtinguished();
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    IEnumerator SpreadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (State != TreeState.OnFire) yield break;

        // Burn this tree
        BurnDown();

        // Spread to nearby alive trees
        List<TreeTile> neighbors = GetNearbyAliveNeighbors();
        int spreadCount = Mathf.Min(neighbors.Count, maxSpreadTargets);

        for (int i = 0; i < spreadCount; i++)
            neighbors[i].BurnDown();

        _manager.OnTreesBurned(1 + spreadCount);
    }

    void BurnDown()
    {
        if (State == TreeState.Burned) return;

        if (_spreadCoroutine != null)
        {
            StopCoroutine(_spreadCoroutine);
            _spreadCoroutine = null;
        }

        State = TreeState.Burned;
        SetVisual(TreeState.Burned);
    }

    List<TreeTile> GetNearbyAliveNeighbors()
    {
        List<TreeTile> result  = new List<TreeTile>();
        List<TreeTile> allTrees = _manager.GetAllTrees();

        foreach (var tree in allTrees)
        {
            if (tree == null || tree == this) continue;
            if (tree.State != TreeState.Alive) continue;

            float dist = Vector2.Distance(transform.position, tree.transform.position);
            if (dist <= spreadRadius)
                result.Add(tree);
        }

        // Shuffle so spread targets are random
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }

    void SetVisual(TreeState state)
    {
        if (_sr == null) return;
        switch (state)
        {
            case TreeState.Alive:   _sr.sprite = aliveSprite;   _sr.color = Color.white; break;
            case TreeState.OnFire:  _sr.sprite = fireSprite;    _sr.color = Color.white; break;
            case TreeState.Burned:  _sr.sprite = burnedSprite;  _sr.color = Color.white; break;
        }
    }
}
