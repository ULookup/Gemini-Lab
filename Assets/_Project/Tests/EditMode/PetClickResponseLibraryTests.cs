#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetClickResponseLibraryTests
    {
        [Test]
        public void CreateDefaultResponses_ReturnsTenNonEmptyResponses()
        {
            string[] responses = PetClickResponseLibrary.CreateDefaultResponses();

            Assert.AreEqual(10, responses.Length);
            foreach (string response in responses)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(response));
            }
        }
    }
}
