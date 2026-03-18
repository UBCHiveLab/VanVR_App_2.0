using System;
using System.Reflection;
using UnityEngine;

public class BrainSceneBinder : MonoBehaviour
{
    [Header("Prefab script to bind")]
    [SerializeField] private LayerSelectionManagerManual layerSelectionManagerManual; // LayerSelectionManagerManual

    [Header("Prefab-internal reference (optional)")]
    [SerializeField] private Transform layersRoot; // if your script needs it; safe to keep internal

    public void Bind(AppSceneReferences refs)
    {
        if (refs == null)
        {
            Debug.LogError("[BrainSceneBinder] AppSceneReferences is null.");
            return;
        }

        if (layerSelectionManagerManual == null)
            layerSelectionManagerManual = GetComponentInChildren<LayerSelectionManagerManual>(true);

        if (layerSelectionManagerManual == null)
        {
            Debug.LogError("[BrainSceneBinder] LayerSelectionManagerManual not assigned/found.");
            return;
        }

        if (layersRoot == null)
            layersRoot = transform.Find("BrainLayers") ?? transform;

        SetMember(layerSelectionManagerManual, new[] { "rightControllerAim", "RightControllerAim" }, refs.rightControllerAim);
        SetMember(layerSelectionManagerManual, new[] { "leftControllerAim",  "LeftControllerAim"  }, refs.leftControllerAim);
        SetMember(layerSelectionManagerManual, new[] { "rightHandAim",       "RightHandAim"       }, refs.rightHandAim);
        SetMember(layerSelectionManagerManual, new[] { "leftHandAim",        "LeftHandAim"        }, refs.leftHandAim);

        SetMember(layerSelectionManagerManual, new[] { "drawingSystem", "DrawingSystem" }, refs.drawingSystem);

        SetMember(layerSelectionManagerManual,
            new[] { "drawingDriversToDisable", "DrawingDriversToDisable" },
            refs.drawingDriversToDisable);

        SetMember(layerSelectionManagerManual, new[] { "layersRoot", "LayersRoot" }, layersRoot);

        layerSelectionManagerManual.RebuildLayerCache();
    }

    static void SetMember(object target, string[] names, object value)
    {
        if (target == null) return;

        var type = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var name in names)
        {
            var field = type.GetField(name, flags);
            if (field != null)
            {
                if (CanAssign(field.FieldType, value))
                    field.SetValue(target, value);
                return;
            }

            var prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanWrite)
            {
                if (CanAssign(prop.PropertyType, value))
                    prop.SetValue(target, value);
                return;
            }
        }
    }

    static bool CanAssign(Type destType, object value)
    {
        if (value == null) return !destType.IsValueType;
        return destType.IsAssignableFrom(value.GetType());
    }
}