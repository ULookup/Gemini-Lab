#nullable enable

namespace GeminiLab.Modules.Gateway
{
    public readonly struct GatewayChatChunkEvent
    {
        public GatewayChatChunkEvent(string traceId, string content)
        {
            TraceId = traceId;
            Content = content;
        }

        public string TraceId { get; }
        public string Content { get; }
    }

    public readonly struct GatewayChatDoneEvent
    {
        public GatewayChatDoneEvent(string traceId, string summary)
        {
            TraceId = traceId;
            Summary = summary;
        }

        public string TraceId { get; }
        public string Summary { get; }
    }

    public readonly struct GatewayErrorEvent
    {
        public GatewayErrorEvent(string traceId, string message, string errorCode)
        {
            TraceId = traceId;
            Message = message;
            ErrorCode = errorCode;
        }

        public string TraceId { get; }
        public string Message { get; }
        public string ErrorCode { get; }
    }

    public readonly struct GatewayWorkStartEvent
    {
        public GatewayWorkStartEvent(string traceId, string message)
        {
            TraceId = traceId;
            Message = message;
        }

        public string TraceId { get; }
        public string Message { get; }
    }
}
