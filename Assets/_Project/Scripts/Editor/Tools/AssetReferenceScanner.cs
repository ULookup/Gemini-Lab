#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor
{
    /// <summary>
    /// Scans project assets (ScriptableObjects, Prefabs) for missing or broken references.
    /// Detects missing Sprites, Materials, MonoScripts, and null serialized fields.
    /// </summary>
    public sealed class AssetReferenceScanner : EditorWindow
    {
        private readonly List<ScanResult> _results = new();
        private Vector2 _scrollPos;
        private bool _showOnlyBroken = true;
        private string _statusText = "Ready.";
        private int _scannedCount, _brokenCount;

        [MenuItem("Tools/Gemini-Lab/Asset Reference Scanner")]
        private static void Open()
        {
            var window = GetWindow<AssetReferenceScanner>(false, "Asset Ref Scanner");
            window.minSize = new Vector2(750f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan All SO & Prefab", GUILayout.Width(160), GUILayout.Height(28)))
                RunScan();
            if (GUILayout.Button("Scan Selected", GUILayout.Width(110), GUILayout.Height(28)))
                RunScanSelected();
            EditorGUILayout.LabelField(_statusText, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            _showOnlyBroken = EditorGUILayout.ToggleLeft("Show only assets with broken references", _showOnlyBroken);

            EditorGUILayout.Space(4);

            // Summary
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Scanned: {_scannedCount}", GUILayout.Width(100));
            GUI.color = _brokenCount > 0 ? Color.red : Color.green;
            EditorGUILayout.LabelField($"Broken: {_brokenCount}", GUILayout.Width(100));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"Healthy: {_scannedCount - _brokenCount}", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Asset", EditorStyles.toolbarButton, GUILayout.Width(300));
            EditorGUILayout.LabelField("Type", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Issues", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var r in _results)
            {
                if (_showOnlyBroken && !r.HasIssues) continue;
                DrawResult(r);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawResult(ScanResult r)
        {
            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (r.HasIssues)
                EditorGUI.DrawRect(rect, new Color(0.4f, 0.1f, 0.05f, 0.6f));

            if (GUILayout.Button(r.AssetPath, EditorStyles.label, GUILayout.Width(300)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(r.AssetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            EditorGUILayout.LabelField(r.AssetType, GUILayout.Width(80));

            if (r.HasIssues)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField(string.Join("; ", r.Issues), EditorStyles.wordWrappedLabel);
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("OK");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RunScan()
        {
            _results.Clear();
            _scannedCount = _brokenCount = 0;
            _statusText = "Scanning...";
            Repaint();

            // Scan all ScriptableObjects
            var assetGuids = AssetDatabase.FindAssets("t:ScriptableObject t:Prefab");
            var scannedPaths = new HashSet<string>();

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/_Project/")) continue;
                if (!scannedPaths.Add(path)) continue;

                var result = ScanAsset(path);
                _results.Add(result);
                _scannedCount++;
                if (result.HasIssues) _brokenCount++;
            }

            _results.Sort((a, b) =>
            {
                int cmp = a.HasIssues.CompareTo(b.HasIssues);
                if (cmp != 0) return 1 - cmp; // Broken first
                return string.CompareOrdinal(a.AssetPath, b.AssetPath);
            });

            _statusText = $"Done. Scanned {_scannedCount} assets, {_brokenCount} with issues.";
        }

        private void RunScanSelected()
        {
            _results.Clear();
            _scannedCount = _brokenCount = 0;
            _statusText = "Scanning selected...";
            Repaint();

            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                var result = ScanAsset(path);
                _results.Add(result);
                _scannedCount++;
                if (result.HasIssues) _brokenCount++;
            }

            _statusText = $"Done. Scanned {_scannedCount} assets, {_brokenCount} with issues.";
        }

        private static ScanResult ScanAsset(string assetPath)
        {
            var result = new ScanResult
            {
                AssetPath = assetPath,
                AssetType = Path.GetExtension(assetPath).ToLowerInvariant() switch
                {
                    ".asset" => "SO",
                    ".prefab" => "Prefab",
                    _ => "Other"
                }
            };

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                result.AddIssue("Cannot load asset (corrupted GUID?)");
                return result;
            }

            // Use SerializedObject to check all serialized references
            var so = new SerializedObject(asset);
            var prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (prop.objectReferenceValue != null)
                    continue;

                // Object reference is null — check if this is a real problem
                if (prop.objectReferenceInstanceIDValue == 0)
                    continue; // Truly null, not "missing"

                // Missing reference (instanceID exists but object is null)
                result.AddIssue($"Missing ref: {prop.displayName} ({prop.type})");
            }

            // For prefabs, also check child components
            if (result.AssetType == "Prefab")
            {
                var go = asset as GameObject;
                if (go != null)
                {
                    CheckGameObjectRecursive(go.transform, result, string.Empty);
                }
            }

            // For SOs, check if it's a FurnitureDefinitionSO with missing sprite
            if (result.AssetType == "SO")
            {
                var defSo = asset as Modules.Furniture.FurnitureDefinitionSO;
                if (defSo != null && defSo.Sprite == null && !string.IsNullOrEmpty(defSo.Id))
                {
                    result.AddIssue("FurnitureDefinitionSO has no Sprite assigned");
                }
            }

            return result;
        }

        private static void CheckGameObjectRecursive(Transform t, ScanResult result, string parentPath)
        {
            var path = string.IsNullOrEmpty(parentPath) ? t.name : $"{parentPath}/{t.name}";
            var components = t.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    result.AddIssue($"Missing component on '{path}'");
                    continue;
                }

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                bool enterChildren = true;
                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (prop.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    if (prop.objectReferenceValue != null)
                        continue;
                    if (prop.objectReferenceInstanceIDValue == 0)
                        continue;

                    result.AddIssue($"Missing ref on '{path}': {comp.GetType().Name}.{prop.displayName}");
                }
            }

            for (int i = 0; i < t.childCount; i++)
                CheckGameObjectRecursive(t.GetChild(i), result, path);
        }

        private sealed class ScanResult
        {
            public string AssetPath = string.Empty;
            public string AssetType = string.Empty;
            public bool HasIssues => _issues.Count > 0;
            public List<string> Issues => _issues;
            private readonly List<string> _issues = new();

            public void AddIssue(string issue)
            {
                if (!_issues.Contains(issue))
                    _issues.Add(issue);
            }
        }
    }
}
