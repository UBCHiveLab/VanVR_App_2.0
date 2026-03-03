using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

// Detects basic hand-tracking pinch, grab and undo gestures and routes them to drawing and grab systems.
public class SimpleHandGestures : MonoBehaviour
{
    [Header("Source")]
    public XRHandTrackingEvents handEvents;

    [Header("Hand Menu (dominant hand source of truth)")]
    [Tooltip("Assign the same HandMenu used by your HandednessToggle / pen tip logic. If left null, it will auto-find one.")]
    public HandMenu handMenu;

    [Header("This Hand")]
    [Tooltip("Which hand THIS component belongs to.")]
    [SerializeField] HandMenu.MenuHandedness thisHand;

    [Header("Drawing System (to know if pen is enabled + Undo target)")]
    [SerializeField] HandDrawingSystem drawingSystem;

    [Header("Pinch behavior when pen is OFF")]
    [Tooltip("If true: when pen is OFF, pinch will act as grab (and pinch will NOT draw).")]
    public bool pinchActsAsGrabWhenPenOff = true;

    [Header("Thresholds (meters)")]
    public float pinchEnter = 0.025f;
    public float pinchExit  = 0.035f;
    public float grabEnter  = 0.030f;
    public float grabExit   = 0.040f;

    [Header("Behavior")]
    [Tooltip("If true, pinch is blocked while grab gesture is active on this hand.")]
    public bool blockPinchWhileGrabbing = true;

    [Tooltip("If true, only one hand can be in draw mode at a time.")]
    public bool singleHandDrawLock = true;

    [Header("Palm Orientation Gate (START only)")]
    [Tooltip("If enabled, pinch will only be allowed to START when the palm is facing away from the headset camera.")]
    public bool requirePalmFacingAwayFromHeadsetToStart = true;

    [Range(0f, 90f)]
    public float palmAwayMaxAngleDeg = 70f;

    public XRHandJointID palmJoint = XRHandJointID.Palm;
    public bool invertPalmAwayAxis = false;

    [Header("Off-hand Undo Gesture (Index + Middle pinch)")]
    [Tooltip("If enabled, the OFF-hand (non-dominant hand) can Undo by pinching IndexTip + MiddleTip together.")]
    public bool enableOffHandUndo = true;

    [Tooltip("Distance threshold to START undo pinch (meters).")]
    public float undoEnter = 0.018f;

    [Tooltip("Distance threshold to END undo pinch (meters).")]
    public float undoExit = 0.026f;

    [Header("Events")]
    public UnityEvent OnPinchStart;
    public UnityEvent OnPinchEnd;
    public UnityEvent OnGrabStart;
    public UnityEvent OnGrabEnd;

    bool pinching;
    bool grabbing;
    bool undoPinching;

    // Global draw owner (shared by both hands)
    static SimpleHandGestures s_currentDrawer;

    // For proxy anchors (if you use it elsewhere)
    public static HandMenu.MenuHandedness? CurrentDrawingHand { get; private set; }

    public static bool LeftIsGrabbing { get; private set; }
    public static bool RightIsGrabbing { get; private set; }

    static bool s_leftControllerGrabbing;
    static bool s_rightControllerGrabbing;

    public static void SetControllerGrabbing(HandMenu.MenuHandedness side, bool value)
    {
        if (side == HandMenu.MenuHandedness.Left) s_leftControllerGrabbing = value;
        else s_rightControllerGrabbing = value;
    }

    public static bool IsHandGrabbing(HandMenu.MenuHandedness side)
    {
        bool handGrab = side == HandMenu.MenuHandedness.Left ? LeftIsGrabbing : RightIsGrabbing;
        bool controllerGrab = side == HandMenu.MenuHandedness.Left ? s_leftControllerGrabbing : s_rightControllerGrabbing;
        return handGrab || controllerGrab;
    }

    void Awake()
    {
        if (handMenu == null)
            handMenu = FindFirstObjectByType<HandMenu>();
    }

    void OnEnable()
    {
        if (handEvents != null)
            handEvents.jointsUpdated.AddListener(OnJointsUpdated);
    }

