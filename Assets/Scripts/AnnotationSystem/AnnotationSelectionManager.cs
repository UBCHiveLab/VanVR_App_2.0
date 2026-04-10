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

    [Header("Panel")]
    public GameObject panelRoot;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI descriptionText;

    private AnnotationMarker _hoveredMarker;
    private AnnotationMarker _selectedMarker;
    private Transform _activePointer;

    void OnEnable()
    {
        rightActivateAction.action.performed += OnRightActivate;
        leftActivateAction.action.performed  += OnLeftActivate;
    }

    void OnDisable()
    {
        rightActivateAction.action.performed -= OnRightActivate;
        leftActivateAction.action.performed  -= OnLeftActivate;
    }

    void Update()
    {
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

        if (_selectedMarker == _hoveredMarker)
        {
            // deselect
            _selectedMarker.Deselect();
            _selectedMarker = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            return;
        }

        // deselect previous
        if (_selectedMarker != null)
            _selectedMarker.Deselect();

        // select new
        _selectedMarker = _hoveredMarker;
        _selectedMarker.Select();

        // update panel
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            if (labelText       != null) labelText.text       = _selectedMarker.data.label;
            if (descriptionText != null) descriptionText.text = _selectedMarker.data.description;
        }
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

        if (_hoveredMarker != null && _hoveredMarker != _selectedMarker)
            _hoveredMarker.OnHoverBegin();
    }

    private void ClearHover()
    {
        if (_hoveredMarker == null) return;
        if (_hoveredMarker != _selectedMarker)
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
}