#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    /// <summary>
    /// Production-ready gateway client with retry, timeout, fallback channel, and offline queue replay.
    /// </summary>
    public sealed class GatewayClient : IGatewayClient, IDisposable
    {
        private const int MaxAckedTraceKeys = 4096;

        private readonly GatewayConfigSO _config;
        private readonly HttpClient _httpClient;
        private readonly IGatewayStream _stream;
        private readonly Queue<GatewayQueuedRequest> _pendingQueue = new();
        private readonly HashSet<string> _ackedTraceIds = new(StringComparer.Ordinal);
        private readonly Queue<string> _ackedTraceOrder = new();
        private readonly object _syncRoot = new();
        private readonly GatewayMetrics _metrics = new();

        public GatewayClient(GatewayConfigSO config, IGatewayStream stream)
        {
            _config = config;
            _stream = stream;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(config.SendTimeoutMs)
            };
        }

        public event Action<GatewayEventEnvelope>? EventReceived;

        public GatewayEnvironment CurrentEnvironment => _config.Environment;

        public bool IsOnline { get; private set; } = true;

        public GatewayRetryPolicy RetryPolicy => new(
            _config.MaxAttempts,
            _config.InitialBackoffMs,
            _config.MaxBackoffMs,
            _config.BackoffMultiplier);

        public GatewayMetrics Metrics => _metrics;

        public Task<GatewaySendResult> SendAsync(GatewayRequest request, CancellationToken cancellationToken = default)
        {
            return SendInternalAsync(request, enqueueOnFailure: true, cancellationToken);
        }

        private async Task<GatewaySendResult> SendInternalAsync(GatewayRequest request, bool enqueueOnFailure, CancellationToken cancellationToken)
        {
            long startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (string.IsNullOrWhiteSpace(request.TraceId))
            {
                request.TraceId = Guid.NewGuid().ToString("N");
            }

            string ackKey = BuildAckKey(request);
            if (request.IsAck && IsTraceAcked(ackKey))
            {
                return new GatewaySendResult(
                    true,
                    GatewayChannelType.Http,
                    new GatewayResponse
                    {
                        TraceId = request.TraceId,
                        Success = true,
                        Message = "Skipped duplicated ACK by key."
                    },
                    GatewayErrorKind.None,
                    string.Empty);
            }

            if (_config.Environment == GatewayEnvironment.Mock)
            {
                GatewaySendResult mockResult = BuildMockResult(request);
                IsOnline = true;
                GatewayLog.Info("send_mock", request.TraceId, "Mock send completed.", request.PlayerId);
                return mockResult;
            }

            GatewaySendResult lastFailure = default;
            int delayMs = _config.InitialBackoffMs;
            for (int attempt = 1; attempt <= _config.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GatewaySendResult result = await SendOnceAsync(request, cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    IsOnline = true;
                    DispatchEventsFromResponse(result.Response);
                    long latencyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startMs;
                    _metrics.RecordSendSuccess(latencyMs);
                    GatewayLog.Info("send_success", request.TraceId, $"channel={result.Channel}", request.PlayerId, latencyMs);
                    return result;
                }

                lastFailure = result;
                bool retriable = IsRetriable(result.ErrorKind);
                if (!retriable || attempt >= _config.MaxAttempts)
                {
                    break;
                }

                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                _metrics.RecordRetry();
                delayMs = Math.Min((int)(delayMs * _config.BackoffMultiplier), _config.MaxBackoffMs);
            }

            IsOnline = false;
            _metrics.RecordSendFailure();
            GatewayLog.Warn("send_failed", request.TraceId, $"{lastFailure.ErrorKind}:{lastFailure.ErrorMessage}", request.PlayerId);
            if (enqueueOnFailure && _config.EnableOfflineQueue)
            {
                EnqueuePending(request);
            }

            return lastFailure;
        }

        public async Task<int> ReplayPendingAsync(CancellationToken cancellationToken = default)
        {
            if (!_config.EnableOfflineQueue)
            {
                return 0;
            }

            List<GatewayQueuedRequest> batch = DequeueBatch(_config.ReplayBatchSize);
            if (batch.Count == 0)
            {
                return 0;
            }

            int succeeded = 0;
            for (int i = 0; i < batch.Count; i++)
            {
                GatewayQueuedRequest queued = batch[i];
                GatewaySendResult result = await SendInternalAsync(queued.Request, enqueueOnFailure: false, cancellationToken).ConfigureAwait(false);
                if (result.Success)
                {
                    succeeded++;
                    _metrics.RecordReplay(true);
                    continue;
                }

                queued.AttemptCount++;
                Requeue(queued);
                _metrics.RecordReplay(false);
            }

            return succeeded;
        }

        public IReadOnlyCollection<string> GetAckedTraceIds()
        {
            lock (_syncRoot)
            {
                return new List<string>(_ackedTraceIds);
            }
        }

        public void MarkAcked(string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_ackedTraceIds.Add(traceId))
                {
                    _ackedTraceOrder.Enqueue(traceId);
                }

                while (_ackedTraceOrder.Count > MaxAckedTraceKeys)
                {
                    string expired = _ackedTraceOrder.Dequeue();
                    _ackedTraceIds.Remove(expired);
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _stream.Dispose();
        }

        private static string BuildAckKey(GatewayRequest request)
        {
            return $"{request.TraceId}:{request.Message}";
        }

        private bool IsTraceAcked(string traceId)
        {
            lock (_syncRoot)
            {
                return _ackedTraceIds.Contains(traceId);
            }
        }

        private GatewaySendResult BuildMockResult(GatewayRequest request)
        {
            GatewayResponse response = new()
            {
                TraceId = request.TraceId,
                Success = true,
                HttpStatusCode = 200,
                Message = "Mock response"
            };

            switch (request.RequestType)
            {
                case GatewayRequestType.Chat:
                    EventReceived?.Invoke(new GatewayEventEnvelope
                    {
                        TraceId = request.TraceId,
                        EventType = GatewayEventType.ChatChunk,
                        Message = $"[Mock] {request.Message}",
                        TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    EventReceived?.Invoke(new GatewayEventEnvelope
                    {
                        TraceId = request.TraceId,
                        EventType = GatewayEventType.ChatDone,
                        Message = "[Mock] chat completed",
                        TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    break;
                case GatewayRequestType.Work:
                    EventReceived?.Invoke(new GatewayEventEnvelope
                    {
                        TraceId = request.TraceId,
                        EventType = GatewayEventType.WorkStart,
                        Message = "[Mock] work started",
                        TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    EventReceived?.Invoke(new GatewayEventEnvelope
                    {
                        TraceId = request.TraceId,
                        EventType = GatewayEventType.WorkDone,
                        Message = "[Mock] work completed",
                        TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    break;
            }

            return new GatewaySendResult(true, GatewayChannelType.Http, response, GatewayErrorKind.None, string.Empty);
        }

        private async Task<GatewaySendResult> SendOnceAsync(GatewayRequest request, CancellationToken cancellationToken)
        {
            if (_config.PreferWebSocket)
            {
                GatewaySendResult wsResult = await TrySendByWebSocketAsync(request, cancellationToken).ConfigureAwait(false);
                if (wsResult.Success || wsResult.ErrorKind == GatewayErrorKind.Timeout)
                {
                    return wsResult;
                }
            }

            return await SendByHttpAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GatewaySendResult> TrySendByWebSocketAsync(GatewayRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_config.WebSocketEndpoint))
            {
                return new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Network, "WebSocket endpoint not configured.");
            }

            bool connected = await _stream.ConnectAsync(
                new Uri(_config.WebSocketEndpoint),
                _config.AuthToken,
                _config.ConnectTimeoutMs,
                cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                return new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Network, "WebSocket connect failed.");
            }

            return await _stream.SendAsync(request, _config.SendTimeoutMs, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GatewaySendResult> SendByHttpAsync(GatewayRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_config.HttpEndpoint))
            {
                return new GatewaySendResult(false, GatewayChannelType.Http, null, GatewayErrorKind.Network, "HTTP endpoint not configured.");
            }

            try
            {
                using HttpRequestMessage httpRequest = new(HttpMethod.Post, _config.HttpEndpoint);
                string requestJson = JsonUtility.ToJson(request);
                httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(_config.AuthToken))
                {
                    httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AuthToken);
                }

                using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new GatewaySendResult(false, GatewayChannelType.Http, null, GatewayErrorKind.Unauthorized, "Unauthorized.");
                }

                GatewayResponse parsedResponse;
                try
                {
                    parsedResponse = JsonUtility.FromJson<GatewayResponse>(responseText) ?? new GatewayResponse();
                }
                catch (Exception ex)
                {
                    return new GatewaySendResult(false, GatewayChannelType.Http, null, GatewayErrorKind.Deserialize, ex.Message);
                }

                parsedResponse.TraceId = string.IsNullOrWhiteSpace(parsedResponse.TraceId) ? request.TraceId : parsedResponse.TraceId;
                parsedResponse.HttpStatusCode = (int)response.StatusCode;
                bool businessSuccess = parsedResponse.Success;
                parsedResponse.Success = response.IsSuccessStatusCode && businessSuccess;

                if (!response.IsSuccessStatusCode)
                {
                    return new GatewaySendResult(false, GatewayChannelType.Http, parsedResponse, GatewayErrorKind.HttpStatus, $"HTTP {(int)response.StatusCode}");
                }

                if (!businessSuccess)
                {
                    return new GatewaySendResult(false, GatewayChannelType.Http, parsedResponse, GatewayErrorKind.Internal, "Gateway business status is false.");
                }

                return new GatewaySendResult(true, GatewayChannelType.Http, parsedResponse, GatewayErrorKind.None, string.Empty);
            }
            catch (TaskCanceledException)
            {
                return new GatewaySendResult(false, GatewayChannelType.Http, null, GatewayErrorKind.Timeout, "HTTP timeout.");
            }
            catch (Exception ex)
            {
                return new GatewaySendResult(false, GatewayChannelType.Http, null, GatewayErrorKind.Network, ex.Message);
            }
        }

        private void DispatchEventsFromResponse(GatewayResponse? response)
        {
            if (response is null || string.IsNullOrWhiteSpace(response.PayloadJson))
            {
                return;
            }

            try
            {
                GatewayEventEnvelope? envelope = JsonUtility.FromJson<GatewayEventEnvelope>(response.PayloadJson);
                if (envelope is null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(envelope.TraceId))
                {
                    envelope.TraceId = response.TraceId;
                }

                if (envelope.TimestampUnixMs <= 0)
                {
                    envelope.TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                EventReceived?.Invoke(envelope);
                GatewayLog.Info("event_received", envelope.TraceId, envelope.EventType.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Gateway] Failed to parse event payload: {ex.Message}");
            }
        }

        private static bool IsRetriable(GatewayErrorKind errorKind)
        {
            return errorKind == GatewayErrorKind.Timeout ||
                   errorKind == GatewayErrorKind.Network ||
                   errorKind == GatewayErrorKind.HttpStatus;
        }

        private void EnqueuePending(GatewayRequest request)
        {
            lock (_syncRoot)
            {
                if (_pendingQueue.Count >= _config.MaxPendingRequests)
                {
                    _ = _pendingQueue.Dequeue();
                }

                _pendingQueue.Enqueue(new GatewayQueuedRequest
                {
                    TraceId = request.TraceId,
                    Request = request,
                    AttemptCount = 0,
                    EnqueuedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
        }

        private List<GatewayQueuedRequest> DequeueBatch(int maxBatchSize)
        {
            List<GatewayQueuedRequest> batch = new(Math.Max(1, maxBatchSize));
            lock (_syncRoot)
            {
                int count = Math.Min(maxBatchSize, _pendingQueue.Count);
                for (int i = 0; i < count; i++)
                {
                    batch.Add(_pendingQueue.Dequeue());
                }
            }

            return batch;
        }

        private void Requeue(GatewayQueuedRequest request)
        {
            lock (_syncRoot)
            {
                _pendingQueue.Enqueue(request);
            }
        }
    }
}
