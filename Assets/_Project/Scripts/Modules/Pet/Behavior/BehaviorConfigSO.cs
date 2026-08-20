#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 每只宠物一张行为表（数值规则文档 §7/§22 BehaviorConfig）。
    /// FinalWeight = BaseWeight × Personality × Energy × Mood × Repeat（§1/§25）。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Pet/Behavior Config", fileName = "BehaviorConfig")]
    public sealed class BehaviorConfigSO : ScriptableObject
    {
        [Serializable]
        public sealed class PersonalityTag
        {
            [Tooltip("作用的性格维度")]
            public PetTrait Trait = PetTrait.Kindness;

            [Tooltip("+1 = 该维度越高越倾向此行为；-1 = 该维度越高越不倾向（如 -害羞 = 越害羞越少门前驻足）")]
            public int Direction = 1;
        }

        [Serializable]
        public sealed class BehaviorEntry
        {
            [Tooltip("行为 Id，见 PetBehaviorIds")]
            public string BehaviorId = string.Empty;

            [Tooltip("显示名（调试/UI）")]
            public string Label = string.Empty;

            [Tooltip("基础权重（§1：原基础概率 ×100），不是最终百分比")]
            [Min(0f)] public float BaseWeight = 10f;

            [Tooltip("行为类型，决定心情倍率（§6）")]
            public BehaviorCategory Category = BehaviorCategory.Neutral;

            [Tooltip("性格标签列表（§2）：逐条按 Direction × Trait × 0.25 累加进性格倍率")]
            public PersonalityTag[] PersonalityTags = Array.Empty<PersonalityTag>();

            [Tooltip("偏好精力（§3）：精力越接近该值，精力倍率越高（0.6~1.4）")]
            [Range(0f, 100f)] public float PreferredEnergy = 50f;

            [Header("完成结算（§12）")]
            public float EnergyDelta;
            public float MoodDelta;

            [Tooltip("冷却秒数，从行为结束时开始计算（§10）；0 = 无冷却")]
            [Min(0f)] public float CooldownSeconds;

            [Tooltip("行为标签（§4）：Active = 活跃行为，低精力时被硬规则过滤")]
            public BehaviorTag Tags = BehaviorTag.None;

            [Header("场景绑定")]
            [Tooltip("对应宠物交互绑定（PetPlayerFurnitureInteractionController）的 InteractionType；" +
                     "Unknown = 原地行为（待机），无需移动")]
            public FurnitureInteractionType BindingInteractionType = FurnitureInteractionType.Unknown;

            [Tooltip(">0 时覆盖绑定时长（例如睡觉需要比绑定动画更久的持续时间）；0 = 使用绑定时长")]
            [Min(0f)] public float OverrideDurationSeconds;

            public bool RequiresMovement => BindingInteractionType != FurnitureInteractionType.Unknown;
        }

        [Header("行为表")]
        public List<BehaviorEntry> Behaviors = new();

        [Header("待机（原地行为）时长")]
        [Min(0.1f)] public float IdleMinSeconds = 3f;
        [Min(0.1f)] public float IdleMaxSeconds = 8f;

        [Header("节奏")]
        [Tooltip("一个行为完成后、抽取下一个行为前的过渡停顿秒数")]
        [Min(0f)] public float TransitionPauseSeconds = 2f;

        public BehaviorEntry? Find(string behaviorId)
        {
            for (int i = 0; i < Behaviors.Count; i++)
            {
                if (string.Equals(Behaviors[i].BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    return Behaviors[i];
                }
            }

            return null;
        }

        public float RollIdleDurationSeconds()
        {
            float min = Mathf.Min(IdleMinSeconds, IdleMaxSeconds);
            float max = Mathf.Max(IdleMinSeconds, IdleMaxSeconds);
            return UnityEngine.Random.Range(min, max);
        }
    }
}
