using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Manages pen strokes, width, color and drawing target for hand/controller drawing.
public class HandDrawingSystem : MonoBehaviour
{
    [Header("Pen Settings")]
    public Transform tip;
    public Material baseDrawMaterial;   // renamed from drawMaterial
    public float penWidth;
    public float minPointDistance = 0.005f;

    [Header("Target Object")]
    public Transform targetObject;

    [Header("Color Settings")]
    public Color currentColor = Color.red;

    [Header("Mode")]
    public bool penEnabled = true;

    [Header("UI")]
    [SerializeField] private Slider widthSlider;
    [SerializeField] private float sliderMin = 0.002f;
    [SerializeField] private float sliderMax = 0.025f;
    [SerializeField] private float sliderValue = 0.025f;

    LineRenderer currentLine;
    readonly List<Vector3> points = new();
    Transform strokeParent;
    readonly List<GameObject> completedStrokes = new();

    [Header("Drawing State")]
    public static bool isDrawing {get; private set;}
    public float blockMenuUntilTime;

    void Start()
    {
        if (widthSlider != null)
        {
            widthSlider.minValue = sliderMin;
            widthSlider.maxValue = sliderMax;
            penWidth = sliderValue;

            // Clamp + sync without firing the event
            penWidth = Mathf.Clamp(penWidth, sliderMin, sliderMax);
            widthSlider.SetValueWithoutNotify(penWidth);
        }
    }
    
    public void SetPenEnabled(bool enabled)
    {
        penEnabled = enabled;

        // If you turn pen off mid-stroke, stop cleanly
        if (!penEnabled)
            StopDrawing();
    }

    public void SetDrawingState(bool isDrawing, float cooldown = 0.15f)
    {
        HandDrawingSystem.isDrawing = isDrawing;
        if (isDrawing)
        {
            blockMenuUntilTime = Time.time + cooldown;
        }
    }
    
    void EnsureStrokeParent()
    {
        if (targetObject != null && strokeParent == null)
        {
            GameObject parentObject = new GameObject("DrawingContainer");
            parentObject.transform.SetParent(targetObject);
            parentObject.transform.localPosition = Vector3.zero;
            parentObject.transform.localRotation = Quaternion.identity;
            parentObject.transform.localScale = Vector3.one;
            strokeParent = parentObject.transform;
        }
    }

    void ApplyWidthToCurrentLine()
    {
        if (currentLine == null) return;

        currentLine.startWidth = penWidth;
        currentLine.endWidth = penWidth;
    }

    // Called by UI slider (already wired)
    public void SetPenWidth(float value)
    {
        penWidth = value;
        ApplyWidthToCurrentLine();
    }

    void ApplyColorToCurrentLine()
    {
        if (currentLine == null) return;

        currentLine.startColor = currentColor;
        currentLine.endColor = currentColor;

        if (currentLine.material != null)
            currentLine.material.color = currentColor;
    }

    public void StartDrawing()
    {
        if (tip == null) return;
        if (currentLine != null) return;
        if (!penEnabled) return;

        EnsureStrokeParent();

        GameObject strokeObject = new GameObject("Stroke");
        if (strokeParent != null)
        {
            strokeObject.transform.SetParent(strokeParent);
            strokeObject.transform.localPosition = Vector3.zero;
            strokeObject.transform.localRotation = Quaternion.identity;
            strokeObject.transform.localScale = Vector3.one;
        }

        currentLine = strokeObject.AddComponent<LineRenderer>();

        // IMPORTANT: instance a unique material per stroke
        currentLine.material = new Material(baseDrawMaterial);

        ApplyWidthToCurrentLine();
        ApplyColorToCurrentLine();

        currentLine.positionCount = 0;
        currentLine.useWorldSpace = (strokeParent == null);
        currentLine.numCapVertices = 4;
        currentLine.numCornerVertices = 4;
        currentLine.alignment = LineAlignment.View;

        points.Clear();
        SetDrawingState(true);
        AddPointIfFarEnough(tip.position, force: true);
    }

    public void UpdateDrawing()
    {
        if (tip == null) return;
        if (currentLine == null) return;
        if (!penEnabled) return;
        AddPointIfFarEnough(tip.position);
    }

    public void StopDrawing()
    {
        if (!penEnabled) return;
        if (currentLine != null && currentLine.gameObject != null)
            completedStrokes.Add(currentLine.gameObject);

        currentLine = null;
        points.Clear();
        SetDrawingState(false, 0.2f);
    }

    public void UndoLastStroke()
    {
        if (completedStrokes.Count == 0) return;

        GameObject lastStroke = completedStrokes[^1];
        completedStrokes.RemoveAt(completedStrokes.Count - 1);
        if (lastStroke != null) Destroy(lastStroke);
    }

    public void ClearAllStrokes()
    {
        // Delete completed strokes
        foreach (var stroke in completedStrokes)
        {
            if (stroke != null)
                Destroy(stroke);
        }
        completedStrokes.Clear();

        // Also clear the current stroke if one is in progress
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
            points.Clear();
        }
    }

    void AddPointIfFarEnough(Vector3 point, bool force = false)
    {
        if (currentLine == null) return;

        if (!force && points.Count > 0)
        {
            Vector3 lastWorldPoint = (strokeParent != null)
                ? currentLine.transform.TransformPoint(points[^1])
                : points[^1];

            if (Vector3.Distance(point, lastWorldPoint) < minPointDistance)
                return;
        }

        Vector3 localPoint = (strokeParent != null)
            ? currentLine.transform.InverseTransformPoint(point)
            : point;

        points.Add(localPoint);
        currentLine.positionCount = points.Count;
        currentLine.SetPosition(points.Count - 1, localPoint);
    }

    // Called by UI dropdown / buttons
    public void SetPenColor(Color color)
    {
        currentColor = color;

        // Optional: recolor active stroke live
        ApplyColorToCurrentLine();
    }

    public void SetTargetObject(Transform newTarget, bool clearStrokesOnSwitch = false)
    {
        if (newTarget == targetObject) return;

        // stop cleanly so the stroke doesn't get parented half-way
        if (currentLine != null) StopDrawing();

        targetObject = newTarget;

        // force EnsureStrokeParent to make a new DrawingContainer under the new target
        strokeParent = null;

        if (clearStrokesOnSwitch)
            ClearAllStrokes();
    }
}