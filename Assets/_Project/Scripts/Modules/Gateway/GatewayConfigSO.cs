#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    [CreateAssetMenu(menuName = "GeminiLab/Gateway/Config", fileName = "GatewayConfig")]
    public sealed class GatewayConfigSO : ScriptableObject
    {
        [Header("Environment")]
        public GatewayEnvironment Environment = GatewayEnvironment.Mock;

        [Header("Endpoints")]
        public string HttpEndpoint = "http://127.0.0.1:38080/api/v1/gateway";
        public string WebSocketEndpoint = "ws://127.0.0.1:38080/ws";

        [Header("Auth")]
        public string AuthToken = string.Empty;

        [Header("Timeout (ms)")]
        [Min(500)] public int SendTimeoutMs = 5000;
        [Min(500)] public int ConnectTimeoutMs = 5000;

        [Header("Retry")]
        [Min(1)] public int MaxAttempts = 3;
        [Min(50)] public int InitialBackoffMs = 200;
        [Min(100)] public int MaxBackoffMs = 2000;
        [Range(1f, 4f)] public float BackoffMultiplier = 2f;

        [Header("Reliability")]
        public bool EnableOfflineQueue = true;
        [Min(1)] public int MaxPendingRequests = 200;
        [Min(1)] public int ReplayBatchSize = 20;
        [Min(100)] public int ReplayIntervalMs = 1000;

        [Header("Transport")]
        public bool PreferWebSocket = true;
    }
}
