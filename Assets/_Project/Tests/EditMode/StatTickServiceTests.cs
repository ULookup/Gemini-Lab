#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>
    /// 数值规则文档口径：
    /// - §11 清醒每现实 3 分钟 Energy -1；睡觉期间不自然衰减
    /// - §13 心情每 5 分钟向 50 回归 1 点（不越过目标）
    /// - 饱食保留既有缓慢衰减
    /// </summary>
    public sealed class StatTickServiceTests
    {
        private static PetContext CreateAwakeContext(PetRuntimeData data, PetStateValueSO config)
        {
            var context = new PetContext(data, config);
            context.EnterState(IdleState.StateName);
            return context;
        }

        [Test]
        public void Tick_Awake_DrainsEnergyAtDocRate()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.AwakeEnergyDrainMinutesPerPoint = 3f; // §11：每 3 分钟 -1 → 180 秒 -1

            PetRuntimeData data = new() { Energy = 100f, Mood = 50f };
            PetContext context = CreateAwakeContext(data, config);

            new StatTickService().Tick(context, 180f);

            Assert.AreEqual(99f, data.Energy, 0.01f);
        }

        [Test]
        public void Tick_Sleeping_DoesNotDrainEnergy()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();

            PetRuntimeData data = new() { Energy = 40f, Mood = 50f };
            var context = new PetContext(data, config);
            context.EnterState(SleepingState.StateName);

            new StatTickService().Tick(context, 600f);

            Assert.AreEqual(40f, data.Energy, 0.01f);
        }

        [Test]
        public void Tick_MoodNeutralReturn_StepsOnePointPerInterval()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.MoodNeutralReturnIntervalSeconds = 300f;

            PetRuntimeData below = new() { Energy = 100f, Mood = 20f };
            new StatTickService().Tick(CreateAwakeContext(below, config), 300f);
            Assert.AreEqual(21f, below.Mood, 0.01f);

            PetRuntimeData above = new() { Energy = 100f, Mood = 80f };
            new StatTickService().Tick(CreateAwakeContext(above, config), 300f);
            Assert.AreEqual(79f, above.Mood, 0.01f);
        }

        [Test]
        public void Tick_MoodNeutralReturn_DoesNotCrossTarget()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.MoodNeutralReturnIntervalSeconds = 300f;
            config.MoodNeutralTarget = 50f;

            PetRuntimeData below = new() { Energy = 100f, Mood = 49.5f };
            new StatTickService().Tick(CreateAwakeContext(below, config), 300f);
            Assert.AreEqual(50f, below.Mood, 0.01f);

            PetRuntimeData above = new() { Energy = 100f, Mood = 50.5f };
            new StatTickService().Tick(CreateAwakeContext(above, config), 300f);
            Assert.AreEqual(50f, above.Mood, 0.01f);
        }

        [Test]
        public void Tick_MoodNeutralReturn_AccumulatesMultipleIntervals()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.MoodNeutralReturnIntervalSeconds = 300f;

            PetRuntimeData data = new() { Energy = 100f, Mood = 20f };
            new StatTickService().Tick(CreateAwakeContext(data, config), 900f);

            Assert.AreEqual(23f, data.Mood, 0.01f);
        }

        [Test]
        public void Tick_Satiety_KeepsSlowDecay()
        {
            PetStateValueSO config = ScriptableObject.CreateInstance<PetStateValueSO>();
            config.SatietyDecayPerSecond = 0.005f;

            PetRuntimeData data = new() { Energy = 100f, Mood = 50f, Satiety = 50f };
            new StatTickService().Tick(CreateAwakeContext(data, config), 100f);

            Assert.AreEqual(49.5f, data.Satiety, 0.01f);
        }

        [Test]
        public void ApplyEnvironmentalBuff_ClampsAndAppliesDelta()
        {
            PetRuntimeData data = new()
            {
                Mood = 97f,
                Energy = 95f,
                Satiety = 50f
            };

            StatTickService.ApplyEnvironmentalBuff(data, moodDelta: 10f, energyDelta: 12f);

            Assert.AreEqual(100f, data.Mood, 0.01f);
            Assert.AreEqual(100f, data.Energy, 0.01f);
            Assert.AreEqual(50f, data.Satiety, 0.01f);
        }
    }
}
