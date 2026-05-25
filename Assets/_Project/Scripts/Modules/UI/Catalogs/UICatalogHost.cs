#nullable enable
using GeminiLab.Core;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// UI Catalog 宿主：在 Boot 场景挂一个此组件，在 Inspector 拖入 `UIFontCatalogSO` / `UIArtCatalogSO` 资产。
    /// Awake 时把服务注册到 <see cref="ServiceLocator"/>，跨场景存活。
    /// </summary>
    public sealed class UICatalogHost : MonoBehaviour
    {
        [SerializeField] private UIFontCatalogSO? _fontCatalog;
        [SerializeField] private UIArtCatalogSO? _artCatalog;

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (_fontCatalog != null)
            {
                ServiceLocator.Register<IUIFontService>(new UIFontService(_fontCatalog));
            }
            else
            {
                Debug.LogWarning("[UICatalogHost] 未绑定 UIFontCatalogSO，TMP 字体将保持 Unity 默认");
            }

            if (_artCatalog != null)
            {
                ServiceLocator.Register<IUIArtService>(new UIArtService(_artCatalog));
            }
        }

        private sealed class UIFontService : IUIFontService
        {
            private readonly UIFontCatalogSO _catalog;
            public UIFontService(UIFontCatalogSO catalog) { _catalog = catalog; }
            public TMP_FontAsset? Get(string key) => _catalog.Get(key);
            public TMP_FontAsset? Default => _catalog.Get("default");
        }

        private sealed class UIArtService : IUIArtService
        {
            private readonly UIArtCatalogSO _catalog;
            public UIArtService(UIArtCatalogSO catalog) { _catalog = catalog; }
            public Sprite? Get(string key) => _catalog.Get(key);
        }
    }
}
