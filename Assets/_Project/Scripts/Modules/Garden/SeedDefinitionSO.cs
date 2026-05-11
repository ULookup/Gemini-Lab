#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Garden
{
    /// <summary>
    /// 种子 → 作物 + 成熟时间的静态定义。运行期只读。
    /// 种子 itemId（例如 seed_sunflower）由 ItemDefSO 那边定义，本 SO 建立种子 → 作物 + 生长时长的映射。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Garden/Seed Definition", fileName = "SeedDef_")]
    public sealed class SeedDefinitionSO : ScriptableObject
    {
        [Tooltip("种子在 Inventory 里的 itemId（例：seed_sunflower）")]
        public string SeedItemId = string.Empty;

        [Tooltip("成熟后产出的作物 itemId（例：crop_sunflower）")]
        public string CropItemId = string.Empty;

        [Tooltip("一次收获的作物数量；默认 1")]
        [Min(1)] public int HarvestCount = 1;

        [Tooltip("从 Seeded 到 Ready 需要的真实秒数；默认 7200 = 2 小时")]
        [Min(1)] public int TotalGrowSeconds = 7200;

        [Tooltip("Growing 阶段的起始秒数（达到这个秒数切到 Growing 外观）；默认 1/3")]
        [Min(1)] public int GrowingStartSeconds = 2400;
    }
}
