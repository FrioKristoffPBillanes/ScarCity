/// <summary>
/// Simple static bridge that carries Phase 1 results into Phase 2.
/// No MonoBehaviour needed — just a static class that survives scene loads.
/// </summary>
public static class ForestFireBridge
{
    /// <summary>
    /// Number of fires that spread before being clicked in Phase 1.
    /// Phase 2 reads this to calculate starting tile count:
    ///     targetTiles = Mathf.Clamp(4 + UncontainedFires * 2, 4, 12)
    /// </summary>
    public static int UncontainedFires = 0;
}
