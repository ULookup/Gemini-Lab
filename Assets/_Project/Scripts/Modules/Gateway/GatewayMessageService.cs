#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    /// <summary>
    /// Entry point for player messages that bridges OpenClaw and pet work commands.
    /// </summary>
    public sealed class GatewayMessageService : IGatewayMessageService
    {
        private readonly IGatewayClient _gatewayClient;
        private readonly IPetCommandLinkService _commandLinkService;
        private readonly PromptContextBuilder _contextBuilder;
        private readonly EventBus _eventBus;

        public GatewayMessageService(
            IGatewayClient gatewayClient,
            IPetCommandLinkService commandLinkService,
            PromptContextBuilder contextBuilder,
            EventBus eventBus)
        {
            _gatewayClient = gatewayClient;
            _commandLinkService = commandLinkService;
            _contextBuilder = contextBuilder;
            _eventBus = eventBus;
        }

        public async Task<string> HandlePlayerMessageAsync(string playerId, string message, bool forceWake, CancellationToken cancellationToken = default)
        {
            string traceId = Guid.NewGuid().ToString("N");
            PetRuntimeData snapshot = BuildRuntimeSnapshot();
            GatewayLog.Info("player_message_received", traceId, "Routing player message to gateway.", playerId);

            GatewayRequest gatewayRequest = _contextBuilder.BuildWorkRequest(traceId, playerId, message, snapshot);
            GatewaySendResult gatewayResult = await _gatewayClient.SendAsync(gatewayRequest, cancellationToken).ConfigureAwait(false);
            if (!gatewayResult.Success)
            {
                _eventBus.Publish(new GatewayErrorEvent(traceId, gatewayResult.ErrorMessage, gatewayResult.ErrorKind.ToString()));
                GatewayLog.Warn("gateway_send_failed", traceId, gatewayResult.ErrorMessage, playerId);
            }

            _ = _commandLinkService.Enqueue(new PetCommandRequest(
                traceId,
                PetCommandType.WorkRequest,
                PetCommandSource.PlayerMessage,
                forceWake,
                priority: 120,
                targetType: PetWorkTargetType.WorkDesk,
                message: message));
            GatewayLog.Info("work_command_enqueued", traceId, "Enqueued work request command.", playerId);
            return traceId;
        }

        public async Task<GatewayDispatchResult> HandleTravelRequestAsync(string playerId, string topic, CancellationToken cancellationToken = default)
        {
            string traceId = Guid.NewGuid().ToString("N");
            PetRuntimeData snapshot = BuildRuntimeSnapshot();
            GatewayRequest request = _contextBuilder.BuildTravelRequest(traceId, playerId, topic, snapshot);
            GatewaySendResult result = await _gatewayClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                _eventBus.Publish(new GatewayErrorEvent(traceId, result.ErrorMessage, result.ErrorKind.ToString()));
                GatewayLog.Warn("travel_request_failed", traceId, result.ErrorMessage, playerId);
                return new GatewayDispatchResult(traceId, false, result.ErrorKind, result.ErrorMessage);
            }

            GatewayLog.Info("travel_request_sent", traceId, "Travel request sent.", playerId);
            return new GatewayDispatchResult(traceId, true, GatewayErrorKind.None, string.Empty);
        }

        private static PetRuntimeData BuildRuntimeSnapshot()
        {
            PetController? controller = UnityEngine.Object.FindFirstObjectByType<PetController>();
            if (controller?.RuntimeData is null)
            {
                return new PetRuntimeData();
            }

            PetRuntimeData source = controller.RuntimeData;
            return new PetRuntimeData
            {
                Mood = source.Mood,
                Energy = source.Energy,
                Satiety = source.Satiety,
                CurrentState = source.CurrentState,
                WorkRequested = source.WorkRequested,
                Position = source.Position,
                TargetPosition = source.TargetPosition,
                TargetFurnitureId = source.TargetFurnitureId,
                LastTraceId = source.LastTraceId,
                ActiveWorkTraceId = source.ActiveWorkTraceId,
                ActiveWorkMessage = source.ActiveWorkMessage,
                RequiredWorkTargetType = source.RequiredWorkTargetType,
                IsAtRequiredWorkTarget = source.IsAtRequiredWorkTarget,
                IsTraveling = source.IsTraveling,
                ActiveTravelTraceId = source.ActiveTravelTraceId,
                ActiveTravelTopic = source.ActiveTravelTopic,
                TravelEndAtSeconds = source.TravelEndAtSeconds,
                TravelCompletedCount = source.TravelCompletedCount
            };
        }
    }
}
