#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Pet.Personality;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>行为选择输入快照（取自 PetRuntimeData + PetStateValueSO 阈值）。</summary>
    public readonly struct BehaviorSelectorInput
    {
        public BehaviorSelectorInput(
            float energy,
            float mood,
            PersonalityVector personality,
            float nowSeconds,
            float forcedSleepEnergyThreshold,
            float sleepExcludedAboveEnergy,
            float activeBehaviorMinEnergy)
        {
            Energy = energy;
            Mood = mood;
            Personality = personality;
            NowSeconds = nowSeconds;
            ForcedSleepEnergyThreshold = forcedSleepEnergyThreshold;
            SleepExcludedAboveEnergy = sleepExcludedAboveEnergy;
            ActiveBehaviorMinEnergy = activeBehaviorMinEnergy;
        }

        public float Energy { get; }
        public float Mood { get; }
        public PersonalityVector Personality { get; }
        public float NowSeconds { get; }

        /// <summary>精力 ≤ 该值时强制睡觉（§4 "0~5 强制进入睡觉"）。</summary>
        public float ForcedSleepEnergyThreshold { get; }

        /// <summary>精力 &gt; 该值时睡觉不进入候选池（§4 "Energy &gt; 70 睡觉不可进入候选池"）。</summary>
        public float SleepExcludedAboveEnergy { get; }

        /// <summary>精力 &lt; 该值时活跃行为（BehaviorTag.Active）被禁止（§4 "6~9 禁止活跃行为"）。</summary>
        public float ActiveBehaviorMinEnergy { get; }
    }

    public readonly struct BehaviorPickResult
    {
        public BehaviorPickResult(BehaviorConfigSO.BehaviorEntry entry, float finalWeight, bool isForced)
        {
            Entry = entry;
            FinalWeight = finalWeight;
            IsForced = isForced;
        }

        public BehaviorConfigSO.BehaviorEntry Entry { get; }
        public float FinalWeight { get; }

        /// <summary>强制行为（如低精力强制睡觉）：绕过权重与冷却。</summary>
        public bool IsForced { get; }
    }

    /// <summary>
    /// 行为选择器（数值规则文档 §25 执行流程）：
    /// 候选生成 → 精力硬规则过滤 → 冷却过滤（Repeat=0）→ 四倍率 → 归一化加权随机。
    /// 纯逻辑、无 Unity 依赖（随机源注入），可完整 EditMode 测试。
    /// </summary>
    public static class BehaviorSelector
    {
        /// <summary>
        /// 抽取下一个行为。random01 返回 [0,1) 均匀随机数。
        /// 返回 null 表示配置里没有任何可执行行为（调用方应保持待机）。
        /// </summary>
        public static BehaviorPickResult? Pick(
            BehaviorConfigSO config,
            BehaviorSelectorInput input,
            BehaviorRuntimeState runtimeState,
            Func<float> random01)
        {
            // 1. 精力硬规则：0~5 强制睡觉（§4）。绕过冷却与权重——避免
            // "睡觉在冷却中 → 低精力宠物无事可做 → 精力归零" 的死锁。
            if (input.Energy <= input.ForcedSleepEnergyThreshold)
            {
                BehaviorConfigSO.BehaviorEntry? sleep = config.Find(PetBehaviorIds.Sleep);
                if (sleep != null)
                {
                    return new BehaviorPickResult(sleep, float.PositiveInfinity, isForced: true);
                }
            }

            float totalWeight = 0f;
            var weights = new List<float>(config.Behaviors.Count);
            for (int i = 0; i < config.Behaviors.Count; i++)
            {
                BehaviorConfigSO.BehaviorEntry entry = config.Behaviors[i];
                float weight = 0f;
                if (PassesEnergyHardRules(entry, input))
                {
                    weight = BehaviorWeightCalculator.FinalWeight(
                        entry, input.Personality, input.Energy, input.Mood, runtimeState, input.NowSeconds);
                }

                weights.Add(weight);
                totalWeight += weight;
            }

            // 全部候选权重为 0（例如全部在冷却中）：兜底回待机，保证宠物始终有事可做。
            if (totalWeight <= 0f)
            {
                BehaviorConfigSO.BehaviorEntry? idle = config.Find(PetBehaviorIds.Idle);
                return idle != null ? new BehaviorPickResult(idle, 0f, isForced: false) : null;
            }

            // 2. 归一化加权随机。跳过 0 权重候选（冷却中/被重复倍率清零），
            // 否则 random01()=0 时 roll=0 会错误命中第一个 0 权重行为。
            float roll = random01() * totalWeight;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }

                roll -= weights[i];
                if (roll <= 0f)
                {
                    return new BehaviorPickResult(config.Behaviors[i], weights[i], isForced: false);
                }
            }

            int last = weights.Count - 1;
            return new BehaviorPickResult(config.Behaviors[last], weights[last], isForced: false);
        }

        /// <summary>
        /// 精力硬规则（§4）：
        /// - Energy &gt; 70：睡觉不可进入候选池；
        /// - Energy 10~29：正常计算（"不能主动发起高强度特殊交互" 当前版本无此类行为，预留）；
        /// - Energy 6~9（&lt; ActiveBehaviorMinEnergy）：禁止活跃行为（BehaviorTag.Active）；
        /// - Energy 0~5：由强制睡觉分支处理，不走这里。
        /// </summary>
        private static bool PassesEnergyHardRules(BehaviorConfigSO.BehaviorEntry entry, BehaviorSelectorInput input)
        {
            if (entry.BehaviorId == PetBehaviorIds.Sleep &&
                input.Energy > input.SleepExcludedAboveEnergy)
            {
                return false;
            }

            if ((entry.Tags & BehaviorTag.Active) != 0 &&
                input.Energy < input.ActiveBehaviorMinEnergy)
            {
                return false;
            }

            return true;
        }
    }
}
