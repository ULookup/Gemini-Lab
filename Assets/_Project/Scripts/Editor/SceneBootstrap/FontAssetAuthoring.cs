#nullable enable
#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>
    /// 作者化入口：从 `Art/Fonts/NotoSansSC-VF.ttf` 生成或修复动态 SDF TMP Font Asset，
    /// 落地到 `Art/Fonts/NotoSansSC_SDF.asset`，并正确持久化材质与 atlas 子资源。
    /// 若目标 asset 已存在且健康，则保持不动；若已存在但损坏，则原位修复以保留引用。
    /// </summary>
    public static class FontAssetAuthoring
    {
        private const string TtfPath = "Assets/_Project/Art/Fonts/NotoSansSC-VF.ttf";
        private const string FontAssetPath = "Assets/_Project/Art/Fonts/NotoSansSC_SDF.asset";

        [MenuItem("Tools/Gemini-Lab/Generate CJK TMP Font Asset")]
        public static void Generate()
        {
            TMP_FontAsset? existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (FontAssetRebake.IsFontAssetHealthy(existingAsset))
            {
                Debug.Log($"[FontAssetAuthoring] Font Asset 已存在且健康，跳过生成：{FontAssetPath}");
                return;
            }

            TMP_FontAsset? fontAsset = FontAssetRebake.CreateOrRepairFontAsset(
                TtfPath,
                FontAssetPath,
                "Noto Sans SC",
                atlasSize: 1024,
                samplingPointSize: 90,
                atlasPadding: 9,
                preBakeCommonCharacters: false);

            if (fontAsset == null)
            {
                Debug.LogError($"[FontAssetAuthoring] 生成或修复失败：{FontAssetPath}");
                return;
            }

            Debug.Log($"[FontAssetAuthoring] 生成或修复完成：{FontAssetPath}");
        }
    }
}
#endif
