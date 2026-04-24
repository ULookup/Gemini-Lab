#nullable enable
using System;
using System.Text;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;

namespace GeminiLab.Modules.UI.ViewModels
{
    /// <summary>
    /// Maintains chat stream state for UI views.
    /// </summary>
    public sealed class ChatViewModel : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly StringBuilder _buffer = new();
        private readonly IDisposable _chunkSub;
        private readonly IDisposable _doneSub;
        private readonly IDisposable _errorSub;

        public ChatViewModel(EventBus eventBus)
        {
            _eventBus = eventBus;
            _chunkSub = _eventBus.Subscribe<GatewayChatChunkEvent>(OnChunk);
            _doneSub = _eventBus.Subscribe<GatewayChatDoneEvent>(OnDone);
            _errorSub = _eventBus.Subscribe<GatewayErrorEvent>(OnError);
        }

        public string CurrentTraceId { get; private set; } = string.Empty;

        public string StreamingText => _buffer.ToString();

        public string LastCompletedSummary { get; private set; } = string.Empty;

        public string LastError { get; private set; } = string.Empty;

        public event Action? Changed;

        public void Dispose()
        {
            _chunkSub.Dispose();
            _doneSub.Dispose();
            _errorSub.Dispose();
        }

        private void OnChunk(GatewayChatChunkEvent payload)
        {
            if (!string.Equals(CurrentTraceId, payload.TraceId, StringComparison.Ordinal))
            {
                CurrentTraceId = payload.TraceId;
                _buffer.Clear();
            }

            _buffer.Append(payload.Content);
            Changed?.Invoke();
        }

        private void OnDone(GatewayChatDoneEvent payload)
        {
            CurrentTraceId = payload.TraceId;
            LastCompletedSummary = payload.Summary;
            Changed?.Invoke();
        }

        private void OnError(GatewayErrorEvent payload)
        {
            CurrentTraceId = payload.TraceId;
            LastError = payload.Message;
            Changed?.Invoke();
        }
    }
}
