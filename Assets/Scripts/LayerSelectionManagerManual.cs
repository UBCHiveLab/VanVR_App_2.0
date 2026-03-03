using System.Collections.Generic;
using UnityEngine;

// Manually raycasts from hands/controllers to select brain layers, handling hover, selection and tinting.
public class LayerSelectionManagerManual : MonoBehaviour
{
    [Header("Pointers (ray origins)")]
    public Transform rightControllerAim;
    public Transform leftControllerAim;
    public Transform rightHandAim;
    public Transform leftHandAim;

    [Header("Raycast")]
    public float maxDistance = 5f;
    public LayerMask physicsMask = ~0;
    public bool debugRay = true;

    [Header("Layers Root")]
    public Transform layersRoot;

    [Header("Drawing disable (disable ALL of these during selection mode)")]
    public HandDrawingSystem drawingSystem;           // optional but recommended
    public MonoBehaviour[] drawingDriversToDisable;   // ControllerPenDriver L/R, SimpleHandGestures L/R, etc.

    [Header("Visuals")]
    public Color hoverTint = new(1f, 1f, 0.3f, 1f);
    public Color selectedTint = new(0.3f, 1f, 0.6f, 1f);
    [Range(0.02f, 1f)] public float unselectedAlpha = 0.15f;

    [Header("Behavior")]
    public bool selectionModeEnabled;
    public bool keepAllOpaqueWhileSelecting = true;

    // internal
    BrainLayer hovered;
    Transform activePointer;
    readonly HashSet<BrainLayer> selected = new();
    readonly List<BrainLayer> allLayers = new();
    readonly Dictionary<Renderer, Color> originalColor = new();
    readonly Dictionary<Renderer, MaterialPropertyBlock> mpbCache = new();

    void Awake()
    {
        if (!layersRoot) layersRoot = transform;
        allLayers.AddRange(layersRoot.GetComponentsInChildren<BrainLayer>(true));

        foreach (var layer in allLayers)
        {
            if (!layer || !layer.rend || !layer.rend.sharedMaterial) continue;
            var r = layer.rend;

            Color c = Color.white;
            if (r.sharedMaterial.HasProperty("_BaseColor"))
                c = r.sharedMaterial.GetColor("_BaseColor");
            else if (r.sharedMaterial.HasProperty("_Color"))
                c = r.sharedMaterial.GetColor("_Color");

            originalColor[r] = c;
            mpbCache[r] = new MaterialPropertyBlock();
        }

        SetSelectionMode(false);
    }

    void Update()
    {
        if (!selectionModeEnabled) return;

        // Choose an active pointer every frame (controllers/hands)
        activePointer = ChooseActivePointer();
        if (!activePointer)
        {
            ClearHoverTint();
            return;
        }

        UpdateHover(activePointer);
    }

    public void SetSelectionMode(bool enabled)
    {
        selectionModeEnabled = enabled;

        // Drawing off during selection mode
        if (drawingSystem) drawingSystem.SetPenEnabled(!enabled);
        if (drawingDriversToDisable != null)
        {
            foreach (var d in drawingDriversToDisable)
                if (d) d.enabled = !enabled;
        }

        if (enabled)
        {
            selected.Clear();

            ClearHoverTint();

            if (keepAllOpaqueWhileSelecting)
                SetAllOpaque();

            RefreshTints();
        }
        else
        {
            ClearHoverTint();
            ApplyOpacityRule();
        }
    }

    // Called by an interactor event (we’ll wire this up in step 2)
    public void ToggleSelectionFromPointer(Transform pointer)
    {
        if (!selectionModeEnabled) return;
        if (!pointer) pointer = activePointer;
        if (!pointer) return;

        // Make sure hover is up-to-date for THIS pointer
        UpdateHover(pointer);

        if (!hovered) return;

        if (selected.Contains(hovered)) selected.Remove(hovered);
        else selected.Add(hovered);

        RefreshTints();
    }

    Transform ChooseActivePointer()
    {
        // Priority order — change if you want
        if (rightControllerAim && rightControllerAim.gameObject.activeInHierarchy) return rightControllerAim;
        if (leftControllerAim && leftControllerAim.gameObject.activeInHierarchy) return leftControllerAim;
        if (rightHandAim && rightHandAim.gameObject.activeInHierarchy) return rightHandAim;
        if (leftHandAim && leftHandAim.gameObject.activeInHierarchy) return leftHandAim;
        return null;
    }

    void UpdateHover(Transform pointer)
    {
        Ray ray = new(pointer.position, pointer.forward);
        if (debugRay) Debug.DrawRay(ray.origin, ray.direction * maxDistance);

        BrainLayer hitLayer = null;
        if (Physics.Raycast(ray, out var hit, maxDistance, physicsMask, QueryTriggerInteraction.Ignore))
            hitLayer = hit.collider.GetComponentInParent<BrainLayer>();

        if (hitLayer == hovered) return;

        ClearHoverTint();
        hovered = hitLayer;
        ApplyHoverTint();
    }

    void ApplyHoverTint()
    {
        if (!hovered || !hovered.rend) return;
        if (selected.Contains(hovered)) return;
        SetColor(hovered.rend, hoverTint, 1f);
    }

    void ClearHoverTint()
    {
        if (!hovered || !hovered.rend) { hovered = null; return; }
        RefreshLayerTint(hovered, 1f);
        hovered = null;
    }

    void RefreshTints()
    {
        foreach (var layer in allLayers)
            RefreshLayerTint(layer, 1f);

        ApplyHoverTint();
    }

    void RefreshLayerTint(BrainLayer layer, float alpha)
    {
        if (!layer || !layer.rend) return;

        if (selected.Contains(layer))
            SetColor(layer.rend, selectedTint, alpha);
        else
            RestoreOriginal(layer.rend, alpha);
    }

    void ApplyOpacityRule()
    {
        bool anySelected = selected.Count > 0;

        foreach (var layer in allLayers)
        {
            if (!layer || !layer.rend) continue;

            if (!anySelected)
            {
                // No selection => whole model opaque, original colors
                RestoreOriginal(layer.rend, 1f);
            }
            else
            {
                // Selection exists => selected opaque, unselected translucent (original colors)
                if (selected.Contains(layer))
                    RestoreOriginal(layer.rend, 1f);
                else
                    RestoreOriginal(layer.rend, unselectedAlpha);
            }
        }
    }


    void SetAllOpaque()
    {
        foreach (var layer in allLayers)
        {
            if (!layer || !layer.rend) continue;
            if (selected.Contains(layer)) SetColor(layer.rend, selectedTint, 1f);
            else RestoreOriginal(layer.rend, 1f);
        }
    }

    void SetColor(Renderer r, Color c, float alpha)
    {
        if (!r) return;
        if (!mpbCache.TryGetValue(r, out var mpb))
        {
            mpb = new MaterialPropertyBlock();
            mpbCache[r] = mpb;
        }

        c.a = alpha;
        r.GetPropertyBlock(mpb);

        if (r.sharedMaterial && r.sharedMaterial.HasProperty("_BaseColor"))
            mpb.SetColor("_BaseColor", c);
        else
            mpb.SetColor("_Color", c);

        r.SetPropertyBlock(mpb);
    }

    void RestoreOriginal(Renderer r, float alpha)
    {
        if (!r) return;
        if (!originalColor.TryGetValue(r, out var c)) c = Color.white;
        c.a = alpha;
        SetColor(r, c, alpha);
    }
}
