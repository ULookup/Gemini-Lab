#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.SceneFlow;
using GeminiLab.Modules.DesktopOverlay;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class DesktopOverlayManagerEditModeTests
    {
        [Test]
        public void ApplyMode_LoadFail_DoesNotAdvance()
        {
            GameObject host = new("OverlayManagerEditModeTest");
            try
            {
                ServiceLocator.Reset();
                ServiceLocator.Register(new EventBus());
                var fakeSceneFlow = new FailingSceneFlow();
                ServiceLocator.Register<ISceneFlowService>(fakeSceneFlow);

                DesktopOverlayManager manager = host.AddComponent<DesktopOverlayManager>();

                manager.ApplyMode(DesktopMode.Overlay);

                Assert.AreEqual(DesktopMode.Apartment, manager.CurrentMode);
                Assert.AreEqual(SceneId.DesktopOverlay, fakeSceneFlow.LastRequested);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                ServiceLocator.Reset();
            }
        }

        private sealed class FailingSceneFlow : ISceneFlowService
        {
            public SceneId CurrentScene => SceneId.Apartment;
            public bool IsLoading => false;
            public SceneId LastRequested { get; private set; }

            public AsyncOperation? LoadAsync(SceneId target, SceneTransitionPayload? payload = null, Action? onCompleted = null)
            {
                LastRequested = target;
                return null;
            }
        }
    }
}
