using UnityEngine;

public class PalmAnchorProxy : MonoBehaviour
{
    public Transform source;     // real tracked anchor
    public HandSide myHand;      // Left or Right

    [Tooltip("If true, blocks menu when this hand is drawing.")]
    public bool blockWhileDrawing = true;

    [Tooltip("If true, blocks menu when this hand is grabbing.")]
    public bool blockWhileGrabbing = true;

    void LateUpdate()
    {
        if (!source) return;

        // Always follow position
        transform.position = source.position;

        bool blocked =
            (blockWhileDrawing && SimpleHandGestures.CurrentDrawingHand == myHand) ||
            (blockWhileGrabbing && SimpleHandGestures.IsHandGrabbing(myHand));

        if (!blocked)
        {
            // Normal follow
            transform.rotation = source.rotation;
            return;
        }

        // --- Robust invalidation ---
        // Force the proxy's "up" axis to be perpendicular to both camera forward and camera/world up,
        // so HandMenu dot checks fail reliably.
        var cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            // fallback: just keep normal rotation
            transform.rotation = source.rotation;
            return;
        }

        Vector3 invalidUp = cam.right;                 // perpendicular to cam.forward and cam.up
        Vector3 invalidForward = Vector3.Cross(invalidUp, cam.up).normalized;
        if (invalidForward.sqrMagnitude < 1e-6f)
            invalidForward = cam.forward;

        transform.rotation = Quaternion.LookRotation(invalidForward, invalidUp);
    }
}
