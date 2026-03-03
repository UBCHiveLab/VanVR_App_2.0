using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

public enum HandSide { Left, Right }

public class SimpleHandGestures : MonoBehaviour
{
    [Header("Source")]
    public XRHandTrackingEvents handEvents;

    [Header("This Hand")]
    [SerializeField] HandSide handSide = HandSide.Left;

    [Header("Drawing System (to know if pen is enabled)")]
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

    [Header("Events")]
    public UnityEvent OnPinchStart;
    public UnityEvent OnPinchEnd;
    public UnityEvent OnGrabStart;
    public UnityEvent OnGrabEnd;

    bool pinching;
    bool grabbing;

    // Global draw owner (shared by both hands)
    static SimpleHandGestures s_currentDrawer;

    // For proxy anchors
    public static HandSide? CurrentDrawingHand { get; private set; }

    public static bool LeftIsGrabbing { get; private set; }
    public static bool RightIsGrabbing { get; private set; }

    static bool s_leftControllerGrabbing;
    static bool s_rightControllerGrabbing;

    public static void SetControllerGrabbing(HandSide side, bool value)
    {
        if (side == HandSide.Left) s_leftControllerGrabbing = value;
        else s_rightControllerGrabbing = value;
    }

    public static bool IsHandGrabbing(HandSide side)
    {
        bool handGrab = side == HandSide.Left ? LeftIsGrabbing : RightIsGrabbing;
        bool controllerGrab = side == HandSide.Left ? s_leftControllerGrabbing : s_rightControllerGrabbing;
        return handGrab || controllerGrab;
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

        if (handSide == HandSide.Left) LeftIsGrabbing = false;
        else RightIsGrabbing = false;

        if (singleHandDrawLock && s_currentDrawer == this)
        {
            s_currentDrawer = null;
            CurrentDrawingHand = null;
        }
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        XRHand hand = args.hand;

        if (!hand.isTracked)
        {
            SetGrab(false);
            SetPinch(false);
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
            return;
        }

        // Distances
        float dThumbIndex  = Vector3.Distance(thumb.position, index.position);
        float dThumbMiddle = Vector3.Distance(thumb.position, middle.position);
        float dThumbRing   = Vector3.Distance(thumb.position, ring.position);
        float dThumbPinky  = Vector3.Distance(thumb.position, pinky.position);

        // Candidate pinch/grab
        float pinchThresh = pinching ? pinchExit : pinchEnter;
        bool pinchCandidate = dThumbIndex < pinchThresh;

        float grabThresh = grabbing ? grabExit : grabEnter;
        bool grabCandidate =
            dThumbMiddle < grabThresh &&
            dThumbRing   < grabThresh &&
            dThumbPinky  < grabThresh;

        // ---- Decide routing based on pen mode ----
        bool penOn = (drawingSystem != null) && drawingSystem.penEnabled;

        if (pinchActsAsGrabWhenPenOff && !penOn)
        {
            // Pen OFF: pinch behaves like grab; pinch should NOT draw
            bool effectiveGrab = grabCandidate || pinchCandidate;

            SetGrab(effectiveGrab);
            SetPinch(false); // ensure drawing never starts
            return;
        }

        // Pen ON: normal behavior (grab can suppress pinch if enabled)
        SetGrab(grabCandidate);

        if (blockPinchWhileGrabbing && grabbing)
            pinchCandidate = false;

        // Palm gate only matters for starting a DRAW (pen ON path)
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
                return true; // fail-open
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
        // Starting pinch: enforce global draw lock (only relevant when pen ON)
        if (value && !pinching)
        {
            if (blockPinchWhileGrabbing && grabbing)
                return;

            if (singleHandDrawLock)
            {
                if (s_currentDrawer != null && s_currentDrawer != this)
                    return;

                s_currentDrawer = this;
                CurrentDrawingHand = handSide;
            }
            else
            {
                CurrentDrawingHand = handSide;
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
                if (CurrentDrawingHand == handSide)
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

        if (handSide == HandSide.Left) LeftIsGrabbing = grabbing;
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
                else if (CurrentDrawingHand == handSide)
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
