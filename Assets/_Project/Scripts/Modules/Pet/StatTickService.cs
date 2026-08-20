#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Centralized stat ticking to avoid scattered direct value writes.
    /// 数值规则文档口径：
    /// - §11 精力自然变化：清醒每现实 <see cref="PetStateValueSO.AwakeEnergyDrainPerRealMinute"/>
    ///   分钟 Energy -1；睡觉期间停止自然衰减（睡眠恢复由睡觉行为完成时一次性结算）。
    /// - §13 心情回归中性：每现实 <see cref="PetStateValueSO.MoodNeutralReturnIntervalSeconds"/>
    ///   秒向 <see cref="PetStateValueSO.MoodNeutralTarget"/> 回归 1 点。
    /// - 饱食：文档未定义，保留既有的缓慢自然衰减（喂食玩法预留 ApplySatietyDelta）。
    /// 行为带来的 Energy/Mood 增减不在此处：由行为完成结算（§12）与交互 buff 走
    /// <see cref="ApplyEnvironmentalBuff"/>。
    /// </summary>
    public sealed class StatTickService
    {
        public void Tick(PetContext context, float deltaTime)
        {
            PetRuntimeData data = context.RuntimeData;
            PetStateValueSO config = context.Config;

            // 饱食缓慢自然衰减（文档未提及，保留既有系统）。
            data.Satiety -= config.SatietyDecayPerSecond * deltaTime;

            // §11：清醒时精力自然消耗；睡觉期间不自然衰减。
            if (!context.IsSleeping)
            {
                data.Energy -= config.AwakeEnergyDrainPerRealMinute / 60f * deltaTime;
            }

            // §13：心情定时向中性值回归（每间隔 ±1，不越过目标值）。
            if (config.MoodNeutralReturnIntervalSeconds > 0f)
            {
                data.NeutralReturnTimerSeconds += deltaTime;
                while (data.NeutralReturnTimerSeconds >= config.MoodNeutralReturnIntervalSeconds)
                {
                    data.NeutralReturnTimerSeconds -= config.MoodNeutralReturnIntervalSeconds;
                    if (data.Mood < config.MoodNeutralTarget)
                    {
                        data.Mood = Mathf.Min(data.Mood + 1f, config.MoodNeutralTarget);
                    }
                    else if (data.Mood > config.MoodNeutralTarget)
                    {
                        data.Mood = Mathf.Max(data.Mood - 1f, config.MoodNeutralTarget);
                    }
                }
            }

            ClampStateValues(data);
        }

        /// <summary>一次性结算交互/行为带来的 Mood/Energy 增减（§12 行为结算也走这里）。</summary>
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
