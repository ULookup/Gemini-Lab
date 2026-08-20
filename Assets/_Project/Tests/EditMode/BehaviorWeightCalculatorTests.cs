#nullable enable
using GeminiLab.Modules.Pet.Behavior;
using GeminiLab.Modules.Pet.Personality;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>四倍率纯函数测试（数值规则文档 §2/§3/§6/§9/§25）。</summary>
    public sealed class BehaviorWeightCalculatorTests
    {
        private static BehaviorConfigSO.BehaviorEntry Entry(
            float baseWeight = 10f,
            BehaviorCategory category = BehaviorCategory.Neutral,
            float preferredEnergy = 50f,
            params BehaviorConfigSO.PersonalityTag[] tags)
        {
            return new BehaviorConfigSO.BehaviorEntry
            {
                BehaviorId = "test",
                BaseWeight = baseWeight,
                Category = category,
                PreferredEnergy = preferredEnergy,
                PersonalityTags = tags
            };
        }

        private static BehaviorConfigSO.PersonalityTag Tag(PetTrait trait, int direction) =>
            new() { Trait = trait, Direction = direction };

        [Test]
        public void PersonalityMultiplier_NoTags_ReturnsOne()
        {
            float multiplier = BehaviorWeightCalculator.PersonalityMultiplier(Entry(), default);
            Assert.AreEqual(1f, multiplier, 0.001f);
        }

        [Test]
        public void PersonalityMultiplier_PositiveTag_RaisesWithTrait()
        {
            // §2：1 + 0.8 × 0.25 = 1.2
            var vector = new PersonalityVector { Kindness = 0.8f };
            float multiplier = BehaviorWeightCalculator.PersonalityMultiplier(
                Entry(tags: new[] { Tag(PetTrait.Kindness, 1) }), vector);
            Assert.AreEqual(1.2f, multiplier, 0.001f);
        }

        [Test]
        public void PersonalityMultiplier_NegativeDirectionTag_LowersWithTrait()
        {
            // “-害羞”标签：害羞越高倍率越低。1 - 0.8 × 0.25 = 0.8
            var vector = new PersonalityVector { Shyness = 0.8f };
            float multiplier = BehaviorWeightCalculator.PersonalityMultiplier(
                Entry(tags: new[] { Tag(PetTrait.Shyness, -1) }), vector);
            Assert.AreEqual(0.8f, multiplier, 0.001f);
        }

        [Test]
        public void PersonalityMultiplier_ClampedToDocBounds()
        {
            var vector = new PersonalityVector { Kindness = 1f, Calmness = 1f, Integrity = 1f, Curiosity = 1f };
            float high = BehaviorWeightCalculator.PersonalityMultiplier(
                Entry(tags: new[]
                {
                    Tag(PetTrait.Kindness, 1), Tag(PetTrait.Calmness, 1),
                    Tag(PetTrait.Integrity, 1), Tag(PetTrait.Curiosity, 1)
                }), vector);
            Assert.AreEqual(BehaviorWeightCalculator.PersonalityMultiplierMax, high, 0.001f);

            var negative = new PersonalityVector { Kindness = -1f, Calmness = -1f, Integrity = -1f, Curiosity = -1f };
            float low = BehaviorWeightCalculator.PersonalityMultiplier(
                Entry(tags: new[]
                {
                    Tag(PetTrait.Kindness, 1), Tag(PetTrait.Calmness, 1),
                    Tag(PetTrait.Integrity, 1), Tag(PetTrait.Curiosity, 1)
                }), negative);
            Assert.AreEqual(BehaviorWeightCalculator.PersonalityMultiplierMin, low, 0.001f);
        }

        [Test]
        public void EnergyMultiplier_AtPreferred_IsMax()
        {
            // §3：0.6 + 0.8 × 1 = 1.4
            float multiplier = BehaviorWeightCalculator.EnergyMultiplier(Entry(preferredEnergy: 60f), 60f);
            Assert.AreEqual(1.4f, multiplier, 0.001f);
        }

        [Test]
        public void EnergyMultiplier_AtMaxDistance_IsMin()
        {
            // §3：|100-0|/100 = 1 → 0.6 + 0.8 × 0 = 0.6
            float multiplier = BehaviorWeightCalculator.EnergyMultiplier(Entry(preferredEnergy: 0f), 100f);
            Assert.AreEqual(0.6f, multiplier, 0.001f);
        }

        [Test]
        public void MoodMultiplier_MatchesDocTable()
        {
            // §6：Rest 低 1.4 / 高 0.7；Active 低 0.7 / 高 1.25；Social 高 1.3；Neutral 恒 1。
            Assert.AreEqual(1.4f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 10f), 0.001f);
            Assert.AreEqual(0.7f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 90f), 0.001f);
            Assert.AreEqual(0.7f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Active, 10f), 0.001f);
            Assert.AreEqual(1.25f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Active, 90f), 0.001f);
            Assert.AreEqual(1.3f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Social, 90f), 0.001f);
            Assert.AreEqual(0.6f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Social, 10f), 0.001f);
            Assert.AreEqual(1f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Neutral, 10f), 0.001f);
            Assert.AreEqual(1f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 50f), 0.001f);
        }

        [Test]
        public void MoodMultiplier_BandEdges()
        {
            // §5：<30 低 / 30~69 中 / >=70 高
            Assert.AreEqual(1.4f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 29.99f), 0.001f);
            Assert.AreEqual(1f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 30f), 0.001f);
            Assert.AreEqual(1f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 69.99f), 0.001f);
            Assert.AreEqual(0.7f, BehaviorWeightCalculator.MoodMultiplier(BehaviorCategory.Rest, 70f), 0.001f);
        }

        [Test]
        public void FinalWeight_IsProductOfFourMultipliers()
        {
            // §25：10 × 1.2（性格）× 1.4（精力）× 1.25（心情-活跃高档）× 1.0（无重复）= 21
            var entry = Entry(10f, BehaviorCategory.Active, 50f, Tag(PetTrait.Kindness, 1));
            var vector = new PersonalityVector { Kindness = 0.8f };
            var state = new BehaviorRuntimeState();

            float weight = BehaviorWeightCalculator.FinalWeight(entry, vector, 50f, 80f, state, 0f);

            Assert.AreEqual(21f, weight, 0.01f);
        }

        [Test]
        public void FinalWeight_OnCooldown_IsZero()
        {
            var entry = Entry();
            entry.BehaviorId = "cooling";
            var state = new BehaviorRuntimeState();
            state.RecordCompletion("cooling", nowSeconds: 0f, cooldownSeconds: 100f);

            float weight = BehaviorWeightCalculator.FinalWeight(entry, default, 50f, 50f, state, 10f);

            Assert.AreEqual(0f, weight, 0.001f);
        }

        [Test]
        public void RepeatMultiplier_PriorityAndValues()
        {
            // §9：冷却 0 > 上一个 0.2 > 最近三个 0.6 > 1.0
            var state = new BehaviorRuntimeState();
            var entry = Entry();
            entry.BehaviorId = "a";

            Assert.AreEqual(1f, BehaviorWeightCalculator.RepeatMultiplier(entry, state, 0f), 0.001f);

            state.RecordCompletion("a", 0f, 0f);
            Assert.AreEqual(0.2f, BehaviorWeightCalculator.RepeatMultiplier(entry, state, 0f), 0.001f);

            state.RecordCompletion("b", 1f, 0f);
            Assert.AreEqual(0.6f, BehaviorWeightCalculator.RepeatMultiplier(entry, state, 1f), 0.001f);

            state.RecordCompletion("a", 2f, 100f);
            Assert.AreEqual(0f, BehaviorWeightCalculator.RepeatMultiplier(entry, state, 3f), 0.001f);

            // 冷却结束后仍受“上一个/最近”规则影响（这里是上一个 → 0.2）。
            Assert.AreEqual(0.2f, BehaviorWeightCalculator.RepeatMultiplier(entry, state, 200f), 0.001f);
        }
    }
}
