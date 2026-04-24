#nullable enable

namespace GeminiLab.Modules.Travel
{
    public enum TravelCommandType
    {
        Depart = 0,
        Complete = 1,
        Fail = 2
    }

    public readonly struct TravelCommand
    {
        public TravelCommand(string traceId, TravelCommandType commandType, string payload)
        {
            TraceId = traceId;
            CommandType = commandType;
            Payload = payload;
        }

        public string TraceId { get; }
        public TravelCommandType CommandType { get; }
        public string Payload { get; }
    }
}
