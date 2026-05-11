#nullable enable
using GeminiLab.Modules.DesktopOverlay;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class DesktopOverlayManagerEditModeTests
    {
        [Test]
        public void ApplyMode_LoadFail_RollsBackMode()
        {
            GameObject host = new("OverlayManagerEditModeTest");
            try
            {
                DesktopOverlayManager manager = host.AddComponent<DesktopOverlayManager>();
                manager.SetSceneLoader(_ => null);

                manager.ApplyMode(DesktopMode.Overlay);

                Assert.AreEqual(DesktopMode.Apartment, manager.CurrentMode);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
