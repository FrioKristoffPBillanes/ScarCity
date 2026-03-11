using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Seismic station — rewritten to use Unity's new Input System
/// via IPointerDownHandler, IDragHandler, IPointerUpHandler.
/// Requires: Physics2DRaycaster on camera + EventSystem in scene.
/// </summary>
public class SeismicStation : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public TextMeshPro  stationLabel;
    public LineRenderer radiusLine;
    public LineRenderer circleRenderer;
    public GameObject   lockedIndicator;

    [Header("Visuals")]
    public Color stationColor  = Color.cyan;
    public Color circleColor   = Color.cyan;
    public float circleWidth   = 0.06f;
    public int   circleSegments = 128;

    [Header("Interaction")]
    public float minRadius = 0.5f;
    public float maxRadius = 18f;

    // ── State ─────────────────────────────────────────────────────────────────
    public string StationName { get; private set; }
    public float  RadiusKm    { get; private set; } = 0f;
    public bool   IsLocked    { get; private set; } = false;

    private Camera _cam;
    private bool   _isDragging = false;

    public event System.Action<SeismicStation> OnCircleConfirmed;

    // ── Init ──────────────────────────────────────────────────────────────────
    public void Initialize(string stationName, Color color, Camera cam)
    {
        StationName  = stationName;
        stationColor = color;
        circleColor  = color;
        _cam         = cam;

        if (stationLabel != null)
        {
            stationLabel.text  = stationName;
            stationLabel.color = color;
        }

        SetupLineRenderer(radiusLine,     color, 0.05f);
        SetupLineRenderer(circleRenderer, color, circleWidth);

        if (lockedIndicator != null)
            lockedIndicator.SetActive(false);
    }

    // ── New Input System Pointer Events ───────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsLocked) return;
        if (!EarthquakeMinigame.Instance.IsThisStationActive(this)) return;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 worldPos = GetPointerWorldPos(eventData);
        float   dist     = Vector2.Distance(transform.position, worldPos);
        RadiusKm         = Mathf.Clamp(dist, minRadius, maxRadius);

        // Draw radius line
        if (radiusLine != null)
        {
            Vector3 dir = (worldPos - transform.position).normalized;
            radiusLine.positionCount = 2;
            radiusLine.SetPosition(0, transform.position);
            radiusLine.SetPosition(1, transform.position + dir * RadiusKm);
        }

        DrawCircle(RadiusKm);
        UpdateLabel();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (RadiusKm >= minRadius)
            LockStation();
    }

    // ── Public ────────────────────────────────────────────────────────────────
    public void Unlock()
    {
        IsLocked = false;
        RadiusKm = 0f;
        if (circleRenderer != null) circleRenderer.positionCount = 0;
        if (radiusLine     != null) radiusLine.positionCount     = 0;
        if (lockedIndicator != null) lockedIndicator.SetActive(false);
        UpdateLabel();
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    void LockStation()
    {
        IsLocked = true;
        if (lockedIndicator != null) lockedIndicator.SetActive(true);
        DrawCircle(RadiusKm);
        UpdateLabel();
        OnCircleConfirmed?.Invoke(this);
    }

    void DrawCircle(float radius)
    {
        if (circleRenderer == null) return;
        circleRenderer.positionCount = circleSegments + 1;
        float angleStep = 360f / circleSegments;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = transform.position.x + Mathf.Cos(angle) * radius;
            float y = transform.position.y + Mathf.Sin(angle) * radius;
            circleRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    void UpdateLabel()
    {
        if (stationLabel == null) return;
        stationLabel.text = RadiusKm > 0
            ? $"{StationName}\n{RadiusKm:F1} km"
            : StationName;
    }

    Vector3 GetPointerWorldPos(PointerEventData eventData)
    {
        Vector3 screenPos = eventData.position;
        screenPos.z = Mathf.Abs(_cam.transform.position.z);
        return _cam.ScreenToWorldPoint(screenPos);
    }

    void SetupLineRenderer(LineRenderer lr, Color color, float width)
    {
        if (lr == null) return;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = color;
        lr.endColor      = color;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.sortingOrder  = 2;
        lr.useWorldSpace = true;
        lr.positionCount = 0;
    }
}