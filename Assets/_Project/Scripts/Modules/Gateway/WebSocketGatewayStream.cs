#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    public sealed class WebSocketGatewayStream : IGatewayStream
    {
        private readonly ClientWebSocket _socket = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly SemaphoreSlim _connectLock = new(1, 1);

        public bool IsConnected => _socket.State == WebSocketState.Open;

        public async Task<bool> ConnectAsync(Uri endpoint, string authToken, int timeoutMs, CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                return true;
            }

            await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);
            try
            {
                if (IsConnected)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(authToken))
                {
                    _socket.Options.SetRequestHeader("Authorization", $"Bearer {authToken}");
                }

                await _socket.ConnectAsync(endpoint, timeoutCts.Token).ConfigureAwait(false);
                return IsConnected;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Gateway/WebSocket] Connect failed: {ex.Message}");
                return false;
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async Task<GatewaySendResult> SendAsync(GatewayRequest request, int timeoutMs, CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                return new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Network, "WebSocket not connected.");
            }

            string requestJson = JsonUtility.ToJson(request);
            byte[] payload = Encoding.UTF8.GetBytes(requestJson);
            ArraySegment<byte> buffer = new(payload);
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);

            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, timeoutCts.Token).ConfigureAwait(false);

                byte[] responseBytes = new byte[8192];
                WebSocketReceiveResult receiveResult = await _socket.ReceiveAsync(new ArraySegment<byte>(responseBytes), timeoutCts.Token).ConfigureAwait(false);
                string responseText = Encoding.UTF8.GetString(responseBytes, 0, receiveResult.Count);
                GatewayResponse response = JsonUtility.FromJson<GatewayResponse>(responseText) ?? new GatewayResponse
                {
                    TraceId = request.TraceId,
                    Success = true,
                    Message = responseText
                };

                return new GatewaySendResult(true, GatewayChannelType.WebSocket, response, GatewayErrorKind.None, string.Empty);
            }
            catch (OperationCanceledException)
            {
                return new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Timeout, "WebSocket send timeout.");
            }
            catch (Exception ex)
            {
                return new GatewaySendResult(false, GatewayChannelType.WebSocket, null, GatewayErrorKind.Network, ex.Message);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutdown", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Gateway/WebSocket] Disconnect failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
            _sendLock.Dispose();
            _connectLock.Dispose();
        }
    }
}
