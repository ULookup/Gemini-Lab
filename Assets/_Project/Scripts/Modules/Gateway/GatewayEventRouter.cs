#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Gateway
{
    /// <summary>
    /// Routes gateway callbacks to local game events and command queue.
    /// </summary>
    public sealed class GatewayEventRouter : IDisposable
    {
        private const int MaxProcessedTerminalEvents = 4096;
        private const int MaxAcceptedTraceIds = 2048;
        private const int MaxDeferredTraceCount = 1024;
        private const int MaxDeferredEventsPerTrace = 8;

        private readonly IGatewayClient _gatewayClient;
        private readonly IPetCommandLinkService _commandLinkService;
        private readonly EventBus _eventBus;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly ConcurrentQueue<GatewayEventEnvelope> _pendingEvents = new();
        private readonly HashSet<string> _processedTerminalEventKeys = new(StringComparer.Ordinal);
        private readonly Queue<string> _processedTerminalEventOrder = new();
        private readonly HashSet<string> _acceptedWorkTraceIds = new(StringComparer.Ordinal);
        private readonly Queue<string> _acceptedWorkTraceOrder = new();
        private readonly Dictionary<string, List<GatewayEventEnvelope>> _deferredWorkEventsByTrace = new(StringComparer.Ordinal);
        private readonly Queue<string> _deferredTraceOrder = new();

        public GatewayEventRouter(IGatewayClient gatewayClient, IPetCommandLinkService commandLinkService, EventBus eventBus)
        {
            _gatewayClient = gatewayClient;
            _commandLinkService = commandLinkService;
            _eventBus = eventBus;
            _gatewayClient.EventReceived += OnGatewayEventReceived;

            _subscriptions.Add(_eventBus.Subscribe<PetWorkStartedEvent>(OnPetWorkStarted));
            _subscriptions.Add(_eventBus.Subscribe<PetWorkCompletedEvent>(OnPetWorkCompleted));
            _subscriptions.Add(_eventBus.Subscribe<PetWorkFailedEvent>(OnPetWorkFailed));
            _subscriptions.Add(_eventBus.Subscribe<PetCommandAcceptedEvent>(OnPetCommandAccepted));
        }

        public void Dispose()
        {
            _gatewayClient.EventReceived -= OnGatewayEventReceived;
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnGatewayEventReceived(GatewayEventEnvelope envelope)
        {
            _pendingEvents.Enqueue(envelope);
        }

        public void ProcessPendingEvents()
        {
            while (_pendingEvents.TryDequeue(out GatewayEventEnvelope envelope))
            {
                HandleEventOnMainThread(envelope);
            }
        }

        private void HandleEventOnMainThread(GatewayEventEnvelope envelope)
        {
            switch (envelope.EventType)
            {
                case GatewayEventType.ChatChunk:
                    _eventBus.Publish(new GatewayChatChunkEvent(envelope.TraceId, envelope.Message));
                    break;
                case GatewayEventType.ChatDone:
                    if (IsDuplicateTerminalEvent(envelope))
                    {
                        return;
                    }

                    _eventBus.Publish(new GatewayChatDoneEvent(envelope.TraceId, envelope.Message));
                    break;
                case GatewayEventType.WorkDone:
                    if (!_acceptedWorkTraceIds.Contains(envelope.TraceId))
                    {
                        DeferWorkEvent(envelope);
                        return;
                    }

                    if (IsDuplicateTerminalEvent(envelope))
                    {
                        return;
                    }

                    _ = _commandLinkService.Enqueue(new PetCommandRequest(
                        envelope.TraceId,
                        PetCommandType.WorkCompleted,
                        PetCommandSource.Gateway,
                        forceWake: false,
                        priority: 150,
                        targetType: PetWorkTargetType.WorkDesk,
                        message: envelope.Message));
                    break;
                case GatewayEventType.WorkFailed:
                case GatewayEventType.Error:
                    if (!_acceptedWorkTraceIds.Contains(envelope.TraceId))
                    {
                        DeferWorkEvent(envelope);
                        return;
                    }

                    if (IsDuplicateTerminalEvent(envelope))
                    {
                        return;
                    }

                    _ = _commandLinkService.Enqueue(new PetCommandRequest(
                        envelope.TraceId,
                        PetCommandType.WorkFailed,
                        PetCommandSource.Gateway,
                        forceWake: false,
                        priority: 150,
                        targetType: PetWorkTargetType.WorkDesk,
                        message: string.IsNullOrWhiteSpace(envelope.Message) ? "Gateway reported error." : envelope.Message));
                    _eventBus.Publish(new GatewayErrorEvent(envelope.TraceId, envelope.Message, envelope.ErrorCode));
                    break;
                case GatewayEventType.WorkStart:
                    if (IsDuplicateTerminalEvent(envelope))
                    {
                        return;
                    }

                    _eventBus.Publish(new GatewayWorkStartEvent(envelope.TraceId, envelope.Message));
                    break;
                default:
                    Debug.Log($"[GatewayEventRouter] Ignored event: {envelope.EventType}");
                    break;
            }
        }

        private void OnPetWorkStarted(PetWorkStartedEvent payload)
        {
            _ = SendAckAsync(payload.TraceId, "work_started", payload.FurnitureId);
        }

        private void OnPetWorkCompleted(PetWorkCompletedEvent payload)
        {
            RemoveAcceptedTrace(payload.TraceId);
            _ = SendAckAsync(payload.TraceId, "work_completed", payload.Message);
        }

        private void OnPetWorkFailed(PetWorkFailedEvent payload)
        {
            RemoveAcceptedTrace(payload.TraceId);
            _ = SendAckAsync(payload.TraceId, "work_failed", payload.Reason);
        }

        private async Task SendAckAsync(string traceId, string status, string message)
        {
            GatewayRequest ackRequest = new()
            {
                TraceId = traceId,
                RequestType = GatewayRequestType.Ack,
                IsAck = true,
                Message = status,
                ContentJson = JsonUtility.ToJson(new AckPayload
                {
                    status = status,
                    detail = message
                })
            };

            GatewaySendResult result = await _gatewayClient.SendAsync(ackRequest).ConfigureAwait(false);
            if (result.Success)
            {
                _gatewayClient.MarkAcked($"{traceId}:{status}");
                return;
            }

            Debug.LogWarning($"[GatewayEventRouter] ACK failed: traceId={traceId}, reason={result.ErrorKind}:{result.ErrorMessage}");
        }

        private void OnPetCommandAccepted(PetCommandAcceptedEvent payload)
        {
            if (payload.CommandType != PetCommandType.WorkRequest)
            {
                return;
            }

            AddAcceptedTrace(payload.TraceId);
            ReplayDeferredWorkEvents(payload.TraceId);
        }

        private bool IsDuplicateTerminalEvent(GatewayEventEnvelope envelope)
        {
            if (envelope.EventType == GatewayEventType.ChatChunk)
            {
                return false;
            }

            string eventKey = $"{envelope.TraceId}:{envelope.EventType}:{envelope.Message}:{envelope.ErrorCode}";
            if (_processedTerminalEventKeys.Add(eventKey))
            {
                _processedTerminalEventOrder.Enqueue(eventKey);
                while (_processedTerminalEventOrder.Count > MaxProcessedTerminalEvents)
                {
                    string expired = _processedTerminalEventOrder.Dequeue();
                    _processedTerminalEventKeys.Remove(expired);
                }

                return false;
            }

            return true;
        }

        private void AddAcceptedTrace(string traceId)
        {
            if (_acceptedWorkTraceIds.Add(traceId))
            {
                _acceptedWorkTraceOrder.Enqueue(traceId);
            }

            while (_acceptedWorkTraceOrder.Count > MaxAcceptedTraceIds)
            {
                string expired = _acceptedWorkTraceOrder.Dequeue();
                _acceptedWorkTraceIds.Remove(expired);
                _deferredWorkEventsByTrace.Remove(expired);
            }
        }

        private void RemoveAcceptedTrace(string traceId)
        {
            _acceptedWorkTraceIds.Remove(traceId);
            _deferredWorkEventsByTrace.Remove(traceId);
        }

        private void DeferWorkEvent(GatewayEventEnvelope envelope)
        {
            if (!_deferredWorkEventsByTrace.TryGetValue(envelope.TraceId, out List<GatewayEventEnvelope>? events))
            {
                events = new List<GatewayEventEnvelope>();
                _deferredWorkEventsByTrace[envelope.TraceId] = events;
                _deferredTraceOrder.Enqueue(envelope.TraceId);
            }

            if (events.Count >= MaxDeferredEventsPerTrace)
            {
                events.RemoveAt(0);
            }
            events.Add(envelope);

            while (_deferredWorkEventsByTrace.Count > MaxDeferredTraceCount && _deferredTraceOrder.Count > 0)
            {
                string expired = _deferredTraceOrder.Dequeue();
                _deferredWorkEventsByTrace.Remove(expired);
            }
        }

        private void ReplayDeferredWorkEvents(string traceId)
        {
            if (!_deferredWorkEventsByTrace.TryGetValue(traceId, out List<GatewayEventEnvelope>? events))
            {
                return;
            }

            _deferredWorkEventsByTrace.Remove(traceId);
            for (int i = 0; i < events.Count; i++)
            {
                HandleEventOnMainThread(events[i]);
            }
        }

        [Serializable]
        private sealed class AckPayload
        {
            public string status = string.Empty;
            public string detail = string.Empty;
        }
    }
}
