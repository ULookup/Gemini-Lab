#nullable enable
#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 一次性工具：从 `Art/Fonts/NotoSansSC-VF.ttf` 生成动态 SDF TMP Font Asset，
    /// 落地到 `Art/Fonts/NotoSansSC_SDF.asset`。
    /// 动态 SDF（Dynamic）模式：运行期遇到新字符时按需 rasterize，不需要预先生成全字符集，
    /// 字体文件从 17MB TTF 压到几十 KB asset。
    /// </summary>
    public static class FontAssetAuthoring
    {
        private const string TtfPath = "Assets/_Project/Art/Fonts/NotoSansSC-VF.ttf";
        private const string FontAssetPath = "Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset";

        [MenuItem("Tools/Gemini-Lab/Generate CJK TMP Font Asset")]
        public static void Generate()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[FontAssetAuthoring] TTF 未找到：{TtfPath}");
                return;
            }

            if (File.Exists(FontAssetPath))
            {
                Debug.Log($"[FontAssetAuthoring] Font Asset 已存在，跳过生成：{FontAssetPath}");
                return;
            }

            // Dynamic SDF：运行期按需烘焙字形；适合 CJK 这种字符集很大的场景
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            if (fontAsset == null)
            {
                Debug.LogError("[FontAssetAuthoring] CreateFontAsset 返回 null");
                return;
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontAssetAuthoring] 生成完成：{FontAssetPath}");
        }
    }
}
#endif
