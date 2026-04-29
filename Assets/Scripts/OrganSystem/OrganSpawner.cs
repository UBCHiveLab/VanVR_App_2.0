using UnityEngine;

public class OrganSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private OrgansCatalog catalog;

    [Header("Spawn")]
    [SerializeField] private Transform organParent;
    [SerializeField] private Vector3 localSpawnPos = new Vector3(0f, 0f, 0.9f);
    [SerializeField] private Vector3 localSpawnEuler = Vector3.zero;

    [Header("Scene refs")]
    [SerializeField] private AppSceneReferences sceneRefs;
    [SerializeField] private HandDrawingSystem drawingSystem;
    [SerializeField] private LabelSpawner labelSpawner;

    private LayerSelectionManagerManual currentLayerSelectionManager;

    private GameObject currentInstance;
    private OrganDefinition currentDef;

    public void SelectById(string organId)
    {
        if (catalog == null) { Debug.LogError("OrgansCatalog missing"); return; }

        var def = catalog.organs.Find(o => o != null && o.organId == organId);
        if (def == null) { Debug.LogError($"No organ with id: {organId}"); return; }

        Select(def);
    }

    public void Select(OrganDefinition def)
    {
        if (def == null || def.prefab == null)
        {
            Debug.LogError("OrganDefinition or prefab is null");
            return;
        }

        if (currentDef == def) return;

        if (currentInstance != null)
        {
            if (labelSpawner != null)
                labelSpawner.ClearAll();
            Destroy(currentInstance);
            currentInstance = null;
        }

        currentDef = def;

        if (organParent == null) { Debug.LogError("organParent is null"); return; }

        currentInstance = Instantiate(def.prefab, organParent);
        Debug.Log("Instantiate OK");

        currentInstance.transform.SetParent(organParent, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        // currentInstance.transform.localScale = Vector3.one;
        Debug.Log("Transform OK");

        var annotationSpawner = currentInstance.GetComponent<AnnotationSpawner>();
        if (annotationSpawner != null)
            annotationSpawner.SpawnAnnotations();
        
        if (labelSpawner != null)
            labelSpawner.SetOrganRoot(currentInstance.transform);

        var brainBinder = currentInstance.GetComponentInChildren<BrainSceneBinder>(true);
        if (brainBinder != null && sceneRefs != null)
        {
            brainBinder.Bind(sceneRefs);
            Debug.Log("BrainBinder OK");
        }

        if (drawingSystem != null)
        {
            var ctx = currentInstance.GetComponentInChildren<OrganContext>(true);
            var target = (ctx != null && ctx.DrawingTarget != null) ? ctx.DrawingTarget : currentInstance.transform;
            drawingSystem.SetTargetObject(target, true);
            Debug.Log("DrawingSystem OK");
        }
    }

    public void ResetCurrentOrgan()
    {
        if (currentInstance == null)
        {
            Debug.LogWarning("No active organ to reset.");
            return;
        }

        ResetTransform reset = currentInstance.GetComponent<ResetTransform>();

        if (reset == null)
        {
            Debug.LogWarning("Active organ does not have ResetTransform.");
            return;
        }

        reset.ResetModel();
    }

    public void SetBrainSelectionMode(bool enabled)
    {
        Debug.Log($"[Organdddder] SetCurrentOrganSelectionMode({enabled})");

        if (currentInstance == null)
        {
            Debug.LogWarning("[OrganSpawner] No active organ instance.");
            return;
        }

        Debug.Log($"[OrganSpawner] currentInstance = {currentInstance.name}");

        if (currentLayerSelectionManager == null)
        {
            Debug.LogWarning("[OrganSpawner] currentLayerSelectionManager is null.");
            currentLayerSelectionManager = currentInstance.GetComponentInChildren<LayerSelectionManagerManual>(true);
        }

        if (currentLayerSelectionManager == null)
        {
            Debug.LogWarning("[OrganSpawner] Current organ has no LayerSelectionManagerManual.");
            return;
        }

        Debug.Log($"[OrganSpawner] Forwarding to manager on {currentLayerSelectionManager.gameObject.name}");
        currentLayerSelectionManager.SetSelectionMode(enabled);
    }

    public void ToggleCurrentOrganLayerSelection(Transform pointer)
    {
        if (currentLayerSelectionManager == null)
        {
            Debug.LogWarning("[OrganSpawner] No current layer selection manager.");
            return;
        }

        if (!currentLayerSelectionManager.selectionModeEnabled)
            return;

        currentLayerSelectionManager.ToggleSelectionFromPointer(pointer);
    }
}