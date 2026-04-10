using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class AnnotationMarker : MonoBehaviour
{
    [Header("Data")]
    public AnnotationPoint data;

    [Header("References")]
    public TextMeshPro idText;

    [Header("Billboard Appearance")]
    public float billboardSize = 0.05f;
    public Color normalColor   = new Color(0.2f, 0.6f, 1f, 1f);
    public Color hoveredColor  = new Color(1f, 0.85f, 0f, 1f);
    public Color selectedColor = new Color(0f, 1f, 0.4f, 1f);

    [Header("Hover Scale")]
    public float lerpSpeed = 8f;

    private Vector3 _normalScale;
    private Vector3 _hoveredScale;
    private Vector3 _targetScale;
    private Color   _targetColor;
    private Transform _head;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _mpb          = new MaterialPropertyBlock();
        _normalScale  = Vector3.one * billboardSize;
        _hoveredScale = Vector3.one * billboardSize * 1.4f;
        _targetScale  = _normalScale;
        _targetColor  = normalColor;

        BuildQuadMesh();
        SetColorImmediate(normalColor);
        transform.localScale = _normalScale;
    }

    void Start()
    {
        _head = Camera.main?.transform;
    }

    void Update()
    {
        // billboard
        if (_head != null)
        {
            Vector3 toHead = _head.position - transform.position;
            if (toHead.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-toHead);
        }

        // smooth scale
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * lerpSpeed);

        // smooth color
        if (_meshRenderer != null)
        {
            _meshRenderer.GetPropertyBlock(_mpb);
            Color current = _mpb.GetColor("_BaseColor");
            _mpb.SetColor("_BaseColor", Color.Lerp(current, _targetColor, Time.deltaTime * lerpSpeed));
            _meshRenderer.SetPropertyBlock(_mpb);
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
        _targetScale = _hoveredScale;
        _targetColor = hoveredColor;
    }

    public void OnHoverEnd()
    {
        _targetScale = _normalScale;
        _targetColor = normalColor;
    }

    public void Select()
    {
        _targetScale = _hoveredScale;
        _targetColor = selectedColor;
    }

    public void Deselect()
    {
        _targetScale = _normalScale;
        _targetColor = normalColor;
    }

    private void BuildQuadMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();

        if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();

        float h = 0.5f;
        var mesh = new Mesh { name = "AnnotationBillboard" };
        mesh.vertices  = new Vector3[] { new(-h,-h,0), new(h,-h,0), new(h,h,0), new(-h,h,0) };
        mesh.uv        = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        if (_meshRenderer.sharedMaterial == null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = normalColor;
            _meshRenderer.sharedMaterial = mat;
        }

        var col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.size   = new Vector3(1f, 1f, 0.01f);
        col.center = Vector3.zero;
    }

    private void SetColorImmediate(Color color)
    {
        if (_meshRenderer == null) return;
        _meshRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", color);
        _meshRenderer.SetPropertyBlock(_mpb);
    }
}