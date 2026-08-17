#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.Collection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>复用已有资源栏文本，作者化绑定苹果余额。</summary>
    public static class ApartmentAppleBalanceAuthoring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";

        public static void Patch()
        {
            if (!System.IO.File.Exists(ScenePath)) return;

            var scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            bool changed = false;
            var duplicatesToDestroy = new List<GameObject>();
            var coinsToDestroy = new List<CoinBalanceDisplay>();
            var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text.name != "BalanceLabel" || text.transform.parent == null ||
                    text.transform.parent.name != "TopResource") continue;

                // 删除上一版错误创建的重复文本，但保留原有 BalanceLabel 的布局和视觉样式。
                var duplicate = text.transform.parent.Find("AppleBalanceLabel");
                if (duplicate != null)
                {
                    duplicatesToDestroy.Add(duplicate.gameObject);
                    changed = true;
                }

                // 原 BalanceLabel 过去由 CoinBalanceDisplay 写入；现在直接复用同一 TMP 文本显示苹果。
                var coinDisplay = text.GetComponent<CoinBalanceDisplay>();
                if (coinDisplay != null)
                {
                    coinsToDestroy.Add(coinDisplay);
                    changed = true;
                }

                if (text.GetComponent<AppleBalanceDisplay>() == null)
                {
                    text.gameObject.AddComponent<AppleBalanceDisplay>();
                    changed = true;
                }

                // 让 Scene 视图也显示新档初始值，保证 Scene/Play 视觉一致。
                if (text.text != "20")
                {
                    text.text = "20";
                    changed = true;
                }
            }

            // 延迟销毁，避免 DestroyImmediate 修改本轮 FindObjectsByType 的枚举结果。
            foreach (var duplicate in duplicatesToDestroy)
                if (duplicate != null) Object.DestroyImmediate(duplicate);
            foreach (var coinDisplay in coinsToDestroy)
                if (coinDisplay != null) Object.DestroyImmediate(coinDisplay);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[ApartmentAppleBalanceAuthoring] 已复用四个 BalanceLabel，清理重复 AppleBalanceLabel 并绑定苹果余额");
        }
    }
}
#endif
