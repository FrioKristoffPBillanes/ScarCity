using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns cars at city entry waypoints and destroys them when they reach
/// exit waypoints. Gives the illusion of traffic flowing through the city.
///
/// SETUP:
///   1. Attach to an empty GameObject called "CarSpawner"
///   2. Assign car prefabs (each needs CarAgent.cs attached)
///   3. Assign entry waypoints — these are your 3 off-screen road entry points
///   4. Mark exit waypoints on the Waypoint component itself (see Waypoint.cs)
///   5. For your already-placed cars: assign their starting waypoint manually
///      in the CarAgent Inspector, then set "Pre Placed" to true so the
///      spawner knows not to count them against the car limit
///
/// FLOW:
///   Entry waypoint → car spawns → drives through city → hits exit waypoint → despawns
///   → spawner waits spawnInterval seconds → spawns a new car at a random entry point
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [Header("Car Prefabs")]
    [Tooltip("All car variants — one is picked randomly per spawn")]
    public List<GameObject> carPrefabs = new List<GameObject>();

    [Header("Spawn Points")]
    [Tooltip("Waypoints at the city's road entry points (your 3 off-screen roads)")]
    public List<Waypoint> entryWaypoints = new List<Waypoint>();

    [Header("Settings")]
    [Tooltip("Max cars allowed in the city at once (including pre-placed ones)")]
    public int maxCars = 25;

    [Tooltip("Seconds between each new car spawn attempt")]
    public float spawnInterval = 3f;

    [Tooltip("How far off-screen the entry waypoints are — cars spawn here")]
    public float spawnOffsetFromEntry = 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private List<GameObject> _activeCars = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (!ValidateSetup()) return;
        StartCoroutine(SpawnLoop());
    }

    // ── Spawn Loop ────────────────────────────────────────────────────────────
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Clean up destroyed/null cars
            _activeCars.RemoveAll(c => c == null);

            if (_activeCars.Count < maxCars)
                SpawnCar();
        }
    }

    void SpawnCar()
    {
        Waypoint entry = entryWaypoints[Random.Range(0, entryWaypoints.Count)];
        if (entry == null) return;

        GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
        GameObject car    = Instantiate(prefab, entry.transform.position,
                                        Quaternion.identity, transform);

        CarAgent agent = car.GetComponent<CarAgent>();
        if (agent == null)
        {
            Debug.LogError($"[CarSpawner] '{prefab.name}' is missing CarAgent!");
            Destroy(car);
            return;
        }

        agent.Initialize(entry);
        agent.OnReachedExit += () => DespawnCar(car);

        _activeCars.Add(car);
    }

    void DespawnCar(GameObject car)
    {
        _activeCars.Remove(car);
        Destroy(car);
    }

    bool ValidateSetup()
    {
        if (carPrefabs.Count == 0)    { Debug.LogError("[CarSpawner] No car prefabs!"); return false; }
        if (entryWaypoints.Count == 0){ Debug.LogError("[CarSpawner] No entry waypoints!"); return false; }
        return true;
    }
}
