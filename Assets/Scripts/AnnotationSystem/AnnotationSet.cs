using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnnotationSet", menuName = "Organ Viz/Annotation Set")]
public class AnnotationSet : ScriptableObject
{
    public string organId;
    public List<AnnotationPoint> points = new();
}