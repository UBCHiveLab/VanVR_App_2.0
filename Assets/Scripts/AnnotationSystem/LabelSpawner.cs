using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the full lifecycle of annotation labels for one organ.
/// 
/// Setup:
///   - Assign annotationSet (the ScriptableObject on the OrganDefinition)
///   - Assign organRoot (the organ's root Transform — the one that gets grabbed/rotated)
///   - Assign labelPrefab
///   - Populate slots[] with the LabelSlot components around the organ
///   - Assign labelsParent (an empty GameObject to keep the hierarchy tidy)
///
/// Usage:
///   Call ToggleAnnotation(annotationPoint) from your interaction system
///   (e.g. when the user clicks a marker dot on the mesh).
/// </summary>
public class LabelSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AnnotationSet annotationSet;

    [Header("Scene References")]
    [SerializeField] private Transform organRoot;
    // public void SetOrganRoot(Transform root) => organRoot = root;

    [Header("Prefab & Slots")]
    [SerializeField] private AnnotationLabelUI labelPrefab;
    [SerializeField] private LabelSlot[] slots;

    [Header("Follow Settings")]
    // private Vector3 organPositionAtLastFollow;

    [Tooltip("Optional parent for instantiated labels — keeps the hierarchy clean.")]
    [SerializeField] private Transform labelsParent;

    // Active labels: annotation point → live label instance
    private readonly Dictionary<AnnotationPoint, AnnotationLabelUI> activeLabels
        = new Dictionary<AnnotationPoint, AnnotationLabelUI>();

    // -----------------------------------------------------------------------

    /// <summary>
    /// Toggle an annotation on or off.
    /// Called from AnnotationMarker (the dot on the mesh) onClick.
    /// </summary>
    public void ToggleAnnotation(AnnotationPoint point)
    {
        if (activeLabels.ContainsKey(point))
            DespawnLabel(point);
        else
            SpawnLabel(point);
    }

    // -----------------------------------------------------------------------
    // Spawn

    public void SetOrganRoot(Transform root) 
    {
        organRoot = root;
    }
    
    private void SpawnLabel(AnnotationPoint point)
    {
        LabelSlot slot = FindNearestFreeSlot(point);

        Vector3 spawnPosition;
        bool hasSlot = slot != null;

        if (hasSlot)
        {
            spawnPosition = slot.transform.position;
        }
        else
        {
            // Fallback: project outward from organ center through annotation point
            spawnPosition = ComputeFallbackPosition(point);
            Debug.LogWarning($"[LabelSpawner] No free slots remaining — using fallback position for '{point.label}'.");
        }

        AnnotationLabelUI label = Instantiate(labelPrefab, spawnPosition, Quaternion.identity, labelsParent);
        label.Initialize(point, organRoot, spawnPosition);
        label.OnClosed += OnLabelClosed;

        activeLabels[point] = label;

        if (hasSlot)
            slot.Occupy(label);
    }

    // -----------------------------------------------------------------------
    // Despawn

    private void DespawnLabel(AnnotationPoint point)
    {
        if (!activeLabels.TryGetValue(point, out AnnotationLabelUI label)) return;

        FreeSlotForLabel(label);
        label.OnClosed -= OnLabelClosed;
        Destroy(label.gameObject);
        activeLabels.Remove(point);
    }

    private void OnLabelClosed(AnnotationLabelUI label)
    {
        if (label.AnnotationPoint != null)
            DespawnLabel(label.AnnotationPoint);
    }

    // -----------------------------------------------------------------------
    // Slot assignment

    /// <summary>
    /// Returns the free slot whose world position is closest to the annotation
    /// point's world position. Minimizes line-renderer crossings naturally.
    /// </summary>
    private LabelSlot FindNearestFreeSlot(AnnotationPoint point)
    {
        Vector3 annotationWorld = organRoot.TransformPoint(point.localPosition);

        LabelSlot nearest = null;
        float nearestDist = float.MaxValue;

        foreach (LabelSlot slot in slots)
        {
            if (slot == null || slot.IsOccupied) continue;

            float dist = Vector3.Distance(slot.transform.position, annotationWorld);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = slot;
            }
        }

        return nearest;
    }

    private void FreeSlotForLabel(AnnotationLabelUI label)
    {
        foreach (LabelSlot slot in slots)
        {
            if (slot != null && slot.OccupyingLabel == label)
            {
                slot.Free();
                return;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Fallback position (when all slots are occupied)

    private Vector3 ComputeFallbackPosition(AnnotationPoint point)
    {
        Vector3 annotationWorld = organRoot.TransformPoint(point.localPosition);
        Vector3 dirFromCenter = (annotationWorld - organRoot.position).normalized;

        // Push the label out by a fixed world-space radius beyond the organ
        float labelRadius = 0.25f;
        return annotationWorld + dirFromCenter * labelRadius;
    }

    // -----------------------------------------------------------------------
    // Public API

    /// <summary>Despawn all active labels at once.</summary>
    public void ClearAll()
    {
        // Copy keys to avoid modifying the dict while iterating
        var points = new List<AnnotationPoint>(activeLabels.Keys);
        foreach (var point in points)
            DespawnLabel(point);
    }

    public bool IsActive(AnnotationPoint point) => activeLabels.ContainsKey(point);
}