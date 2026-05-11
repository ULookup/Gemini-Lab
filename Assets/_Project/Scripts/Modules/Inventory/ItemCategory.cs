#nullable enable

namespace GeminiLab.Modules.Inventory
{
    /// <summary>物品分类。影响物品栏 UI 标签页和跨系统消费路径。</summary>
    public enum ItemCategory
    {
        Misc = 0,
        Seed = 1,
        Crop = 2,
        Consumable = 3,
        TravelSouvenir = 4,
        Currency = 5
    }
}
