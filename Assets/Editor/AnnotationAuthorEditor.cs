using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(AnnotationAuthor))]
public class AnnotationAuthorEditor : Editor
{
    private bool _placementMode = false;
    private AnnotationAuthor _author;

    private GUIStyle _overlayStyle;
    private GUIStyle _selectedStyle;
    private GUIStyle _normalStyle;

    private int _selectedIndex = -1;

    private void OnEnable()
    {
        _author = (AnnotationAuthor)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        _placementMode = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Organ Definition ──────────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Annotation Authoring", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _author.organDefinition = (OrganDefinition)EditorGUILayout.ObjectField(
            "Organ Definition", _author.organDefinition, typeof(OrganDefinition), false);

        EditorGUILayout.Space(8);

        // ── Placement Mode Button ─────────────────────────────────────────
        GUI.backgroundColor = _placementMode ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.9f, 0.3f);
        if (GUILayout.Button(_placementMode ? "🔴  Exit Placement Mode" : "🟢  Enter Placement Mode", GUILayout.Height(32)))
        {
            _placementMode = !_placementMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (_placementMode)
        {
            EditorGUILayout.HelpBox("Click on the organ surface in the Scene view to place annotation markers.", MessageType.Info);
        }

        EditorGUILayout.Space(8);

        // ── Points List ───────────────────────────────────────────────────
        EditorGUILayout.LabelField($"Annotation Points ({_author.points.Count})", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        for (int i = 0; i < _author.points.Count; i++)
        {
            var point = _author.points[i];
            bool isSelected = _selectedIndex == i;

            // header row
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = isSelected ? new Color(0.6f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button($"  {point.id}.  {point.label}", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
            {
                _selectedIndex = isSelected ? -1 : i;
                // focus scene view on this point
                if (_selectedIndex == i)
                    FocusPoint(point.localPosition);
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(_author, "Remove Annotation Point");
                _author.points.RemoveAt(i);
                if (_selectedIndex >= _author.points.Count)
                    _selectedIndex = _author.points.Count - 1;
                EditorUtility.SetDirty(_author);
                break;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // expanded edit fields when selected
            if (isSelected)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();

                point.id          = EditorGUILayout.IntField("ID", point.id);
                point.label       = EditorGUILayout.TextField("Label", point.label);
                point.description = EditorGUILayout.TextArea(point.description, GUILayout.MinHeight(48));
                point.localPosition = EditorGUILayout.Vector3Field("Local Position", point.localPosition);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(_author);

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(8);

        // ── Save Button ───────────────────────────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("💾  Save to AnnotationSet", GUILayout.Height(32)))
        {
            SaveToAnnotationSet();
        }
        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }

    // ── Scene GUI ─────────────────────────────────────────────────────────

    private void OnSceneGUI(SceneView sceneView)
    {
        // draw overlay label
        if (_placementMode)
        {
            Handles.BeginGUI();

            if (_overlayStyle == null)
            {
                _overlayStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize  = 16,
                    alignment = TextAnchor.UpperCenter
                };
                _overlayStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            }

            float w = sceneView.position.width;
            GUI.Label(new Rect(0, 8, w, 30), "PLACEMENT MODE ON  —  Click organ surface to place  |  ESC to exit", _overlayStyle);

            Handles.EndGUI();
        }

        // draw gizmos for all existing points
        DrawPointGizmos(sceneView);

        // handle placement clicks
        if (!_placementMode) return;

        Event e = Event.current;

        // exit on Escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            _placementMode = false;
            sceneView.Repaint();
            e.Use();
            return;
        }

        // consume left click for placement
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (TryRaycastOrganSurface(ray, out Vector3 worldHit))
            {
                Vector3 localPos = _author.transform.InverseTransformPoint(worldHit);
                PlacePoint(localPos);
                e.Use();
            }
        }

        // keep scene view consuming input while in placement mode
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void DrawPointGizmos(SceneView sceneView)
    {
        if (_author == null) return;

        for (int i = 0; i < _author.points.Count; i++)
        {
            var point = _author.points[i];
            Vector3 world = _author.transform.TransformPoint(point.localPosition);
            bool isSelected = _selectedIndex == i;

            float size = HandleUtility.GetHandleSize(world) * 0.15f;
            Color normalCol   = isSelected ? new Color(0.4f, 0.8f, 1f)  : new Color(1f, 0.85f, 0.1f);
            Color occludedCol = isSelected ? new Color(0.2f, 0.4f, 0.5f, 0.3f) : new Color(0.5f, 0.4f, 0.0f, 0.3f);

            // ── Pass 1: normal depth-tested gizmo ────────────────────────────
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = normalCol;
            if (Handles.Button(world, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                _selectedIndex = isSelected ? -1 : i;
                if (_selectedIndex == i) FocusPoint(point.localPosition);
                Repaint();
            }

            // ── Pass 2: occluded dimmed gizmo (shows through mesh) ────────────
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.color = occludedCol;
            Handles.SphereHandleCap(0, world, Quaternion.identity, size, EventType.Repaint);

            // ── Reset zTest ───────────────────────────────────────────────────
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            // ── Label visible ─────────────────────────────────────────────────
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            GUIStyle visibleStyle = new GUIStyle(EditorStyles.boldLabel);
            visibleStyle.normal.textColor = Color.white;
            Handles.color = Color.white;
            Handles.Label(world + Vector3.up * size * 1.5f, $"{point.id}. {point.label}", visibleStyle);

            // ── Label occluded ────────────────────────────────────────────────
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            GUIStyle occludedStyle = new GUIStyle(EditorStyles.boldLabel);
            occludedStyle.normal.textColor = new Color(1f, 1f, 1f, 0.3f);
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.Label(world + Vector3.up * size * 1.5f, $"{point.id}. {point.label}", occludedStyle);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            // ── Move handle when selected ─────────────────────────────────────
            if (isSelected)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_author, "Move Annotation Point");
                    point.localPosition = _author.transform.InverseTransformPoint(newWorld);
                    EditorUtility.SetDirty(_author);
                }
            }
        }

        // make sure we always reset zTest after drawing
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
    }

    /*
    private bool TryRaycastOrganSurface(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        float closest = float.MaxValue;
        bool hit = false;

        var meshFilters = _author.GetComponentsInChildren<MeshFilter>();

        foreach (var mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Transform t = mf.transform;
            int[] tris = mesh.triangles;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                // convert all verts to world space
                Vector3 v0 = t.TransformPoint(verts[tris[i]]);
                Vector3 v1 = t.TransformPoint(verts[tris[i + 1]]);
                Vector3 v2 = t.TransformPoint(verts[tris[i + 2]]);

                // back-face culling in world space
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
                if (Vector3.Dot(normal, ray.direction) >= 0f) continue;

                if (RayTriangleIntersect(ray, v0, v1, v2, out float dist))
                {
                    if (dist < closest)
                    {
                        closest = dist;
                        hitPoint = ray.origin + ray.direction * dist;
                        hit = true;
                    }
                }
            }
        }

        return hit;
    }
    */

    private bool TryRaycastOrganSurface(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        object hit = HandleUtility.RaySnap(ray);
        if (hit != null)
        {
            RaycastHit rch = (RaycastHit)hit;
            if (rch.transform != null && 
                (rch.transform == _author.transform || rch.transform.IsChildOf(_author.transform)))
            {
                hitPoint = rch.point;
                return true;
            }
        }

        return false;
    }

    // Möller–Trumbore intersection
    private bool RayTriangleIntersect(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
    {
        t = 0f;
        Vector3 e1   = v1 - v0;
        Vector3 e2   = v2 - v0;
        Vector3 h    = Vector3.Cross(ray.direction, e2);
        float   det  = Vector3.Dot(e1, h);

        if (Mathf.Abs(det) < 1e-6f) return false;

        float   invDet = 1f / det;
        Vector3 s      = ray.origin - v0;
        float   u      = Vector3.Dot(s, h) * invDet;
        if (u < 0f || u > 1f) return false;

        Vector3 q = Vector3.Cross(s, e1);
        float   v = Vector3.Dot(ray.direction, q) * invDet;
        if (v < 0f || u + v > 1f) return false;

        t = Vector3.Dot(e2, q) * invDet;
        return t > 1e-4f;
    }

    private void PlacePoint(Vector3 localPos)
    {
        Undo.RecordObject(_author, "Place Annotation Point");

        var point = new AnnotationAuthor.AuthoringPoint
        {
            id            = _author.points.Count + 1,
            label         = "New Annotation",
            description   = "",
            localPosition = localPos
        };

        _author.points.Add(point);
        _selectedIndex = _author.points.Count - 1;
        EditorUtility.SetDirty(_author);
        Repaint();
    }

    private void FocusPoint(Vector3 localPos)
    {
        Vector3 world = _author.transform.TransformPoint(localPos);
        SceneView.lastActiveSceneView?.LookAt(world, SceneView.lastActiveSceneView.rotation, 0.3f);
    }

    private void SaveToAnnotationSet()
    {
        if (_author.organDefinition == null)
        {
            EditorUtility.DisplayDialog("Missing OrganDefinition",
                "Please assign an OrganDefinition before saving.", "OK");
            return;
        }

        // create AnnotationSet if it doesn't exist yet
        var annotationSet = _author.organDefinition.annotations;
        if (annotationSet == null)
        {
            annotationSet = ScriptableObject.CreateInstance<AnnotationSet>();
            annotationSet.organId = _author.organDefinition.name;

            string path = $"Assets/Data/Annotations/{_author.organDefinition.name}_AnnotationSet.asset";
            System.IO.Directory.CreateDirectory("Assets/Data/Annotations");
            AssetDatabase.CreateAsset(annotationSet, path);

            _author.organDefinition.annotations = annotationSet;
            EditorUtility.SetDirty(_author.organDefinition);
        }

        // clear and repopulate
        annotationSet.organId = _author.organDefinition.name;
        annotationSet.points.Clear();

        foreach (var p in _author.points)
        {
            var ap = ScriptableObject.CreateInstance<AnnotationPoint>();
            ap.id            = p.id;
            ap.label         = p.label;
            ap.description   = p.description;
            ap.localPosition = p.localPosition;
            ap.name          = $"AP_{p.id}_{p.label}";

            AssetDatabase.AddObjectToAsset(ap, annotationSet);
            annotationSet.points.Add(ap);
        }

        EditorUtility.SetDirty(annotationSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Saved", 
            $"Saved {_author.points.Count} annotation points to {annotationSet.name}.", "OK");
    }
}