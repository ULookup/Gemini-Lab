#nullable enable
using GeminiLab.Modules.HubUI.Panels;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor
{
    public sealed class DebugDisplayWindow : EditorWindow
    {
        private const string AssetPath = "Assets/_Project/Resources/DebugDisplaySettings.asset";

        private DebugDisplaySettingsSO? _settings;
        private bool _foldoutCategories = true;
        private bool _pendingRefresh;

        [MenuItem("Tools/Gemini-Lab/Debug Display Manager")]
        private static void Open()
        {
            var window = GetWindow<DebugDisplayWindow>(false, "Debug Display");
            window.minSize = new Vector2(300f, 280f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateSettings();
        }

        private void LoadOrCreateSettings()
        {
            _settings = AssetDatabase.LoadAssetAtPath<DebugDisplaySettingsSO>(AssetPath);
            if (_settings != null)
                return;

            // Search for existing anywhere
            var guids = AssetDatabase.FindAssets("t:DebugDisplaySettingsSO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<DebugDisplaySettingsSO>(path);
                return;
            }

            // Create new
            var dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            _settings = CreateInstance<DebugDisplaySettingsSO>();
            AssetDatabase.CreateAsset(_settings, AssetPath);
            AssetDatabase.SaveAssets();
            DebugDisplaySettingsSO.InvalidateCache();
            Debug.Log($"[DebugDisplay] Created settings at {AssetPath}");
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Could not load or create DebugDisplaySettings.\n" +
                    "Try re-opening the window or manually creating a DebugDisplaySettingsSO asset.",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();

            DrawMasterToggle();
            EditorGUILayout.Space(8);

            _foldoutCategories = EditorGUILayout.Foldout(_foldoutCategories, "Categories", true);
            if (_foldoutCategories)
            {
                EditorGUI.indentLevel++;
                DrawCategoryToggles();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(12);
            DrawQuickActions();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                DebugDisplaySettingsSO.InvalidateCache();
                _pendingRefresh = true;
            }

            if (_pendingRefresh)
            {
                _pendingRefresh = false;
                RefreshPreviewObjects();
            }
        }

        private void DrawMasterToggle()
        {
            var so = new SerializedObject(_settings);
            var masterProp = so.FindProperty("_enableDebugDisplay");
            if (masterProp != null)
            {
                so.Update();
                EditorGUILayout.PropertyField(masterProp, new GUIContent("Enable Debug Display",
                    "Master toggle. When off, all debug displays are hidden."));
                so.ApplyModifiedProperties();
            }
        }

        private void DrawCategoryToggles()
        {
            var so = new SerializedObject(_settings);

            DrawToggle(so, "_enableChatPreview", "Chat Preview",
                "Chat message list editor preview bubbles.");
            DrawToggle(so, "_enableTarotPreview", "Tarot Preview",
                "Tarot card / reading bubble / summary editor preview.");
            DrawToggle(so, "_enablePlaceholderObjects", "Placeholder Objects",
                "Placeholder GameObjects created at runtime.");
            DrawToggle(so, "_enableVerboseLogging", "Verbose Logging",
                "Verbose runtime debug logs from subsystems.");

            so.ApplyModifiedProperties();
        }

        private static void DrawToggle(SerializedObject so, string propertyName, string label, string tooltip)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All", GUILayout.Height(28)))
            {
                SetAllToggles(true);
            }
            if (GUILayout.Button("Disable All", GUILayout.Height(28)))
            {
                SetAllToggles(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Scene Previews", GUILayout.Height(24)))
            {
                RefreshPreviewObjects();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            var status = _settings.IsDebugDisplayEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>";
            var chat = _settings.IsChatPreviewEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>";
            var tarot = _settings.IsTarotPreviewEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>";
            var placeholder = _settings.IsPlaceholderObjectsEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>";
            var log = _settings.IsVerboseLoggingEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status Summary");
            EditorGUILayout.LabelField($"  Master: {status}", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"  Chat Preview: {chat}", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"  Tarot Preview: {tarot}", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"  Placeholder Objects: {placeholder}", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.LabelField($"  Verbose Logging: {log}", new GUIStyle(EditorStyles.label) { richText = true });
            EditorGUILayout.EndVertical();
        }

        private void SetAllToggles(bool value)
        {
            if (_settings == null) return;

            var so = new SerializedObject(_settings);
            so.Update();

            SetToggle(so, "_enableDebugDisplay", value);
            SetToggle(so, "_enableChatPreview", value);
            SetToggle(so, "_enableTarotPreview", value);
            SetToggle(so, "_enablePlaceholderObjects", value);
            SetToggle(so, "_enableVerboseLogging", value);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
            DebugDisplaySettingsSO.InvalidateCache();
        }

        private static void SetToggle(SerializedObject so, string propertyName, bool value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.boolValue = value;
        }

        private static void RefreshPreviewObjects()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DebugDisplaySettingsSO>(AssetPath);
            if (settings == null) return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var bubbles = root.GetComponentsInChildren<ReadingBubble>(true);
                foreach (var b in bubbles)
                {
                    var go = b.gameObject;
                    if (!go.activeSelf && settings.IsTarotPreviewEnabled)
                        go.SetActive(true);
                    else if (go.activeSelf && !settings.IsTarotPreviewEnabled)
                        go.SetActive(false);
                }

                var summaries = root.GetComponentsInChildren<TarotSummaryPreview>(true);
                foreach (var s in summaries)
                {
                    var go = s.gameObject;
                    if (!go.activeSelf && settings.IsTarotPreviewEnabled)
                        go.SetActive(true);
                    else if (go.activeSelf && !settings.IsTarotPreviewEnabled)
                        go.SetActive(false);
                }
            }

            Debug.Log($"[DebugDisplay] Preview objects refreshed (Tarot: {(settings.IsTarotPreviewEnabled ? "ON" : "OFF")})");
        }
    }
}
