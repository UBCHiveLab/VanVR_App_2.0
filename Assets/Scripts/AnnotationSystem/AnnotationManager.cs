using UnityEngine;
using TMPro;

public class AnnotationManager : MonoBehaviour
{
    public static AnnotationManager Instance { get; private set; }

    [Header("References")]
    public Transform panelAnchor;
    public GameObject panelRoot;           // the panel GameObject itself
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI descriptionText;

    private AnnotationMarker _currentSelected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void OnMarkerSelected(AnnotationMarker marker)
    {
        if (_currentSelected != null && _currentSelected != marker)
            _currentSelected.Deselect();

        _currentSelected = marker;

        // update panel content
        if (labelText != null)       labelText.text       = marker.data.label;
        if (descriptionText != null) descriptionText.text = marker.data.description;

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void OnMarkerDeselected(AnnotationMarker marker)
    {
        if (_currentSelected == marker)
        {
            _currentSelected = null;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}