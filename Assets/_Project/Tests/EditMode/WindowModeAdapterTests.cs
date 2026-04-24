#nullable enable
using GeminiLab.Modules.DesktopOverlay;
using NUnit.Framework;

namespace GeminiLab.Tests.EditMode
{
    public sealed class WindowModeAdapterTests
    {
        [Test]
        public void SetModeAndClickThrough_UpdatesAdapterState()
        {
            WindowModeAdapter adapter = new();

            adapter.SetMode(DesktopMode.Overlay);
            adapter.SetClickThrough(true);

            Assert.AreEqual(DesktopMode.Overlay, adapter.CurrentMode);
            Assert.IsTrue(adapter.IsClickThrough);
        }
    }
}
