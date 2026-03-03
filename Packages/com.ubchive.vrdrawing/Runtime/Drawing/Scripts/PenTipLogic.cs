using UnityEngine;

public class PenTipLogic : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your drawing/pen controller script that already has penWidth/currentColor/penEnabled.")]
    public HandDrawingSystem pen;

    [Tooltip("The pen tip sphere root (the GameObject you want to show/hide).")]
    public GameObject penTipRoot;

    [Tooltip("Renderer whose material/color should change (sphere MeshRenderer usually).")]
    public Renderer penTipRenderer;

    [Header("Scaling")]
    [Tooltip("Multiplier to convert penWidth (meters) into a sphere diameter/scale that feels right.")]
    public float widthToSphereScale = 1.0f;

    [Tooltip("Clamp the visual size so it doesn't get too tiny/huge.")]
    public float minVisualScale = 0.002f;
    public float maxVisualScale = 0.05f;

    [Header("Material Handling")]
    [Tooltip("If true, instances a unique material for the pen tip so it won’t recolor other objects using the same material.")]
    public bool instanceMaterial = true;

    Material _mat;

    void Awake()
    {
        if (penTipRoot == null) penTipRoot = gameObject;

        if (penTipRenderer != null && instanceMaterial)
        {
            // Make sure we don't recolor a shared material used elsewhere
            _mat = penTipRenderer.material; // this instantiates at runtime
        }
    }

    void OnDestroy()
    {
        // Optional cleanup: only destroy if we instantiated it
        // In practice Unity manages renderer.material instances fine,
        // but you can uncomment if you want strict cleanup.
        // if (_mat != null) Destroy(_mat);
    }

    void LateUpdate()
    {
        if (pen == null || penTipRenderer == null)
            return;

        // 1) Visibility
        bool visible = pen.penEnabled;
        penTipRenderer.enabled = visible;

        if (!visible) return;

        // 2) Scale (local scale since you're parented under Pinch Point Stabilized)
        float s = Mathf.Clamp(pen.penWidth * widthToSphereScale, minVisualScale, maxVisualScale);
        transform.localScale = new Vector3(s, s, s);

        // 3) Color
        Color c = pen.currentColor;

        // URP uses _BaseColor; built-in often uses _Color
        var m = penTipRenderer.material;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }
}
