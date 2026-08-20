#nullable enable
namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 心情分档（数值规则文档 §5）。只在运行时计算，不额外保存。
    /// </summary>
    public enum MoodBand
    {
        /// <summary>0 ~ 29</summary>
        Low = 0,

        /// <summary>30 ~ 69</summary>
        Normal = 1,

        /// <summary>70 ~ 100</summary>
        High = 2
    }

    public static class MoodBandExtensions
    {
        public static MoodBand FromMood(float mood)
        {
            if (mood < 30f)
            {
                return MoodBand.Low;
            }

            return mood >= 70f ? MoodBand.High : MoodBand.Normal;
        }
    }
}
