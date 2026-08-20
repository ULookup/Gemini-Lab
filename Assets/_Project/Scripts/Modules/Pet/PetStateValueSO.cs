#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Configurable pet stat thresholds and defaults.
    /// 数值口径见 docs/数值规则_策划补齐版.md：
    /// - 精力自然消耗 §11；心情回归 §13；精力硬规则 §4；移动能耗 §8。
    /// 旧版 FSM 阈值（Sleep*/BedSeek*/LeisureSeek*）仅保留给非公寓场景（WorldMap）的
    /// FSM 寻路分支使用，公寓桌宠的行为由 BehaviorConfigSO 权重系统驱动。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Pet/PetStateValueConfig", fileName = "PetStateValueConfig")]
    public sealed class PetStateValueSO : ScriptableObject
    {
        [Header("Initial（§23）")]
        [Range(0f, 100f)] public float InitialMood = 50f;
        [Range(0f, 100f)] public float InitialEnergy = 100f;
        [Range(0f, 100f)] public float InitialSatiety = 75f;

        [Header("Energy Drain（§11）")]
        [Tooltip("清醒状态下每现实多少分钟 Energy -1（文档推荐 3 分钟；睡觉期间不自然衰减）")]
        [Min(0.01f)] public float AwakeEnergyDrainMinutesPerPoint = 3f;

        /// <summary>折算成每秒消耗，供 Tick 使用。</summary>
        public float AwakeEnergyDrainPerRealMinute => 1f / AwakeEnergyDrainMinutesPerPoint;

        [Header("Mood Neutral Return（§13）")]
        [Tooltip("每隔多少现实秒心情向中性值回归 1 点（文档：5 分钟）")]
        [Min(1f)] public float MoodNeutralReturnIntervalSeconds = 300f;
        [Tooltip("心情回归的中性目标值（文档：50）")]
        [Range(0f, 100f)] public float MoodNeutralTarget = 50f;

        [Header("Satiety（文档未定义，保留既有缓慢衰减）")]
        [Min(0f)] public float SatietyDecayPerSecond = 0.005f;

        [Header("Energy Hard Rules（§4）")]
        [Tooltip("精力 ≤ 该值时强制进入睡觉（绕过权重与冷却）")]
        [Range(0f, 100f)] public float ForcedSleepEnergyThreshold = 5f;
        [Tooltip("精力 > 该值时睡觉不进入候选池")]
        [Range(0f, 100f)] public float SleepExcludedAboveEnergy = 70f;
        [Tooltip("精力 < 该值时禁止活跃行为（BehaviorTag.Active）与主动交流")]
        [Range(0f, 100f)] public float ActiveBehaviorMinEnergy = 10f;

        [Header("Movement Cost（§8）")]
        [Tooltip("单次普通移动的精力消耗（移动距离超过阈值时结算一次，不逐帧扣）")]
        [Min(0f)] public float MoveEnergyCost = 1f;
        [Tooltip("移动距离超过该值（世界单位）才结算移动能耗；短距离白走")]
        [Min(0f)] public float MoveEnergyCostMinDistance = 1.5f;

        [Header("Command Penalty")]
        [Range(0f, 100f)] public float ForceWakeMoodPenalty = 30f;

        [Header("Work")]
        [Min(1f)] public float WorkStateTimeoutSeconds = 30f;

        [Header("Legacy FSM Thresholds（仅 WorldMap FSM 寻路分支使用）")]
        [Range(0f, 100f)] public float SleepEnterEnergyThreshold = 25f;
        [Range(0f, 100f)] public float SleepExitEnergyThreshold = 70f;
        [Range(0f, 100f)] public float DaytimeBedSeekEnergyThreshold = 15f;
        [Range(0f, 100f)] public float NighttimeBedSeekEnergyThreshold = 40f;
        [Range(0f, 100f)] public float LeisureSeekMoodThreshold = 85f;
        [Range(0, 23)] public int NightHourStart = 22;
        [Range(0, 23)] public int NightHourEnd = 6;
    }
}
