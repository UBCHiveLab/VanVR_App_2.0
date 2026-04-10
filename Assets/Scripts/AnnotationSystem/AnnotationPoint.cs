using UnityEngine;

[CreateAssetMenu(fileName = "NewAnnotationPoint", menuName = "Organ Viz/Annotation Point")]
public class AnnotationPoint : ScriptableObject
{
    public int id;
    public string label;
    [TextArea] public string description;
    public Vector3 localPosition;
}