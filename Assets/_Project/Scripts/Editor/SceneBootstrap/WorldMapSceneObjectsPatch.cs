#nullable enable
#if UNITY_EDITOR
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
        private const float GroundY = -3f;

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

            var rootT = root.transform;

            EnsurePlaceholder(rootT, "Cabin", new Vector3(-12f, GroundY - 0.2f, 0),
                new Vector2(2.5f, 3f), new Color(0.6f, 0.45f, 0.25f, 1f),
                "小木屋", "小木屋 · 功能待接入");

            EnsurePlaceholder(rootT, "WishingTree", new Vector3(-6f, GroundY + 0.2f, 0),
                new Vector2(1.8f, 3.5f), new Color(0.35f, 0.6f, 0.25f, 1f),
                "祈愿树", "祈愿树 · 功能待接入");

            EnsurePlaceholder(rootT, "Mailbox", new Vector3(6f, GroundY + 0.5f, 0),
                new Vector2(1f, 1.5f), new Color(0.7f, 0.3f, 0.25f, 1f),
                "邮箱", "邮箱 · 功能待接入");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldMapSceneObjectsPatch] 场景物占位已增量添加");
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
