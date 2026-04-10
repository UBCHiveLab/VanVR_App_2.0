using UnityEngine;

// Helper for logging simple gesture events to the console.
public class DebugLogHelper : MonoBehaviour
{
    public void Log() => Debug.Log($"{name}: Gesture performed");
    public void LogEnded() => Debug.Log($"{name}: Gesture ended");
}