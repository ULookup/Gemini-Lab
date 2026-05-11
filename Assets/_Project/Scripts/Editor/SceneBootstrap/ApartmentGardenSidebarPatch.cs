#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI;
using GeminiLab.Modules.HubUI.Panels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// Phase G 专用的"只加不破"补丁：
    /// - 给 UI_Sidebar/Sidebar 末尾追加 Btn_Garden（若已存在则跳过）
    /// - 把 SidebarController._tabGarden 指向这个按钮
    /// - 在 UI_Sidebar 下创建 Panel_Garden（若已存在则跳过），挂 GardenPanelStub
    ///
    /// 区别于 <see cref="ApartmentSidebarAuthoring"/>：不会清掉现有 UI_Sidebar 结构，
    /// 因此 Phase D 对 Panel_Inventory / Panel_Collection 做的真实 UI 改造会保留。
    /// </summary>
    public static class ApartmentGardenSidebarPatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        [MenuItem("Tools/Gemini-Lab/Patch Apartment Sidebar (add Garden only)")]
        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var sidebarRoot = GameObject.Find("UI_Sidebar");
            if (sidebarRoot == null)
            {
                Debug.LogError("[ApartmentGardenSidebarPatch] 未找到 UI_Sidebar，请先跑 Author Apartment Sidebar");
                return;
            }

            int uiLayer = sidebarRoot.layer;

            // 1) 找 Sidebar 本体（按钮挂在这下面）
            var sidebarGo = FindChild(sidebarRoot.transform, "Sidebar");
            if (sidebarGo == null)
            {
                Debug.LogError("[ApartmentGardenSidebarPatch] 未找到 UI_Sidebar/Sidebar");
                return;
            }

            // 2) 追加 Btn_Garden（若已存在则复用）
            var gardenBtnTr = sidebarGo.transform.Find("Btn_Garden");
            Button gardenBtn;
            if (gardenBtnTr == null)
            {
                gardenBtn = MakeTab(sidebarGo, uiLayer, "Btn_Garden", "Garden");
            }
            else
            {
                gardenBtn = gardenBtnTr.GetComponent<Button>();
                if (gardenBtn == null) gardenBtn = gardenBtnTr.gameObject.AddComponent<Button>();
            }

            // 3) SidebarController._tabGarden 绑定
            var sidebarController = sidebarGo.GetComponent<SidebarController>();
            if (sidebarController != null)
            {
                var so = new SerializedObject(sidebarController);
                var tabProp = so.FindProperty("_tabGarden");
                if (tabProp != null)
                {
                    tabProp.objectReferenceValue = gardenBtn;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning("[ApartmentGardenSidebarPatch] SidebarController 没有 _tabGarden 字段，脚本版本过旧？");
                }
            }
            else
            {
                Debug.LogWarning("[ApartmentGardenSidebarPatch] UI_Sidebar/Sidebar 上没挂 SidebarController");
            }

            // 4) 创建 Panel_Garden（若已存在就跳过）
            var panelGardenTr = sidebarRoot.transform.Find("Panel_Garden");
            GameObject panelGarden;
            if (panelGardenTr == null)
            {
                panelGarden = CreateStubPanel<GardenPanelStub>(sidebarRoot, uiLayer, "Panel_Garden", "Garden (WIP)");
            }
            else
            {
                panelGarden = panelGardenTr.gameObject;
                if (panelGarden.GetComponent<GardenPanelStub>() == null)
                {
                    panelGarden.AddComponent<GardenPanelStub>();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApartmentGardenSidebarPatch] Garden tab + Panel_Garden 已增量接入，不影响其他面板");
        }

        private static GameObject? FindChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            return t != null ? t.gameObject : null;
        }

        private static Button MakeTab(GameObject parent, int uiLayer, string name, string labelText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 56);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            return btn;
        }

        private static GameObject CreateStubPanel<T>(GameObject parent, int uiLayer, string name, string labelText) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = uiLayer;
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900, 620);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(go.transform, false);
            contentGo.layer = uiLayer;
            var crt = contentGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var img = contentGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(contentGo.transform, false);
            labelGo.layer = uiLayer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48;
            tmp.color = Color.white;

            var stub = go.AddComponent<T>();
            var so = new SerializedObject(stub);
            var contentProp = so.FindProperty("_content");
            if (contentProp != null)
            {
                contentProp.objectReferenceValue = contentGo;
                so.ApplyModifiedProperties();
            }

            contentGo.SetActive(false);
            return go;
        }
    }
}
#endif
