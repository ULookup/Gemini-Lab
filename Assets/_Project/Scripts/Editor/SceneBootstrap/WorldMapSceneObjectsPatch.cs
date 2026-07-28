#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.WorldMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 增量添加 WorldMap 场景物占位（小木屋/祈愿树/邮箱）。
    /// 幂等：已存在的对象跳过。不会清空已有场景内容。
    /// </summary>
    public static class WorldMapSceneObjectsPatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("_SceneRoot");
            if (root == null)
            {
                Debug.LogError("[WorldMapSceneObjectsPatch] _SceneRoot 不存在，请先跑 Author WorldMap Scene");
                return;
            }

            // 清理旧的占位物体（已被 PSD 导入的真实美术资源替代）
            foreach (var name in new[] { "Cabin", "WishingTree", "Mailbox" })
            {
                var old = root.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // 配置桥的可步行表面，移动轮廓直接来自桥对象的 PolygonCollider2D 上轮廓。
            SetupBridgeWalkableSurface();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapSceneObjectsPatch] 旧占位物体已清理 + 桥 PolygonCollider2D 上轮廓已作为可步行表面");
        }

        /// <summary>配置"桥"为桌宠可步行表面：WalkableSurface 直接读取桥的 PolygonCollider2D 上轮廓。</summary>
        private static void SetupBridgeWalkableSurface()
        {
            var bg = GameObject.Find("桥");
            if (bg == null)
            {
                Debug.LogWarning("[WorldMapSceneObjectsPatch] 未找到桥对象，跳过桥碰撞配置");
                return;
            }

            if (bg.GetComponent<PolygonCollider2D>() == null)
            {
                Debug.LogWarning("[WorldMapSceneObjectsPatch] 桥缺少 PolygonCollider2D，WalkableSurface 无法读取桥面上轮廓");
                return;
            }

            var ws = bg.GetComponent<WalkableSurface>();
            if (ws == null)
            {
                ws = bg.AddComponent<WalkableSurface>();
                Debug.Log("[WorldMapSceneObjectsPatch] 桥 WalkableSurface 已添加");
            }

            var wsSo = new SerializedObject(ws);
            wsSo.FindProperty("_overrideBounds").boolValue = false;
            wsSo.FindProperty("_yOffset").floatValue = 0f;
            wsSo.ApplyModifiedProperties();

            Debug.Log("[WorldMapSceneObjectsPatch] 桥 WalkableSurface 已改为读取 PolygonCollider2D 上轮廓");
        }

        private static void EnsurePlaceholder(Transform parent, string name, Vector3 pos,
            Vector2 size, Color color, string label, string clickMessage)
        {
            var existing = parent.Find(name);
            if (existing != null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingLayerName = "Furniture";
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var clickable = go.AddComponent<ClickableSceneObject>();
            var clkSo = new SerializedObject(clickable);
            clkSo.FindProperty("_displayName").stringValue = label;
            clkSo.FindProperty("_clickMessage").stringValue = clickMessage;
            clkSo.ApplyModifiedProperties();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, size.y * 0.5f + 0.5f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = label;
            tmp.fontSize = 3;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 11;
        }
    }
}
#endif
