using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Placed on the root of the annotation label prefab.
/// Follows the organ's translation and scale but not its rotation.
/// Always faces the main camera (billboard).
/// Owns the LineRenderer connecting this label to the annotation point on the mesh.
/// </summary>
public class AnnotationLabelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button closeButton;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Follow Settings")]
    private Vector3 slotWorldPositionAtInit;
    private Vector3 organPositionAtInit;
    private Vector3 organPositionAtLastFollow;
    private Vector3 targetWorldPosition;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float followThreshold = 0.07f;

    [SerializeField] private LabelDragHandle dragHandle;
    private bool isDragging = false;
    

    // Runtime state
    private AnnotationPoint annotationPoint;
    private Transform organRoot;

    // The label's position expressed in organ-local space (without rotation).
    // Stored so it moves correctly when the organ translates/scales.
    private Vector3 localOffsetPosition;

    // AnnotationSpawner sets this so the label can notify it on close
    public System.Action<AnnotationLabelUI> OnClosed;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
        }

        // Hook grab events
        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectEntered.AddListener(_ => OnDragBegin());
            grab.selectExited.AddListener(_ => OnDragEnd());
        }
    }

    /// <summary>
    /// Call this immediately after instantiation.
    /// slotWorldPosition is the world-space position of the assigned slot at
    /// spawn time; we convert it to our rotation-free local space internally.
    /// </summary>
    public void Initialize(AnnotationPoint point, Transform organ, Vector3 slotWorldPosition)
    {
        annotationPoint = point;
        organRoot = organ;

        // Explicitly wire the drag handle
        if (dragHandle != null)
            dragHandle.parentLabel = this;

        slotWorldPositionAtInit = slotWorldPosition;
        organPositionAtInit = organ.position;
        organPositionAtLastFollow = organ.position;
        targetWorldPosition = slotWorldPosition;
        transform.position = slotWorldPosition;

        PopulateUI();
        UpdateLineRenderer();
    }

    private void LateUpdate()
    {
        if (organRoot == null) return;
        UpdateTransform();
        UpdateLineRenderer();
    }

    // -----------------------------------------------------------------------
    // Transform follow: translation + scale, no rotation, then billboard

    private void UpdateTransform()
    {
        if (!isDragging)
        {
            float delta = Vector3.Distance(organRoot.position, organPositionAtLastFollow);
            if (delta >= followThreshold)
            {
                Vector3 totalOrganDelta = organRoot.position - organPositionAtInit;
                targetWorldPosition = slotWorldPositionAtInit + totalOrganDelta;
                organPositionAtLastFollow = organRoot.position;
            }

            transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * followSpeed);
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>
    /// Converts a world position into organ-local space using only
    /// translation and scale — rotation is deliberately excluded.
    /// </summary>
    private Vector3 WorldToOrganLocalNoRotation(Vector3 worldPos)
    {
        Vector3 localScale = organRoot.lossyScale;
        Vector3 delta = worldPos - organRoot.position;
        return new Vector3(
            localScale.x != 0 ? delta.x / localScale.x : 0,
            localScale.y != 0 ? delta.y / localScale.y : 0,
            localScale.z != 0 ? delta.z / localScale.z : 0
        );
    }

    /// <summary>
    /// Inverse of above: local (no-rotation) → world space.
    /// </summary>
    private Vector3 OrganLocalNoRotationToWorld(Vector3 localPos)
    {
        Vector3 localScale = organRoot.lossyScale;
        return organRoot.position + new Vector3(
            localPos.x * localScale.x,
            localPos.y * localScale.y,
            localPos.z * localScale.z
        );
    }

    // -----------------------------------------------------------------------
    // Line renderer

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || annotationPoint == null || organRoot == null) return;

        Vector3 annotationWorld = organRoot.TransformPoint(annotationPoint.localPosition);

        // Determine which side of the label the annotation is on
        Vector3 toAnnotation = annotationWorld - transform.position;
        float dot = Vector3.Dot(transform.right, toAnnotation);

        // Pick left or right edge depending on which side the annotation falls
        Vector3 labelAnchor = transform.position + transform.right * (dot >= 0 ? GetPanelHalfWidth() : -GetPanelHalfWidth());

        lineRenderer.SetPosition(0, annotationWorld);
        lineRenderer.SetPosition(1, labelAnchor);
    }

    private float GetPanelHalfWidth()
    {
        // Approximate half-width of the canvas panel in world units.
        // Adjust this to match your actual canvas RectTransform width * canvas scale.
        return 0.08f;
    }

    // -----------------------------------------------------------------------
    // UI population

    private void PopulateUI()
    {
        if (annotationPoint == null) return;

        if (idText != null)
            idText.text = annotationPoint.id.ToString();
        if (labelText != null)
            labelText.text = annotationPoint.label;
        if (descriptionText != null)
            descriptionText.text = annotationPoint.description;

        // Defer collider resize by one frame so ContentSizeFitter has updated
        StartCoroutine(RefreshColliderNextFrame());
    }

    private System.Collections.IEnumerator RefreshColliderNextFrame()
    {
        yield return null; // wait one frame
        if (dragHandle != null)
            dragHandle.RefreshColliderSize();
    }

    public void OnDragBegin()
    {
        isDragging = true;
    }

    public void OnDragEnd()
    {
        isDragging = false;

        // Treat current world position as the new base, 
        // relative to where the organ is right now
        slotWorldPositionAtInit = transform.position;
        organPositionAtInit = organRoot.position;
        organPositionAtLastFollow = organRoot.position;
        targetWorldPosition = transform.position;
    }
    
    public void SetDragPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        targetWorldPosition = worldPos;
    }

    // -----------------------------------------------------------------------
    // Close

    private void Close()
    {
        OnClosed?.Invoke(this);
    }

    // -----------------------------------------------------------------------
    // Drag support: called by AnnotationSpawner or a grab interactable
    // when the user repositions the label manually.

    /// <summary>
    /// Call this from your XRGrabInteractable selectExited callback (or
    /// equivalent) with the label's current world position to lock it in place.
    /// </summary>
    public void SetWorldPosition(Vector3 worldPos)
    {
        localOffsetPosition = WorldToOrganLocalNoRotation(worldPos);
    }

    public AnnotationPoint AnnotationPoint => annotationPoint;
}