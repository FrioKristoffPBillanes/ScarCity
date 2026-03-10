using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main controller for the Earthquake Triangulation minigame.
///
/// Gameplay:
///   - 3 seismic stations are placed on the grid
///   - Player draws a radius line from each station (the P-wave travel distance)
///   - Where all 3 circles intersect = the epicenter
///   - Player clicks "Submit" — accuracy is measured against the actual epicenter
///   - Close enough = Succeed(), too far = Fail()
///
/// SETUP:
///   1. Create an empty GameObject "EarthquakeMinigame", attach this script
///   2. Assign stationPrefab, intersectionDotPrefab, and UI references
///   3. This script inherits MinigameResult — Succeed()/Fail() auto-return to city
/// </summary>
public class EarthquakeMinigame : MinigameResult
{
    public static EarthquakeMinigame Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("The SeismicStation prefab")]
    public GameObject stationPrefab;

    [Tooltip("Small dot sprite shown at circle intersection points")]
    public GameObject intersectionDotPrefab;

    [Header("Station Setup")]
    [Tooltip("Positions of the 3 stations in km (world units)")]
    public Vector2[] stationPositions = new Vector2[]
    {
        new Vector2(-6f,  4f),
        new Vector2( 5f,  5f),
        new Vector2( 0f, -6f)
    };

    public Color[] stationColors = new Color[]
    {
        new Color(0.2f, 0.8f, 1f),   // cyan
        new Color(1f, 0.6f, 0.2f),   // orange
        new Color(0.6f, 1f, 0.3f)    // green
    };

    [Header("Actual Epicenter (set per puzzle)")]
    [Tooltip("Where the real epicenter is — hidden from player")]
    public Vector2 actualEpicenter = new Vector2(1.5f, 1.0f);

    [Tooltip("How close the intersection needs to be to count as correct (km)")]
    public float acceptableErrorKm = 1.5f;

    [Header("UI")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public GameObject      submitButton;     // active once all 3 stations locked
    public GameObject      resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Debug (editor only)")]
    [Tooltip("Show a marker at the actual epicenter for testing")]
    public bool showEpicenterInEditor = true;
    public GameObject debugEpicenterMarker;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private SeismicStation[] _stations;
    private int              _activeStationIndex = 0;
    private List<GameObject> _intersectionDots = new List<GameObject>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnStations();
        UpdateUI();

        if (submitButton != null) submitButton.SetActive(false);
        if (resultPanel  != null) resultPanel.SetActive(false);

        // Debug: show real epicenter location in editor
        if (showEpicenterInEditor && debugEpicenterMarker != null)
            debugEpicenterMarker.transform.position = new Vector3(actualEpicenter.x, actualEpicenter.y, 0);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Used by SeismicStation to check if it should respond to input.</summary>
    public bool IsThisStationActive(SeismicStation station)
    {
        if (_activeStationIndex >= _stations.Length) return false;
        return _stations[_activeStationIndex] == station;
    }

    /// <summary>Called by the Submit UI button.</summary>
    public void OnSubmitPressed()
    {
        // Find the best intersection point from all circle pairs
        Vector2? intersectionResult = FindBestIntersection();

        if (intersectionResult == null)
        {
            ShowStatus("Circles don't intersect at a common point — try again!");
            return;
        }

        Vector2 guessedEpicenter = intersectionResult.Value;
        float   error            = Vector2.Distance(guessedEpicenter, actualEpicenter);

        ShowResult(error <= acceptableErrorKm, guessedEpicenter, error);
    }

    // ── Station Spawning ──────────────────────────────────────────────────────
    void SpawnStations()
    {
        _stations = new SeismicStation[stationPositions.Length];
        string[] names = { "Station A", "Station B", "Station C" };

        for (int i = 0; i < stationPositions.Length; i++)
        {
            Vector3    pos = new Vector3(stationPositions[i].x, stationPositions[i].y, 0);
            GameObject go  = Instantiate(stationPrefab, pos, Quaternion.identity);
            go.name        = names[i];

            SeismicStation station = go.GetComponent<SeismicStation>();
            station.Initialize(names[i], stationColors[i % stationColors.Length], Camera.main);
            station.OnCircleConfirmed += HandleCircleConfirmed;

            _stations[i] = station;
        }
    }

    // ── Station Interaction ───────────────────────────────────────────────────
    void HandleCircleConfirmed(SeismicStation station)
    {
        _activeStationIndex++;
        ClearIntersectionDots();

        if (_activeStationIndex >= _stations.Length)
        {
            // All 3 done — check intersections
            ShowAllIntersectionDots();
            if (submitButton != null) submitButton.SetActive(true);
            ShowStatus("All stations set! Check the intersection, then submit.");
        }
        else
        {
            UpdateUI();
        }
    }

    // ── Intersection Math ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds the point where all 3 circles come closest together.
    /// Checks all 3 circle pairs, averages their intersection points.
    /// Returns null if circles don't intersect.
    /// </summary>
    Vector2? FindBestIntersection()
    {
        var candidates = new List<Vector2>();

        // Check each pair of circles
        for (int i = 0; i < _stations.Length; i++)
        {
            for (int j = i + 1; j < _stations.Length; j++)
            {
                var points = CircleIntersections(
                    _stations[i].transform.position, _stations[i].RadiusKm,
                    _stations[j].transform.position, _stations[j].RadiusKm
                );
                candidates.AddRange(points);
            }
        }

        if (candidates.Count == 0) return null;

        // Find the candidate point that minimizes total distance to all 3 circles
        Vector2 best      = candidates[0];
        float   bestScore = float.MaxValue;

        foreach (var candidate in candidates)
        {
            float score = 0f;
            foreach (var station in _stations)
            {
                float distToCenter = Vector2.Distance(candidate, station.transform.position);
                float error        = Mathf.Abs(distToCenter - station.RadiusKm);
                score += error;
            }
            if (score < bestScore)
            {
                bestScore = score;
                best      = candidate;
            }
        }

        return best;
    }

    /// <summary>Returns the 0, 1, or 2 intersection points of two circles.</summary>
    List<Vector2> CircleIntersections(Vector2 c1, float r1, Vector2 c2, float r2)
    {
        var result = new List<Vector2>();
        float d = Vector2.Distance(c1, c2);

        // No intersection cases
        if (d > r1 + r2)        return result; // too far apart
        if (d < Mathf.Abs(r1 - r2)) return result; // one inside the other
        if (d == 0)              return result; // same center

        float a = (r1 * r1 - r2 * r2 + d * d) / (2f * d);
        float h = Mathf.Sqrt(Mathf.Max(0, r1 * r1 - a * a));

        Vector2 midpoint = c1 + a * (c2 - c1) / d;
        Vector2 perp     = new Vector2(-(c2.y - c1.y), c2.x - c1.x) / d;

        result.Add(midpoint + h * perp);
        if (h > 0.001f)
            result.Add(midpoint - h * perp);

        return result;
    }

    // ── Intersection Dots ─────────────────────────────────────────────────────
    void ShowAllIntersectionDots()
    {
        if (intersectionDotPrefab == null) return;

        for (int i = 0; i < _stations.Length; i++)
        {
            for (int j = i + 1; j < _stations.Length; j++)
            {
                var points = CircleIntersections(
                    _stations[i].transform.position, _stations[i].RadiusKm,
                    _stations[j].transform.position, _stations[j].RadiusKm
                );

                foreach (var pt in points)
                {
                    GameObject dot = Instantiate(
                        intersectionDotPrefab,
                        new Vector3(pt.x, pt.y, 0),
                        Quaternion.identity
                    );
                    _intersectionDots.Add(dot);
                }
            }
        }
    }

    void ClearIntersectionDots()
    {
        foreach (var dot in _intersectionDots)
            if (dot != null) Destroy(dot);
        _intersectionDots.Clear();
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    void UpdateUI()
    {
        if (_activeStationIndex >= _stations.Length) return;
        string name = _stations[_activeStationIndex].StationName;

        if (instructionText != null)
            instructionText.text = $"Click and drag from {name} to set its detection radius.";

        ShowStatus($"Waiting for {name}...");
    }

    void ShowStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void ShowResult(bool success, Vector2 guess, float errorKm)
    {
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            if (success)
                resultText.text = $"✓ Epicenter located!\nError: {errorKm:F2} km — within acceptable range.";
            else
                resultText.text = $"✗ Too far off.\nYour guess was {errorKm:F2} km from the actual epicenter.\nAcceptable range: {acceptableErrorKm} km";
        }

        // Delay slightly so player can read the result before scene transitions
        Invoke(success ? nameof(Succeed) : nameof(Fail), 2.5f);
    }
}
