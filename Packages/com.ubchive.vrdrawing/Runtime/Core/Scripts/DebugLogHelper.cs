using UnityEngine;

public class DebugLogHelper : MonoBehaviour
{
    public void Log() => Debug.Log($"{name}: Gesture performed");
    public void LogEnded() => Debug.Log($"{name}: Gesture ended");
}