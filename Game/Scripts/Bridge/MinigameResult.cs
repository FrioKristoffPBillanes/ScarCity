using UnityEngine;

/// <summary>
/// Base class to put on the controller script of EVERY minigame scene.
/// Each minigame just inherits this and calls Succeed() or Fail().
///
/// Example — in your EarthquakeTriangulation script:
///
///     public class EarthquakeMinigame : MinigameResult
///     {
///         void CheckAnswer()
///         {
///             float error = Vector2.Distance(playerGuess, actualEpicenter);
///             if (error < acceptableRadius)
///                 Succeed();
///             else
///                 Fail();
///         }
///     }
/// </summary>
public abstract class MinigameResult : MonoBehaviour
{
    [Header("Minigame Settings")]
    [Tooltip("Name shown at the top of the minigame screen")]
    public string minigameTitle = "Minigame";

    [Tooltip("Brief one-liner shown to the player before they start")]
    [TextArea] public string instructions = "";

    // Whether this minigame has already reported a result
    protected bool _finished = false;

    /// <summary>Call this when the player successfully solves the minigame.</summary>
    protected void Succeed()
    {
        if (_finished) return;
        _finished = true;
        OnSucceed();
        MinigameBridge.Instance.ReturnToCity(true);
    }

    /// <summary>Call this when the player fails (wrong answer, time runs out, etc.).</summary>
    protected void Fail()
    {
        if (_finished) return;
        _finished = true;
        OnFail();
        MinigameBridge.Instance.ReturnToCity(false);
    }

    /// <summary>Override to play a success animation/sound before returning.</summary>
    protected virtual void OnSucceed() { }

    /// <summary>Override to play a failure animation/sound before returning.</summary>
    protected virtual void OnFail() { }
}
