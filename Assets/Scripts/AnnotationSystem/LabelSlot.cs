using UnityEngine;

/// <summary>
/// Marker component placed on each preset slot GameObject around the organ.
/// The slot GameObjects should be children of a dedicated "LabelSlots"
/// empty that is NOT a child of the organ mesh — parent it to the same object
/// as the organ root, or to the scene root, so it doesn't inherit organ rotation.
///
/// At runtime, AnnotationSpawner reads slot world positions at spawn time,
/// so even if you do parent slots to the organ, just ensure you call
/// Initialize() before the organ rotates after scene load.
/// </summary>
public class LabelSlot : MonoBehaviour
{
    [Tooltip("Set automatically by AnnotationSpawner at runtime.")]
    public bool IsOccupied { get; private set; }

    private AnnotationLabelUI occupyingLabel;

    public void Occupy(AnnotationLabelUI label)
    {
        IsOccupied = true;
        occupyingLabel = label;
    }

    public void Free()
    {
        IsOccupied = false;
        occupyingLabel = null;
    }

    public AnnotationLabelUI OccupyingLabel => occupyingLabel;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(0.3f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.02f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.03f);
    }
#endif
}