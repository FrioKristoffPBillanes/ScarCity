using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A node in the traffic waypoint graph.
///
/// SETUP:
///   1. Create empty GameObject, attach this script, place on road surface
///   2. Drag neighboring waypoints into nextWaypoints
///   3. For exit waypoints (off-screen road ends): tick "Is Exit"
///   4. For entry waypoints: leave Is Exit unchecked, assign to CarSpawner
///
/// TWO-LANE ROADS:
///   Place two waypoints side by side per road segment, ~1 unit apart.
///   Lane 1 flows one direction, Lane 2 flows the other.
///   Never connect Lane 1 to Lane 2 (no U-turns mid-road).
///   Only cross lanes at intersection waypoints.
/// </summary>
public class Waypoint : MonoBehaviour
{
    [Tooltip("Valid next waypoints from this node")]
    public List<Waypoint> nextWaypoints = new List<Waypoint>();

    [Tooltip("Tick this for waypoints at the city's road exits (off-screen). " +
             "Cars reaching this node will be despawned.")]
    public bool isExit = false;

    [Tooltip("Gizmo color in Scene view — use different colors per lane for clarity")]
    public Color gizmoColor = Color.yellow;

    // ── Editor Visuals ────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        Gizmos.color = isExit ? Color.red : gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.3f);

        Gizmos.color = Color.green;
        foreach (var next in nextWaypoints)
        {
            if (next == null) continue;
            Gizmos.DrawLine(transform.position, next.transform.position);

            // Arrowhead at 70% along the line
            Vector3 dir  = (next.transform.position - transform.position).normalized;
            Vector3 mid  = Vector3.Lerp(transform.position, next.transform.position, 0.7f);
            Gizmos.DrawLine(mid, mid + Quaternion.Euler(0,  30, 0) * (-dir) * 0.6f);
            Gizmos.DrawLine(mid, mid + Quaternion.Euler(0, -30, 0) * (-dir) * 0.6f);
        }
    }

    public Waypoint GetRandomNext()
    {
        var valid = nextWaypoints.FindAll(w => w != null);
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }
}