    void OnDisable()
    {
        if (handEvents != null)
            handEvents.jointsUpdated.RemoveListener(OnJointsUpdated);

        // Clear grabbing state for this hand
        if (thisHand == HandMenu.MenuHandedness.Left) LeftIsGrabbing = false;
        else RightIsGrabbing = false;

        // Release draw lock if we owned it
        if (singleHandDrawLock && s_currentDrawer == this)
        {
            s_currentDrawer = null;
            CurrentDrawingHand = null;
        }

        pinching = false;
        grabbing = false;
        undoPinching = false;
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        if (handMenu == null)
            return; // no handedness source — fail closed to avoid weirdness

        XRHand hand = args.hand;

        if (!hand.isTracked)
        {
            SetGrab(false);
            SetPinch(false);
            undoPinching = false;
            return;
        }

        if (!TryPose(hand, XRHandJointID.ThumbTip, out var thumb) ||
            !TryPose(hand, XRHandJointID.IndexTip, out var index) ||
            !TryPose(hand, XRHandJointID.MiddleTip, out var middle) ||
            !TryPose(hand, XRHandJointID.RingTip, out var ring) ||
            !TryPose(hand, XRHandJointID.LittleTip, out var pinky))
        {
            SetGrab(false);
            SetPinch(false);
            undoPinching = false;
            return;
        }

        // Distances
        float dThumbIndex   = Vector3.Distance(thumb.position, index.position);
        float dThumbMiddle  = Vector3.Distance(thumb.position, middle.position);
        float dThumbRing    = Vector3.Distance(thumb.position, ring.position);
        float dThumbPinky   = Vector3.Distance(thumb.position, pinky.position);
        float dIndexMiddle  = Vector3.Distance(index.position, middle.position);

        // Candidate pinch/grab
        float pinchThresh = pinching ? pinchExit : pinchEnter;
        bool pinchCandidate = dThumbIndex < pinchThresh;

        float grabThresh = grabbing ? grabExit : grabEnter;
        bool grabCandidate =
            dThumbMiddle < grabThresh &&
            dThumbRing   < grabThresh &&
            dThumbPinky  < grabThresh;

        // --- Determine dominant/off-hand from HandMenu ---
        var dominant = handMenu.menuHandedness;
        bool isDrawHand = (thisHand != dominant);
        bool isOffHand = !isDrawHand;

        // ---- Off-hand Undo (Index + Middle pinch) ----
        if (enableOffHandUndo && isOffHand)
        {
            float undoThresh = undoPinching ? undoExit : undoEnter;
            bool undoCandidate = dIndexMiddle < undoThresh;

            if (undoCandidate && !undoPinching)
                drawingSystem?.UndoLastStroke();

            undoPinching = undoCandidate;
        }
        else
        {
            undoPinching = false;
        }

        // ---- Decide routing based on pen mode ----
        bool penOn = (drawingSystem != null) && drawingSystem.penEnabled;

        if (pinchActsAsGrabWhenPenOff && !penOn)
        {
            // Pen OFF: pinch behaves like grab; pinch should NOT draw
            bool effectiveGrab = grabCandidate || pinchCandidate;
            SetGrab(effectiveGrab);
            SetPinch(false);
            return;
        }

        // Pen ON:
        SetGrab(grabCandidate);

        // If NOT draw hand, never allow pinch-draw
        if (!isDrawHand)
            pinchCandidate = false;

        // Grab can suppress pinch if enabled
        if (blockPinchWhileGrabbing && grabbing)
            pinchCandidate = false;

        // Palm gate only matters for starting a draw
        if (!pinching && pinchCandidate && requirePalmFacingAwayFromHeadsetToStart)
        {
            if (!CheckPalmFacingAway(hand))
                pinchCandidate = false;
        }

        SetPinch(pinchCandidate);
    }

    bool TryPose(XRHand hand, XRHandJointID id, out Pose pose)
    {
        pose = default;
        return hand.GetJoint(id).TryGetPose(out pose);
    }

    bool CheckPalmFacingAway(XRHand hand)
    {
        Transform head = Camera.main != null ? Camera.main.transform : null;
        if (head == null)
            return true; // fail-open

        Pose palmPose;
        if (!TryPose(hand, palmJoint, out palmPose))
        {
            if (!TryPose(hand, XRHandJointID.Wrist, out palmPose))
                return true;
        }

        Vector3 handToHead = head.position - palmPose.position;
        if (handToHead.sqrMagnitude < 1e-6f)
            return true;

        handToHead.Normalize();

        Vector3 palmAway = (palmPose.rotation * Vector3.forward);
        if (invertPalmAwayAxis)
            palmAway = -palmAway;

        palmAway.Normalize();

        float cosMin = Mathf.Cos(palmAwayMaxAngleDeg * Mathf.Deg2Rad);
        float dot = Vector3.Dot(palmAway, handToHead);

        return dot >= cosMin;
    }

    void SetPinch(bool value)
    {
        // Starting pinch: enforce global draw lock
        if (value && !pinching)
        {
            if (blockPinchWhileGrabbing && grabbing)
                return;

            if (singleHandDrawLock)
            {
                if (s_currentDrawer != null && s_currentDrawer != this)
                    return;

                s_currentDrawer = this;
                CurrentDrawingHand = thisHand;
            }
            else
            {
                CurrentDrawingHand = thisHand;
            }
        }

        // Ending pinch: release global lock if we own it
        if (!value && pinching)
        {
            if (singleHandDrawLock)
            {
                if (s_currentDrawer == this)
                {
                    s_currentDrawer = null;
                    CurrentDrawingHand = null;
                }
            }
            else
            {
                if (CurrentDrawingHand == thisHand)
                    CurrentDrawingHand = null;
            }
        }

        if (value == pinching) return;

        pinching = value;
        if (pinching) OnPinchStart?.Invoke();
        else OnPinchEnd?.Invoke();
    }

    void SetGrab(bool value)
    {
        if (value == grabbing) return;

        grabbing = value;

        if (thisHand == HandMenu.MenuHandedness.Left) LeftIsGrabbing = grabbing;
        else RightIsGrabbing = grabbing;

        if (grabbing)
        {
            // If grab starts, forcibly stop drawing on this hand
            if (pinching)
            {
                pinching = false;

                if (singleHandDrawLock && s_currentDrawer == this)
                {
                    s_currentDrawer = null;
                    CurrentDrawingHand = null;
                }
                else if (CurrentDrawingHand == thisHand)
                {
                    CurrentDrawingHand = null;
                }

                OnPinchEnd?.Invoke();
            }

            OnGrabStart?.Invoke();
        }
        else
        {
            OnGrabEnd?.Invoke();
        }
    }
}
