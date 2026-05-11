#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Garden
{
    /// <summary>
    /// 花园对外门面。3×3 共 9 格，实时生长（默认 2 小时成熟）。
    /// 运行态存档走 <see cref="GeminiLab.Core.Persistence.IPersistentService"/>（Key=<c>garden</c>）。
    /// </summary>
    public interface IGardenService
    {
        /// <summary>地块格子数（默认 9）。</summary>
        int PlotCount { get; }

        /// <summary>当前全部地块快照（只读拷贝）。</summary>
        IReadOnlyList<GardenPlot> GetAllPlots();

        /// <summary>按索引查询单格；越界返回 Empty。</summary>
        GardenPlot Get(int plotIndex);

        /// <summary>种子下地：消耗 Inventory 一颗种子 + 记录种植时间。</summary>
        bool Plant(int plotIndex, string seedItemId);

        /// <summary>收获：从 Ready 的格子取走作物并给 Inventory + Collection。</summary>
        bool Harvest(int plotIndex);

        /// <summary>
        /// 用 IGameClock 推进所有地块的 Stage。
        /// 订阅每帧 Update 或面板打开时调。幂等。
        /// </summary>
        void Refresh();

        /// <summary>剩余秒数（Empty/Ready 返回 0）。</summary>
        int GetRemainingSeconds(int plotIndex);
    }
}
