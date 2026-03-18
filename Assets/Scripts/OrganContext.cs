using UnityEngine;

public class OrganContext : MonoBehaviour
{
    [SerializeField] private Transform drawingTarget;

    // This is what OrganSpawner will read
    public Transform DrawingTarget => drawingTarget != null ? drawingTarget : transform;
}