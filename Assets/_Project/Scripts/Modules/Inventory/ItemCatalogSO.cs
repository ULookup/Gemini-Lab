#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// ItemDefSO 索引。运行时通过 id 查找定义。
    /// 新增道具只要把 SO 拖进来，不改代码。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Inventory/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ItemDefSO> _items = new();

        private Dictionary<string, ItemDefSO>? _lookup;

        public IReadOnlyList<ItemDefSO> All => _items;

        public ItemDefSO? Get(string id)
        {
            EnsureLookup();
            return _lookup!.TryGetValue(id, out var def) ? def : null;
        }

        public bool TryGet(string id, out ItemDefSO? def)
        {
            EnsureLookup();
            return _lookup!.TryGetValue(id, out def);
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, ItemDefSO>(_items.Count);
            foreach (var item in _items)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    _lookup[item.Id] = item;
                }
            }
        }

        private void OnValidate() { _lookup = null; }

#if UNITY_EDITOR
        public void SetItemsEditorOnly(IEnumerable<ItemDefSO> items)
        {
            _items = new List<ItemDefSO>(items);
            _lookup = null;
        }
#endif
    }
}
