using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Earthquake Triangulation minigame.
///
/// How success is determined:
///   1. Player draws 3 circles (one per station)
///   2. The best intersection point from each circle PAIR is found (3 points total)
///   3. The circumcircle of those 3 points is calculated
///   4. If the circumcircle's CENTER is within acceptableErrorKm of the actual
///      epicenter, the player succeeds
///
/// This is forgiving by design — players don't need pixel-perfect radii,
/// just roughly correct ones. The circumcircle averages out the error.
/// </summary>
public class EarthquakeMinigame : MinigameResult
{
    public static EarthquakeMinigame Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    public GameObject stationPrefab;
    public GameObject intersectionDotPrefab;

    [Tooltip("Prefab for the result circumcircle (a LineRenderer-based circle)")]
    public GameObject circumcirclePrefab;

    [Header("Station Setup")]
    public Vector2[] stationPositions = new Vector2[]
    {
        new Vector2(-6f,  4f),
        new Vector2( 5f,  5f),
        new Vector2( 0f, -6f)
    };

    public Color[] stationColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f),
        new Color(1f,   0.6f, 0.2f),
        new Color(0.6f, 1f,   0.3f)
    };

    [Header("Epicenter")]
    public Vector2 actualEpicenter   = new Vector2(1.36f, 0.87f);
    public float   acceptableErrorKm = 2.5f;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public GameObject      submitButton;
    public GameObject      resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Debug")]
    public bool       showEpicenterInEditor = true;
    public GameObject debugEpicenterMarker;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private SeismicStation[] _stations;
    private int              _activeStationIndex = 0;
    private List<GameObject> _intersectionDots   = new List<GameObject>();
    private GameObject       _circumcircleObj;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake() { Instance = this; }

    void Start()
    {
        SpawnStations();
        UpdateUI();
        if (submitButton != null) submitButton.SetActive(false);
        if (resultPanel  != null) resultPanel.SetActive(false);

        if (showEpicenterInEditor && debugEpicenterMarker != null)
            debugEpicenterMarker.transform.position = new Vector3(actualEpicenter.x, actualEpicenter.y, 0);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public bool IsThisStationActive(SeismicStation station)
    {
        if (_activeStationIndex >= _stations.Length) return false;
        return _stations[_activeStationIndex] == station;
    }

    public void OnSubmitPressed()
    {
        // Get the best intersection point from each circle pair
        Vector2? ab = BestIntersectionForPair(0, 1);
        Vector2? bc = BestIntersectionForPair(1, 2);
        Vector2? ac = BestIntersectionForPair(0, 2);

        if (ab == null || bc == null || ac == null)
        {
            ShowStatus("Some circles don't intersect — try adjusting your radii!");
            return;
        }

        // Calculate circumcircle of the 3 best intersection points
        Vector2? circumcenter = Circumcenter(ab.Value, bc.Value, ac.Value);

        if (circumcenter == null)
        {
            // Points are collinear — fall back to centroid
            circumcenter = (ab.Value + bc.Value + ac.Value) / 3f;
        }

        // Draw the circumcircle visually
        float circumradius = Vector2.Distance(circumcenter.Value, ab.Value);
        DrawCircumcircle(circumcenter.Value, circumradius);

        // Judge success based on circumcenter vs actual epicenter
        float error = Vector2.Distance(circumcenter.Value, actualEpicenter);
        ShowResult(error <= acceptableErrorKm, circumcenter.Value, error);
    }

    // ── Spawning ──────────────────────────────────────────────────────────────
    void SpawnStations()
    {
        _stations = new SeismicStation[stationPositions.Length];
        string[] names = { "Station A", "Station B", "Station C" };

        for (int i = 0; i < stationPositions.Length; i++)
        {
            Vector3    pos = new Vector3(stationPositions[i].x, stationPositions[i].y, 0);
            GameObject go  = Instantiate(stationPrefab, pos, Quaternion.identity);
            go.name = names[i];

            SeismicStation s = go.GetComponent<SeismicStation>();
            s.Initialize(names[i], stationColors[i % stationColors.Length], Camera.main);
            s.OnCircleConfirmed += HandleCircleConfirmed;
            _stations[i] = s;
        }
    }

    // ── Station Flow ──────────────────────────────────────────────────────────
    void HandleCircleConfirmed(SeismicStation station)
    {
        _activeStationIndex++;
        ClearIntersectionDots();

        if (_activeStationIndex >= _stations.Length)
        {
            ShowBestIntersectionDots();
            if (submitButton != null) submitButton.SetActive(true);
            ShowStatus("All stations set! Look for where the circles cluster, then submit.");
        }
        else
        {
            UpdateUI();
        }
    }

    // ── Intersection Math ─────────────────────────────────────────────────────

    /// <summary>
    /// From all intersection points between two circles, returns the one
    /// closest to the third station's circle edge (most likely the epicenter side).
    /// </summary>
    Vector2? BestIntersectionForPair(int i, int j)
    {
        var points = CircleIntersections(
            _stations[i].transform.position, _stations[i].RadiusKm,
            _stations[j].transform.position, _stations[j].RadiusKm);

        if (points.Count == 0) return null;
        if (points.Count == 1) return points[0];

        // Pick the point closest to the third station's circle
        int     k        = 3 - i - j; // the index of the third station (0+1+2=3)
        Vector2 thirdPos = _stations[k].transform.position;
        float   thirdR   = _stations[k].RadiusKm;

        // Closest to the third circle's edge
        float   best  = float.MaxValue;
        Vector2 pick  = points[0];

        foreach (var p in points)
        {
            float dist = Mathf.Abs(Vector2.Distance(p, thirdPos) - thirdR);
            if (dist < best) { best = dist; pick = p; }
        }

        return pick;
    }

    /// <summary>Returns the 0, 1, or 2 intersection points of two circles.</summary>
    List<Vector2> CircleIntersections(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        var   result = new List<Vector2>();
        float d      = Vector2.Distance(c1, c2);

        if (d > r1 + r2 || d < Mathf.Abs(r1 - r2) || d == 0) return result;

        float   a    = (r1 * r1 - r2 * r2 + d * d) / (2f * d);
        float   h    = Mathf.Sqrt(Mathf.Max(0, r1 * r1 - a * a));
        Vector2 mid  = c1 + a * (c2 - c1) / d;
        Vector2 perp = new Vector2(-(c2.y - c1.y), c2.x - c1.x) / d;

        result.Add(mid + h * perp);
        if (h > 0.001f) result.Add(mid - h * perp);

        return result;
    }

    /// <summary>
    /// Returns the circumcenter of triangle ABC — the point equidistant
    /// from all 3 vertices. Returns null if points are collinear.
    /// </summary>
    Vector2? Circumcenter(Vector2 a, Vector2 b, Vector2 c)
    {
        float ax = a.x, ay = a.y;
        float bx = b.x, by = b.y;
        float cx = c.x, cy = c.y;

        float D = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Mathf.Abs(D) < 0.0001f) return null; // collinear

        float ux = ((ax * ax + ay * ay) * (by - cy)
                  + (bx * bx + by * by) * (cy - ay)
                  + (cx * cx + cy * cy) * (ay - by)) / D;

        float uy = ((ax * ax + ay * ay) * (cx - bx)
                  + (bx * bx + by * by) * (ax - cx)
                  + (cx * cx + cy * cy) * (bx - ax)) / D;

        return new Vector2(ux, uy);
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    /// <summary>Shows one dot per circle pair at the best intersection point.</summary>
    void ShowBestIntersectionDots()
    {
        if (intersectionDotPrefab == null) return;

        int[,] pairs = { {0,1}, {1,2}, {0,2} };
        for (int p = 0; p < 3; p++)
        {
            Vector2? pt = BestIntersectionForPair(pairs[p,0], pairs[p,1]);
            if (pt == null) continue;

            GameObject dot = Instantiate(intersectionDotPrefab,
                new Vector3(pt.Value.x, pt.Value.y, 0), Quaternion.identity);
            _intersectionDots.Add(dot);
        }
    }

    void DrawCircumcircle(Vector2 center, float radius)
    {
        if (circumcirclePrefab == null) return;

        if (_circumcircleObj != null) Destroy(_circumcircleObj);
        _circumcircleObj = Instantiate(circumcirclePrefab,
            new Vector3(center.x, center.y, 0), Quaternion.identity);

        LineRenderer lr = _circumcircleObj.GetComponent<LineRenderer>();
        if (lr == null) return;

        int   segments  = 128;
        float angleStep = 360f / segments;
        lr.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius,
                0));
        }
    }

    void ClearIntersectionDots()
    {
        foreach (var d in _intersectionDots) if (d != null) Destroy(d);
        _intersectionDots.Clear();
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    void UpdateUI()
    {
        if (_activeStationIndex >= _stations.Length) return;
        string name = _stations[_activeStationIndex].StationName;

        if (instructionText != null)
            instructionText.text =
                $"Click and drag from <b>{name}</b> to set its P-wave detection radius.";

        ShowStatus($"Waiting for {name}...");
    }

    void ShowStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void ShowResult(bool success, Vector2 circumcenter, float errorKm)
    {
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = success
                ? $"EPICENTER LOCATED!\nEstimated center: ({circumcenter.x:F1}, {circumcenter.y:F1})\nError: {errorKm:F2} km"
                : $"NOT CLOSE ENOUGH.\nEstimated center was {errorKm:F2} km off.\nAcceptable range: {acceptableErrorKm} km - try again!";
        }

        Invoke(success ? nameof(Succeed) : nameof(Fail), 2.5f);
    }
}