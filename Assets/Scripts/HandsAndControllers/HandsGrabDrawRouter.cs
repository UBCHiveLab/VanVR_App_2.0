using UnityEngine;

// Routes between hand grab and draw gestures, starting strokes or manual grabs as appropriate.
public class HandsGrabDrawRouter : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor nearFarInteractor;
    public Transform pinchPoint;
    public HandDrawingSystem drawing;

    bool isGrabbing;
    bool isDrawing;

    void Update()
    {
        if (isDrawing && drawing != null)
            drawing.UpdateDrawing();
    }

    // Hook Static Hand Gesture (Draw Pinch) -> Gesture Performed
    public void OnDrawGesturePerformed()
    {
        if (isGrabbing) return;
        if (isDrawing) return;
        if (drawing == null || pinchPoint == null) return;

        drawing.tip = pinchPoint;
        drawing.StartDrawing();
        isDrawing = true;
    }

    // Hook Static Hand Gesture (Draw Pinch) -> Gesture Ended
    public void OnDrawGestureEnded()
    {
        StopDrawing();
    }

    // Hook Static Hand Gesture (Grab Gesture) -> Gesture Performed
    public void OnGrabGesturePerformed()
    {
        StopDrawing();

        if (isGrabbing) return;
        if (nearFarInteractor == null) return;
        if (!nearFarInteractor.hasHover) return;

        foreach (var h in nearFarInteractor.interactablesHovered)
        {
            if (h is UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable selectable)
            {
                nearFarInteractor.StartManualInteraction(selectable);
                isGrabbing = true;
                return;
            }
        }
    }

    // Hook Static Hand Gesture (Grab Gesture) -> Gesture Ended
    public void OnGrabGestureEnded()
    {
        if (!isGrabbing) return;
        nearFarInteractor.EndManualInteraction();
        isGrabbing = false;
    }

    void StopDrawing()
    {
        if (!isDrawing) return;
        drawing.StopDrawing();
        isDrawing = false;
    }
}