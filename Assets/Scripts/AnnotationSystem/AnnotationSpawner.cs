using UnityEngine;

public class AnnotationSpawner : MonoBehaviour
{
    [Header("References")]
    public OrganDefinition organDefinition;
    public GameObject annotationMarkerPrefab;

    [Header("Settings")]
    public Transform headTransform;   // assigned at runtime by whatever spawns the organ

    private void Start()
    {
        SpawnAnnotations();
    }

    public void SpawnAnnotations()
    {
        Debug.Log($"[AnnotationSpawner] SpawnAnnotations called on {gameObject.name}");

        if (organDefinition == null)
        {
            Debug.LogWarning($"[AnnotationSpawner] No OrganDefinition assigned on {gameObject.name}");
            return;
        }

        Debug.Log($"[AnnotationSpawner] OrganDefinition: {organDefinition.name}");

        if (organDefinition.annotations == null)
        {
            Debug.LogWarning($"[AnnotationSpawner] annotations is null on {organDefinition.name}");
            return;
        }

        Debug.Log($"[AnnotationSpawner] Annotation points count: {organDefinition.annotations.points.Count}");

        if (organDefinition.annotations.points.Count == 0)
            return;

        if (annotationMarkerPrefab == null)
        {
            Debug.LogWarning($"[AnnotationSpawner] No AnnotationMarker prefab assigned on {gameObject.name}");
            return;
        }

        if (headTransform == null)
            headTransform = Camera.main?.transform;

        foreach (var point in organDefinition.annotations.points)
        {
            Debug.Log($"[AnnotationSpawner] Spawning point {point.id} at {point.localPosition}");

            GameObject go = Instantiate(annotationMarkerPrefab, transform);
            Vector3 dirFromCenter = point.localPosition.normalized;
            go.transform.localPosition = point.localPosition + dirFromCenter * 0.005f;
            go.transform.localRotation = Quaternion.identity;
            go.name = $"Annotation_{point.id}_{point.label}";

            var marker = go.GetComponent<AnnotationMarker>();
            if (marker != null)
                marker.Initialize(point, headTransform);
            else
                Debug.LogWarning($"[AnnotationSpawner] No AnnotationMarker component found on prefab");
        }
    }
}