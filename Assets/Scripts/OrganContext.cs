using UnityEngine;

// Supplies the drawing target transform for a specific organ instance.
public class OrganContext : MonoBehaviour, IOrganContext
{
    [SerializeField] private Transform drawingTarget;

    public Transform GetDrawingTarget()
    {
        return drawingTarget != null ? drawingTarget : transform;
    }
}