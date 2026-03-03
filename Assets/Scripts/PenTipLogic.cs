using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

// Controls pen tip visibility, size and color based on pen settings and handedness.
public class PenTipLogic : MonoBehaviour
{
    [Header("References")]
    public HandDrawingSystem pen;
    public GameObject penTipRoot;
    public Renderer penTipRenderer;

    [Header("Handedness")]
    [Tooltip("Which hand this pen tip belongs to.")]
    public HandMenu.MenuHandedness thisHand;

    [Tooltip("Reference to the same HandMenu used by HandednessToggle.")]
    public HandMenu handMenu;

    [Header("Scaling")]
    public float widthToSphereScale = 1.0f;
    public float minVisualScale = 0.002f;
    public float maxVisualScale = 0.025f;

    [Header("Material Handling")]
    public bool instanceMaterial = true;

    Material _mat;

    void Awake()
    {
        if (penTipRoot == null)
            penTipRoot = gameObject;

        if (handMenu == null)
            handMenu = FindFirstObjectByType<HandMenu>();

        if (penTipRenderer != null && instanceMaterial)
            _mat = penTipRenderer.material;
    }

    void LateUpdate()
    {
        if (pen == null || penTipRenderer == null || handMenu == null)
            return;

        // visible if dominant hand
        bool isNotDominant = handMenu.menuHandedness != thisHand;
        bool visible = pen.penEnabled && isNotDominant;

        penTipRenderer.enabled = visible;

        if (!visible)
            return;

        // scale based on pen width
        float s = Mathf.Clamp(
            pen.penWidth * widthToSphereScale,
            minVisualScale,
            maxVisualScale
        );

        transform.localScale = new Vector3(s, s, s);

        // pen color
        Color c = pen.currentColor;

        var m = penTipRenderer.material;

        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color"))
            m.SetColor("_Color", c);
    }
}
