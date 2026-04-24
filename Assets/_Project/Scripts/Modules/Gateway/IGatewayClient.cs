#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.Gateway
{
    public interface IGatewayStream : IDisposable
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync(Uri endpoint, string authToken, int timeoutMs, CancellationToken cancellationToken);
        Task<GatewaySendResult> SendAsync(GatewayRequest request, int timeoutMs, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
    }

    public interface IGatewayClient
    {
        event Action<GatewayEventEnvelope>? EventReceived;

        GatewayEnvironment CurrentEnvironment { get; }

        bool IsOnline { get; }

        GatewayRetryPolicy RetryPolicy { get; }

        GatewayMetrics Metrics { get; }

        Task<GatewaySendResult> SendAsync(GatewayRequest request, CancellationToken cancellationToken = default);

        Task<int> ReplayPendingAsync(CancellationToken cancellationToken = default);

        IReadOnlyCollection<string> GetAckedTraceIds();

        void MarkAcked(string traceId);
    }
}
