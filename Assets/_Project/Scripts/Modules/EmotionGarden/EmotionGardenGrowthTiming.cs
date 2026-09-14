using System;

namespace GeminiLab.Modules.EmotionGarden
{
    public static class EmotionGardenGrowthTiming
    {
        // 10分钟发芽
        public const float SproutSeconds = 10f*60f;

        // 60分钟成熟
        public const float BloomSeconds = 60f*60f;

        public static TimeSpan BloomDuration =>
            TimeSpan.FromSeconds(BloomSeconds);
    }
}
