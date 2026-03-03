using UnityEngine;
using UnityEngine.UI;

// UI card showing an organ entry and invoking a callback when clicked.
public class OrganCardUI : MonoBehaviour
{
    // [SerializeField] private Image thumbnailImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button button;

    private System.Action onClick;

    public void Bind(string displayName, System.Action onClick)
    {
        // thumbnailImage.sprite = thumbnail;
        nameText.text = displayName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}