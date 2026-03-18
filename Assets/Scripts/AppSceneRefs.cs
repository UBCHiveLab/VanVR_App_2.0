using UnityEngine;

public class AppSceneReferences : MonoBehaviour
{
    [Header("XR Aim / Ray Origins")]
    public Transform rightControllerAim;
    public Transform leftControllerAim;
    public Transform rightHandAim;
    public Transform leftHandAim;

    [Header("Drawing")]
    public HandDrawingSystem drawingSystem;

    [Header("Pen Drivers / Tools")]
    public MonoBehaviour[] drawingDriversToDisable;
}