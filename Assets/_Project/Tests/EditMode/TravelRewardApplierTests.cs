#nullable enable
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Travel;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class TravelRewardApplierTests
    {
        [Test]
        public void ApplyTravelReward_AdjustsStatsAndCount()
        {
            PetRuntimeData runtimeData = new()
            {
                Mood = 50f,
                Energy = 80f,
                Satiety = 70f,
                TravelCompletedCount = 0
            };

            TravelRewardApplier.ApplyTravelReward(runtimeData, qualityLevel: 2);

            Assert.AreEqual(66f, runtimeData.Mood);
            Assert.AreEqual(70f, runtimeData.Energy);
            Assert.AreEqual(58f, runtimeData.Satiety);
            Assert.AreEqual(1, runtimeData.TravelCompletedCount);
        }
    }
}
