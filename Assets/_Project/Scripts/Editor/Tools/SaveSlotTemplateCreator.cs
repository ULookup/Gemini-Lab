#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 在当前场景的 Panel_SaveSlots 下创建/更新 SlotTemplate 模板。
    /// 优先以用户在 Hierarchy 中选中的槽位行作为模板源；未选中时走代码生成兜底。
    /// </summary>
    public static class SaveSlotTemplateCreator
    {
        [MenuItem("Tools/Gemini-Lab/Create or Update Slot Template")]
        public static void Execute()
        {
            var panel = FindSaveSlotsPanel();
            if (panel == null)
            {
                Debug.LogWarning("[SlotTemplate] 未找到 Panel_SaveSlots，请先打开 MainMenu 场景。");
                return;
            }

            int layer = panel.layer;
            var panelComp = panel.GetComponent<SaveSlotsPanel>();
            GameObject? selected = Selection.activeGameObject;

            // Use selected object as template source if it's under the panel hierarchy
            GameObject? newTemplate = null;
            if (selected != null && selected.transform.IsChildOf(panel.transform))
            {
                newTemplate = BuildTemplateFromSelection(selected, panel);
            }

            // Fallback: code-generated template
            if (newTemplate == null)
            {
                if (selected != null)
                    Debug.Log("[SlotTemplate] 选中的对象不在 Panel_SaveSlots 层级下，使用代码生成模板。");
                else
                    Debug.Log("[SlotTemplate] 未选中任何对象，使用代码生成模板。");
                newTemplate = BuildTemplate(panel, layer);
            }

            // Remove existing template
            var existing = panel.transform.Find("SlotTemplate");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            newTemplate.name = "SlotTemplate";

            // Wire _slotTemplate
            if (panelComp != null)
            {
                var so = new SerializedObject(panelComp);
                so.FindProperty("_slotTemplate").objectReferenceValue = newTemplate;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            EditorSceneManager.SaveScene(panel.gameObject.scene);

            Selection.activeGameObject = newTemplate;
            EditorGUIUtility.PingObject(newTemplate);
            Debug.Log("[SlotTemplate] 模板已创建/更新，可在 Hierarchy 中展开 SlotTemplate 编辑美术资源。");
        }

        private static GameObject BuildTemplateFromSelection(GameObject selected, GameObject panel)
        {
            // Use the selected object directly — no cloning.
            // Reparent to panel root, set inactive, clean up listeners.
            selected.transform.SetParent(panel.transform, false);

            foreach (var btn in selected.GetComponentsInChildren<Button>(true))
                btn.onClick.RemoveAllListeners();

            selected.SetActive(false);

            Debug.Log($"[SlotTemplate] 已将选中的 '{selected.name}' 直接作为模板（未复制，保留原物体全部属性）。");
            return selected;
        }

        private static GameObject? FindSaveSlotsPanel()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var panel = root.GetComponentInChildren<SaveSlotsPanel>(true);
                if (panel != null) return panel.gameObject;
            }
            return null;
        }

        private static GameObject BuildTemplate(GameObject panel, int layer)
        {
            var root = new GameObject("SlotTemplate");
            root.transform.SetParent(panel.transform, false);
            root.layer = layer;
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 110);
            var bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.06f);

            // Summary
            var summaryGo = new GameObject("Summary");
            summaryGo.transform.SetParent(root.transform, false);
            summaryGo.layer = layer;
            var srt = summaryGo.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = new Vector2(0.55f, 1);
            srt.offsetMin = new Vector2(16, 8);
            srt.offsetMax = new Vector2(-8, -8);
            var summary = summaryGo.AddComponent<TextMeshProUGUI>();
            summary.fontSize = 18;
            summary.color = Color.white;
            summary.alignment = TextAlignmentOptions.MidlineLeft;
            summary.text = "Slot_1：预览槽位";
            summaryGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            // Buttons
            BuildButton(root, layer, "Btn_读取", "读取", new Vector2(0.55f, 0), new Vector2(0.70f, 1));
            BuildButton(root, layer, "Btn_保存", "新建 / 覆盖", new Vector2(0.70f, 0), new Vector2(0.88f, 1));
            BuildButton(root, layer, "Btn_删除", "删除", new Vector2(0.88f, 0), new Vector2(1.0f, 1));

            root.SetActive(false);
            return root;
        }

        private static void BuildButton(GameObject parent, int layer, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = layer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(6, 20);
            rt.offsetMax = new Vector2(-6, -20);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.3f, 0.45f, 1f);
            go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();
        }
    }
}
#endif
