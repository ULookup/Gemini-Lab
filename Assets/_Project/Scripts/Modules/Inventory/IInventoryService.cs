#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// 物品栏对外门面。所有增删改都广播 <see cref="InventoryChangedEvent"/>。
    /// 存储结构对外隐藏；面板/旅行/花园/塔罗等业务只通过接口交互。
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>当前持有的全部堆叠快照（只读拷贝）。</summary>
        IReadOnlyList<ItemStack> GetAllStacks();

        /// <summary>某个 itemId 的持有总数（多格堆叠累加）。</summary>
        int GetTotalCount(string itemId);

        /// <summary>
        /// 增加道具。按 stackable + maxPerStack 规则分堆。
        /// 返回实际成功加入的数量（理论上 = count）。
        /// </summary>
        int Add(string itemId, int count);

        /// <summary>
        /// 扣除道具。count 不足时不执行。返回是否成功。
        /// </summary>
        bool TryRemove(string itemId, int count);

        /// <summary>清空物品栏（调试 / 新档）。</summary>
        void Clear();
    }
}
