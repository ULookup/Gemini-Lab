#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Centralized stat ticking to avoid scattered direct value writes.
    /// 行为：
    /// - 睡眠中：Energy 按 <see cref="PetStateValueSO.SleepingEnergyRecoveryPerSecond"/> 回复；
    ///   Mood 稳定少量回复；Satiety 仍持续衰减（睡觉不会管饱）
    /// - 清醒时：Energy 持续衰减；Satiety 持续衰减；Mood 在精力/饱食充裕时回复；
    ///   在任一触底时反向扣除并把回复速度降到 <see cref="PetStateValueSO.MoodRecoveryPenaltyFactor"/> 倍
    /// </summary>
    public sealed class StatTickService
    {
        private const float LowSatietyThreshold = 30f;
        private const float LowEnergyThreshold = 20f;

        public void Tick(PetContext context, float deltaTime)
        {
            PetRuntimeData data = context.RuntimeData;
            PetStateValueSO config = context.Config;

            // Satiety 始终衰减（睡觉也会饿）
            data.Satiety -= config.SatietyDecayPerSecond * deltaTime;

            if (context.IsSleeping)
            {
                data.Energy += config.SleepingEnergyRecoveryPerSecond * deltaTime;
                data.Mood += config.MoodRecoveryPerSecond * 0.5f * deltaTime;
            }
            else
            {
                data.Energy -= config.AwakeEnergyDecayPerSecond * deltaTime;

                bool lowSatiety = data.Satiety <= LowSatietyThreshold;
                bool lowEnergy = data.Energy <= LowEnergyThreshold;

                float moodRecovery = config.MoodRecoveryPerSecond;
                if (lowSatiety || lowEnergy)
                {
                    moodRecovery *= config.MoodRecoveryPenaltyFactor;
                }

                data.Mood += moodRecovery * deltaTime;

                if (lowSatiety)
                {
                    data.Mood -= config.LowSatietyMoodPenaltyPerSecond * deltaTime;
                }
                if (lowEnergy)
                {
                    data.Mood -= config.LowEnergyMoodPenaltyPerSecond * deltaTime;
                }
            }

            ClampStateValues(data);
        }

        public static void ApplyEnvironmentalBuff(PetRuntimeData data, float moodDelta, float energyDelta)
        {
            data.Mood += moodDelta;
            data.Energy += energyDelta;
            ClampStateValues(data);
        }

        public static void ApplySatietyDelta(PetRuntimeData data, float satietyDelta)
        {
            data.Satiety += satietyDelta;
            ClampStateValues(data);
        }

        private static void ClampStateValues(PetRuntimeData data)
        {
            data.Energy = Mathf.Clamp(data.Energy, 0f, 100f);
            data.Mood = Mathf.Clamp(data.Mood, 0f, 100f);
            data.Satiety = Mathf.Clamp(data.Satiety, 0f, 100f);
        }
    }
}
