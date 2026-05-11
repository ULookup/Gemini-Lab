#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Configurable phase-1 pet stat thresholds and defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Pet/PetStateValueConfig", fileName = "PetStateValueConfig")]
    public sealed class PetStateValueSO : ScriptableObject
    {
        [Header("Initial")]
        [Range(0f, 100f)] public float InitialMood = 60f;
        [Range(0f, 100f)] public float InitialEnergy = 100f;
        [Range(0f, 100f)] public float InitialSatiety = 75f;

        [Header("State Thresholds")]
        [Range(0f, 100f)] public float SleepEnterEnergyThreshold = 25f;
        [Range(0f, 100f)] public float SleepExitEnergyThreshold = 70f;

        [Header("Autonomous Targeting")]
        [Range(0f, 100f)] public float DaytimeBedSeekEnergyThreshold = 15f;
        [Range(0f, 100f)] public float NighttimeBedSeekEnergyThreshold = 40f;
        [Range(0f, 100f)] public float LeisureSeekMoodThreshold = 85f;
        [Range(0, 23)] public int NightHourStart = 22;
        [Range(0, 23)] public int NightHourEnd = 6;

        [Header("Tick Speeds (Per Second)")]
        [Min(0f)] public float AwakeEnergyDecayPerSecond = 8f;
        [Min(0f)] public float SleepingEnergyRecoveryPerSecond = 15f;
        [Min(0f)] public float MoodRecoveryPerSecond = 4f;
        [Tooltip("饱食度每秒自然下降（被饥饿曲线驱动）")]
        [Min(0f)] public float SatietyDecayPerSecond = 1.2f;
        [Tooltip("Hunger（饱食 <=30）时心情每秒扣除")]
        [Min(0f)] public float LowSatietyMoodPenaltyPerSecond = 0.6f;
        [Tooltip("Exhausted（精力 <=20）时心情每秒扣除")]
        [Min(0f)] public float LowEnergyMoodPenaltyPerSecond = 0.8f;
        [Tooltip("饱食或精力不足时 MoodRecoveryPerSecond 的倍率；0 = 完全停止恢复")]
        [Range(0f, 1f)] public float MoodRecoveryPenaltyFactor = 0.2f;

        [Header("Command Penalty")]
        [Range(0f, 100f)] public float ForceWakeMoodPenalty = 30f;

        [Header("Work")]
        [Min(1f)] public float WorkStateTimeoutSeconds = 30f;
    }
}
