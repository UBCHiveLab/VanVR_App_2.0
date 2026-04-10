using UnityEngine;

// Represents a single brain layer mesh and caches its renderer.
public class BrainLayer : MonoBehaviour
{
    public Renderer rend;

    void Awake()
    {
        if (!rend) rend = GetComponentInChildren<Renderer>();
    }
}
