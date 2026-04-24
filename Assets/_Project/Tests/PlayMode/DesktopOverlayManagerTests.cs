#nullable enable
using GeminiLab.Modules.DesktopOverlay;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.PlayMode
{
    public sealed class DesktopOverlayManagerTests
    {
        [Test]
        public void ToggleMode_SwitchesBetweenApartmentAndOverlay()
        {
            GameObject host = new("OverlayManagerTest");
            try
            {
                DesktopOverlayManager manager = host.AddComponent<DesktopOverlayManager>();
                Assert.AreEqual(DesktopMode.Apartment, manager.CurrentMode);

                manager.ToggleMode();
                Assert.AreEqual(DesktopMode.Overlay, manager.CurrentMode);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
