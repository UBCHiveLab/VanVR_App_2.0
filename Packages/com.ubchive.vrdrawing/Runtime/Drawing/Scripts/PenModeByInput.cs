using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PenModeByInput : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] HandDrawingSystem drawing;
    [SerializeField] Toggle penToggle;

    bool _desiredPenEnabled;
    bool _suppress;

    void Awake()
    {
        if (drawing == null)
            drawing = FindFirstObjectByType<HandDrawingSystem>();
    }

    void OnEnable()
    {
        if (penToggle != null)
            penToggle.onValueChanged.AddListener(OnUserToggle);

        // Startup + runtime changes
        XRInputModalityManager.currentInputMode.SubscribeAndUpdate(OnInputModeChanged);
    }

    void OnDisable()
    {
        if (penToggle != null)
            penToggle.onValueChanged.RemoveListener(OnUserToggle);
    }

    void OnInputModeChanged(XRInputModalityManager.InputMode mode)
    {
        if (drawing == null) return;

        if (mode == XRInputModalityManager.InputMode.MotionController)
            SetPenAndDesired(true);     // controller => ON
        else if (mode == XRInputModalityManager.InputMode.TrackedHand)
            SetPenAndDesired(false);    // hands => OFF
    }

    void OnUserToggle(bool enabled)
    {
        if (_suppress) return;
        if (drawing == null) return;

        _desiredPenEnabled = enabled;
        drawing.SetPenEnabled(enabled);
    }

    void SetPenAndDesired(bool enabled)
    {
        _desiredPenEnabled = enabled;
        drawing.SetPenEnabled(enabled);
        ForceToggleToDesired(); // try immediately (works if UI active)
    }

    void LateUpdate()
    {
        // Keep UI synced even if other scripts/UI refreshes fight it,
        // and also covers the case where the menu/toggle becomes active later.
        ForceToggleToDesired();
    }

    void ForceToggleToDesired()
    {
        if (penToggle == null) return;
        if (!penToggle.isActiveAndEnabled) return;

        if (penToggle.isOn == _desiredPenEnabled) return;

        _suppress = true;
        penToggle.isOn = _desiredPenEnabled; // updates visuals + triggers other listeners
        _suppress = false;

        Canvas.ForceUpdateCanvases();
    }
}
