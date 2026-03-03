using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

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
    public HandSide myHand;                 // Left or Right
    public InputActionReference grabAction; // GripButton

    [Header("UI Block (Near-Far)")]
    [SerializeField] XRUIInputModule uiInputModule; // drag XR UI Input Module here
    [SerializeField] int pointerId; // set unique id per controller
    [SerializeField] bool blockDrawingWhenHoveringUI = true;

    static ControllerPenDriver s_currentDrawer;
    bool isHeld;

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

        if (s_currentDrawer == this && isHeld)
            drawing.UpdateDrawing();

        UpdateWidthFromStick();
    }

    bool IsOverUI()
    {
        if (uiInputModule == null) return false;
        return uiInputModule.IsPointerOverGameObject(pointerId);
    }

    void OnActivateStarted(InputAction.CallbackContext ctx)
    {
        //if (myHand != SetHandedness.DrawHand)
        //    return;

        if (blockDrawingWhenHoveringUI && IsOverUI())
            return;
        
        if (drawing == null || controllerTip == null) return;
        if (!drawing.penEnabled) return;

        if (s_currentDrawer != null && s_currentDrawer != this)
            return;

        s_currentDrawer = this;

        drawing.tip = controllerTip;
        isHeld = true;
        drawing.StartDrawing();
    }

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
