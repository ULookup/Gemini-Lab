#nullable enable
using TMPro;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// TMP 字体查询服务。由业务代码调用，避免直接持有 <see cref="UIFontCatalogSO"/>。
    /// </summary>
    public interface IUIFontService
    {
        /// <summary>获取指定 key 对应的字体；未配置时返回 null，调用方自行决定 fallback。</summary>
        TMP_FontAsset? Get(string key);

        /// <summary>获取 `default` 字体；通常用于未指定 key 的 TMP 文本。</summary>
        TMP_FontAsset? Default { get; }
    }
}
