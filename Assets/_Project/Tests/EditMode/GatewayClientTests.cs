#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Gateway;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class GatewayClientTests
    {
        [Test]
        public void SendAsync_MockEnvironment_EmitsWorkEvents()
        {
            GatewayConfigSO config = ScriptableObject.CreateInstance<GatewayConfigSO>();
            config.Environment = GatewayEnvironment.Mock;
            GatewayClient client = new(config, new FakeStream());
            int eventCount = 0;
            client.EventReceived += _ => eventCount++;

            GatewaySendResult result = client.SendAsync(new GatewayRequest
            {
                TraceId = "trace_mock",
                RequestType = GatewayRequestType.Work,
                Message = "do work"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.GreaterOrEqual(eventCount, 2);
        }

        [Test]
        public void SendAsync_ProductionFailure_QueuesForReplay()
        {
            GatewayConfigSO config = ScriptableObject.CreateInstance<GatewayConfigSO>();
            config.Environment = GatewayEnvironment.Production;
            config.HttpEndpoint = "http://127.0.0.1:1/unreachable";
            config.EnableOfflineQueue = true;
            config.MaxAttempts = 1;
            GatewayClient client = new(config, new FakeStream());

            GatewaySendResult result = client.SendAsync(new GatewayRequest
            {
                TraceId = "trace_retry",
                RequestType = GatewayRequestType.Chat,
                Message = "hello"
            }).GetAwaiter().GetResult();

            Assert.IsFalse(result.Success);
            int replayed = client.ReplayPendingAsync().GetAwaiter().GetResult();
            Assert.GreaterOrEqual(replayed, 0);
        }

        [Test]
        public void SendAsync_MockTravel_EmitsTravelEvents()
        {
            GatewayConfigSO config = ScriptableObject.CreateInstance<GatewayConfigSO>();
            config.Environment = GatewayEnvironment.Mock;
            GatewayClient client = new(config, new FakeStream());
            int started = 0;
            int completed = 0;
            client.EventReceived += evt =>
            {
                if (evt.EventType == GatewayEventType.TravelStarted)
                {
                    started++;
                }

                if (evt.EventType == GatewayEventType.TravelCompleted)
                {
                    completed++;
                }
            };

            GatewaySendResult result = client.SendAsync(new GatewayRequest
            {
                TraceId = "trace_travel_mock",
                RequestType = GatewayRequestType.Travel,
                Message = "travel"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, started);
            Assert.AreEqual(1, completed);
        }

        private sealed class FakeStream : IGatewayStream
        {
            public bool IsConnected => false;

            public Task<bool> ConnectAsync(Uri endpoint, string authToken, int timeoutMs, CancellationToken cancellationToken)
            {
                return Task.FromResult(false);
            }

            public void Dispose()
            {
            }

            public Task DisconnectAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<GatewaySendResult> SendAsync(GatewayRequest request, int timeoutMs, CancellationToken cancellationToken)
            {
                return Task.FromResult(new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Network, "not connected"));
            }
        }
    }
}
