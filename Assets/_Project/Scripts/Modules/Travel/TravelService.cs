#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Travel
{
    /// <summary>
    /// Orchestrates travel lifecycle: depart, timeline updates and reward application.
    /// </summary>
    public sealed class TravelService : ITravelService, IDisposable
    {
        private const int MaxTimelineEntries = 128;

        private readonly List<TravelTimelineEntry> _timeline = new();
        private readonly HashSet<string> _startedTraceIds = new(StringComparer.Ordinal);
        private readonly IGatewayMessageService _gatewayMessageService;
        private readonly EventBus _eventBus;
        private readonly PetController _petController;
        private readonly IDisposable _travelStartedSub;
        private readonly IDisposable _travelCompletedSub;
        private readonly IDisposable _travelFailedSub;

        public TravelService(
            IGatewayMessageService gatewayMessageService,
            EventBus eventBus,
            PetController petController)
        {
            _gatewayMessageService = gatewayMessageService;
            _eventBus = eventBus;
            _petController = petController;
            _travelStartedSub = _eventBus.Subscribe<GatewayTravelStartedEvent>(OnTravelStarted);
            _travelCompletedSub = _eventBus.Subscribe<GatewayTravelCompletedEvent>(OnTravelCompleted);
            _travelFailedSub = _eventBus.Subscribe<GatewayTravelFailedEvent>(OnTravelFailed);
        }

        public IReadOnlyList<TravelTimelineEntry> Timeline => _timeline;

        public bool IsTraveling => _petController.RuntimeData?.IsTraveling ?? false;

        public async Task<string> DepartAsync(string playerId, string topic, CancellationToken cancellationToken = default)
        {
            GatewayDispatchResult dispatchResult = await _gatewayMessageService.HandleTravelRequestAsync(playerId, topic, cancellationToken).ConfigureAwait(false);
            string traceId = dispatchResult.TraceId;
            if (!dispatchResult.Success)
            {
                AddTimeline(traceId, $"DepartFailed: {dispatchResult.ErrorMessage}");
                _eventBus.Publish(new TravelFailedEvent(traceId, dispatchResult.ErrorMessage));
                return traceId;
            }

            PetRuntimeData? runtimeData = _petController.RuntimeData;
            if (runtimeData is null)
            {
                return traceId;
            }

            runtimeData.IsTraveling = true;
            runtimeData.ActiveTravelTraceId = traceId;
            runtimeData.ActiveTravelTopic = topic;
            runtimeData.TravelEndAtSeconds = runtimeData.RuntimeTimeSeconds + 90f;
            AddTimeline(traceId, $"Departed: {topic}");
            return traceId;
        }

        public void Dispose()
        {
            _travelStartedSub.Dispose();
            _travelCompletedSub.Dispose();
            _travelFailedSub.Dispose();
        }

        private void OnTravelStarted(GatewayTravelStartedEvent payload)
        {
            PetRuntimeData? runtimeData = _petController.RuntimeData;
            if (runtimeData is null ||
                !runtimeData.IsTraveling ||
                !string.Equals(runtimeData.ActiveTravelTraceId, payload.TraceId, StringComparison.Ordinal) ||
                !_startedTraceIds.Add(payload.TraceId))
            {
                return;
            }

            AddTimeline(payload.TraceId, $"Gateway started: {payload.Message}");
            _eventBus.Publish(new TravelStartedEvent(payload.TraceId, payload.Message));
        }

        private void OnTravelCompleted(GatewayTravelCompletedEvent payload)
        {
            PetRuntimeData? runtimeData = _petController.RuntimeData;
            if (runtimeData is null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(runtimeData.ActiveTravelTraceId) ||
                !string.Equals(runtimeData.ActiveTravelTraceId, payload.TraceId, StringComparison.Ordinal))
            {
                return;
            }

            TravelRewardApplier.ApplyTravelReward(runtimeData);
            runtimeData.IsTraveling = false;
            runtimeData.ActiveTravelTraceId = string.Empty;
            runtimeData.ActiveTravelTopic = string.Empty;
            _startedTraceIds.Remove(payload.TraceId);
            AddTimeline(payload.TraceId, $"Completed: {payload.Summary}");
            _eventBus.Publish(new TravelCompletedEvent(payload.TraceId, payload.Summary));
        }

        private void OnTravelFailed(GatewayTravelFailedEvent payload)
        {
            PetRuntimeData? runtimeData = _petController.RuntimeData;
            if (runtimeData is null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(runtimeData.ActiveTravelTraceId) ||
                !string.Equals(runtimeData.ActiveTravelTraceId, payload.TraceId, StringComparison.Ordinal))
            {
                return;
            }

            runtimeData.IsTraveling = false;
            runtimeData.ActiveTravelTraceId = string.Empty;
            runtimeData.ActiveTravelTopic = string.Empty;
            _startedTraceIds.Remove(payload.TraceId);
            AddTimeline(payload.TraceId, $"Failed: {payload.Reason}");
            _eventBus.Publish(new TravelFailedEvent(payload.TraceId, payload.Reason));
        }

        private void AddTimeline(string traceId, string message)
        {
            _timeline.Add(new TravelTimelineEntry
            {
                TraceId = traceId,
                Message = message,
                TimeSeconds = _petController.RuntimeData?.RuntimeTimeSeconds ?? 0f
            });

            while (_timeline.Count > MaxTimelineEntries)
            {
                _timeline.RemoveAt(0);
            }
        }
    }
}
