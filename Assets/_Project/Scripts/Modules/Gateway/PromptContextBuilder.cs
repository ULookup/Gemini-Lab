#nullable enable
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    /// <summary>
    /// Builds compact payload context from runtime pet state for OpenClaw requests.
    /// </summary>
    public sealed class PromptContextBuilder
    {
        public GatewayRequest BuildChatRequest(string traceId, string playerId, string message, PetRuntimeData runtimeData)
        {
            return new GatewayRequest
            {
                TraceId = traceId,
                PlayerId = playerId,
                Message = message,
                RequestType = GatewayRequestType.Chat,
                PetState = runtimeData.CurrentState,
                Personality = BuildPersonality(runtimeData),
                ContentJson = JsonUtility.ToJson(new PromptContextPayload
                {
                    mood = runtimeData.Mood,
                    energy = runtimeData.Energy,
                    satiety = runtimeData.Satiety,
                    state = runtimeData.CurrentState,
                    workRequested = runtimeData.WorkRequested,
                    targetFurnitureId = runtimeData.TargetFurnitureId
                })
            };
        }

        public GatewayRequest BuildWorkRequest(string traceId, string playerId, string message, PetRuntimeData runtimeData)
        {
            GatewayRequest request = BuildChatRequest(traceId, playerId, message, runtimeData);
            request.RequestType = GatewayRequestType.Work;
            return request;
        }

        public GatewayRequest BuildTravelRequest(string traceId, string playerId, string topic, PetRuntimeData runtimeData)
        {
            GatewayRequest request = BuildChatRequest(traceId, playerId, topic, runtimeData);
            request.RequestType = GatewayRequestType.Travel;
            return request;
        }

        private static string BuildPersonality(PetRuntimeData runtimeData)
        {
            if (runtimeData.Mood >= 70f)
            {
                return "upbeat";
            }

            if (runtimeData.Mood <= 30f)
            {
                return "tired";
            }

            return "balanced";
        }

        [System.Serializable]
        private sealed class PromptContextPayload
        {
            public float mood;
            public float energy;
            public float satiety;
            public string state = string.Empty;
            public bool workRequested;
            public string targetFurnitureId = string.Empty;
        }
    }
}
