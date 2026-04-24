#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.Gateway
{
    public enum GatewayEnvironment
    {
        Mock = 0,
        Production = 1
    }

    public enum GatewayChannelType
    {
        Http = 0,
        WebSocket = 1
    }

    public enum GatewayEventType
    {
        Unknown = 0,
        ChatChunk = 1,
        ChatDone = 2,
        WorkStart = 3,
        WorkDone = 4,
        WorkFailed = 5,
        Error = 6
    }

    public enum GatewayRequestType
    {
        Chat = 0,
        Work = 1,
        Ack = 2
    }

    public enum GatewayErrorKind
    {
        None = 0,
        Timeout = 1,
        Network = 2,
        HttpStatus = 3,
        Deserialize = 4,
        Unauthorized = 5,
        Internal = 6
    }

    [Serializable]
    public sealed class GatewayRequest
    {
        public string TraceId = string.Empty;
        public GatewayRequestType RequestType;
        public string Message = string.Empty;
        public string PlayerId = string.Empty;
        public string PetState = string.Empty;
        public string Personality = string.Empty;
        public string ContentJson = "{}";
        public bool IsAck;
    }

    [Serializable]
    public sealed class GatewayResponse
    {
        public string TraceId = string.Empty;
        public bool Success;
        public string Message = string.Empty;
        public string PayloadJson = "{}";
        public int HttpStatusCode;
    }

    [Serializable]
    public sealed class GatewayEventEnvelope
    {
        public string TraceId = string.Empty;
        public GatewayEventType EventType;
        public string PayloadJson = "{}";
        public string Message = string.Empty;
        public string ErrorCode = string.Empty;
        public long TimestampUnixMs;
    }

    public readonly struct GatewayRetryPolicy
    {
        public GatewayRetryPolicy(int maxAttempts, int initialDelayMs, int maxDelayMs, float backoffMultiplier)
        {
            MaxAttempts = maxAttempts;
            InitialDelayMs = initialDelayMs;
            MaxDelayMs = maxDelayMs;
            BackoffMultiplier = backoffMultiplier;
        }

        public int MaxAttempts { get; }
        public int InitialDelayMs { get; }
        public int MaxDelayMs { get; }
        public float BackoffMultiplier { get; }
    }

    public readonly struct GatewaySendResult
    {
        public GatewaySendResult(bool success, GatewayChannelType channel, GatewayResponse? response, GatewayErrorKind errorKind, string errorMessage)
        {
            Success = success;
            Channel = channel;
            Response = response;
            ErrorKind = errorKind;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; }
        public GatewayChannelType Channel { get; }
        public GatewayResponse? Response { get; }
        public GatewayErrorKind ErrorKind { get; }
        public string ErrorMessage { get; }
    }

    [Serializable]
    public sealed class GatewayQueuedRequest
    {
        public string TraceId = string.Empty;
        public GatewayRequest Request = new();
        public int AttemptCount;
        public long EnqueuedUnixMs;
    }

    [Serializable]
    public sealed class GatewayPendingQueueSnapshot
    {
        public List<GatewayQueuedRequest> Requests = new();
    }
}
