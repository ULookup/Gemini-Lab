#nullable enable
using System.Collections.Generic;
using System.Linq;
using GeminiLab.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor
{
    /// <summary>
    /// Scans the current scene for UI panels (Canvas objects) and reports their registration status,
    /// component health, and hierarchy depth.
    /// </summary>
    public sealed class UIPanelDiagnosticWindow : EditorWindow
    {
        private readonly List<PanelEntry> _entries = new();
        private Vector2 _scrollPos;
        private bool _showOnlyIssues = true;

        [MenuItem("Tools/Gemini-Lab/UI Panel Diagnostic")]
        private static void Open()
        {
            var window = GetWindow<UIPanelDiagnosticWindow>(false, "UI Panel Diagnostic");
            window.minSize = new Vector2(700f, 400f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Diagnose all Canvas-based UI panels in the active scene.",
                EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Scan Scene", GUILayout.Width(100), GUILayout.Height(28)))
                ScanScene();
            EditorGUILayout.EndHorizontal();

            _showOnlyIssues = EditorGUILayout.ToggleLeft("Show only panels with issues", _showOnlyIssues);

            EditorGUILayout.Space(6);

            // Summary
            int ok = _entries.Count(e => !e.HasIssues);
            int issues = _entries.Count(e => e.HasIssues);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Panels: {_entries.Count}", GUILayout.Width(120));
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"Healthy: {ok}", GUILayout.Width(90));
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField($"Issues: {issues}", GUILayout.Width(80));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Panel Name", EditorStyles.toolbarButton, GUILayout.Width(200));
            EditorGUILayout.LabelField("Canvas", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("PanelId", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Active", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUILayout.LabelField("Issues", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var entry in _entries)
            {
                if (_showOnlyIssues && !entry.HasIssues) continue;
                DrawEntry(entry);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(PanelEntry e)
        {
            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (e.HasIssues)
                EditorGUI.DrawRect(rect, new Color(0.35f, 0.1f, 0.1f, 0.5f));

            if (GUILayout.Button(e.Name, EditorStyles.label, GUILayout.Width(200)))
            {
                if (e.GameObject != null)
                {
                    Selection.activeGameObject = e.GameObject;
                    EditorGUIUtility.PingObject(e.GameObject);
                }
            }

            EditorGUILayout.LabelField(e.HasCanvas ? "Yes" : "No", GUILayout.Width(60));
            EditorGUILayout.LabelField(e.PanelId, GUILayout.Width(80));

            var activeColor = e.IsActive ? GUI.color = Color.green : GUI.color = Color.gray;
            EditorGUILayout.LabelField(e.IsActive ? "Yes" : "No", GUILayout.Width(50));
            GUI.color = Color.white;

            var issueColor = e.HasIssues ? GUI.color = Color.yellow : GUI.color = Color.white;
            EditorGUILayout.LabelField(e.IssueSummary);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void ScanScene()
        {
            _entries.Clear();

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var allGOs = new List<GameObject>();
            foreach (var root in roots)
                CollectRecursive(root.transform, allGOs);

            // Find all top-level panels (GameObjects with Canvas at root or under a UI root)
            var panelCandidates = new List<GameObject>();
            foreach (var go in allGOs)
            {
                var canvas = go.GetComponent<Canvas>();
                if (canvas == null) continue;
                // A panel is typically a direct child of a Canvas or a Canvas itself
                if (go.transform.parent == null || go.transform.parent.GetComponent<Canvas>() == null)
                {
                    panelCandidates.Add(go);
                }
            }

            // Also find all GameObjects with IUIPanel implementors
            foreach (var go in allGOs)
            {
                var panels = go.GetComponents<MonoBehaviour>();
                foreach (var mb in panels)
                {
                    if (mb is IUIPanel && !panelCandidates.Contains(go))
                        panelCandidates.Add(go);
                }
            }

            var seen = new HashSet<GameObject>();
            foreach (var go in panelCandidates)
            {
                if (!seen.Add(go)) continue;

                var entry = new PanelEntry
                {
                    GameObject = go,
                    Name = go.name,
                    IsActive = go.activeInHierarchy,
                };

                var canvas = go.GetComponent<Canvas>();
                entry.HasCanvas = canvas != null;

                // Check IUIPanel implementors
                var panelImpls = go.GetComponents<MonoBehaviour>().OfType<IUIPanel>().ToList();
                entry.PanelId = panelImpls.Count > 0 ? panelImpls[0].Id.ToString() : "Not Registered";

                if (panelImpls.Count == 0)
                    entry.AddIssue("No IUIPanel implementation");

                // Check essential components
                if (canvas != null && !canvas.enabled)
                    entry.AddIssue("Canvas disabled");

                var raycaster = go.GetComponent<GraphicRaycaster>();
                if (raycaster == null && canvas != null)
                    entry.AddIssue("No GraphicRaycaster");

                // Check for TMP references
                var tmpTexts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var tmp in tmpTexts)
                {
                    if (tmp.font == null)
                        entry.AddIssue($"TMP '{tmp.name}' has no font");
                }

                // Check for missing Image sprites
                var images = go.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.sprite == null && img.type != Image.Type.Filled)
                        entry.AddIssue($"Image '{img.name}' has no sprite");
                }

                _entries.Add(entry);
            }

            _entries.Sort((a, b) =>
            {
                int cmp = a.HasIssues.CompareTo(b.HasIssues);
                if (cmp != 0) return 1 - cmp; // Issues first
                return string.CompareOrdinal(a.Name, b.Name);
            });
        }

        private static void CollectRecursive(Transform t, List<GameObject> list)
        {
            list.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
                CollectRecursive(t.GetChild(i), list);
        }

        private sealed class PanelEntry
        {
            public GameObject? GameObject;
            public string Name = string.Empty;
            public bool IsActive;
            public bool HasCanvas;
            public string PanelId = string.Empty;
            public bool HasIssues => _issues.Count > 0;
            public string IssueSummary => _issues.Count == 0 ? "OK" : string.Join("; ", _issues);
            private readonly List<string> _issues = new();

            public void AddIssue(string issue) => _issues.Add(issue);
        }
    }
}
