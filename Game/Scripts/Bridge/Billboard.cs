using UnityEngine;

/// <summary>
/// Makes this GameObject always face the main camera.
/// Attach to the CrisisMarker root so the icon and canvas
/// always face the player regardless of camera angle.
/// </summary>
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;
        transform.rotation = Camera.main.transform.rotation;
    }
}
