using UnityEngine;

// Caches the original transform and resets the model back to its starting pose and scale.
public class ResetTransform : MonoBehaviour
{
    Vector3 initialPosition;
    Quaternion initialRotation;
    Vector3 initialScale;

    void Awake()
    {
        // Cache original transform
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
    }

    public void ResetModel()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
    }
}
