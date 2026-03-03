using UnityEngine;

// Manually builds a simple organ selection grid and hooks cards to the organ switcher.
public class OrganPickerUIManual : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private OrganCardUI cardPrefab;

    [Header("Organs (manual for now)")]
    // [SerializeField] private Sprite brainThumb;
    // [SerializeField] private Sprite heartThumb;

    [SerializeField] private OrganSwitcher switcher;

    void Start()
    {
        Build();
    }

    public void Build()
    {
        // Clear existing
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        // Brain card
        var brainCard = Instantiate(cardPrefab, gridParent);
        brainCard.Bind("Brain", () =>
        {
            switcher.ShowBrain();
        });

        // Heart card
        var heartCard = Instantiate(cardPrefab, gridParent);
        heartCard.Bind("Heart", () =>
        {
            switcher.ShowHeart();
        });
    }
}