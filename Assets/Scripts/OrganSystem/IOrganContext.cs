using UnityEngine;

// Provides a drawing target transform for an organ or model.
public interface IOrganContext
{
    // Return the transform that should be used as the "Target Object" for drawing
    Transform GetDrawingTarget();
}