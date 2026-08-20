#nullable enable
using GeminiLab.Modules.Pet.Behavior;
using GeminiLab.Modules.Pet.Personality;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>行为选择器测试（数值规则文档 §4 硬规则 / §9 重复 / §25 加权随机）。</summary>
    public sealed class BehaviorSelectorTests
    {
        private static BehaviorConfigSO.BehaviorEntry Entry(
            string id,
            float baseWeight = 10f,
            BehaviorCategory category = BehaviorCategory.Neutral,
            float preferredEnergy = 50f,
            BehaviorTag tags = BehaviorTag.None)
        {
            return new BehaviorConfigSO.BehaviorEntry
            {
                BehaviorId = id,
                BaseWeight = baseWeight,
                Category = category,
                PreferredEnergy = preferredEnergy,
                Tags = tags
            };
        }

        private static BehaviorConfigSO Config(params BehaviorConfigSO.BehaviorEntry[] entries)
        {
            var config = ScriptableObject.CreateInstance<BehaviorConfigSO>();
            config.Behaviors.AddRange(entries);
            return config;
        }

        private static BehaviorSelectorInput Input(float energy, float mood = 50f, float now = 0f) =>
            new(energy, mood, default(PersonalityVector), now,
                forcedSleepEnergyThreshold: 5f,
                sleepExcludedAboveEnergy: 70f,
                activeBehaviorMinEnergy: 10f);

        [Test]
        public void Pick_ForcedSleep_WhenEnergyAtOrBelowThreshold()
        {
            var config = Config(Entry(PetBehaviorIds.Idle), Entry(PetBehaviorIds.Sleep));

            var result = BehaviorSelector.Pick(config, Input(energy: 5f), new BehaviorRuntimeState(), () => 0.99f);

            Assert.NotNull(result);
            Assert.AreEqual(PetBehaviorIds.Sleep, result!.Value.Entry.BehaviorId);
            Assert.IsTrue(result.Value.IsForced);
        }

        [Test]
        public void Pick_ForcedSleep_BypassesCooldown()
        {
            // 睡觉在冷却中也不能拦住强制睡觉，否则低精力宠物会死锁到精力归零。
            var config = Config(Entry(PetBehaviorIds.Idle), Entry(PetBehaviorIds.Sleep));
            var state = new BehaviorRuntimeState();
            state.RecordCompletion(PetBehaviorIds.Sleep, nowSeconds: 0f, cooldownSeconds: 300f);

            var result = BehaviorSelector.Pick(config, Input(energy: 3f, now: 10f), state, () => 0.99f);

            Assert.NotNull(result);
            Assert.AreEqual(PetBehaviorIds.Sleep, result!.Value.Entry.BehaviorId);
            Assert.IsTrue(result.Value.IsForced);
        }

        [Test]
        public void Pick_ExcludesSleep_WhenEnergyAbove70()
        {
            var config = Config(Entry(PetBehaviorIds.Idle), Entry(PetBehaviorIds.Sleep));

            var result = BehaviorSelector.Pick(config, Input(energy: 80f), new BehaviorRuntimeState(), () => 0.999f);

            Assert.NotNull(result);
            Assert.AreEqual(PetBehaviorIds.Idle, result!.Value.Entry.BehaviorId);
        }

        [Test]
        public void Pick_ExcludesActiveTagged_WhenEnergyBelow10()
        {
            var config = Config(
                Entry("quiet", category: BehaviorCategory.Quiet),
                Entry("sport", category: BehaviorCategory.Active, tags: BehaviorTag.Active));

            // 精力 8：高于强制睡觉线 5，但低于活跃行为线 10 → 活跃行为被硬规则过滤。
            var result = BehaviorSelector.Pick(config, Input(energy: 8f), new BehaviorRuntimeState(), () => 0.999f);

            Assert.NotNull(result);
            Assert.AreEqual("quiet", result!.Value.Entry.BehaviorId);
        }

        [Test]
        public void Pick_ZeroRollNeverSelectsZeroWeightEntry()
        {
            // random01()=0 时 roll=0，冷却中的 0 权重候选也不能被命中。
            var config = Config(Entry("a"), Entry("b"));
            var state = new BehaviorRuntimeState();
            state.RecordCompletion("a", nowSeconds: 0f, cooldownSeconds: 100f);

            var result = BehaviorSelector.Pick(config, Input(energy: 50f, now: 10f), state, () => 0f);

            Assert.NotNull(result);
            Assert.AreEqual("b", result!.Value.Entry.BehaviorId);
        }

        [Test]
        public void Pick_PrefersNonRepeatedBehavior()
        {
            // 两个完全相同的行为，a 是上一个完成的（重复倍率 0.2）→ 应选 b。
            var config = Config(Entry("a"), Entry("b"));
            var state = new BehaviorRuntimeState();
            state.RecordCompletion("a", nowSeconds: 0f, cooldownSeconds: 0f);

            var result = BehaviorSelector.Pick(config, Input(energy: 50f, now: 1f), state, () => 0.5f);

            Assert.NotNull(result);
            Assert.AreEqual("b", result!.Value.Entry.BehaviorId);
        }

        [Test]
        public void Pick_WeightedDistribution_MatchesWeights()
        {
            var config = Config(Entry("heavy", baseWeight: 30f), Entry("light", baseWeight: 10f));
            var rng = new System.Random(12345);
            var state = new BehaviorRuntimeState();

            int heavy = 0, light = 0;
            const int trials = 4000;
            for (int i = 0; i < trials; i++)
            {
                var result = BehaviorSelector.Pick(
                    config, Input(energy: 50f), state, () => (float)rng.NextDouble());
                Assert.NotNull(result);
                if (result!.Value.Entry.BehaviorId == "heavy") heavy++;
                else light++;
            }

            // 期望 3:1 → 3000/1000；容差 ±10%。
            Assert.AreEqual(3000, heavy, 300);
            Assert.AreEqual(1000, light, 300);
        }

        [Test]
        public void Pick_FallbacksToIdle_WhenAllCandidatesOnCooldown()
        {
            var config = Config(Entry(PetBehaviorIds.Idle), Entry("a"));
            var state = new BehaviorRuntimeState();
            state.RecordCompletion(PetBehaviorIds.Idle, nowSeconds: 0f, cooldownSeconds: 100f);
            state.RecordCompletion("a", nowSeconds: 0f, cooldownSeconds: 100f);

            var result = BehaviorSelector.Pick(config, Input(energy: 50f, now: 10f), state, () => 0.5f);

            Assert.NotNull(result);
            Assert.AreEqual(PetBehaviorIds.Idle, result!.Value.Entry.BehaviorId);
            Assert.IsFalse(result.Value.IsForced);
        }

        [Test]
        public void Pick_ReturnsNull_WhenConfigHasNoBehaviors()
        {
            var config = Config();

            var result = BehaviorSelector.Pick(config, Input(energy: 50f), new BehaviorRuntimeState(), () => 0.5f);

            Assert.IsNull(result);
        }
    }
}
