#nullable enable
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 编辑器启动 / 域重载时，确保 Boot 场景始终加载，
    /// 并触发 BootstrapRoot 上所有组件的 Awake，使 ServiceLocator 服务在编辑器里随时可用。
    /// </summary>
    [InitializeOnLoad]
    public static class EditorBootSceneLoader
    {
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string BootstrapRootName = "BootstrapRoot";

        // Awake 调用顺序：GameBootstrap 必须先跑（它 Reset + 注册核心服务）
        private static readonly string[] AwakeOrder =
        {
            "GameBootstrap",
            "TarotRuntimeBootstrap",
            "CollectionRuntimeBootstrap",
            "SettingsRuntimeBootstrap",
            "InventoryRuntimeBootstrap",
            "PersonalityEvolutionBootstrap",
            "GardenRuntimeBootstrap",
            "UICatalogHost",
            "ToastOverlayController",
            "UIInputRouter",
        };

        static EditorBootSceneLoader()
        {
            EditorApplication.delayCall += () =>
            {
                bool bootWasLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (SceneManager.GetSceneAt(i).path == BootScenePath)
                    {
                        bootWasLoaded = true;
                        break;
                    }
                }

                if (!bootWasLoaded)
                {
                    EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Additive);
                }

                // 无论 Boot 是否已加载，域重载后都需要重新初始化
                EditorApplication.delayCall += () =>
                {
                    InitBootstraps();
                    EditorApplication.delayCall += PopulateEditorUI;
                };
            };
        }

        private static void InitBootstraps()
        {
            var root = GameObject.Find(BootstrapRootName);
            if (root == null)
            {
                Debug.LogWarning($"[EditorBootSceneLoader] 未找到 {BootstrapRootName}");
                return;
            }

            // 重置 GameBootstrap 的 static _initialized 守卫，使其在域重载后可以重新执行
            var gbComp = root.GetComponent("GameBootstrap");
            if (gbComp != null)
            {
                var initField = gbComp.GetType().GetField("_initialized",
                    BindingFlags.NonPublic | BindingFlags.Static);
                initField?.SetValue(null, false);
            }

            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            foreach (var typeName in AwakeOrder)
            {
                var comp = root.GetComponent(typeName);
                if (comp == null) continue;

                var awakeMethod = comp.GetType().GetMethod("Awake", flags);
                if (awakeMethod == null) continue;

                awakeMethod.Invoke(comp, null);
                Debug.Log($"[EditorBootSceneLoader] {typeName}.Awake() invoked");
            }
        }

        private static void PopulateEditorUI()
        {
            // 遍历所有加载的场景，找到 TarotPanelStub 并预填充图鉴 / 历史
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    var stubs = root.GetComponentsInChildren<GeminiLab.Modules.HubUI.Panels.TarotPanelStub>(true);
                    foreach (var stub in stubs)
                    {
                        // EnsureServices
                        var ensureMethod = stub.GetType().GetMethod("EnsureServices", flags);
                        ensureMethod?.Invoke(stub, null);

                        // PopulateGuide
                        var guideMethod = stub.GetType().GetMethod("PopulateGuide", flags);
                        guideMethod?.Invoke(stub, null);

                        // PopulateHistory
                        var histMethod = stub.GetType().GetMethod("PopulateHistory", flags);
                        histMethod?.Invoke(stub, null);

                        Debug.Log($"[EditorBootSceneLoader] Populated UI for {stub.name} in {scene.name}");
                    }
                }
            }
        }

        [MenuItem("GeminiLab/Initialize Editor Services", priority = 100)]
        public static void ManualInitialize()
        {
            bool bootWasLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == BootScenePath)
                {
                    bootWasLoaded = true;
                    break;
                }
            }

            if (!bootWasLoaded)
            {
                EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Additive);
            }

            EditorApplication.delayCall += () =>
            {
                InitBootstraps();
                EditorApplication.delayCall += PopulateEditorUI;
            };
        }
    }
}
#endif
