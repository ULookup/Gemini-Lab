#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.UI.Catalogs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 给 MainMenu / Apartment / WorldMap 场景里所有 TMP_Text 回填 TMPFontBinder，
    /// 并把骨架占位英文标签替换成中文。
    /// </summary>
    public static class TMPBinderBackfillAuthoring
    {
        private static readonly (string scenePath, IReadOnlyDictionary<string, string> labelMap)[] Targets = new[]
        {
            ("Assets/_Project/Scenes/MainMenu/MainMenu.unity", (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
            {
                { "Start", "开始" },
                { "Save Slots", "存档" },
                { "Settings", "设置" }
            }),
            ("Assets/_Project/Scenes/Apartment/Apartment_Main.unity", new Dictionary<string, string>
            {
                { "<<", "收" },
                { "Status", "宠物状态" },
                { "Tarot", "每日塔罗" },
                { "Collection", "收藏" },
                { "Inventory", "物品栏" },
                { "Pet Status (WIP)", "宠物状态（施工中）" },
                { "Tarot (WIP)", "每日塔罗（施工中）" },
                { "Collection (WIP)", "收藏（施工中）" },
                { "Inventory (WIP)", "物品栏（施工中）" },
                { "→ World Map", "→ 大地图" }
            }),
            ("Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity", new Dictionary<string, string>
            {
                { "Return", "返回公寓" },
                { "Garden", "花园" }
            })
        };

        [MenuItem("Tools/Gemini-Lab/Author TMP Binder Backfill")]
        public static void Author()
        {
            foreach (var (scenePath, labelMap) in Targets)
            {
                var scene = EditorSceneManager.GetActiveScene().path == scenePath
                    ? EditorSceneManager.GetActiveScene()
                    : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var texts = Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
                int bound = 0;
                int replaced = 0;
                foreach (var tmp in texts)
                {
                    if (tmp.gameObject.GetComponent<TMPFontBinder>() == null)
                    {
                        tmp.gameObject.AddComponent<TMPFontBinder>();
                        bound++;
                    }
                    if (labelMap.TryGetValue(tmp.text, out var zh))
                    {
                        Undo.RecordObject(tmp, "Replace label");
                        tmp.text = zh;
                        EditorUtility.SetDirty(tmp);
                        replaced++;
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[TMPBinderBackfill] {scenePath}: bound={bound}, labelsReplaced={replaced}");
            }
        }
    }
}
#endif
