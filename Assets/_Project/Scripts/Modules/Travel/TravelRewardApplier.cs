#nullable enable
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Travel
{
    /// <summary>
    /// Applies travel completion rewards to runtime pet attributes.
    /// </summary>
    public static class TravelRewardApplier
    {
        public static void ApplyTravelReward(PetRuntimeData runtimeData, int qualityLevel = 1)
        {
            runtimeData.Mood = Clamp01To100(runtimeData.Mood + (8f * qualityLevel));
            runtimeData.Energy = Clamp01To100(runtimeData.Energy - (5f * qualityLevel));
            runtimeData.Satiety = Clamp01To100(runtimeData.Satiety - (6f * qualityLevel));
            runtimeData.TravelCompletedCount += 1;
        }

        private static float Clamp01To100(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 100f ? 100f : value;
        }
    }
}
