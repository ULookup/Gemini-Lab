#nullable enable
using System;

namespace GeminiLab.Modules.Garden
{
    /// <summary>花园地块的生长阶段。实时生长：Seeded → Growing → Ready。</summary>
    public enum GardenStage
    {
        Empty = 0,
        Seeded = 1,
        Growing = 2,
        Ready = 3,
        Withered = 4
    }

    /// <summary>
    /// 一块地的运行态数据。
    /// 时间以 UTC Ticks（long）存，方便 JSON 序列化；IGameClock 负责计算实际经过的秒数。
    /// </summary>
    [Serializable]
    public struct GardenPlot
    {
        public int Index;
        public GardenStage Stage;
        public string SeedItemId;
        public string CropItemId;
        public long PlantedAtUtcTicks;
        public int TotalGrowSeconds;
    }

    /// <summary>单格地块变化（种下 / 推进 / 收获）。</summary>
    public readonly struct GardenPlotChangedEvent
    {
        public GardenPlotChangedEvent(GardenPlot plot) { Plot = plot; }
        public GardenPlot Plot { get; }
    }

    /// <summary>收获完成后广播，面板做收获特效 / Collection 归档。</summary>
    public readonly struct GardenHarvestedEvent
    {
        public GardenHarvestedEvent(int plotIndex, string cropItemId, int count)
        {
            PlotIndex = plotIndex;
            CropItemId = cropItemId;
            Count = count;
        }

        public int PlotIndex { get; }
        public string CropItemId { get; }
        public int Count { get; }
    }
}
