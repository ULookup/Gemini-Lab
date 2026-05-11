#nullable enable
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Reflection;
using GeminiLab.Modules.UI.Catalogs;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 一次性工具：在 `Assets/_Project/ScriptableObjects/UIArt/` 下生成
    /// `UIFontCatalog.asset` 与 `UIArtCatalog.asset`，并往字体目录的 `default` key 填入 NotoSansSC_SDF。
    /// 重复执行是幂等的：已存在的 asset 会被复用。
    /// </summary>
    public static class UICatalogAuthoring
    {
        private const string FolderPath = "Assets/_Project/ScriptableObjects/UIArt";
        private const string FontCatalogPath = FolderPath + "/UIFontCatalog.asset";
        private const string ArtCatalogPath = FolderPath + "/UIArtCatalog.asset";
        private const string DefaultFontAssetPath = "Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset";

        [MenuItem("Tools/Gemini-Lab/Author UI Catalogs")]
        public static void Author()
        {
            EnsureFolder(FolderPath);

            var fontCatalog = AssetDatabase.LoadAssetAtPath<UIFontCatalogSO>(FontCatalogPath);
            if (fontCatalog == null)
            {
                fontCatalog = ScriptableObject.CreateInstance<UIFontCatalogSO>();
                AssetDatabase.CreateAsset(fontCatalog, FontCatalogPath);
            }

            var artCatalog = AssetDatabase.LoadAssetAtPath<UIArtCatalogSO>(ArtCatalogPath);
            if (artCatalog == null)
            {
                artCatalog = ScriptableObject.CreateInstance<UIArtCatalogSO>();
                AssetDatabase.CreateAsset(artCatalog, ArtCatalogPath);
            }

            // 往 Font Catalog 里塞 default 槽位
            var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
            if (defaultFont == null)
            {
                Debug.LogWarning($"[UICatalogAuthoring] 默认字体未找到：{DefaultFontAssetPath}");
            }
            else
            {
                UpsertFontEntry(fontCatalog, "default", defaultFont, "全局默认 TMP 字体（含 CJK），Noto Sans SC Dynamic SDF");
                UpsertFontEntry(fontCatalog, "title", defaultFont, "标题 / Logo 用；美术确定装饰字体前与 default 相同");
                UpsertFontEntry(fontCatalog, "bubble", defaultFont, "聊天 / 塔罗解读气泡用");
                EditorUtility.SetDirty(fontCatalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UICatalogAuthoring] Catalog 落地完成：{FontCatalogPath} / {ArtCatalogPath}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        private static void UpsertFontEntry(UIFontCatalogSO catalog, string key, TMP_FontAsset font, string description)
        {
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("_entries");
            int foundIndex = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var el = entries.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("key").stringValue == key)
                {
                    foundIndex = i;
                    break;
                }
            }
            if (foundIndex < 0)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                foundIndex = entries.arraySize - 1;
            }
            var entry = entries.GetArrayElementAtIndex(foundIndex);
            entry.FindPropertyRelative("key").stringValue = key;
            entry.FindPropertyRelative("font").objectReferenceValue = font;
            entry.FindPropertyRelative("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
