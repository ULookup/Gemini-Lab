#nullable enable
using GeminiLab.Modules.Tarot;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class TarotSummaryResultTests
    {
        [Test]
        public void FromJson_ValidJson_ParsesCorrectly()
        {
            string json = @"{
                ""fortuneLevel"": 5,
                ""luckyHint"": {
                    ""color"": ""金色"",
                    ""number"": ""8"",
                    ""time"": ""黄昏"",
                    ""action"": ""主动出击""
                },
                ""advice"": ""今日宜大胆行动。""
            }";

            var result = TarotSummaryResult.FromJson(json);

            Assert.AreEqual(5, result.fortuneLevel);
            Assert.AreEqual("金色", result.luckyHint.color);
            Assert.AreEqual("8", result.luckyHint.number);
            Assert.AreEqual("黄昏", result.luckyHint.time);
            Assert.AreEqual("主动出击", result.luckyHint.action);
            Assert.AreEqual("今日宜大胆行动。", result.advice);
        }

        [Test]
        public void FromJson_ClampsFortuneLevel()
        {
            string json = @"{""fortuneLevel"": 99, ""luckyHint"": {}, ""advice"": ""x""}";
            var result = TarotSummaryResult.FromJson(json);
            Assert.AreEqual(5, result.fortuneLevel);
        }

        [Test]
        public void FromJson_InvalidJson_ReturnsDefault()
        {
            var result = TarotSummaryResult.FromJson("not json");
            Assert.AreEqual(3, result.fortuneLevel);
            Assert.IsNotNull(result.luckyHint);
            Assert.IsNotNull(result.advice);
        }

        [Test]
        public void Default_ReturnsSaneValues()
        {
            var result = TarotSummaryResult.Default();
            Assert.AreEqual(3, result.fortuneLevel);
            Assert.IsNotNull(result.luckyHint.color);
            Assert.IsNotNull(result.advice);
        }
    }
}
