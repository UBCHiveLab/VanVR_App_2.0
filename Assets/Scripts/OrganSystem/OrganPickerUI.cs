using UnityEngine;

public class OrganPickerUI : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private OrganCardUI cardPrefab;

    [Header("Data")]
    [SerializeField] private OrgansCatalog catalog;

    [Header("Action")]
    [SerializeField] private OrganSpawner spawner;

    void Start()
    {
        Build();
    }

    public void Build()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var def in catalog.organs)
        {
            if (def == null) continue;

            var card = Instantiate(cardPrefab, gridParent);

            // Your current OrganCardUI only takes a name + click action
            card.Bind(def.displayName, () => spawner.Select(def));
        }
    }
}