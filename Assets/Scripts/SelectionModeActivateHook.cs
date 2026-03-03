using UnityEngine;
using UnityEngine.InputSystem;

// Bridges an input action to the manual layer selection manager to toggle selection at the pointer.
public class SelectionModeActivateHook : MonoBehaviour
{
    public LayerSelectionManagerManual manager;

    [Tooltip("Use the FAR aim transform (controller aim / far caster origin), not poke.")]
    public Transform pointerOrigin;

    [Tooltip("Bind this to the same action you assigned in NearFarInteractor -> Activate Input.")]
    public InputActionReference activateAction;

    void OnEnable()
    {
        if (activateAction != null)
        {
            activateAction.action.Enable();
            activateAction.action.performed += OnActivatePerformed;
        }
    }

    void OnDisable()
    {
        if (activateAction != null)
        {
            activateAction.action.performed -= OnActivatePerformed;
            activateAction.action.Disable();
        }
    }

    void OnActivatePerformed(InputAction.CallbackContext ctx)
    {
        if (!manager || !manager.selectionModeEnabled) return;
        manager.ToggleSelectionFromPointer(pointerOrigin);
    }
}
