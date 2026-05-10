#nullable enable
using GeminiLab.Core;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// 挂在 TMP_Text 同一 GameObject 上：Awake 时自动从 <see cref="IUIFontService"/> 取字体并赋给 TMP 组件。
    /// 美术换字体只需要改 Catalog `.asset`，不需要改 Prefab / 代码。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DefaultExecutionOrder(-1000)]
    public sealed class TMPFontBinder : MonoBehaviour
    {
        [SerializeField] private string _fontKey = "default";

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Apply()
        {
            var tmp = GetComponent<TMP_Text>();
            if (tmp == null) return;

            TMPro.TMP_FontAsset? font = null;
            if (ServiceLocator.TryResolve(out IUIFontService? fontService) && fontService is not null)
            {
                font = fontService.Get(_fontKey);
            }
            else
            {
                // Fallback：当 Boot 未进入（例如直接从 Apartment 场景 Play 调试）时，
                // 通过已加载资产直接查一次 UIFontCatalogSO，取 default/指定 key 的字体。
                var catalogs = Resources.FindObjectsOfTypeAll<UIFontCatalogSO>();
                if (catalogs != null && catalogs.Length > 0)
                {
                    font = catalogs[0].Get(_fontKey);
                }
            }

            if (font != null && !ReferenceEquals(tmp.font, font))
            {
                tmp.font = font;
                tmp.SetAllDirty();
            }
        }
    }
}
