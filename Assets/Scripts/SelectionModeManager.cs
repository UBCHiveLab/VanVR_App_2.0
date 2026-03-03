using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Toggles between manipulation and layer-selection modes, updating interaction masks and layer opacity.
public class SelectionModeManager : MonoBehaviour
{
    [Header("References")]
    public Transform layersRoot;                  // parent containing all LayerSelectable children
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable parentGrab;         // LayerObject grab interactable
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor[] interactorsToSwitch; // ray + direct if you have both
    public MonoBehaviour drawingSystemToDisable;  // your HandDrawingSystem OR driver script

    [Header("Interaction Layer Masks")]
    public InteractionLayerMask manipulationMask; // set to Manipulation
    public InteractionLayerMask layerSelectMask;  // set to LayerSelect

    [Header("Opacity")]
    [Range(0.02f, 1f)] public float unselectedAlpha = 0.15f;

    // runtime
    readonly List<LayerSelectable> allLayers = new();
    readonly HashSet<LayerSelectable> selectedLayers = new();

    public bool SelectionModeEnabled { get; private set; }

    void Awake()
    {
        if (!layersRoot) layersRoot = transform;

        allLayers.Clear();
        allLayers.AddRange(layersRoot.GetComponentsInChildren<LayerSelectable>(true));
        foreach (var layer in allLayers)
            layer.Initialize(this);

        // Default: manipulation mode
        ApplyInteractorMasks(selectionMode: false);
        SetDrawingEnabled(true);
    }

    // Called by UI Toggle
    public void SetSelectionMode(bool enabled)
    {
        Debug.Log($"[UI] SetSelectionMode called. enabled={enabled}", this);
        
        if (SelectionModeEnabled == enabled) return;

        SelectionModeEnabled = enabled;

        // Switch what your interactor can hit
        ApplyInteractorMasks(selectionMode: enabled);

        // Disable drawing while selecting
        SetDrawingEnabled(!enabled);

        if (!enabled)
        {
            // Leaving selection mode => apply final opacity state
            ApplyFinalOpacity();
        }
        else
        {
            // Entering selection mode => optional: reset all to opaque while choosing
            SetAllOpaque();
        }
    }

    public void ToggleLayer(LayerSelectable layer)
    {
        if (layer == null) return;

        layer.isSelected = !layer.isSelected;

        if (layer.isSelected) selectedLayers.Add(layer);
        else selectedLayers.Remove(layer);

        // Optional live feedback while selecting:
        // make selected opaque and unselected slightly faded (or keep all opaque until exit)
        // Here: keep all opaque during selection mode for clarity, but you can change this.
    }

    void ApplyInteractorMasks(bool selectionMode)
    {
        // parent grab can stay enabled; masks ensure it isn't targetable during selection mode
        // But we also optionally disable parent grab to avoid any weirdness:
        if (parentGrab) parentGrab.enabled = !selectionMode;

        if (interactorsToSwitch == null) return;
        foreach (var it in interactorsToSwitch)
        {
            if (!it) continue;
            it.interactionLayers = selectionMode ? layerSelectMask : manipulationMask;
        }
    }

    void SetDrawingEnabled(bool enabled)
    {
        if (!drawingSystemToDisable) return;

        // simplest: enable/disable the script
        drawingSystemToDisable.enabled = enabled;

        // If you prefer calling a method like SetPenEnabled(false),
        // you can change this to reference your HandDrawingSystem and call it.
    }

    void SetAllOpaque()
    {
        foreach (var layer in allLayers)
        {
            if (!layer || !layer.targetRenderer) continue;
            SetRendererAlpha(layer.targetRenderer, 1f);
        }
    }

    void ApplyFinalOpacity()
    {
        foreach (var layer in allLayers)
        {
            if (!layer || !layer.targetRenderer) continue;

            float a = layer.isSelected ? 1f : unselectedAlpha;
            SetRendererAlpha(layer.targetRenderer, a);
        }
    }

    static readonly Dictionary<Renderer, MaterialPropertyBlock> s_mpbs = new();

    static void SetRendererAlpha(Renderer r, float alpha)
    {
        if (!r || !r.sharedMaterial) return;

        if (!s_mpbs.TryGetValue(r, out var mpb) || mpb == null)
        {
            mpb = new MaterialPropertyBlock();
            s_mpbs[r] = mpb;
        }

        r.GetPropertyBlock(mpb);

        if (r.sharedMaterial.HasProperty("_BaseColor"))
        {
            var c = r.sharedMaterial.GetColor("_BaseColor");
            c.a = alpha;
            mpb.SetColor("_BaseColor", c);
        }
        else if (r.sharedMaterial.HasProperty("_Color"))
        {
            var c = r.sharedMaterial.GetColor("_Color");
            c.a = alpha;
            mpb.SetColor("_Color", c);
        }

        r.SetPropertyBlock(mpb);
    }


    // Optional utility for UI button: clear selections
    public void ClearSelections()
    {
        selectedLayers.Clear();
        foreach (var layer in allLayers)
            if (layer) layer.isSelected = false;

        if (!SelectionModeEnabled)
            ApplyFinalOpacity();
    }
}
