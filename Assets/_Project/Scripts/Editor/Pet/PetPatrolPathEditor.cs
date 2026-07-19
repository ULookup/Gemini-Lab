#nullable enable
using GeminiLab.Modules.Pet;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor
{
    [CustomEditor(typeof(PetPatrolPath))]
    public sealed class PetPatrolPathEditor : UnityEditor.Editor
    {
        private PetPatrolPath _path = null!;
        private int _selectedWaypoint = -1;
        private Tool _previousTool;

        private static readonly Color LineColor = new(0.3f, 0.85f, 0.5f);
        private static readonly Color PointColor = new(0.2f, 0.9f, 0.3f);
        private static readonly Color SelectedColor = new(1f, 0.8f, 0.2f);
        private const float HandleSize = 0.2f;
        private const float PickSize = 0.3f;

        private void OnEnable()
        {
            _path = (PetPatrolPath)target;
        }

        private void OnSceneGUI()
        {
            if (_path == null) return;

            var pathTransform = _path.transform;
            var waypointCount = _path.GetWaypointCount();

            Handles.color = LineColor;

            // Draw lines between waypoints
            for (int i = 0; i < waypointCount - 1; i++)
            {
                Handles.DrawLine(_path.GetWorldWaypoint(i), _path.GetWorldWaypoint(i + 1), 2f);
            }

            if (_path.Loop && waypointCount > 1)
            {
                Handles.DrawLine(_path.GetWorldWaypoint(waypointCount - 1), _path.GetWorldWaypoint(0), 2f);
            }

            // Draw waypoint handles
            for (int i = 0; i < waypointCount; i++)
            {
                var worldPos = _path.GetWorldWaypoint(i);
                var size = HandleUtility.GetHandleSize(worldPos) * HandleSize;

                Handles.color = i == _selectedWaypoint ? SelectedColor : PointColor;

                if (Handles.Button(worldPos, Quaternion.identity, size, size * 1.2f, Handles.SphereHandleCap))
                {
                    _selectedWaypoint = i;
                    Repaint();
                }

                if (i == _selectedWaypoint)
                {
                    EditorGUI.BeginChangeCheck();
                    var newPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_path, "Move Patrol Waypoint");
                        _path.MoveWaypoint(i, (Vector2)(newPos - pathTransform.position));
                    }
                }
            }

            // Add waypoint by clicking on empty space
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                Event.current.shift)
            {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var plane = new Plane(Vector3.forward, pathTransform.position);
                if (plane.Raycast(ray, out float dist))
                {
                    var hitPoint = ray.GetPoint(dist);
                    var localPos = (Vector2)(hitPoint - pathTransform.position);

                    // Check if near existing waypoint
                    bool nearExisting = false;
                    for (int i = 0; i < waypointCount; i++)
                    {
                        if (Vector2.Distance(_path.GetWorldWaypoint(i), hitPoint) < HandleSize * 2f)
                        {
                            _selectedWaypoint = i;
                            nearExisting = true;
                            break;
                        }
                    }

                    if (!nearExisting)
                    {
                        Undo.RecordObject(_path, "Add Patrol Waypoint");
                        _path.AddWaypoint(localPos);
                        _selectedWaypoint = _path.GetWaypointCount() - 1;
                        Event.current.Use();
                    }
                }
            }

            // Delete selected waypoint with Delete key
            if (_selectedWaypoint >= 0 && Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
            {
                Undo.RecordObject(_path, "Delete Patrol Waypoint");
                _path.RemoveWaypoint(_selectedWaypoint);
                _selectedWaypoint = Mathf.Min(_selectedWaypoint, waypointCount - 1);
                Event.current.Use();
                Repaint();
            }

            // Deselect with Escape
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _selectedWaypoint = -1;
                Event.current.Use();
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var waypointsProp = serializedObject.FindProperty("_waypoints");
            var loopProp = serializedObject.FindProperty("_loop");
            var waitTimeProp = serializedObject.FindProperty("_waitTimeAtWaypoint");
            var gizmoColorProp = serializedObject.FindProperty("_gizmoColor");
            var gizmoRadiusProp = serializedObject.FindProperty("_gizmoRadius");

            EditorGUILayout.PropertyField(loopProp);
            EditorGUILayout.PropertyField(waitTimeProp);
            EditorGUILayout.PropertyField(gizmoColorProp);
            EditorGUILayout.PropertyField(gizmoRadiusProp);

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Waypoints ({_path.GetWaypointCount()})", EditorStyles.boldLabel);

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                Undo.RecordObject(_path, "Add Patrol Waypoint");
                _path.AddWaypoint(Vector2.zero);
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Clear Waypoints",
                    "Remove all waypoints?", "Clear", "Cancel"))
                {
                    Undo.RecordObject(_path, "Clear Patrol Waypoints");
                    _path.ClearWaypoints();
                    _selectedWaypoint = -1;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Scene View controls:\n" +
                "- Shift+Click: Add waypoint\n" +
                "- Click waypoint: Select\n" +
                "- Drag handle: Move selected\n" +
                "- Delete/Backspace: Remove selected\n" +
                "- Escape: Deselect",
                MessageType.Info);

            if (waypointsProp != null)
            {
                for (int i = 0; i < waypointsProp.arraySize; i++)
                {
                    var elem = waypointsProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal();

                    var bgColor = i == _selectedWaypoint ? SelectedColor : Color.clear;
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    if (bgColor != Color.clear)
                        EditorGUI.DrawRect(rect, bgColor);

                    EditorGUILayout.LabelField($"#{i}", GUILayout.Width(30));
                    EditorGUILayout.PropertyField(elem, GUIContent.none);

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        Undo.RecordObject(_path, "Remove Patrol Waypoint");
                        _path.RemoveWaypoint(i);
                        if (_selectedWaypoint >= i) _selectedWaypoint--;
                        serializedObject.Update();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
