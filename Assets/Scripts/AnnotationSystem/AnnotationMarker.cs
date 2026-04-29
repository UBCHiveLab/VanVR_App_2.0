using UnityEngine;
using TMPro;

public class AnnotationMarker : MonoBehaviour
{
    [Header("Data")]
    public AnnotationPoint data;

    [Header("References")]
    public TextMeshPro idText;
    public MeshRenderer discRenderer;

    [Header("Billboard Appearance")]
    public Color normalColor   = new Color(0.2f, 0.6f, 1f, 1f);
    public Color hoveredColor  = new Color(1f, 0.85f, 0f, 1f);
    public Color selectedColor = new Color(0f, 1f, 0.4f, 1f);

    [Header("Hover Scale")]
    public float lerpSpeed = 8f;

    [HideInInspector] public float markerWorldSize = 0.005f;

    private float _normalWorldSize;
    private float _hoveredWorldSize;
    private float _targetWorldSize;
    private Color _targetColor;
    private Transform _head;
    private MaterialPropertyBlock _mpb;

    public bool IsSelected { get; private set; }

    void Awake()
    {
        _mpb         = new MaterialPropertyBlock();
        _targetColor = normalColor;

        SetColorImmediate(normalColor);

        // ensure collider exists on root for raycasting
        var col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.size   = new Vector3(1f, 0.05f, 1f);
        col.center = Vector3.zero;
    }

    void Start()
    {
        _head             = Camera.main?.transform;
        _normalWorldSize  = markerWorldSize;
        _hoveredWorldSize = markerWorldSize * 1.4f;
        _targetWorldSize  = _normalWorldSize;
    }

    void Update()
    {
        // billboard — always face camera
        if (_head != null)
        {
            Vector3 toHead = _head.position - transform.position;
            if (toHead.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-toHead);
        }

        // enforce world-space size every frame
        float parentWorldScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
        if (parentWorldScale > 0f)
        {
            float currentWorldSize = Mathf.Lerp(
                transform.localScale.x * parentWorldScale,
                _targetWorldSize,
                Time.deltaTime * lerpSpeed
            );
            float localSize = currentWorldSize / parentWorldScale;
            transform.localScale = Vector3.one * localSize;
        }

        // smooth color
        if (discRenderer != null)
        {
            discRenderer.GetPropertyBlock(_mpb);
            Color current = _mpb.GetColor("_BaseColor");
            _mpb.SetColor("_BaseColor", Color.Lerp(current, _targetColor, Time.deltaTime * lerpSpeed));
            discRenderer.SetPropertyBlock(_mpb);
        }
    }

    public void Initialize(AnnotationPoint point, Transform headTransform)
    {
        data  = point;
        _head = headTransform;
        if (idText != null) idText.text = point.id.ToString();
    }

    public void OnHoverBegin()
    {
        _targetWorldSize = _hoveredWorldSize;
        _targetColor     = hoveredColor;
    }

    public void OnHoverEnd()
    {
        _targetWorldSize = _normalWorldSize;
        _targetColor     = normalColor;
    }

    public void Select()
    {
        _targetWorldSize = _hoveredWorldSize;
        _targetColor     = selectedColor;
        IsSelected = true;
    }

    public void Deselect()
    {
        _targetWorldSize = _normalWorldSize;
        _targetColor     = normalColor;
        IsSelected = false;
    }

    private void SetColorImmediate(Color color)
    {
        if (discRenderer == null) return;
        discRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", color);
        discRenderer.SetPropertyBlock(_mpb);
    }
}