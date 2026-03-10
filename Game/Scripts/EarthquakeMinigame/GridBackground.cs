using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Procedurally generates a grid in world space where 1 Unity unit = 1 km.
/// Attach to an empty GameObject in your EarthquakeTriangulation scene.
///
/// SETUP:
///   1. Attach this script to an empty GameObject called "Grid"
///   2. Assign lineMaterial (a simple Sprites/Default material works fine)
///   3. Assign labelPrefab (a World Space TextMeshPro prefab)
///   4. Camera should be Orthographic at position (0, 0, -10)
/// </summary>
public class GridBackground : MonoBehaviour
{
    [Header("Grid Dimensions")]
    [Tooltip("Half-width of the grid in km (so gridRadius=10 makes a 20x20 grid)")]
    public int gridRadius = 10;

    [Tooltip("Spacing between grid lines in km")]
    public int gridStep = 1;

    [Header("Visuals")]
    public Material lineMaterial;

    [Tooltip("Color of minor grid lines")]
    public Color minorLineColor = new Color(0.7f, 0.8f, 0.7f, 0.3f);

    [Tooltip("Color of the X and Y axes")]
    public Color axisColor = new Color(0.3f, 0.5f, 0.3f, 0.8f);

    [Tooltip("Width of minor grid lines")]
    public float minorLineWidth = 0.03f;

    [Tooltip("Width of axis lines")]
    public float axisLineWidth = 0.07f;

    [Header("Labels")]
    [Tooltip("TextMeshPro prefab (World Space canvas or TextMeshPro 3D)")]
    public GameObject labelPrefab;

    [Tooltip("Font size for km coordinate labels")]
    public float labelSize = 0.4f;

    public Color labelColor = new Color(0.2f, 0.4f, 0.2f, 0.9f);

    // Tracks spawned objects for cleanup
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        for (int i = -gridRadius; i <= gridRadius; i += gridStep)
        {
            bool isAxis = (i == 0);

            // Vertical line (X = i)
            CreateLine(
                new Vector3(i, -gridRadius, 0),
                new Vector3(i, gridRadius, 0),
                isAxis ? axisColor : minorLineColor,
                isAxis ? axisLineWidth : minorLineWidth
            );

            // Horizontal line (Y = i)
            CreateLine(
                new Vector3(-gridRadius, i, 0),
                new Vector3(gridRadius, i, 0),
                isAxis ? axisColor : minorLineColor,
                isAxis ? axisLineWidth : minorLineWidth
            );

            // Labels along the bottom edge (X axis values)
            if (i != 0 && labelPrefab != null)
            {
                SpawnLabel(i.ToString() + " km", new Vector3(i, -gridRadius - 0.8f, 0));
                SpawnLabel(i.ToString() + " km", new Vector3(-gridRadius - 1.2f, i, 0));
            }
        }

        // Axis labels
        if (labelPrefab != null)
        {
            SpawnLabel("0", new Vector3(-0.5f, -0.5f, 0));
            SpawnLabel("E →",  new Vector3(gridRadius + 0.5f, 0.3f, 0));
            SpawnLabel("N ↑",  new Vector3(0.3f, gridRadius + 0.5f, 0));
        }
    }

    void CreateLine(Vector3 start, Vector3 end, Color color, float width)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.SetParent(transform);
        _spawnedObjects.Add(go);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material    = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor  = color;
        lr.endColor    = color;
        lr.startWidth  = width;
        lr.endWidth    = width;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = -1; // behind everything
        lr.useWorldSpace = true;
    }

    void SpawnLabel(string text, Vector3 worldPos)
    {
        GameObject label = Instantiate(labelPrefab, worldPos, Quaternion.identity, transform);
        _spawnedObjects.Add(label);

        TextMeshPro tmp = label.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text      = text;
            tmp.fontSize  = labelSize;
            tmp.color     = labelColor;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    void OnDestroy()
    {
        foreach (var obj in _spawnedObjects)
            if (obj != null) Destroy(obj);
    }
}
