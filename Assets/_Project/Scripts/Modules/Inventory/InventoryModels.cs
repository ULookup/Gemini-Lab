#nullable enable
using System;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>单格堆叠。</summary>
    [Serializable]
    public struct ItemStack
    {
        public string ItemId;
        public int Count;

        public ItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    }

    /// <summary>物品栏变化事件。订阅方无参刷新自己的视图。</summary>
    public readonly struct InventoryChangedEvent
    {
        public InventoryChangedEvent(string itemId, int delta)
        {
            ItemId = itemId;
            Delta = delta;
        }

        public string ItemId { get; }
        /// <summary>正 = 增加；负 = 减少；0 = 其他（例如整盘重置）。</summary>
        public int Delta { get; }
    }
}
