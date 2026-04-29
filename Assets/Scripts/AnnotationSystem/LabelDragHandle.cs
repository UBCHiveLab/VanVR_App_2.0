using UnityEngine;

public class LabelDragHandle : MonoBehaviour
{
    public AnnotationLabelUI parentLabel;

    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private BoxCollider dragCollider;

    private void Awake()
    {
        if (parentLabel == null)
            parentLabel = GetComponentInParent<AnnotationLabelUI>();
    }

    // Call this from AnnotationLabelUI after PopulateUI() so the
    // ContentSizeFitter has had a chance to update the canvas size
    public void RefreshColliderSize()
    {
        if (dragCollider == null) return;
        dragCollider.size = new Vector3(1f, 1f, 0.005f);
        dragCollider.center = Vector3.zero;
    }
}