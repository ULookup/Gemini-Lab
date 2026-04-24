#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.UI.ViewModels;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// UI adapter for chat panel; binds view model and gateway message service.
    /// </summary>
    public sealed class ChatPanelController : MonoBehaviour
    {
        private ChatViewModel? _viewModel;
        private IGatewayMessageService? _gatewayMessageService;

        public string LastRenderedText { get; private set; } = string.Empty;

        public string LastError { get; private set; } = string.Empty;

        private void Awake()
        {
            EventBus eventBus = ServiceLocator.TryResolve(out EventBus? resolved) && resolved is not null
                ? resolved
                : new EventBus();
            if (!ServiceLocator.TryResolve(out EventBus? _))
            {
                ServiceLocator.Register(eventBus);
            }

            _viewModel = new ChatViewModel(eventBus);
            _viewModel.Changed += RefreshFromViewModel;
            ServiceLocator.TryResolve(out _gatewayMessageService);
        }

        private void OnDestroy()
        {
            if (_viewModel is not null)
            {
                _viewModel.Changed -= RefreshFromViewModel;
                _viewModel.Dispose();
            }
        }

        public void SubmitPlayerMessage(string playerId, string message, bool forceWake = false)
        {
            if (_gatewayMessageService is null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _ = _gatewayMessageService.HandlePlayerMessageAsync(playerId, message, forceWake);
        }

        private void RefreshFromViewModel()
        {
            if (_viewModel is null)
            {
                return;
            }

            LastRenderedText = string.IsNullOrWhiteSpace(_viewModel.StreamingText)
                ? _viewModel.LastCompletedSummary
                : _viewModel.StreamingText;
            LastError = _viewModel.LastError;
        }
    }
}
