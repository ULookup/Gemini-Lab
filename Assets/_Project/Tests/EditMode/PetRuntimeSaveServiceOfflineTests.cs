#nullable enable
using System;
using GeminiLab.Modules.Pet;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    /// <summary>离线规则测试（数值规则文档 §20）+ pet_runtime 存档 v2 兼容。</summary>
    public sealed class PetRuntimeSaveServiceOfflineTests
    {
        private PetRoster _roster = null!;
        private PetRuntimeData _angel = null!;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _roster = new PetRoster();
            _angel = new PetRuntimeData
            {
                Mood = 80f,
                Energy = 40f,
                Satiety = 33f,
                RuntimeTimeSeconds = 1234f,
                TravelCompletedCount = 2,
                CurrentState = "Idle"
            };
            _roster.Register(PetId.Angel, _angel);
            _now = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        }

        private PetRuntimeSaveService CreateService() => new(_roster, utcNow: () => _now);

        private string CaptureAndAdvance(TimeSpan offline)
        {
            string json = CreateService().CaptureJson();
            _now += offline;

            // 模拟“关闭期间运行态被重置”：恢复前先改成别的值。
            _angel.Mood = 11f;
            _angel.Energy = 99f;
            _angel.Satiety = 1f;
            return json;
        }

        [Test]
        public void Offline_TwoHours_MoodRegressesByAtMost6Points()
        {
            // §20 文档示例：关闭时 Mood=80，离线 2 小时，回来后 Mood=74。
            string json = CaptureAndAdvance(TimeSpan.FromHours(2));

            Assert.IsTrue(CreateService().RestoreJson(json));

            Assert.AreEqual(74f, _angel.Mood, 0.001f);
            Assert.AreEqual(40f, _angel.Energy, 0.001f); // 离线不衰减，原样恢复
            Assert.AreEqual(33f, _angel.Satiety, 0.001f);
        }

        [Test]
        public void Offline_TenMinutes_MoodRegresses2Points()
        {
            // 每 5 分钟 1 点（与在线 §13 同节奏）。
            string json = CaptureAndAdvance(TimeSpan.FromMinutes(10));

            Assert.IsTrue(CreateService().RestoreJson(json));

            Assert.AreEqual(78f, _angel.Mood, 0.001f);
        }

        [Test]
        public void Offline_BelowNeutral_RegressesUpward()
        {
            _angel.Mood = 20f;
            string json = CaptureAndAdvance(TimeSpan.FromMinutes(15));

            Assert.IsTrue(CreateService().RestoreJson(json));

            Assert.AreEqual(23f, _angel.Mood, 0.001f);
        }

        [Test]
        public void Offline_RegressionDoesNotCrossNeutral()
        {
            _angel.Mood = 53f;
            string json = CaptureAndAdvance(TimeSpan.FromHours(5));

            Assert.IsTrue(CreateService().RestoreJson(json));

            Assert.AreEqual(50f, _angel.Mood, 0.001f);
        }

        [Test]
        public void V1Payload_RestoresWithoutOfflineRegression()
        {
            // v1 存档没有 savedAtUtcTicks：按原值恢复，不做离线回归。
            const string v1Json =
                "{\"version\":1,\"entries\":[{" +
                "\"petId\":0,\"mood\":80,\"energy\":40,\"satiety\":33," +
                "\"runtimeTime\":1234,\"travelCompletedCount\":2," +
                "\"currentState\":\"Idle\",\"lastInteractionFurnitureId\":\"bed\"," +
                "\"lastInteractionSummary\":\"睡觉\"}]}";
            _now += TimeSpan.FromHours(10);

            Assert.IsTrue(CreateService().RestoreJson(v1Json));

            Assert.AreEqual(80f, _angel.Mood, 0.001f);
            Assert.AreEqual(40f, _angel.Energy, 0.001f);
            Assert.AreEqual("bed", _angel.LastInteractionFurnitureId);
        }

        [Test]
        public void CorruptJson_FailsAndKeepsState()
        {
            var service = CreateService();

            Assert.IsFalse(service.RestoreJson("{broken"));
            Assert.IsFalse(service.RestoreJson(string.Empty));
            Assert.AreEqual(80f, _angel.Mood, 0.001f);
        }

        [Test]
        public void RoundTrip_RestoresRuntimeFields()
        {
            _angel.CurrentState = "Sleeping";
            _angel.LastInteractionFurnitureId = "harp";
            _angel.LastInteractionSummary = "弹琴 / 安静 (Mood +2, Energy -2)";

            string json = CaptureAndAdvance(TimeSpan.FromMinutes(1)); // 不足 5 分钟 → 不回归

            Assert.IsTrue(CreateService().RestoreJson(json));

            Assert.AreEqual(80f, _angel.Mood, 0.001f);
            Assert.AreEqual(1234f, _angel.RuntimeTimeSeconds, 0.001f);
            Assert.AreEqual(2, _angel.TravelCompletedCount);
            Assert.AreEqual("Sleeping", _angel.CurrentState);
            Assert.AreEqual("harp", _angel.LastInteractionFurnitureId);
            Assert.AreEqual("弹琴 / 安静 (Mood +2, Energy -2)", _angel.LastInteractionSummary);
        }
    }
}
