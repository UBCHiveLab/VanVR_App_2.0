using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

// Drives the drawing system from controller input (trigger, thumbstick, grab and undo).
public class ControllerPenDriver : MonoBehaviour
{
    [Header("Drawing Target")]
    public HandDrawingSystem drawing;
    public Transform controllerTip;

    [Header("Input Actions")]
    public InputActionReference activate;       // trigger
    public InputActionReference undoButton;     // A/X
    public InputActionReference stick;          // thumbstick (Vector2)

    [Header("Pen Width via Thumbstick")]
    public float widthChangePerSecond = 0.02f;
    [Range(0f, 1f)] public float deadzone = 0.2f;
    public float minWidth = 0.002f;
    public float maxWidth = 0.025f;

    [Header("Optional UI Sync")]
    public Slider widthSlider;

    [Header("Grab (for hiding menu)")]
    [Tooltip("Which hand THIS controller belongs to.")]
    public HandMenu.MenuHandedness myHand;

    [Tooltip("Grip button action for this controller.")]
    public InputActionReference grabAction;

    [Header("Dominant hand (optional gate)")]
    [Tooltip("Assign the same HandMenu used by your handedness toggle. If null, auto-find one.")]
    [SerializeField] HandMenu handMenu;

    [Tooltip("If true, only the dominant hand can draw with controller input.")]
    [SerializeField] bool onlyDominantHandCanDraw = false;

    [Header("UI Block (Near-Far)")]
    [SerializeField] XRUIInputModule uiInputModule;
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor nearFarInteractor;
    [SerializeField] bool blockDrawingWhenHoveringUI = true;

    static ControllerPenDriver s_currentDrawer;
    bool isHeld;

    void Awake()
    {
        s_currentDrawer = null;
        if (handMenu == null)
            handMenu = FindFirstObjectByType<HandMenu>();
    }

    void OnEnable()
    {
        Enable(activate);
        Enable(undoButton);
        Enable(stick);
        Enable(grabAction);

        if (activate != null)
        {
            activate.action.started += OnActivateStarted;
            activate.action.canceled += OnActivateCanceled;
        }

        if (undoButton != null)
            undoButton.action.performed += OnUndoPerformed;

        if (grabAction != null)
        {
            grabAction.action.started += OnGrabStarted;
            grabAction.action.canceled += OnGrabCanceled;
        }
    }

    void OnDisable()
    {
        if (activate != null)
        {
            activate.action.started -= OnActivateStarted;
            activate.action.canceled -= OnActivateCanceled;
        }

        if (undoButton != null)
            undoButton.action.performed -= OnUndoPerformed;

        if (grabAction != null)
        {
            grabAction.action.started -= OnGrabStarted;
            grabAction.action.canceled -= OnGrabCanceled;
        }

        // Clear controller-grab state
        SimpleHandGestures.SetControllerGrabbing(myHand, false);

        if (s_currentDrawer == this)
            s_currentDrawer = null;
    }

    void Update()
    {
        if (drawing == null) return;

        if (!drawing.penEnabled)
        {
            if (isHeld)
            {
                isHeld = false;
                if (s_currentDrawer == this)
                    s_currentDrawer = null;

                drawing.StopDrawing();
            }
            return;
        }

        // If handedness switched and this controller is no longer allowed, stop drawing.
        if (onlyDominantHandCanDraw && isHeld && handMenu != null)
        {
            bool iAmDrawHand = (myHand != handMenu.menuHandedness);
            if (!iAmDrawHand)
            {
                isHeld = false;
                if (s_currentDrawer == this) s_currentDrawer = null;
                drawing.StopDrawing();
                return;
            }
        }

        if (s_currentDrawer == this && isHeld)
            drawing.UpdateDrawing();

        UpdateWidthFromStick();
    }

    bool IsOverUI()
    {
        if (!blockDrawingWhenHoveringUI) return false;

        if (uiInputModule == null)
            uiInputModule = UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();

        if (uiInputModule == null) return false;
        if (nearFarInteractor == null) return false;

        // Get the pointer ID specifically for this interactor
        if (nearFarInteractor.TryGetUIModel(out var uiModel))
        {
            return uiInputModule.IsPointerOverGameObject(uiModel.pointerId);
        }

        return false;
    }

    // Called when the trigger button on the controller is pressed (also called "Activate" in the input system).
    void OnActivateStarted(InputAction.CallbackContext ctx)
    {
        if (s_currentDrawer != null && s_currentDrawer != this && !s_currentDrawer.isHeld)
            s_currentDrawer = null;
        
        if (blockDrawingWhenHoveringUI && IsOverUI())
            return;

        if (drawing == null || controllerTip == null) return;
        if (!drawing.penEnabled) return;

        // Optional: only dominant hand can draw
        if (onlyDominantHandCanDraw && handMenu != null)
        {
            bool iAmDrawHand = (myHand != handMenu.menuHandedness);
            if (!iAmDrawHand)
                return;
        }

        if (s_currentDrawer != null && s_currentDrawer != this)
            return;

        s_currentDrawer = this;

        drawing.tip = controllerTip;
        isHeld = true;
        drawing.StartDrawing();
    }

    // Called when the trigger button on the controller is released (also called "Activate" in the input system).
    void OnActivateCanceled(InputAction.CallbackContext ctx)
    {
        if (!isHeld) return;

        isHeld = false;

        if (s_currentDrawer == this)
        {
            drawing.StopDrawing();
            s_currentDrawer = null;
        }
    }

    void OnUndoPerformed(InputAction.CallbackContext ctx)
    {
        if (drawing == null || !drawing.penEnabled) return;
        drawing.UndoLastStroke();
    }

    void OnGrabStarted(InputAction.CallbackContext ctx)
    {
        SimpleHandGestures.SetControllerGrabbing(myHand, true);
    }

    void OnGrabCanceled(InputAction.CallbackContext ctx)
    {
        SimpleHandGestures.SetControllerGrabbing(myHand, false);
    }

    // Updates the pen width based on the thumbstick input.
    void UpdateWidthFromStick()
    {
        if (stick == null || drawing == null) return;

        Vector2 v = stick.action.ReadValue<Vector2>();
        float x = v.x;

        if (Mathf.Abs(x) < deadzone) return;

        float newWidth = drawing.penWidth + x * widthChangePerSecond * Time.deltaTime;
        newWidth = Mathf.Clamp(newWidth, minWidth, maxWidth);

        drawing.SetPenWidth(newWidth);

        if (widthSlider != null)
            widthSlider.SetValueWithoutNotify(newWidth);
    }

    static void Enable(InputActionReference a)
    {
        if (a?.action == null) return;
        if (!a.action.enabled) a.action.Enable();
    }
}
