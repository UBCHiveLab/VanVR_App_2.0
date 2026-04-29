using UnityEngine;

public class AnnotationSpawner : MonoBehaviour
{
    [Header("References")]
    public OrganDefinition organDefinition;
    public GameObject annotationMarkerPrefab;

    [Header("Settings")]
    public Transform headTransform;
    public float markerWorldSize = 0.005f;

    public void SpawnAnnotations()
    {
        if (organDefinition == null)
        {
            Debug.LogWarning($"[AnnotationSpawner] No OrganDefinition on {gameObject.name}");
            return;
        }

        if (organDefinition.annotations == null || organDefinition.annotations.points.Count == 0)
            return;

        if (annotationMarkerPrefab == null)
        {
            Debug.LogWarning($"[AnnotationSpawner] No marker prefab on {gameObject.name}");
            return;
        }

        if (headTransform == null)
            headTransform = Camera.main?.transform;

        foreach (var point in organDefinition.annotations.points)
        {
            GameObject go = Instantiate(annotationMarkerPrefab, transform);
            go.name = $"Annotation_{point.id}_{point.label}";

            Vector3 dirFromCenter = point.localPosition.normalized;
            go.transform.localPosition = point.localPosition + dirFromCenter * 0.005f;
            go.transform.localRotation = Quaternion.identity;

            var marker = go.GetComponent<AnnotationMarker>();
            if (marker != null)
            {
                marker.markerWorldSize = markerWorldSize;
                marker.Initialize(point, headTransform);
            }
        }
    }
}