#nullable enable
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Travel;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class TravelServiceTests
    {
        [Test]
        public void DepartAsync_DispatchFailed_DoesNotEnterTraveling()
        {
            GameObject host = new("TravelServiceTest");
            try
            {
                PetController petController = host.AddComponent<PetController>();
                EventBus eventBus = new();
                FakeGatewayMessageService gatewayMessageService = new(
                    new GatewayDispatchResult("trace_fail", false, GatewayErrorKind.Network, "network down"));

                using TravelService service = new(gatewayMessageService, eventBus, petController);
                _ = service.DepartAsync("p1", "trip").GetAwaiter().GetResult();

                Assert.IsFalse(service.IsTraveling);
                Assert.AreEqual("trace_fail", service.Timeline[0].TraceId);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private sealed class FakeGatewayMessageService : IGatewayMessageService
        {
            private readonly GatewayDispatchResult _result;

            public FakeGatewayMessageService(GatewayDispatchResult result)
            {
                _result = result;
            }

            public Task<string> HandlePlayerMessageAsync(string playerId, string message, bool forceWake, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("noop");
            }

            public Task<GatewayDispatchResult> HandleTravelRequestAsync(string playerId, string topic, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_result);
            }
        }
    }
}
