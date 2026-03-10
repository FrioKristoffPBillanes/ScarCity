using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns CrisisMarkers at defined spawn points around the city.
/// Spawn rate increases over time to ramp up difficulty.
/// 
/// SETUP:
///   1. Create an empty GameObject named "CrisisSpawner" in your CityScene.
///   2. Attach this script.
///   3. In the Inspector, assign:
///        • crisisMarkerPrefab  → your CrisisMarker prefab
///        • spawnPoints         → empty GameObjects placed over crisis-eligible
///                                locations on your city map
///        • crisisTypes         → fill in the list below (one entry per minigame)
/// </summary>
public class CrisisSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn Points")]
    [Tooltip("The CrisisMarker prefab — the clickable icon that appears on the city")]
    public GameObject crisisMarkerPrefab;

    [Tooltip("Empty GameObjects placed at valid spawn locations on the city map")]
    public Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [Tooltip("Seconds between spawns at the start of the game")]
    public float initialSpawnInterval = 20f;

    [Tooltip("Seconds between spawns at the end of the game (ramps from initial → this)")]
    public float finalSpawnInterval = 8f;

    [Tooltip("Max crises active at the same time")]
    public int maxActiveCrises = 4;

    [Header("Crisis Types")]
    public List<CrisisType> crisisTypes = new List<CrisisType>();

    // ── Internal ──────────────────────────────────────────────────────────────
    private List<GameObject> _activeCrises = new List<GameObject>();
    private HashSet<int>     _occupiedSpawnIndices = new HashSet<int>();

    void Start()
    {
        if (!ValidateSetup()) return;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        // Wait until GameManager says the game is active
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.gameActive);

        while (GameManager.Instance.gameActive)
        {
            float t        = 1f - (GameManager.Instance.timeRemaining / GameManager.Instance.totalTime);
            float interval = Mathf.Lerp(initialSpawnInterval, finalSpawnInterval, t);

            yield return new WaitForSeconds(interval);

            if (GameManager.Instance.gameActive)
                TrySpawnCrisis();
        }
    }

    void TrySpawnCrisis()
    {
        // Clean up destroyed/resolved markers first
        _activeCrises.RemoveAll(c => c == null);

        if (_activeCrises.Count >= maxActiveCrises) return;

        int spawnIndex = GetFreeSpawnIndex();
        if (spawnIndex < 0) return; // no free spots

        CrisisType type = PickRandomCrisisType();
        if (type == null) return;

        GameObject marker = Instantiate(crisisMarkerPrefab, spawnPoints[spawnIndex].position, Quaternion.identity);
        CrisisMarker markerComponent = marker.GetComponent<CrisisMarker>();

        if (markerComponent != null)
        {
            markerComponent.Initialize(type, spawnIndex, this);
        }

        _activeCrises.Add(marker);
        _occupiedSpawnIndices.Add(spawnIndex);
    }

    /// <summary>Called by CrisisMarker when it is resolved or times out.</summary>
    public void OnCrisisRemoved(int spawnIndex)
    {
        _occupiedSpawnIndices.Remove(spawnIndex);
    }

    int GetFreeSpawnIndex()
    {
        // Build a shuffled list of indices that aren't occupied
        List<int> available = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!_occupiedSpawnIndices.Contains(i))
                available.Add(i);
        }
        if (available.Count == 0) return -1;
        return available[Random.Range(0, available.Count)];
    }

    CrisisType PickRandomCrisisType()
    {
        if (crisisTypes.Count == 0) return null;
        // Simple weighted-random could go here later; for now, uniform random
        return crisisTypes[Random.Range(0, crisisTypes.Count)];
    }

    bool ValidateSetup()
    {
        if (crisisMarkerPrefab == null) { Debug.LogError("[CrisisSpawner] crisisMarkerPrefab is not assigned!"); return false; }
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogError("[CrisisSpawner] No spawn points assigned!"); return false; }
        if (crisisTypes.Count == 0) { Debug.LogWarning("[CrisisSpawner] No crisis types defined — nothing will spawn."); }
        return true;
    }
}

/// <summary>
/// Data container describing one type of crisis/minigame.
/// Fill these in via the Inspector on your CrisisSpawner.
/// </summary>
[System.Serializable]
public class CrisisType
{
    [Tooltip("Internal ID — must exactly match the scene name in Build Settings")]
    public string minigameSceneName;

    [Tooltip("Display name shown on the crisis popup")]
    public string displayName;

    [Tooltip("Icon shown on the city map for this crisis type")]
    public Sprite mapIcon;

    [Tooltip("Seconds before this crisis auto-fails if unresolved")]
    public float timeLimit = 45f;

    [Tooltip("HP damage dealt to the city if this crisis is failed")]
    public int hpDamageOnFail = 1;
}
