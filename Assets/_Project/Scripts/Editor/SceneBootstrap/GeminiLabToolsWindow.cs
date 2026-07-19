#nullable enable
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    public sealed class GeminiLabToolsWindow : EditorWindow
    {
        private enum Tab { 场景搭建, 模块连线, UI面板, 调试 }

        private Tab _currentTab = Tab.场景搭建;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Gemini-Lab/打开工具窗口")]
        public static void ShowWindow()
        {
            var window = GetWindow<GeminiLabToolsWindow>("Gemini-Lab 工具");
            window.minSize = new Vector2(360, 420);
            window.Show();
        }

        private void OnGUI()
        {
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, new[] { "场景搭建", "模块连线", "UI面板", "调试" });
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_currentTab)
            {
                case Tab.场景搭建: DrawSceneTab(); break;
                case Tab.模块连线: DrawBootstrapTab(); break;
                case Tab.UI面板:   DrawUIPanelTab(); break;
                case Tab.调试:     DrawDebugTab(); break;
            }

            GUILayout.EndScrollView();
        }

        // ── 场景搭建 ────────────────────────────────────────

        private void DrawSceneTab()
        {
            Section("WorldMap 室外场景");
            ToolButton("重建 WorldMap 场景（破坏性，清空重建）", () =>
            {
                WorldMapSceneAuthoring.Author();
            });
            EditorGUILayout.HelpBox("会清空 _SceneRoot 下所有对象并完整重建。日常添加用下方增量按钮。", MessageType.Warning);

            ToolButton("增量添加场景物（小木屋/祈愿树/邮箱）", () =>
            {
                WorldMapSceneObjectsPatch.Patch();
            });

            ToolButton("增量添加花园 Zone（3×3 花圃 + 点击情绪输入）", () =>
            {
                WorldMapGardenZonePatch.Patch();
            });

            Section("Apartment 公寓场景");
            ToolButton("重建 Apartment 侧边栏（破坏性）", () =>
            {
                ApartmentSidebarAuthoring.Author();
            });
        }

        // ── 模块连线 ────────────────────────────────────────

        private void DrawBootstrapTab()
        {
            Section("BootstrapRoot 挂载");

            ToolButton("Boot: 挂载 EmotionGarden RuntimeBootstrap", () =>
            {
                BootEmotionGardenBootstrapAuthoring.Author();
            });

            ToolButton("Boot: 挂载 Garden RuntimeBootstrap", () =>
            {
                BootGardenBootstrapAuthoring.Author();
            });
        }

        // ── UI 面板 ─────────────────────────────────────────

        private void DrawUIPanelTab()
        {
            Section("WorldMap 场景 UI");
            ToolButton("添加情绪花园面板（情绪输入/每周培育/情绪图鉴）", () =>
            {
                WorldMapEmotionGardenUIPatch.Patch();
            });
            EditorGUILayout.HelpBox("幂等，可重复执行。需 Canvas 已存在（跑过 Author WorldMap Scene）。", MessageType.Info);

            Section("Apartment 场景 UI");
            ToolButton("增量添加 Garden 面板到侧边栏", () =>
            {
                ApartmentGardenSidebarPatch.Patch();
            });
        }

        // ── 调试 ────────────────────────────────────────────

        private void DrawDebugTab()
        {
            Section("自动初始化");
            var isDone = EditorPrefs.GetInt("GeminiLab.AutoSetupDone", 0) >= 2;
            EditorGUILayout.HelpBox(
                isDone ? "自动初始化已完成。下次编译不会重复执行。" : "自动初始化尚未执行。切换回 Unity 编译后会自动运行。",
                isDone ? MessageType.Info : MessageType.Warning);

            if (GUILayout.Button("重置自动初始化（下次编译重新执行）"))
            {
                AutoSetup.Reset();
            }

            Section("ServiceLocator");
            if (GUILayout.Button("打印所有已注册服务"))
            {
                var field = typeof(GeminiLab.Core.ServiceLocator).GetField(
                    "_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Debug.Log(field != null
                    ? $"[ServiceLocator] registry = {field.GetValue(null)}"
                    : "[ServiceLocator] 无法读取内部字典");
            }

            Section("EventBus");
            if (GUILayout.Button("打印 EventBus 订阅者数量"))
            {
                if (GeminiLab.Core.ServiceLocator.TryResolve(out GeminiLab.Core.Events.EventBus? bus) && bus != null)
                {
                    var field = typeof(GeminiLab.Core.Events.EventBus).GetField(
                        "_subscribers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Debug.Log(field != null
                        ? $"[EventBus] _subscribers = {field.GetValue(bus)}"
                        : "[EventBus] 无法读取内部字段");
                }
                else
                {
                    Debug.Log("[EventBus] 未注册");
                }
            }

            Section("当前场景");
            if (GUILayout.Button("打印场景信息"))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                Debug.Log($"[Scene] path={scene.path}, name={scene.name}, rootCount={scene.rootCount}");
                foreach (var go in scene.GetRootGameObjects())
                    Debug.Log($"  Root: {go.name}");
            }
        }

        // ── helpers ─────────────────────────────────────────

        private static void Section(string title)
        {
            GUILayout.Space(8);
            GUILayout.Label(title, EditorStyles.boldLabel);
        }

        private static void ToolButton(string label, System.Action action)
        {
            if (GUILayout.Button(label, GUILayout.Height(32)))
            {
                action();
            }
        }
    }
}
#endif
