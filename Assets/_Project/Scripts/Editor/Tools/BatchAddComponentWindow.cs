#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    public sealed class BatchAddComponentWindow : EditorWindow
    {
        private string _search = "";
        private bool _includeChildren;
        private Vector2 _typeScroll;
        private Vector2 _selectionScroll;
        private Type? _selectedType;

        private static readonly List<Type> AllTypes;
        private List<Type> _filtered = new();

        static BatchAddComponentWindow()
        {
            AllTypes = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => !t.IsAbstract
                         && !t.IsGenericType
                         && !t.IsDefined(typeof(ObsoleteAttribute), true)
                         && t.Namespace is not null
                         && !t.Namespace.StartsWith("UnityEditor", StringComparison.Ordinal)
                         && !t.Namespace.StartsWith("UnityEngineInternal", StringComparison.Ordinal))
                .OrderBy(t => t.Name)
                .ToList();
        }

        [MenuItem("Tools/Gemini-Lab/批量添加组件")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchAddComponentWindow>("批量添加组件");
            window.minSize = new Vector2(350, 420);
            window.Show();
        }

        private void OnEnable()
        {
            _filtered = new List<Type>(AllTypes);
        }

        private void OnGUI()
        {
            GUILayout.Space(6);

            // ── 搜索栏（模仿 Inspector Add Component） ──
            EditorGUI.BeginChangeCheck();
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                _filtered = string.IsNullOrWhiteSpace(_search)
                    ? new List<Type>(AllTypes)
                    : AllTypes.Where(t => t.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                                       || (t.Namespace != null && t.Namespace.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0))
                              .ToList();
                _selectedType = null;
            }

            // ── 类型列表 ──
            GUILayout.Space(4);
            GUILayout.Label($"组件列表 ({_filtered.Count})", EditorStyles.boldLabel);
            _typeScroll = GUILayout.BeginScrollView(_typeScroll, GUILayout.Height(180));

            foreach (var type in _filtered)
            {
                var isSelected = type == _selectedType;
                var bg = isSelected ? GUI.skin.box : GUIStyle.none;

                GUILayout.BeginHorizontal(bg);
                if (GUILayout.Button(type.Name, EditorStyles.label, GUILayout.ExpandWidth(true)))
                {
                    _selectedType = type;
                    _search = type.Name;
                }

                if (type.Namespace != null)
                {
                    var nsColor = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                    GUILayout.Label(type.Namespace, nsColor);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (_filtered.Count == 0)
                EditorGUILayout.HelpBox("没有匹配的组件", MessageType.Info);

            GUILayout.Space(8);

            // ── 选中类型 ──
            if (_selectedType != null)
            {
                EditorGUILayout.HelpBox($"已选择: {_selectedType.FullName}", MessageType.Info);
            }

            // ── 选项 ──
            _includeChildren = EditorGUILayout.Toggle("包含子物体（递归）", _includeChildren);

            // ── 选中对象 ──
            GUILayout.Space(4);
            var selected = Selection.gameObjects;
            GUILayout.Label(selected.Length > 0 ? $"已选中 {selected.Length} 个对象" : "未选中对象", EditorStyles.boldLabel);

            if (selected.Length > 0)
            {
                _selectionScroll = GUILayout.BeginScrollView(_selectionScroll, GUILayout.Height(80));
                foreach (var go in selected)
                    EditorGUILayout.LabelField($"  {go.name}");
                GUILayout.EndScrollView();
            }

            // ── 执行按钮 ──
            GUILayout.Space(8);
            GUI.enabled = _selectedType != null && selected.Length > 0;
            if (GUILayout.Button("添加组件", GUILayout.Height(36)))
            {
                AddComponentToSelected();
            }
            GUI.enabled = true;
        }

        private void AddComponentToSelected()
        {
            if (_selectedType == null) return;

            var selected = Selection.gameObjects;
            var targets = _includeChildren
                ? selected.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                          .Select(t => t.gameObject)
                          .Distinct()
                          .ToArray()
                : selected;

            int added = 0;
            foreach (var go in targets)
            {
                if (go.GetComponent(_selectedType) != null)
                    continue;

                // 保存 Transform 状态：AddComponent 会触发 OnValidate，某些组件会修改 position
                var savedPos = go.transform.localPosition;
                var savedRot = go.transform.localRotation;
                var savedScale = go.transform.localScale;

                Undo.AddComponent(go, _selectedType);

                // 恢复，防止 OnValidate 意外修改
                go.transform.localPosition = savedPos;
                go.transform.localRotation = savedRot;
                go.transform.localScale = savedScale;

                added++;
            }

            Debug.Log($"[批量添加组件] 已为 {added} 个对象添加 {_selectedType.Name}");
        }

        private void OnSelectionChange()
        {
            Repaint();
        }
    }
}
#endif
