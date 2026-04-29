using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class AnnotationSelectionManager : MonoBehaviour
{
    [Header("Pointers (same as your LayerSelectionManagerManual)")]
    public Transform rightControllerAim;
    public Transform leftControllerAim;
    public Transform rightHandAim;
    public Transform leftHandAim;

    [Header("Input Actions")]
    public InputActionReference rightActivateAction;
    public InputActionReference leftActivateAction;

    [Header("Raycast")]
    public float maxDistance = 5f;
    public LayerMask annotationMask = ~0;

    [Header("Label Spawner")]
    public LabelSpawner labelSpawner;

    [Header("Select Actions (for dragging)")]
    public InputActionReference rightSelectAction;
    public InputActionReference leftSelectAction;

    [Header("Drag")]
    public float dragDistance = 1f;

    private LabelDragHandle _draggedHandle;
    private Transform _draggingPointer;

    private AnnotationMarker _hoveredMarker;
    private AnnotationMarker _selectedMarker;
    private Transform _activePointer;

    void OnEnable()
    {
        rightActivateAction.action.performed += OnRightActivate;
        leftActivateAction.action.performed  += OnLeftActivate;
        rightSelectAction.action.performed   += OnRightSelectBegin;
        leftSelectAction.action.performed    += OnLeftSelectBegin;
        rightSelectAction.action.canceled    += OnSelectEnd;
        leftSelectAction.action.canceled     += OnSelectEnd;
    }

    void OnDisable()
    {
        rightActivateAction.action.performed -= OnRightActivate;
        leftActivateAction.action.performed  -= OnLeftActivate;
        rightSelectAction.action.performed   -= OnRightSelectBegin;
        leftSelectAction.action.performed    -= OnLeftSelectBegin;
        rightSelectAction.action.canceled    -= OnSelectEnd;
        leftSelectAction.action.canceled     -= OnSelectEnd;
    }

    void Update()
    {
        if (_draggedHandle != null && _draggingPointer != null)
        {
            Debug.Log("[Drag] Drag update running");
            Vector3 targetPos = _draggingPointer.position + _draggingPointer.forward * dragDistance;
            _draggedHandle.parentLabel.SetDragPosition(targetPos);
            return;
        }
        
        // Handle active drag
        if (_draggedHandle != null && _draggingPointer != null)
        {
            Vector3 targetPos = _draggingPointer.position + _draggingPointer.forward * dragDistance;
            _draggedHandle.parentLabel.SetDragPosition(targetPos);
            return; // skip hover updates while dragging
        }

        _activePointer = ChooseActivePointer();
        if (_activePointer == null)
        {
            ClearHover();
            return;
        }

        UpdateHover(_activePointer);
    }

    private void OnRightActivate(InputAction.CallbackContext ctx)
    {
        if (_hoveredMarker != null) TrySelect();
    }

    private void OnLeftActivate(InputAction.CallbackContext ctx)
    {
        if (_hoveredMarker != null) TrySelect();
    }

    private void TrySelect()
    {
        if (_hoveredMarker == null) return;

        // Toggle the marker's visual state
        if (_hoveredMarker.IsSelected)
            _hoveredMarker.Deselect();
        else
            _hoveredMarker.Select();

        // Toggle the label
        labelSpawner.ToggleAnnotation(_hoveredMarker.data);
    }

    private void UpdateHover(Transform pointer)
    {
        Ray ray = new(pointer.position, pointer.forward);
        AnnotationMarker hitMarker = null;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, annotationMask))
            hitMarker = hit.collider.GetComponentInParent<AnnotationMarker>();

        if (hitMarker == _hoveredMarker) return;

        ClearHover();
        _hoveredMarker = hitMarker;
        _hoveredMarker?.OnHoverBegin();
    }

    private void ClearHover()
    {
        if (_hoveredMarker == null) return;
        _hoveredMarker.OnHoverEnd();
        _hoveredMarker = null;
    }

    private Transform ChooseActivePointer()
    {
        if (rightControllerAim && rightControllerAim.gameObject.activeInHierarchy) return rightControllerAim;
        if (leftControllerAim  && leftControllerAim.gameObject.activeInHierarchy)  return leftControllerAim;
        if (rightHandAim       && rightHandAim.gameObject.activeInHierarchy)        return rightHandAim;
        if (leftHandAim        && leftHandAim.gameObject.activeInHierarchy)         return leftHandAim;
        return null;
    }

    private void OnRightSelectBegin(InputAction.CallbackContext ctx)
    {
        Debug.Log("[Drag] Right select began");
        TryBeginDrag(rightControllerAim != null && rightControllerAim.gameObject.activeInHierarchy 
            ? rightControllerAim : rightHandAim);
    }

    private void OnLeftSelectBegin(InputAction.CallbackContext ctx)
    {
        Debug.Log("[Drag] Left select began");
        TryBeginDrag(leftControllerAim != null && leftControllerAim.gameObject.activeInHierarchy 
            ? leftControllerAim : leftHandAim);
    }

    private void OnSelectEnd(InputAction.CallbackContext ctx)
    {
        if (_draggedHandle != null)
        {
            _draggedHandle.parentLabel.OnDragEnd();
            _draggedHandle = null;
            _draggingPointer = null;
        }
    }

    private void TryBeginDrag(Transform pointer)
    {
        if (pointer == null)
        {
            Debug.Log("[Drag] pointer is null");
            return;
        }

        Ray ray = new(pointer.position, pointer.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, annotationMask))
        {
            Debug.Log($"[Drag] Raycast hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            LabelDragHandle handle = hit.collider.GetComponentInParent<LabelDragHandle>();
            if (handle == null)
            {
                Debug.Log("[Drag] Hit object has no LabelDragHandle in parent chain");
            }
            else
            {
                Debug.Log($"[Drag] Found handle on {handle.gameObject.name}");
                Debug.Log($"[Drag] parentLabel is null: {handle.parentLabel == null}");
                
                _draggedHandle = handle;
                _draggingPointer = pointer;
                handle.parentLabel.OnDragBegin();

                dragDistance = Vector3.Distance(pointer.position, handle.parentLabel.transform.position);
                Debug.Log($"[Drag] dragDistance set to {dragDistance}, draggingPointer: {_draggingPointer.name}");
            }
        }
        else
        {
            Debug.Log("[Drag] Raycast hit nothing");
        }
    }
}