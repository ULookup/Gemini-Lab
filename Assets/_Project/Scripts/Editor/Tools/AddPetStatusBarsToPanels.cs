#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using GeminiLab.Modules.UI.Catalogs;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 为 Panel_SpaceSys paraboard 下每个 bar 创建 Fill 子物体（Filled Image）+ Value 子物体（TMP_Text），
    /// 并为 Panel_PetStatus 的现有 _value 文本连线。
    /// </summary>
    public static class AddPetStatusBarsToPanels
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        // SpaceSys bar → 对应的 PetStatus fill bar（用于复用胶囊形 sprite）
        private static readonly (string barName, string fillField, string textField, string petStatusFillName)[] SpaceSysBars =
        {
            ("mood_angel",   "_angelMoodFill",   "_angelMoodText",   "mood_bar_angel"),
            ("energe_angel", "_angelEnergyFill", "_angelEnergyText", "energy_bar_angel"),
            ("mood_devil",   "_devilMoodFill",   "_devilMoodText",   "mood_bar_devil"),
            ("energe_devil", "_devilEnergyFill", "_devilEnergyText", "energy_bar_devil"),
        };

        private static readonly (string valueGoName, string textField)[] PetStatusTexts =
        {
            ("mood_bar_angel_value",   "_angelMoodText"),
            ("mood_bar_devil_value",   "_evilMoodText"),
            ("energy_bar_angel_value", "_angelEnergyText"),
            ("energy_bar_devil_value", "_evilEnergyText"),
        };

        [MenuItem("Tools/Gemini-Lab/Add Pet Status Bars To Panels")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CleanupOldBarGroups();
            RevertBarImageTypes();
            int spaceSysDone = SetupSpaceSysPanel();
            int petStatusDone = SetupPetStatusPanel();

            if (spaceSysDone > 0 || petStatusDone > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AddPetStatusBars] SpaceSys: {spaceSysDone}/8 字段已连线, PetStatus: {petStatusDone}/4 字段已连线");
            }
            else
            {
                Debug.Log("[AddPetStatusBars] 所有字段已就绪，无需操作");
            }
        }

        /// <summary>
        /// 删除之前版本工具错误创建的顶层 bar group GameObject。
        /// </summary>
        private static void CleanupOldBarGroups()
        {
            var panel = GameObject.Find("Panel_SpaceSys");
            if (panel == null) return;
            var content = panel.transform.Find("Content");
            if (content == null) return;

            var oldNames = new[] { "AngelMoodBar", "AngelEnergyBar", "DevilMoodBar", "DevilEnergyBar" };
            foreach (var name in oldNames)
            {
                var old = content.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
        }

        /// <summary>
        /// 把之前版本误改的父级 bar Image 从 Filled 恢复为 Simple。
        /// </summary>
        private static void RevertBarImageTypes()
        {
            var panel = GameObject.Find("Panel_SpaceSys");
            if (panel == null) return;
            var content = panel.transform.Find("Content");
            if (content == null) return;
            var paraboard = content.Find("paraboard");
            if (paraboard == null) return;

            foreach (var (barName, _, _, _) in SpaceSysBars)
            {
                var bar = paraboard.Find(barName);
                if (bar == null) continue;
                var img = bar.GetComponent<Image>();
                if (img != null && img.type == Image.Type.Filled)
                {
                    img.type = Image.Type.Simple;
                    img.fillAmount = 1f;
                }
            }
        }

        // ======================== SpaceSys ========================

        private static int SetupSpaceSysPanel()
        {
            var panel = GameObject.Find("Panel_SpaceSys");
            if (panel == null) { Debug.LogError("[AddPetStatusBars] 找不到 Panel_SpaceSys"); return 0; }

            var content = panel.transform.Find("Content");
            if (content == null) { Debug.LogError("[AddPetStatusBars] Panel_SpaceSys 没有 Content"); return 0; }

            var paraboard = content.Find("paraboard");
            if (paraboard == null) { Debug.LogError("[AddPetStatusBars] Content 下没有 paraboard"); return 0; }

            var stub = panel.GetComponent<SpaceSysPanelStub>();
            if (stub == null) { Debug.LogError("[AddPetStatusBars] Panel_SpaceSys 没有 SpaceSysPanelStub"); return 0; }

            // 从 PetStatus 面板找到对应填充条的 sprite
            var petStatusPanel = GameObject.Find("Panel_PetStatus");
            var spriteMap = new Dictionary<string, Sprite?>();
            if (petStatusPanel != null)
            {
                foreach (var (_, _, _, petStatusFillName) in SpaceSysBars)
                {
                    var src = FindRecursive(petStatusPanel.transform, petStatusFillName);
                    spriteMap[petStatusFillName] = src?.GetComponent<Image>()?.sprite;
                }
            }

            var so = new SerializedObject(stub);
            int count = 0;

            foreach (var (barName, fillField, textField, petStatusFillName) in SpaceSysBars)
            {
                var bar = paraboard.Find(barName);
                if (bar == null) { Debug.LogWarning($"[AddPetStatusBars] paraboard 下找不到 {barName}"); continue; }

                spriteMap.TryGetValue(petStatusFillName, out var sprite);
                var fill = EnsureFillChild(bar, "Fill", sprite);
                var text = EnsureValueTextChild(bar, "Value");

                if (WireField(so, fillField, fill)) count++;
                if (WireField(so, textField, text)) count++;
            }

            so.ApplyModifiedProperties();
            return count;
        }

        // ======================== PetStatus ========================

        private static int SetupPetStatusPanel()
        {
            var panel = GameObject.Find("Panel_PetStatus");
            if (panel == null) { Debug.LogError("[AddPetStatusBars] 找不到 Panel_PetStatus"); return 0; }

            var stub = panel.GetComponent<ProfilePanelStub>();
            if (stub == null) { Debug.LogError("[AddPetStatusBars] Panel_PetStatus 没有 ProfilePanelStub"); return 0; }

            var so = new SerializedObject(stub);
            int count = 0;

            foreach (var (valueGoName, textField) in PetStatusTexts)
            {
                var valueGo = FindRecursive(panel.transform, valueGoName);
                if (valueGo == null) { Debug.LogWarning($"[AddPetStatusBars] 找不到 {valueGoName}"); continue; }

                var tmp = valueGo.GetComponent<TMP_Text>();
                if (tmp == null) { Debug.LogWarning($"[AddPetStatusBars] {valueGoName} 没有 TMP_Text 组件"); continue; }

                if (WireField(so, textField, tmp)) count++;
            }

            so.ApplyModifiedProperties();
            return count;
        }

        // ======================== helpers ========================

        /// <summary>
        /// 确保 bar 下有一个 Fill 子物体（Image, Filled/Horizontal）。
        /// 位置锚定在父级右侧区域，左侧留给图标。
        /// </summary>
        private static Image EnsureFillChild(Transform bar, string name, Sprite? sprite)
        {
            var existing = bar.Find(name);
            GameObject go;
            if (existing != null)
            {
                var existImg = existing.GetComponent<Image>();
                if (existImg != null)
                {
                    existImg.type = Image.Type.Filled;
                    existImg.fillMethod = Image.FillMethod.Horizontal;
                    existImg.fillOrigin = 0;
                    existImg.fillAmount = 1f;
                    if (sprite != null) existImg.sprite = sprite;
                    return existImg;
                }
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(bar, false);
            }

            go.layer = bar.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.1f);
            rt.anchorMax = new Vector2(0.85f, 0.9f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            img.color = Color.white;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        /// <summary>
        /// 确保 bar 下有一个 Value 子物体（TMP_Text），放在右侧显示数值。
        /// </summary>
        private static TMP_Text EnsureValueTextChild(Transform bar, string name)
        {
            var existing = bar.Find(name);
            GameObject go;
            if (existing != null)
            {
                var existTmp = existing.GetComponent<TMP_Text>();
                if (existTmp != null) return existTmp;
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(bar, false);
            }

            go.layer = bar.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.8f, 0);
            rt.anchorMax = new Vector2(0.98f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var tmp = go.GetComponent<TMP_Text>() ?? go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.text = "--";

            if (go.GetComponent<TMPFontBinder>() == null)
                go.AddComponent<TMPFontBinder>();

            return tmp;
        }

        private static bool WireField(SerializedObject so, string fieldName, Object? value)
        {
            if (value == null) return false;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return false;
            if (prop.objectReferenceValue == value) return false;
            prop.objectReferenceValue = value;
            return true;
        }

        private static Transform? FindRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
