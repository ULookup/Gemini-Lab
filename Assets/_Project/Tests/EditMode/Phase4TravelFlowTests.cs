#nullable enable
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Travel;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class Phase4TravelFlowTests
    {
        [Test]
        public void RewardApply_IsClampedToValidRange()
        {
            PetRuntimeData runtimeData = new()
            {
                Mood = 98f,
                Energy = 3f,
                Satiety = 4f
            };

            TravelRewardApplier.ApplyTravelReward(runtimeData, qualityLevel: 2);

            Assert.AreEqual(100f, runtimeData.Mood);
            Assert.AreEqual(0f, runtimeData.Energy);
            Assert.AreEqual(0f, runtimeData.Satiety);
        }
    }
}
