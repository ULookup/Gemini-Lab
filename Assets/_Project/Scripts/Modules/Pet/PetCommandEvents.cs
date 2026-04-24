#nullable enable

namespace GeminiLab.Modules.Pet
{
    public readonly struct PetCommandAcceptedEvent
    {
        public PetCommandAcceptedEvent(string traceId, bool forceWake)
        {
            TraceId = traceId;
            ForceWake = forceWake;
        }

        public string TraceId { get; }
        public bool ForceWake { get; }
    }

    public readonly struct PetWakePenaltyAppliedEvent
    {
        public PetWakePenaltyAppliedEvent(string traceId, float moodDelta)
        {
            TraceId = traceId;
            MoodDelta = moodDelta;
        }

        public string TraceId { get; }
        public float MoodDelta { get; }
    }

    public readonly struct PetCommandRejectedEvent
    {
        public PetCommandRejectedEvent(string traceId, string reason)
        {
            TraceId = traceId;
            Reason = reason;
        }

        public string TraceId { get; }
        public string Reason { get; }
    }
}
