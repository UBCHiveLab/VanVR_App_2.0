using System.Collections.Generic;
using UnityEngine;

// This component is for editor-time authoring only.
// It will be stripped from builds automatically since it lives in an Editor folder.
public class AnnotationAuthor : MonoBehaviour
{
    public OrganDefinition organDefinition;

    [System.Serializable]
    public class AuthoringPoint
    {
        public int id;
        public string label;
        [TextArea] public string description;
        public Vector3 localPosition;
    }

    public List<AuthoringPoint> points = new();
}