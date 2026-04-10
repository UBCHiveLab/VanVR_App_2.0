using UnityEngine;

// ScriptableObject describing an organ prefab, thumbnail and display metadata.
[CreateAssetMenu(menuName = "Organs/Organ Definition")]
public class OrganDefinition : ScriptableObject
{
    public string organId;
    public string displayName;
    public Sprite thumbnail;
    public GameObject prefab;
    public AnnotationSet annotations;
}