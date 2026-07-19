#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor
{
    public static class UIIsolationTool
    {
        private const string MenuIsolate = "Tools/Gemini-Lab/UI Isolation/Isolate Selected %#i";
        private const string MenuShowAll = "Tools/Gemini-Lab/UI Isolation/Show All %#u";
        private const string MenuShowAllForce = "Tools/Gemini-Lab/UI Isolation/Show All UI (Force) %#o";

        private static bool _hasStoredStates;
        private static readonly List<(GameObject obj, bool wasActive)> _storedStates = new();

        [MenuItem(MenuIsolate, true)]
        private static bool IsolateSelected_Validate()
        {
            return Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponentInParent<Canvas>() != null;
        }

        [MenuItem(MenuIsolate)]
        private static void IsolateSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return;

            var canvas = selected.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[UI Iso] Selected object is not under a Canvas.");
                return;
            }

            var canvasRoot = canvas.rootCanvas.gameObject;

            RestoreAll();
            _hasStoredStates = true;

            // Walk up from selection to Canvas root.
            // At each level, hide every sibling that isn't on the ancestry path.
            var t = selected.transform;
            while (t != null && t.gameObject != canvasRoot)
            {
                var parent = t.parent;
                if (parent == null) break;

                for (int i = 0; i < parent.childCount; i++)
                {
                    var sibling = parent.GetChild(i).gameObject;
                    if (sibling == t.gameObject)
                        continue;

                    // Don't re-hide something already handled at a higher level
                    if (!sibling.activeSelf)
                        continue;

                    _storedStates.Add((sibling, sibling.activeSelf));
                    sibling.SetActive(false);
                }

                t = parent;
            }

            EditorApplication.RepaintHierarchyWindow();
            Selection.activeGameObject = selected;
            Debug.Log($"[UI Iso] Isolated: {selected.name} (Ctrl+Shift+U to show all)");
        }

        [MenuItem(MenuShowAll, true)]
        private static bool ShowAll_Validate() => _hasStoredStates;

        [MenuItem(MenuShowAll)]
        private static void ShowAll() => RestoreAll();

        [MenuItem(MenuShowAllForce, true)]
        private static bool ShowAllForce_Validate() => Object.FindObjectOfType<Canvas>(true) != null;

        [MenuItem(MenuShowAllForce)]
        private static void ShowAllForce()
        {
            var canvas = Object.FindObjectOfType<Canvas>(true);
            if (canvas == null) return;

            var canvasRoot = canvas.rootCanvas.gameObject;
            RecursiveSetActive(canvasRoot.transform, true);
            _storedStates.Clear();
            _hasStoredStates = false;
            EditorApplication.RepaintHierarchyWindow();
            Debug.Log("[UI Iso] All UI elements force-enabled.");
        }

        private static void RecursiveSetActive(Transform t, bool active)
        {
            t.gameObject.SetActive(active);
            for (int i = 0; i < t.childCount; i++)
                RecursiveSetActive(t.GetChild(i), active);
        }

        private static void RestoreAll()
        {
            foreach (var (obj, wasActive) in _storedStates)
            {
                if (obj != null)
                    obj.SetActive(wasActive);
            }
            _storedStates.Clear();
            _hasStoredStates = false;
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
