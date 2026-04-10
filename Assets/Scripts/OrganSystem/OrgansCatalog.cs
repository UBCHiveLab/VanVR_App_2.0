using System.Collections.Generic;
using UnityEngine;

// ScriptableObject holding a catalog of available organ definitions.
[CreateAssetMenu(menuName = "Organs/Organs Catalog")]
public class OrgansCatalog : ScriptableObject
{
    public List<OrganDefinition> organs = new();
}