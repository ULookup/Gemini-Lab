#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.UI.Catalogs
{
    /// <summary>
    /// 静态 UI 图（按钮、标题、图标、9-slice 边框等）资源目录。
    /// 所有 UI Prefab 禁止直接引用 Sprite，必须通过此 Catalog 按 key 获取。
    /// 美术替换只需修改对应 .asset 的 Sprite 引用，不需要改 Prefab / 代码。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/UI/UI Art Catalog", fileName = "UIArtCatalog")]
    public sealed class UIArtCatalogSO : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            [Tooltip("代码中用于查找的 key，例如 btn_start / tab_tarot / bubble_angel")]
            public string key = string.Empty;

            [Tooltip("美术交付后指向真实 Sprite；占位阶段允许为空或指向占位图")]
            public Sprite? sprite;

            [Tooltip("给美术/策划看的说明：这个槽位应该放什么图")]
            [TextArea] public string description = string.Empty;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, Sprite?>? _lookup;

        public Sprite? Get(string key)
        {
            EnsureLookup();
            if (_lookup!.TryGetValue(key, out Sprite? sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"[UIArtCatalog] 未登记的 key：{key}");
            return null;
        }

        public bool TryGet(string key, out Sprite? sprite)
        {
            EnsureLookup();
            return _lookup!.TryGetValue(key, out sprite);
        }

        private void EnsureLookup()
        {
            if (_lookup is not null)
            {
                return;
            }

            _lookup = new Dictionary<string, Sprite?>(_entries.Count);
            foreach (Entry entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    _lookup[entry.key] = entry.sprite;
                }
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
