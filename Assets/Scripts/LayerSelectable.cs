using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
// Represents a selectable brain layer and forwards selection events to the selection manager.
public class LayerSelectable : MonoBehaviour
{
    [Header("Renderer to fade (usually this layer mesh)")]
    public Renderer targetRenderer;

    [HideInInspector] public bool isSelected;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    SelectionModeManager manager;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void Initialize(SelectionModeManager m)
    {
        manager = m;
    }

    void OnEnable()
    {
        // toggle selection when selected (trigger/click)
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Only allow toggling while selection mode is ON
        if (manager == null || !manager.SelectionModeEnabled)
            return;

        manager.ToggleLayer(this);
    }
}
