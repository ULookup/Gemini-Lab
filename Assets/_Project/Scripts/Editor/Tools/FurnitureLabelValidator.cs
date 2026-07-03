#nullable enable
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor
{
    /// <summary>
    /// Scans the Apartment scene for furniture label/binding completeness.
    /// </summary>
    public sealed class FurnitureLabelValidator : EditorWindow
    {
        private readonly List<FurnitureEntry> _entries = new();
        private Vector2 _scrollPos;
        private string _scenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        private bool _showOnlyIssues = true;
        private int _totalCount, _okCount, _warningCount, _errorCount;

        [MenuItem("Tools/Gemini-Lab/Furniture Label Validator")]
        private static void Open()
        {
            var window = GetWindow<FurnitureLabelValidator>(false, "Furniture Labels");
            window.minSize = new Vector2(700f, 400f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene:", GUILayout.Width(45));
            _scenePath = EditorGUILayout.TextField(_scenePath);
            if (GUILayout.Button("Scan", GUILayout.Width(60), GUILayout.Height(22)))
                ScanScene();
            EditorGUILayout.EndHorizontal();

            _showOnlyIssues = EditorGUILayout.ToggleLeft("Show only issues", _showOnlyIssues);

            EditorGUILayout.Space(4);

            // Summary
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total: {_totalCount}", GUILayout.Width(70));
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"OK: {_okCount}", GUILayout.Width(60));
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField($"Warnings: {_warningCount}", GUILayout.Width(80));
            GUI.color = Color.red;
            EditorGUILayout.LabelField($"Errors: {_errorCount}", GUILayout.Width(70));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Object", EditorStyles.toolbarButton, GUILayout.Width(180));
            EditorGUILayout.LabelField("Hint", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUILayout.LabelField("Binding", EditorStyles.toolbarButton, GUILayout.Width(55));
            EditorGUILayout.LabelField("Furniture", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("DefId / Label", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var entry in _entries)
            {
                if (_showOnlyIssues && entry.Status == EntryStatus.OK) continue;
                DrawEntry(entry);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(FurnitureEntry e)
        {
            var bgColor = e.Status switch
            {
                EntryStatus.Error => new Color(0.35f, 0.1f, 0.1f),
                EntryStatus.Warning => new Color(0.3f, 0.25f, 0.05f),
                _ => Color.clear
            };

            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (bgColor != Color.clear)
                EditorGUI.DrawRect(rect, bgColor);

            if (GUILayout.Button(e.Name, EditorStyles.label, GUILayout.Width(180)))
            {
                if (e.GameObject != null)
                {
                    Selection.activeGameObject = e.GameObject;
                    EditorGUIUtility.PingObject(e.GameObject);
                }
            }

            EditorGUILayout.LabelField(e.HasHint ? "Yes" : "No", GUILayout.Width(50));
            EditorGUILayout.LabelField(e.HasBinding ? "Yes" : "No", GUILayout.Width(55));
            EditorGUILayout.LabelField(e.HasFurniture ? "Yes" : "No", GUILayout.Width(60));
            EditorGUILayout.LabelField(e.DetailInfo);
            EditorGUILayout.EndHorizontal();
        }

        private void ScanScene()
        {
            _entries.Clear();
            _totalCount = _okCount = _warningCount = _errorCount = 0;

            // Check if target scene is already loaded
            Scene scene = SceneManager.GetSceneByPath(_scenePath);
            if (!scene.isLoaded)
            {
                if (System.IO.File.Exists(_scenePath))
                {
                    scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Additive);
                }
                else
                {
                    Debug.LogError($"[FurnitureValidator] Scene not found: {_scenePath}");
                    return;
                }
            }

            EditorSceneManager.SetActiveScene(scene);

            var allObjects = scene.GetRootGameObjects();
            var allGOs = new List<GameObject>();
            foreach (var root in allObjects)
                CollectRecursive(root.transform, allGOs);

            // Find ApartmentSceneFurnitureBindings
            ApartmentSceneFurnitureBindings? bindings = null;
            foreach (var go in allGOs)
            {
                bindings = go.GetComponent<ApartmentSceneFurnitureBindings>();
                if (bindings != null) break;
            }

            var processed = new HashSet<GameObject>();

            foreach (var go in allGOs)
            {
                var hint = go.GetComponent<SceneFurnitureDefinitionHint>();
                var furniture = go.GetComponent<Furniture>();
                var sr = go.GetComponent<SpriteRenderer>();

                // Skip objects that are clearly not furniture
                if (hint == null && furniture == null && sr == null) continue;
                // Skip known non-furniture (cameras, canvas, etc.)
                if (go.GetComponent<Camera>() != null || go.GetComponent<Canvas>() != null) continue;
                // Skip if no SpriteRenderer and no hint/furniture (likely a container)
                if (sr == null && hint == null && furniture == null) continue;

                processed.Add(go);

                var entry = new FurnitureEntry
                {
                    GameObject = go,
                    Name = go.name,
                    HasHint = hint != null,
                    HasFurniture = furniture != null,
                };

                // Check hint data
                if (hint != null)
                {
                    entry.DefinitionId = hint.DefinitionId;
                    entry.Category = hint.Category.ToString();
                    entry.InteractionType = hint.InteractionType.ToDisplayLabel();

                    if (hint.Category == FurnitureCategory.Unknown)
                        entry.Issues.Add("Category=Unknown");
                    if (hint.InteractionType == FurnitureInteractionType.Unknown)
                        entry.Issues.Add("Interaction=Unknown");
                    if (string.IsNullOrWhiteSpace(hint.DefinitionId))
                        entry.Issues.Add("DefinitionId empty");
                }

                // Check binding
                entry.HasBinding = false;
                if (bindings != null && hint != null && !string.IsNullOrWhiteSpace(hint.DefinitionId))
                {
                    // Use serializedObject to check private bindings array
                    var so = new SerializedObject(bindings);
                    var bindingsProp = so.FindProperty("_bindings");
                    if (bindingsProp != null)
                    {
                        for (int i = 0; i < bindingsProp.arraySize; i++)
                        {
                            var elem = bindingsProp.GetArrayElementAtIndex(i);
                            var targetProp = elem.FindPropertyRelative("_target");
                            var defIdProp = elem.FindPropertyRelative("_definitionId");

                            if (targetProp?.objectReferenceValue == go ||
                                (defIdProp != null && defIdProp.stringValue == hint.DefinitionId))
                            {
                                entry.HasBinding = true;
                                break;
                            }
                        }
                    }
                }

                // Check SpriteRenderer
                if (sr == null)
                    entry.Issues.Add("No SpriteRenderer");
                else if (sr.sprite == null)
                    entry.Issues.Add("No Sprite assigned");

                // Determine status
                if (entry.Issues.Count > 0)
                {
                    entry.DetailInfo = string.Join(", ", entry.Issues);
                    entry.Status = entry.Issues.Exists(i => i.Contains("No Sprite") || i.Contains("DefinitionId empty"))
                        ? EntryStatus.Error
                        : EntryStatus.Warning;
                }
                else if (!entry.HasBinding && hint != null)
                {
                    entry.DetailInfo = "OK (unbound scene decor?)";
                    entry.Status = EntryStatus.Warning;
                }
                else
                {
                    entry.DetailInfo = $"OK | {entry.Category} | {entry.InteractionType}";
                    entry.Status = EntryStatus.OK;
                }

                _entries.Add(entry);
                _totalCount++;
                switch (entry.Status)
                {
                    case EntryStatus.OK: _okCount++; break;
                    case EntryStatus.Warning: _warningCount++; break;
                    case EntryStatus.Error: _errorCount++; break;
                }
            }

            _entries.Sort((a, b) =>
            {
                int cmp = a.Status.CompareTo(b.Status);
                if (cmp != 0) return cmp;
                return string.CompareOrdinal(a.Name, b.Name);
            });
        }

        private static void CollectRecursive(Transform t, List<GameObject> list)
        {
            list.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
                CollectRecursive(t.GetChild(i), list);
        }

        private enum EntryStatus { OK, Warning, Error }

        private sealed class FurnitureEntry
        {
            public GameObject? GameObject;
            public string Name = string.Empty;
            public bool HasHint;
            public bool HasBinding;
            public bool HasFurniture;
            public string DefinitionId = string.Empty;
            public string Category = string.Empty;
            public string InteractionType = string.Empty;
            public string DetailInfo = string.Empty;
            public EntryStatus Status;
            public readonly List<string> Issues = new();
        }
    }
}
