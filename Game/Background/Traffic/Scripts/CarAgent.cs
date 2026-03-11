using UnityEngine;
using System;

/// <summary>
/// Drives a car along the waypoint graph.
/// Fires OnReachedExit when it hits an exit waypoint so CarSpawner can despawn it.
///
/// SETUP FOR PRE-PLACED CARS (already in your scene):
///   1. Attach this script to each car
///   2. Assign "Start Waypoint" in the Inspector to the nearest waypoint
///   3. The car will start driving immediately on Play
///   4. Pre-placed cars are NOT tracked by CarSpawner — they just drive forever
///      and loop back if they hit an exit (see loopOnExit toggle)
/// </summary>
public class CarAgent : MonoBehaviour
{
    [Header("Starting Waypoint")]
    [Tooltip("The waypoint this car starts from. " +
             "For pre-placed cars, assign this manually in the Inspector.")]
    public Waypoint startWaypoint;

    [Header("Movement")]
    public float speed               = 8f;
    public float waypointReachDist   = 0.5f;
    public float rotationSpeed       = 5f;
    public float minSpeedMultiplier  = 0.85f;
    public float maxSpeedMultiplier  = 1.15f;

    [Header("Pre-Placed Car Settings")]
    [Tooltip("If true, this car was manually placed in the scene. " +
             "It will loop back to its start waypoint on exit instead of despawning.")]
    public bool isPrePlaced = false;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired when the car reaches an exit waypoint. CarSpawner listens to this.</summary>
    public event Action OnReachedExit;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Waypoint _currentTarget;
    private float    _actualSpeed;
    private bool     _active = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        // Auto-start for pre-placed cars that have a waypoint assigned
        if (isPrePlaced && startWaypoint != null)
            Initialize(startWaypoint);
    }

    void Update()
    {
        if (!_active || _currentTarget == null) return;
        MoveTowardTarget();
        RotateTowardTarget();
        CheckIfReached();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by CarSpawner (or Start for pre-placed cars).</summary>
    public void Initialize(Waypoint start)
    {
        _currentTarget = start;
        _actualSpeed   = speed * UnityEngine.Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
        _active        = true;

        if (!isPrePlaced)
            transform.position = start.transform.position;
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    void MoveTowardTarget()
    {
        Vector3 dir = (_currentTarget.transform.position - transform.position).normalized;
        transform.position += dir * _actualSpeed * Time.deltaTime;
    }

    void RotateTowardTarget()
    {
        Vector3 dir = _currentTarget.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        target = Quaternion.Euler(0, target.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    void CheckIfReached()
    {
        if (Vector3.Distance(transform.position, _currentTarget.transform.position) > waypointReachDist)
            return;

        if (_currentTarget.isExit)
        {
            if (isPrePlaced)
            {
                // Loop back to start instead of despawning
                _currentTarget = startWaypoint;
            }
            else
            {
                // Notify spawner to despawn this car
                _active = false;
                OnReachedExit?.Invoke();
            }
            return;
        }

        Waypoint next = _currentTarget.GetRandomNext();

        if (next == null)
        {
            Debug.LogWarning($"[CarAgent] '{name}' reached a dead-end waypoint with no next! " +
                              "Check your waypoint connections.");
            _active = false;
            return;
        }

        _currentTarget = next;
    }
}
