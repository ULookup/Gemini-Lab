#nullable enable
#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GeminiLab.Editor
{
    /// <summary>
    /// 重新烘焙 LXGWWenKai SDF 字体，扩大图集到 4096 并预烘焙常用汉字，
    /// 解决部分中文显示方块的问题。
    /// </summary>
    public static class FontAssetRebake
    {
        private const string SourceTtfPath = "Assets/TextMesh Pro/Fonts/LXGWWenKai-Regular.ttf";
        private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai SDF.asset";
        private const int AtlasSize = 4096;
        private const int SamplingPointSize = 36;
        private const int AtlasPadding = 9;

        [MenuItem("Tools/Gemini-Lab/Re-Bake LXGW WenKai Font (4096 Atlas)")]
        public static void ReBake()
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(SourceTtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[FontAssetRebake] 源字体未找到：{SourceTtfPath}");
                return;
            }

            // 生成常用汉字字符集
            string charSet = GenerateCharacterSet();
            Debug.Log($"[FontAssetRebake] 字符集大小: {charSet.Length}");

            // 删除旧资源
            AssetDatabase.DeleteAsset(FontAssetPath);

            // 用大图集 + 动态模式 + 多图集支持 创建新 Font Asset
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: SamplingPointSize,
                atlasPadding: AtlasPadding,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: AtlasSize,
                atlasHeight: AtlasSize,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError("[FontAssetRebake] CreateFontAsset 返回 null");
                return;
            }

            // 分批预烘焙常用汉字，避免一次性传入太多字符导致编辑器卡死
            int batchSize = 512;
            int bakedCount = 0;
            for (int i = 0; i < charSet.Length; i += batchSize)
            {
                int len = Mathf.Min(batchSize, charSet.Length - i);
                string batch = charSet.Substring(i, len);
                fontAsset.TryAddCharacters(batch);
                bakedCount += len;

                if ((i / batchSize) % 5 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "烘焙字体",
                        $"正在烘焙 LXGW WenKai... {bakedCount}/{charSet.Length}",
                        (float)bakedCount / charSet.Length);
                }
            }

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();

            // 验证
            int atlasTexCount = fontAsset.atlasTextures?.Length ?? 0;
            int totalAtlasW = 0;
            if (fontAsset.atlasTextures != null)
            {
                foreach (var tex in fontAsset.atlasTextures)
                {
                    if (tex != null) totalAtlasW += tex.width;
                }
            }

            Debug.Log($"[FontAssetRebake] 完成！字符表条目: {fontAsset.characterTable.Count}, " +
                      $"字形数: {fontAsset.glyphTable.Count}, 图集纹理数: {atlasTexCount}, " +
                      $"图集总宽度: {totalAtlasW}px");
        }

        private static string GenerateCharacterSet()
        {
            var sb = new StringBuilder();

            // ASCII 可打印字符
            for (int i = 0x0020; i <= 0x007E; i++)
                sb.Append((char)i);

            // CJK 标点符号
            for (int i = 0x3000; i <= 0x303F; i++)
                sb.Append((char)i);

            // CJK 统一汉字 — 最常用区间（约 5100 字，覆盖绝大多数中文场景）
            // U+4E00 ~ U+6200
            for (int i = 0x4E00; i <= 0x6200; i++)
                sb.Append((char)i);

            // 全角字符（含全角英数、中文标点）
            for (int i = 0xFF00; i <= 0xFFEF; i++)
                sb.Append((char)i);

            // 常用补充：部分次常用汉字区间
            for (int i = 0x6500; i <= 0x6800; i++)
                sb.Append((char)i);

            return sb.ToString();
        }
    }
}
#endif
