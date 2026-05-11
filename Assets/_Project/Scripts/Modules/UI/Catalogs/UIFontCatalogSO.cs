#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// TMP 字体目录。动态文本（聊天气泡、塔罗解读、数值）通过此 Catalog 获取统一字体。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/UI/UI Font Catalog", fileName = "UIFontCatalog")]
    public sealed class UIFontCatalogSO : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            [Tooltip("代码中用于查找的 key，例如 default / title / bubble")]
            public string key = string.Empty;

            public TMP_FontAsset? font;

            [TextArea] public string description = string.Empty;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, TMP_FontAsset?>? _lookup;

        public TMP_FontAsset? Get(string key)
        {
            EnsureLookup();
            return _lookup!.TryGetValue(key, out TMP_FontAsset? font) ? font : null;
        }

        private void EnsureLookup()
        {
            if (_lookup is not null)
            {
                return;
            }

            _lookup = new Dictionary<string, TMP_FontAsset?>(_entries.Count);
            foreach (Entry entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    _lookup[entry.key] = entry.font;
                }
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
