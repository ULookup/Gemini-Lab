#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// 单件道具的静态定义。运行时 InventoryService 只存 id + count 堆叠信息。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Inventory/Item Definition", fileName = "ItemDef")]
    public sealed class ItemDefSO : ScriptableObject
    {
        [Tooltip("代码中唯一标识。例：seed_carrot / crop_tomato")]
        [SerializeField] private string _id = string.Empty;

        [SerializeField] private string _displayNameZh = string.Empty;

        [SerializeField] private ItemCategory _category = ItemCategory.Misc;

        [SerializeField] private Sprite? _icon;

        [Tooltip("是否可堆叠；false 表示每个单独占格")]
        [SerializeField] private bool _stackable = true;

        [Tooltip("单格最大堆叠数；stackable=false 时忽略")]
        [SerializeField, Min(1)] private int _maxPerStack = 99;

        [TextArea]
        [SerializeField] private string _tooltip = string.Empty;

        public string Id => _id;
        public string DisplayNameZh => _displayNameZh;
        public ItemCategory Category => _category;
        public Sprite? Icon => _icon;
        public bool Stackable => _stackable;
        public int MaxPerStack => _stackable ? Mathf.Max(1, _maxPerStack) : 1;
        public string Tooltip => _tooltip;
    }
}
