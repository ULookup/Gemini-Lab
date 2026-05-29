#nullable enable
using GeminiLab.Modules.HubUI;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class ApartmentViewportInputBridgeTests
    {
        [Test]
        public void TryLocalPointToViewportPoint_Center_ReturnsHalfCoordinates()
        {
            Rect rect = new(-100f, -50f, 200f, 100f);

            bool ok = ApartmentViewportInputBridge.TryLocalPointToViewportPoint(
                Vector2.zero,
                rect,
                out Vector2 viewportPoint);

            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), viewportPoint);
        }

        [Test]
        public void TryLocalPointToViewportPoint_OutsideRect_ReturnsFalse()
        {
            Rect rect = new(-100f, -50f, 200f, 100f);

            bool ok = ApartmentViewportInputBridge.TryLocalPointToViewportPoint(
                new Vector2(120f, 0f),
                rect,
                out _);

            Assert.IsFalse(ok);
        }

        [Test]
        public void TryLocalPointToViewportPoint_InvalidRect_ReturnsFalse()
        {
            Rect rect = new(0f, 0f, 0f, 100f);

            bool ok = ApartmentViewportInputBridge.TryLocalPointToViewportPoint(
                Vector2.zero,
                rect,
                out _);

            Assert.IsFalse(ok);
        }
    }
}
