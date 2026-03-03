using UnityEngine;

// Shows the selected organ in the scene and updates the drawing system's target accordingly.
public class OrganSwitcher : MonoBehaviour
{
    [Header("Organs in scene")]
    [SerializeField] private GameObject brainRoot;
    [SerializeField] private GameObject heartRoot;

    [Header("Optional: drawing system that needs a target")]
    [SerializeField] private HandDrawingSystem drawingSystem;
    [SerializeField] private Transform brainDrawingTarget; // BrainLayers
    [SerializeField] private Transform heartDrawingTarget; // Heart root (or any container)

    public enum Organ { Brain, Heart }

    private Organ current;

    void Start()
    {
        Show(Organ.Brain); // default
    }

    public void ShowBrain() => Show(Organ.Brain);
    public void ShowHeart() => Show(Organ.Heart);

    public void Show(Organ organ)
    {
        current = organ;

        brainRoot.SetActive(organ == Organ.Brain);
        heartRoot.SetActive(organ == Organ.Heart);

        // Update drawing target (important!)
        if (drawingSystem != null)
        {
            if (organ == Organ.Brain)
            {
                drawingSystem.SetTargetObject(brainDrawingTarget, true);
            }
            else
            {
                drawingSystem.SetTargetObject(heartDrawingTarget, true);
            }
        }

        // If you have brain layer selection UI, toggle it here
        // brainLayerUI.SetActive(organ == Organ.Brain);
    }
}