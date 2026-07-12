#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.Tools
{
    /// <summary>
    /// 给所有面板补上 TopResource（若缺失），并确保 StubPanelBase._balanceText 连线到 BalanceLabel 的 TMP_Text。
    /// </summary>
    public static class AddCoinBalanceToPanels
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        private static readonly string[] PanelNames =
        {
            "Panel_PetStatus",
            "Panel_SpaceSys",
            "Panel_Tarot",
            "Panel_Collection",
            "Panel_Inventory",
            "Panel_Garden",
        };

        [MenuItem("Tools/Gemini-Lab/Add Coin Balance To All Panels")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1) Find template TopResource from ANY existing panel
            Transform? template = null;
            foreach (var panelName in PanelNames)
            {
                var panel = GameObject.Find(panelName);
                if (panel == null) continue;
                var content = panel.transform.Find("Content");
                if (content == null) continue;
                template = content.Find("TopResource");
                if (template != null) break;
            }

            if (template == null)
            {
                Debug.LogError("[AddCoinBalance] 所有面板都没有 TopResource，请先通过 Sidebar Author 工具创建");
                return;
            }

            Debug.Log($"[AddCoinBalance] 模板来自 {template.parent.parent.name}/{template.parent.name}/{template.name}");

            // 2) Clone to missing panels + wire _balanceText
            int cloned = 0;
            int wired = 0;
            foreach (var panelName in PanelNames)
            {
                var panel = GameObject.Find(panelName);
                if (panel == null) continue;
                var content = panel.transform.Find("Content");
                if (content == null) continue;

                var topResource = content.Find("TopResource");
                if (topResource == null)
                {
                    var clone = Object.Instantiate(template.gameObject, content);
                    clone.name = "TopResource";
                    var cloneRt = clone.GetComponent<RectTransform>();
                    cloneRt.anchorMin = new Vector2(0.5f, 0.5f);
                    cloneRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cloneRt.pivot = new Vector2(0.5f, 0.5f);
                    cloneRt.anchoredPosition = new Vector2(582, 442);
                    cloneRt.sizeDelta = new Vector2(295, 91);
                    topResource = clone.transform;
                    cloned++;
                }

                // Wire _balanceText on StubPanelBase
                var stub = panel.GetComponent<StubPanelBase>();
                if (stub == null)
                {
                    Debug.LogWarning($"[AddCoinBalance] {panelName} 没有 StubPanelBase 组件，跳过连线");
                    continue;
                }

                var balanceLabel = topResource.Find("BalanceLabel");
                if (balanceLabel == null) continue;
                var tmp = balanceLabel.GetComponent<TMP_Text>();
                if (tmp == null) continue;

                var so = new SerializedObject(stub);
                var prop = so.FindProperty("_balanceText");
                if (prop != null && prop.objectReferenceValue != tmp)
                {
                    prop.objectReferenceValue = tmp;
                    so.ApplyModifiedProperties();
                    wired++;
                }
            }

            if (cloned > 0 || wired > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AddCoinBalance] 完成：克隆 {cloned} 个 TopResource，连线 {wired} 个 _balanceText");
            }
            else
            {
                Debug.Log("[AddCoinBalance] 所有面板都已就绪，无需操作");
            }
        }
    }
}
#endif
