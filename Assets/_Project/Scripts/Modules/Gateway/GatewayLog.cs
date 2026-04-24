#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    public static class GatewayLog
    {
        public static void Info(string eventType, string traceId, string message, string? playerId = null, long latencyMs = 0)
        {
            Debug.Log(Format("INFO", eventType, traceId, message, playerId, latencyMs));
        }

        public static void Warn(string eventType, string traceId, string message, string? playerId = null, long latencyMs = 0)
        {
            Debug.LogWarning(Format("WARN", eventType, traceId, message, playerId, latencyMs));
        }

        private static string Format(string level, string eventType, string traceId, string message, string? playerId, long latencyMs)
        {
            string sanitizedPlayerId = Mask(playerId);
            return $"[Gateway][{level}] event={eventType} traceId={traceId} playerId={sanitizedPlayerId} latencyMs={latencyMs} msg={message}";
        }

        public static string Mask(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "null";
            }

            if (raw.Length <= 4)
            {
                return "****";
            }

            return $"{raw[..2]}***{raw[^2..]}";
        }
    }
}
