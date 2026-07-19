#nullable enable
#if UNITY_EDITOR
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GeminiLab.Editor
{
    /// <summary>
    /// 重新烘焙项目内使用的动态中文 TMP 字体，并确保字体资产、材质和 atlas 纹理一起持久化落盘。
    /// </summary>
    public static class FontAssetRebake
    {
        private const int AtlasSize = 4096;
        private const int SamplingPointSize = 36;
        private const int AtlasPadding = 9;
        private const int BatchSize = 512;

        private const string WenKaiSourceTtfPath = "Assets/TextMesh Pro/Fonts/LXGWWenKai-Regular.ttf";
        private const string WenKaiFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai SDF.asset";
        private const string NotoSourceTtfPath = "Assets/_Project/Art/Fonts/NotoSansSC-VF.ttf";
        private const string NotoFontAssetPath = "Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset";

        [MenuItem("Tools/Gemini-Lab/Re-Bake LXGW WenKai Font (4096 Atlas)")]
        public static void ReBake()
        {
            CreateOrRepairFontAsset(
                WenKaiSourceTtfPath,
                WenKaiFontAssetPath,
                "LXGW WenKai",
                AtlasSize,
                SamplingPointSize,
                AtlasPadding,
                preBakeCommonCharacters: true);
        }

        [MenuItem("Tools/Gemini-Lab/Re-Bake Noto Sans SC Font (UI Catalog)")]
        public static void ReBakeNotoSansSc()
        {
            CreateOrRepairFontAsset(
                NotoSourceTtfPath,
                NotoFontAssetPath,
                "Noto Sans SC",
                AtlasSize,
                SamplingPointSize,
                AtlasPadding,
                preBakeCommonCharacters: true);
        }

        public static bool IsFontAssetHealthy(TMP_FontAsset? fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null)
            {
                return false;
            }

            Texture2D[]? atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null || atlasTextures.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < atlasTextures.Length; i++)
            {
                if (atlasTextures[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RepairProjectFontsIfNeeded()
        {
            bool repairedAny = false;

            repairedAny |= RepairFontIfNeeded(
                NotoSourceTtfPath,
                NotoFontAssetPath,
                "Noto Sans SC",
                AtlasSize,
                SamplingPointSize,
                AtlasPadding,
                preBakeCommonCharacters: true);

            repairedAny |= RepairFontIfNeeded(
                WenKaiSourceTtfPath,
                WenKaiFontAssetPath,
                "LXGW WenKai",
                AtlasSize,
                SamplingPointSize,
                AtlasPadding,
                preBakeCommonCharacters: true);

            if (repairedAny)
            {
                Debug.Log("[TMP_FONT_AUTO_REPAIR] 已完成损坏 TMP 字体资产修复，请清空 Console 后重新验证中文显示。");
            }

            return repairedAny;
        }

        public static TMP_FontAsset? CreateOrRepairFontAsset(
            string sourceTtfPath,
            string fontAssetPath,
            string displayName,
            int atlasSize,
            int samplingPointSize,
            int atlasPadding,
            bool preBakeCommonCharacters)
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(sourceTtfPath);
            if (ttf == null)
            {
                Debug.LogError($"[FontAssetRebake] 源字体未找到：{sourceTtfPath}");
                return null;
            }

            TMP_FontAsset? existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            TMP_FontAsset? generatedAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: samplingPointSize,
                atlasPadding: atlasPadding,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: atlasSize,
                atlasHeight: atlasSize,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (generatedAsset == null)
            {
                Debug.LogError($"[FontAssetRebake] CreateFontAsset 返回 null：{displayName}");
                return null;
            }

            generatedAsset.name = existingAsset != null
                ? existingAsset.name
                : Path.GetFileNameWithoutExtension(fontAssetPath);

            string? charSet = null;
            TMP_FontAsset targetAsset;
            bool createdNewAsset = existingAsset == null;

            try
            {
                if (preBakeCommonCharacters)
                {
                    charSet = GenerateCharacterSet();
                    Debug.Log($"[FontAssetRebake] {displayName} 字符集大小: {charSet.Length}");

                    int bakedCount = 0;
                    for (int i = 0; i < charSet.Length; i += BatchSize)
                    {
                        int len = Mathf.Min(BatchSize, charSet.Length - i);
                        string batch = charSet.Substring(i, len);
                        generatedAsset.TryAddCharacters(batch);
                        bakedCount += len;

                        if ((i / BatchSize) % 5 == 0)
                        {
                            EditorUtility.DisplayProgressBar(
                                "烘焙字体",
                                $"正在烘焙 {displayName}... {bakedCount}/{charSet.Length}",
                                (float)bakedCount / charSet.Length);
                        }
                    }
                }

                if (createdNewAsset)
                {
                    AssetDatabase.CreateAsset(generatedAsset, fontAssetPath);
                    targetAsset = generatedAsset;
                }
                else
                {
                    targetAsset = existingAsset!;
                    RemoveGeneratedSubAssets(targetAsset);
                    EditorUtility.CopySerialized(generatedAsset, targetAsset);
                }

                targetAsset.name = Path.GetFileNameWithoutExtension(fontAssetPath);
                PersistGeneratedSubAssets(targetAsset);
                EditorUtility.SetDirty(targetAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(fontAssetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                int atlasTexCount = targetAsset.atlasTextures?.Length ?? 0;
                int totalAtlasW = 0;
                if (targetAsset.atlasTextures != null)
                {
                    foreach (Texture2D? tex in targetAsset.atlasTextures)
                    {
                        if (tex != null)
                        {
                            totalAtlasW += tex.width;
                        }
                    }
                }

                Debug.Log($"[FontAssetRebake] {displayName} 完成！字符表条目: {targetAsset.characterTable.Count}, " +
                          $"字形数: {targetAsset.glyphTable.Count}, 图集纹理数: {atlasTexCount}, " +
                          $"图集总宽度: {totalAtlasW}px, 资产路径: {fontAssetPath}");

                return targetAsset;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!createdNewAsset)
                {
                    Object.DestroyImmediate(generatedAsset);
                }
            }
        }

        public static void PersistGeneratedSubAssets(TMP_FontAsset fontAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(fontAsset);

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                if (AssetDatabase.GetAssetPath(fontAsset.material) != assetPath)
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                EditorUtility.SetDirty(fontAsset.material);
            }

            if (fontAsset.atlasTextures == null)
            {
                return;
            }

            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D? tex = fontAsset.atlasTextures[i];
                if (tex == null)
                {
                    continue;
                }

                tex.name = i == 0
                    ? $"{fontAsset.name} Atlas"
                    : $"{fontAsset.name} Atlas {i}";

                if (AssetDatabase.GetAssetPath(tex) != assetPath)
                {
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }

                EditorUtility.SetDirty(tex);
            }
        }

        private static bool RepairFontIfNeeded(
            string sourceTtfPath,
            string fontAssetPath,
            string displayName,
            int atlasSize,
            int samplingPointSize,
            int atlasPadding,
            bool preBakeCommonCharacters)
        {
            TMP_FontAsset? fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (IsFontAssetHealthy(fontAsset))
            {
                return false;
            }

            Debug.Log($"[TMP_FONT_AUTO_REPAIR] 检测到损坏字体：{displayName} -> {fontAssetPath}，开始原位修复。");

            return CreateOrRepairFontAsset(
                sourceTtfPath,
                fontAssetPath,
                displayName,
                atlasSize,
                samplingPointSize,
                atlasPadding,
                preBakeCommonCharacters) != null;
        }

        private static void RemoveGeneratedSubAssets(TMP_FontAsset fontAsset)
        {
            string assetPath = AssetDatabase.GetAssetPath(fontAsset);
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in subAssets)
            {
                if (ReferenceEquals(asset, fontAsset))
                {
                    continue;
                }

                Object.DestroyImmediate(asset, true);
            }
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
