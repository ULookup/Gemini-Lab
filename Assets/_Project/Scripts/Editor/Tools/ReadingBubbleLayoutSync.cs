#nullable enable
#if UNITY_EDITOR
using GeminiLab.Modules.HubUI.Panels;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Editor.Tools
{
    public static class ReadingBubbleLayoutSync
    {
        [MenuItem("Tools/Gemini-Lab/Sync Reading Bubble Layouts")]
        public static void Sync()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[BubbleSync] 请先在 Hierarchy 中选中一个调好的 ReadingBubble 作为模板。");
                return;
            }

            var template = selected.GetComponent<ReadingBubble>();
            if (template == null)
            {
                Debug.LogWarning("[BubbleSync] 选中的对象没有 ReadingBubble 组件。");
                return;
            }

            // Determine group by name keyword: "Angel" → sync only Angel bubbles, "Devil" → sync only Devil bubbles
            string[] keywords;
            if (selected.name.Contains("Angel"))
                keywords = new[] { "Angel" };
            else if (selected.name.Contains("Devil"))
                keywords = new[] { "Devil" };
            else
                keywords = new[] { "Angel", "Devil" }; // fallback: sync all

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            int bubbleCount = 0;
            int childCount = 0;
            foreach (var root in roots)
            {
                var allBubbles = root.GetComponentsInChildren<ReadingBubble>(true);
                foreach (var bubble in allBubbles)
                {
                    if (bubble == template) continue;

                    // Only sync same-group bubbles
                    if (!MatchesGroup(bubble.name, keywords)) continue;

                    // Sync the bubble's own RectTransform
                    var targetRt = bubble.GetComponent<RectTransform>();
                    if (targetRt != null)
                    {
                        Undo.RecordObject(targetRt, "Sync Bubble Layout");
                        CopyRectValues(selected.GetComponent<RectTransform>(), targetRt);
                        EditorUtility.SetDirty(targetRt);
                        bubbleCount++;
                    }

                    // Sync children by matching names
                    childCount += SyncChildren(selected.transform, bubble.transform);
                }
            }

            var groupLabel = keywords.Length == 1 ? keywords[0] : "All";
            Debug.Log($"[BubbleSync] 已将 {bubbleCount} 个 {groupLabel} 气泡及其 {childCount} 个子物体布局同步为 '{selected.name}'。");
        }

        private static int SyncChildren(Transform templateParent, Transform targetParent)
        {
            int synced = 0;
            foreach (Transform templateChild in templateParent)
            {
                var targetChild = targetParent.Find(templateChild.name);
                if (targetChild == null) continue;

                var templateRt = templateChild.GetComponent<RectTransform>();
                var targetRt = targetChild.GetComponent<RectTransform>();
                if (templateRt == null || targetRt == null) continue;

                Undo.RecordObject(targetRt, "Sync Child Layout");
                CopyRectValues(templateRt, targetRt);
                EditorUtility.SetDirty(targetRt);
                synced++;

                // Recurse into grandchildren
                synced += SyncChildren(templateChild, targetChild);
            }
            return synced;
        }

        private static bool MatchesGroup(string name, string[] keywords)
        {
            foreach (var kw in keywords)
                if (name.Contains(kw)) return true;
            return false;
        }

        private static void CopyRectValues(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;
            dst.pivot = src.pivot;
            dst.localScale = src.localScale;
            dst.localRotation = src.localRotation;
        }
    }
}
#endif
