#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 将 WorldMap 中已经存在的场景物作者化为可悬停、可点击对象。
    /// 只补组件和序列化参数，不创建新的视觉占位物，也不覆盖对象的现有位置、Sprite 或 Collider 尺寸。
    /// </summary>
    public static class WorldMapInteractiveObjectAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap/WorldMap_Main.unity";
        private const float DefaultHoverScaleMultiplier = 1.06f;
        private const float DefaultTransitionSeconds = 0.08f;

        private static readonly (string Name, bool IsCabin)[] Targets =
        {
            ("室内", true),
            ("邮箱", false),
            ("大树 1", false),
            ("大树 2", false),
            ("大树 3", false),
            ("大树 4", false),
            ("大树 5", false)
        };

        public static void Patch()
        {
            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;
            foreach (var target in Targets)
            {
                var go = GameObject.Find(target.Name);
                if (go == null)
                {
                    Debug.LogWarning($"[WorldMapInteractiveObjectAuthoring] 未找到「{target.Name}」，跳过");
                    continue;
                }

                changed |= EnsureCollider(go, target.Name);
                changed |= EnsureFeedback(go, target.Name);

                if (target.IsCabin)
                {
                    changed |= EnsureCabinPortal(go);
                }
                else
                {
                    changed |= EnsureClickable(go, target.Name);
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[WorldMapInteractiveObjectAuthoring] 室内、邮箱和 5 棵大树的悬停缩放与点击入口已作者化");
        }

        private static bool EnsureCollider(GameObject go, string displayName)
        {
            var collider = go.GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider2D>(go);
                collider.isTrigger = false;
                Debug.Log($"[WorldMapInteractiveObjectAuthoring] {displayName} 已补 BoxCollider2D");
                return true;
            }

            if (collider.isTrigger)
            {
                collider.isTrigger = false;
                EditorUtility.SetDirty(collider);
                return true;
            }

            return false;
        }

        private static bool EnsureFeedback(GameObject go, string displayName)
        {
            var feedback = go.GetComponent<WorldMapInteractiveObjectFeedback>();
            bool created = false;
            if (feedback == null)
            {
                feedback = Undo.AddComponent<WorldMapInteractiveObjectFeedback>(go);
                created = true;
            }

            var so = new SerializedObject(feedback);
            bool changed = false;
            if (created)
            {
                var multiplier = so.FindProperty("_hoverScaleMultiplier");
                if (multiplier != null)
                {
                    multiplier.floatValue = DefaultHoverScaleMultiplier;
                    changed = true;
                }

                var transition = so.FindProperty("_transitionSeconds");
                if (transition != null)
                {
                    transition.floatValue = DefaultTransitionSeconds;
                    changed = true;
                }

                var requireTopmost = so.FindProperty("_requireTopmostCollider");
                if (requireTopmost != null)
                {
                    requireTopmost.boolValue = false;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feedback);
            }

            return created || changed;
        }

        private static bool EnsureCabinPortal(GameObject go)
        {
            var oldClickable = go.GetComponent<ClickableSceneObject>();
            if (oldClickable != null)
            {
                Object.DestroyImmediate(oldClickable);
            }

            if (go.GetComponent<CabinReturnPortal>() != null)
            {
                return oldClickable != null;
            }

            Undo.AddComponent<CabinReturnPortal>(go);
            return true;
        }

        private static bool EnsureClickable(GameObject go, string displayName)
        {
            var clickable = go.GetComponent<ClickableSceneObject>();
            bool created = false;
            if (clickable == null)
            {
                clickable = Undo.AddComponent<ClickableSceneObject>(go);
                created = true;
            }

            var so = new SerializedObject(clickable);
            var displayNameProperty = so.FindProperty("_displayName");
            var clickMessageProperty = so.FindProperty("_clickMessage");
            bool changed = false;

            if (displayNameProperty != null && displayNameProperty.stringValue != displayName)
            {
                displayNameProperty.stringValue = displayName;
                changed = true;
            }

            const string clickMessage = "点击了 {0}（具体交互待接入）";
            if (clickMessageProperty != null && clickMessageProperty.stringValue != clickMessage)
            {
                clickMessageProperty.stringValue = clickMessage;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(clickable);
            }

            return created || changed;
        }
    }
}
#endif
