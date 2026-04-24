#nullable enable

namespace GeminiLab.Modules.Travel
{
    public readonly struct TravelStartedEvent
    {
        public TravelStartedEvent(string traceId, string topic)
        {
            TraceId = traceId;
            Topic = topic;
        }

        public string TraceId { get; }
        public string Topic { get; }
    }

    public readonly struct TravelCompletedEvent
    {
        public TravelCompletedEvent(string traceId, string summary)
        {
            TraceId = traceId;
            Summary = summary;
        }

        public string TraceId { get; }
        public string Summary { get; }
    }

    public readonly struct TravelFailedEvent
    {
        public TravelFailedEvent(string traceId, string reason)
        {
            TraceId = traceId;
            Reason = reason;
        }

        public string TraceId { get; }
        public string Reason { get; }
    }
}
