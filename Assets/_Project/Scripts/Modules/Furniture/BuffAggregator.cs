#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Furniture
{
    public static class BuffAggregator
    {
        public static EnvironmentalBuff Sum(IReadOnlyList<Furniture> furnitureList)
        {
            EnvironmentalBuff total = default;
            for (int i = 0; i < furnitureList.Count; i++)
            {
                EnvironmentalBuff buff = furnitureList[i].Definition.Buff;
                total.MoodDelta += buff.MoodDelta;
                total.EnergyDelta += buff.EnergyDelta;
            }

            return total;
        }
    }
}
