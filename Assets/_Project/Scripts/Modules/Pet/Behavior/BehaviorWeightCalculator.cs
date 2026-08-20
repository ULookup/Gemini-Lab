#nullable enable
using GeminiLab.Modules.Pet.Personality;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 行为权重四倍率（数值规则文档 §1~§6、§9）。全部为纯函数，便于 EditMode 测试。
    /// 最终公式（§25）：FinalWeight = BaseWeight × Personality × Energy × Mood × Repeat。
    /// </summary>
    public static class BehaviorWeightCalculator
    {
        /// <summary>性格倍率上下限（§2：0.5 ~ 1.5）。</summary>
        public const float PersonalityMultiplierMin = 0.5f;
        public const float PersonalityMultiplierMax = 1.5f;

        /// <summary>单维性格对倍率的影响系数（§2：×0.25）。</summary>
        public const float TraitInfluenceScale = 0.25f;

        /// <summary>精力倍率底数/幅值（§3：0.6 + 0.8 ×  proximity，范围 0.6~1.4）。</summary>
        public const float EnergyMultiplierBase = 0.6f;
        public const float EnergyMultiplierSpan = 0.8f;

        /// <summary>
        /// 性格倍率（§2）：Clamp(1 + Σ(Direction × (Trait-50)/50 × 0.25), 0.5, 1.5)。
        /// 内部性格以 -1..1 存储，与文档 0~100 量表的 (Trait-50)/50 完全等价。
        /// </summary>
        public static float PersonalityMultiplier(BehaviorConfigSO.BehaviorEntry entry, PersonalityVector personality)
        {
            float sum = 0f;
            for (int i = 0; i < entry.PersonalityTags.Length; i++)
            {
                BehaviorConfigSO.PersonalityTag tag = entry.PersonalityTags[i];
                float direction = tag.Direction >= 0 ? 1f : -1f;
                sum += direction * personality.GetTrait(tag.Trait) * TraitInfluenceScale;
            }

            return Mathf.Clamp(1f + sum, PersonalityMultiplierMin, PersonalityMultiplierMax);
        }

        /// <summary>
        /// 精力倍率（§3）：0.6 + 0.8 × (1 - |Energy - PreferredEnergy| / 100)，范围 0.6 ~ 1.4。
        /// </summary>
        public static float EnergyMultiplier(BehaviorConfigSO.BehaviorEntry entry, float energy)
        {
            float proximity = 1f - Mathf.Abs(energy - entry.PreferredEnergy) / 100f;
            return EnergyMultiplierBase + EnergyMultiplierSpan * proximity;
        }

        /// <summary>
        /// 心情倍率（§6 倍率表）：按行为类型 × 心情档位查表。
        /// </summary>
        public static float MoodMultiplier(BehaviorCategory category, float mood)
        {
            return MoodMultiplier(category, MoodBandExtensions.FromMood(mood));
        }

        public static float MoodMultiplier(BehaviorCategory category, MoodBand band)
        {
            return category switch
            {
                BehaviorCategory.Neutral => 1f,
                BehaviorCategory.Rest => band switch
                {
                    MoodBand.Low => 1.4f,
                    MoodBand.High => 0.7f,
                    _ => 1f
                },
                BehaviorCategory.Quiet => band switch
                {
                    MoodBand.Low => 1.15f,
                    MoodBand.High => 0.95f,
                    _ => 1f
                },
                BehaviorCategory.Active => band switch
                {
                    MoodBand.Low => 0.7f,
                    MoodBand.High => 1.25f,
                    _ => 1f
                },
                BehaviorCategory.Social => band switch
                {
                    MoodBand.Low => 0.6f,
                    MoodBand.High => 1.3f,
                    _ => 1f
                },
                _ => 1f
            };
        }

        /// <summary>重复倍率（§9）：冷却 0 / 上一个 0.2 / 最近三个 0.6 / 未出现 1.0。</summary>
        public static float RepeatMultiplier(
            BehaviorConfigSO.BehaviorEntry entry,
            BehaviorRuntimeState runtimeState,
            float nowSeconds)
        {
            return runtimeState.RepeatMultiplierFor(entry.BehaviorId, nowSeconds);
        }

        /// <summary>最终权重（§25）。任一倍率为 0 时结果为 0（冷却中的行为被排除）。</summary>
        public static float FinalWeight(
            BehaviorConfigSO.BehaviorEntry entry,
            PersonalityVector personality,
            float energy,
            float mood,
            BehaviorRuntimeState runtimeState,
            float nowSeconds)
        {
            return entry.BaseWeight
                   * PersonalityMultiplier(entry, personality)
                   * EnergyMultiplier(entry, energy)
                   * MoodMultiplier(entry.Category, mood)
                   * RepeatMultiplier(entry, runtimeState, nowSeconds);
        }
    }
}
