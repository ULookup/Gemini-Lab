#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Apple;
using GeminiLab.Modules.EmotionGarden;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class AppleResourceServiceTests
    {
        private FakeGameClock _clock = null!;
        private AppleService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeGameClock
            {
                Now = new DateTime(2026, 8, 14, 8, 0, 0, DateTimeKind.Local),
                UtcNow = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            _service = new AppleService(_clock, new EventBus());
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Reset();
        }

        [Test]
        public void NewService_StartsWithTwentyApples()
        {
            Assert.AreEqual(20, _service.Balance);
        }

        [Test]
        public void TreeGeneration_IsTimeBasedAndSurvivesSaveRestoreWithoutDuplication()
        {
            _service.EnsureTree("world_tree_1");
            _clock.Advance(TimeSpan.FromHours(12));

            Assert.AreEqual(2, _service.GetPendingCount("world_tree_1"));
            string saved = _service.CaptureJson();

            var restored = new AppleService(_clock, new EventBus());
            Assert.IsTrue(restored.RestoreJson(saved));
            Assert.AreEqual(2, restored.GetPendingCount("world_tree_1"));
            Assert.AreEqual(0, restored.ShakeTree("world_tree_1") - 2);
            Assert.AreEqual(22, restored.Balance);
            Assert.AreEqual(0, restored.ShakeTree("world_tree_1"));
        }

        [Test]
        public void TreeGeneration_IsCappedAndOnlyShakeTransfersCachedApples()
        {
            _service.EnsureTree("world_tree_3");
            _clock.Advance(TimeSpan.FromDays(10));

            Assert.AreEqual(3, _service.GetPendingCount("world_tree_3"));
            Assert.AreEqual(20, _service.Balance);
            Assert.AreEqual(3, _service.ShakeTree("world_tree_3"));
            Assert.AreEqual(23, _service.Balance);
        }

        [Test]
        public void SpendRejectsInsufficientBalanceAndDoesNotGoNegative()
        {
            Assert.IsFalse(_service.TrySpend(21));
            Assert.AreEqual(20, _service.Balance);
            Assert.IsTrue(_service.TrySpend(5));
            Assert.AreEqual(15, _service.Balance);
        }

        [Test]
        public void BloomingAFlowerRewardsOneAppleOnlyOnce()
        {
            ServiceLocator.Register<IAppleService>(_service);
            var eventBus = new EventBus();
            var host = new GameObject("AppleRewardGardenTest");
            try
            {
                var garden = host.AddComponent<EmotionGardenService>();
                garden.Initialize(_clock, eventBus);

                EmotionFlowerData? flower = garden.SubmitEmotion("喜悦", "开心", "angel");
                Assert.IsTrue(flower.HasValue);
                Assert.IsTrue(garden.SetBloomed(flower!.Value.FlowerId));
                Assert.AreEqual(21, _service.Balance);
                Assert.IsFalse(garden.SetBloomed(flower.Value.FlowerId));
                Assert.AreEqual(21, _service.Balance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
