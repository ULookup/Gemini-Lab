#nullable enable
using System;

namespace GeminiLab.Modules.Travel
{
    [Serializable]
    public sealed class TravelTimelineEntry
    {
        public string TraceId = string.Empty;
        public string Message = string.Empty;
        public float TimeSeconds;
    }
}
