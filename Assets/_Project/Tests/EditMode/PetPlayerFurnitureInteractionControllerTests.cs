#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetPlayerFurnitureInteractionControllerTests
    {
        [TestCase(PetSelfInteractionVariant.BesideDoor, "beside door")]
        [TestCase(PetSelfInteractionVariant.Flower, "flower")]
        [TestCase(PetSelfInteractionVariant.PlayingMusic, "playing music")]
        [TestCase(PetSelfInteractionVariant.Read, "read")]
        [TestCase(PetSelfInteractionVariant.Sleep, "sleep")]
        public void ToVariantKey_ReturnsExpectedFolderName(PetSelfInteractionVariant variant, string expectedKey)
        {
            Assert.AreEqual(expectedKey, variant.ToVariantKey());
        }
    }
}
