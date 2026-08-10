#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 复用 WorldMap 三个花卉 UI 面板中 FlowerImage 与 SoilImage 的 RectTransform 布局。
    /// 只复制布局，不复制 Sprite、颜色、启用状态或其他 Image 属性。
    /// </summary>
    public sealed class WorldMapFlowerSoilLayoutWindow : EditorWindow
    {
        private const string WindowTitle = "WorldMap 花卉布局复用";
        private const string WeeklyPanelName = "Panel_WeeklyGarden";
        private const string CollectionPanelName = "Panel_EmotionCollection";

        [SerializeField] private GameObject? _weeklyReference;
        [SerializeField] private GameObject? _codexReference;
        [SerializeField] private GameObject? _detailReference;

        private Vector2 _scrollPosition;
        private string _statusMessage = "请选择一个包含 FlowerImage 和 SoilImage 的参考对象。";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/Gemini-Lab/WorldMap 花卉布局复用")]
        private static void Open()
        {
            var window = GetWindow<WorldMapFlowerSoilLayoutWindow>(false, WindowTitle);
            window.minSize = new Vector2(440f, 430f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "先在 Scene 中调整好一个参考对象，再拖入对应字段。按钮只复制 FlowerImage 和 SoilImage 的布局参数，执行后请保存场景。",
                MessageType.Info);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawReferenceSection(
                "每周种植面板",
                "参考日期格",
                ref _weeklyReference,
                "复制到每周种植面板",
                ApplyWeeklyLayout,
                "目标：CellTemplate 与 Day0~Day6");

            DrawReferenceSection(
                "图鉴列表面板",
                "参考图鉴卡片",
                ref _codexReference,
                "复制到图鉴列表面板",
                ApplyCodexLayout,
                "目标：CodexCardSlot_00~11");

            DrawReferenceSection(
                "图鉴详情面板",
                "参考详情花型",
                ref _detailReference,
                "复制到图鉴详情面板",
                ApplyDetailLayout,
                "目标：只复制同名 Variant 的 FlowerArt 与 SoilImage");

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);

            EditorGUILayout.EndScrollView();
        }

        private void DrawReferenceSection(
            string sectionTitle,
            string fieldLabel,
            ref GameObject? reference,
            string buttonLabel,
            Action applyAction,
            string targetDescription)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(targetDescription, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            reference = (GameObject?)EditorGUILayout.ObjectField(
                fieldLabel,
                reference,
                typeof(GameObject),
                true);
            if (GUILayout.Button("使用当前选择", GUILayout.Width(92f)))
            {
                if (Selection.activeGameObject != null)
                {
                    reference = Selection.activeGameObject;
                    SetStatus($"已设置“{sectionTitle}”参考对象：{reference.name}", MessageType.Info);
                }
                else
                {
                    SetStatus("当前没有选中的 GameObject。", MessageType.Warning);
                }
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(reference == null))
            {
                if (GUILayout.Button(buttonLabel, GUILayout.Height(30f)))
                {
                    applyAction();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void ApplyWeeklyLayout()
        {
            ApplyLayoutToTargets(
                _weeklyReference,
                FindTargets(WeeklyPanelName, IsWeeklyCell),
                "每周种植面板");
        }

        private void ApplyCodexLayout()
        {
            ApplyLayoutToTargets(
                _codexReference,
                FindTargets(CollectionPanelName, IsCodexCard),
                "图鉴列表面板");
        }

        private void ApplyDetailLayout()
        {
            if (_detailReference == null)
            {
                SetStatus("图鉴详情面板没有设置参考花型。", MessageType.Warning);
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            var detailRoot = FindSceneObject(scene, "DetailView");
            var sourceVariant = FindDetailVariant(_detailReference.transform);
            if (detailRoot == null || sourceVariant == null)
            {
                SetStatus("详情页参考对象必须是 FlowerImage/Variant_XX（或其子节点）。", MessageType.Warning);
                return;
            }

            if (!TryFindDetailPair(sourceVariant, out var sourceFlower, out var sourceSoil))
            {
                SetStatus($"参考花型“{sourceVariant.name}”缺少 FlowerArt 或 SoilImage。", MessageType.Warning);
                return;
            }

            var detailFlowerRoot = detailRoot.transform.Find("FlowerImage");
            var targetVariant = detailFlowerRoot != null
                ? FindNamedChildOrSelf(detailFlowerRoot, sourceVariant.name)
                : null;
            if (targetVariant == null || !TryFindDetailPair(targetVariant, out var targetFlower, out var targetSoil))
            {
                SetStatus($"详情页中没有找到同名花型“{sourceVariant.name}”。", MessageType.Warning);
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"复用详情花型 {sourceVariant.name} 布局");
            CopyLayout(LayoutSnapshot.Read(sourceFlower), targetFlower, "详情花枝布局");
            CopyLayout(LayoutSnapshot.Read(sourceSoil), targetSoil, "详情土壤布局");
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            SetStatus($"已将“{sourceVariant.name}”的 FlowerArt 与 SoilImage 布局复用到详情页同名花型。请保存场景。", MessageType.Info);
        }

        private void ApplyLayoutToTargets(
            GameObject? reference,
            IReadOnlyList<Transform> targetRoots,
            string panelLabel)
        {
            if (reference == null)
            {
                SetStatus($"{panelLabel}没有设置参考对象。", MessageType.Warning);
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                SetStatus("当前没有有效的已保存场景。", MessageType.Error);
                return;
            }

            if (reference.scene != scene)
            {
                SetStatus("参考对象必须来自当前激活场景。", MessageType.Warning);
                return;
            }

            if (!TryReadPair(reference.transform, out var flowerLayout, out var soilLayout, out var sourceDescription))
            {
                SetStatus($"参考对象“{reference.name}”下没有同时找到 FlowerImage 和 SoilImage。", MessageType.Warning);
                return;
            }

            var validTargets = targetRoots
                .Where(t => t != null && t.gameObject.scene == scene)
                .Distinct()
                .ToList();
            if (validTargets.Count == 0)
            {
                SetStatus($"没有找到{panelLabel}的目标对象。请确认 WorldMap_Main 当前已打开。", MessageType.Warning);
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"复用{panelLabel}花卉布局");
            int changedCount = 0;
            int skippedCount = 0;

            foreach (var target in validTargets)
            {
                if (!TryFindPair(target, out var targetFlower, out var targetSoil))
                {
                    skippedCount++;
                    continue;
                }

                CopyLayout(flowerLayout, targetFlower, $"{panelLabel} FlowerImage");
                CopyLayout(soilLayout, targetSoil, $"{panelLabel} SoilImage");
                changedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            string suffix = skippedCount > 0 ? $"，跳过 {skippedCount} 个缺少配对节点的对象" : string.Empty;
            SetStatus($"已从 {sourceDescription} 复用到 {changedCount} 个{panelLabel}对象{suffix}。请保存场景。", MessageType.Info);
        }

        private static IReadOnlyList<Transform> FindTargets(string panelName, Func<Transform, bool> predicate)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var panel = FindSceneObject(scene, panelName);
            if (panel == null)
            {
                return Array.Empty<Transform>();
            }

            return panel.GetComponentsInChildren<Transform>(true)
                .Where(predicate)
                .ToList();
        }

        private static bool IsWeeklyCell(Transform transform)
        {
            return transform.name == "CellTemplate" ||
                   (transform.name.StartsWith("Day", StringComparison.Ordinal) &&
                    int.TryParse(transform.name.Substring(3), out _));
        }

        private static bool IsCodexCard(Transform transform)
        {
            return transform.name.StartsWith("CodexCardSlot_", StringComparison.Ordinal);
        }

        private static GameObject? FindSceneObject(Scene scene, string objectName)
        {
            if (!scene.IsValid()) return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var match = FindRecursive(root.transform, objectName);
                if (match != null) return match.gameObject;
            }

            return null;
        }

        private static Transform? FindRecursive(Transform current, string objectName)
        {
            if (current.name == objectName) return current;

            for (int i = 0; i < current.childCount; i++)
            {
                var match = FindRecursive(current.GetChild(i), objectName);
                if (match != null) return match;
            }

            return null;
        }

        private static bool TryReadPair(
            Transform reference,
            out LayoutSnapshot flowerLayout,
            out LayoutSnapshot soilLayout,
            out string sourceDescription)
        {
            if (!TryFindPair(reference, out var flower, out var soil))
            {
                flowerLayout = default;
                soilLayout = default;
                sourceDescription = reference.name;
                return false;
            }

            flowerLayout = LayoutSnapshot.Read(flower);
            soilLayout = LayoutSnapshot.Read(soil);
            sourceDescription = reference.name;
            return true;
        }

        private static bool TryFindPair(
            Transform root,
            out RectTransform flower,
            out RectTransform soil)
        {
            var pairRoot = root.name == "FlowerImage" || root.name == "SoilImage"
                ? root.parent ?? root
                : root;
            var flowerTransform = FindNamedChildOrSelf(pairRoot, "FlowerImage");
            var soilTransform = FindNamedChildOrSelf(pairRoot, "SoilImage");
            flower = flowerTransform != null ? flowerTransform.GetComponent<RectTransform>()! : null!;
            soil = soilTransform != null ? soilTransform.GetComponent<RectTransform>()! : null!;
            return flower != null && soil != null;
        }

        private static Transform? FindDetailVariant(Transform reference)
        {
            Transform? current = reference;
            while (current != null)
            {
                if (current.name.StartsWith("Variant_", StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static bool TryFindDetailPair(
            Transform root,
            out RectTransform flower,
            out RectTransform soil)
        {
            var flowerTransform = FindNamedChildOrSelf(root, "FlowerArt");
            var soilTransform = FindNamedChildOrSelf(root, "SoilImage");
            flower = flowerTransform != null ? flowerTransform.GetComponent<RectTransform>()! : null!;
            soil = soilTransform != null ? soilTransform.GetComponent<RectTransform>()! : null!;
            return flower != null && soil != null;
        }

        private static Transform? FindNamedChildOrSelf(Transform root, string name)
        {
            if (root.name == name) return root;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root && child.name == name) return child;
            }

            return null;
        }

        private static void CopyLayout(LayoutSnapshot snapshot, RectTransform target, string undoLabel)
        {
            Undo.RecordObject(target, undoLabel);
            snapshot.ApplyTo(target);
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        [Serializable]
        private readonly struct LayoutSnapshot
        {
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _anchoredPosition;
            private readonly Vector2 _sizeDelta;
            private readonly Vector2 _pivot;
            private readonly Vector3 _localScale;

            private LayoutSnapshot(
                Vector2 anchorMin,
                Vector2 anchorMax,
                Vector2 anchoredPosition,
                Vector2 sizeDelta,
                Vector2 pivot,
                Vector3 localScale)
            {
                _anchorMin = anchorMin;
                _anchorMax = anchorMax;
                _anchoredPosition = anchoredPosition;
                _sizeDelta = sizeDelta;
                _pivot = pivot;
                _localScale = localScale;
            }

            public static LayoutSnapshot Read(RectTransform source)
            {
                return new LayoutSnapshot(
                    source.anchorMin,
                    source.anchorMax,
                    source.anchoredPosition,
                    source.sizeDelta,
                    source.pivot,
                    source.localScale);
            }

            public void ApplyTo(RectTransform target)
            {
                target.anchorMin = _anchorMin;
                target.anchorMax = _anchorMax;
                target.anchoredPosition = _anchoredPosition;
                target.sizeDelta = _sizeDelta;
                target.pivot = _pivot;
                target.localScale = _localScale;
            }
        }
    }
}
#endif
