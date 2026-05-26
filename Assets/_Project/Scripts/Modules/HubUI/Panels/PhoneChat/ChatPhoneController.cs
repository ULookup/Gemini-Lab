#nullable enable
using System.Collections;
using System.Threading;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public enum PhoneState
    {
        Collapsed,
        AnimatingIn,
        Open,
        AnimatingOut
    }

    public sealed class ChatPhoneController : MonoBehaviour
    {
        [SerializeField] private PhoneAnimController _animController = null!;
        [SerializeField] private ChatInputHandler _inputHandler = null!;
        [SerializeField] private ChatMessageListView _messageListView = null!;
        [SerializeField] private GameObject _collapsedButtonRoot = null!;
        [SerializeField] private GameObject _closeButton = null!;
        [SerializeField] private KeyCode _closeKey = KeyCode.Escape;

        public PhoneState CurrentState { get; private set; } = PhoneState.Collapsed;

        private CancellationTokenSource? _currentCts;

        private void Awake()
        {
            _collapsedButtonRoot.SetActive(true);
            _closeButton.SetActive(false);
            _inputHandler.OnSubmitMessage += HandleUserMessage;
        }

        private async void Start()
        {
            // Load chat history
            if (Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence))
            {
                await persistence!.LoadAsync();
                _messageListView.AddMessagesFromHistory(persistence.History);
            }
        }

        private void Update()
        {
            if (CurrentState == PhoneState.Open && Input.GetKeyDown(_closeKey))
            {
                ClosePhone();
            }
        }

        private void OnDestroy()
        {
            _inputHandler.OnSubmitMessage -= HandleUserMessage;
            _currentCts?.Cancel();
            _currentCts?.Dispose();

            // Save chat history
            if (Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence))
            {
                _ = persistence!.SaveAsync();
            }
        }

        public void OnCollapsedButtonClicked()
        {
            if (CurrentState != PhoneState.Collapsed) return;
            StartCoroutine(OpenRoutine());
        }

        public void ClosePhone()
        {
            if (CurrentState != PhoneState.Open) return;
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            CurrentState = PhoneState.AnimatingIn;
            _collapsedButtonRoot.SetActive(false);
            _closeButton.SetActive(true);
            yield return _animController.PlayOpenAnim();
            CurrentState = PhoneState.Open;
            _inputHandler.Focus();
        }

        private IEnumerator CloseRoutine()
        {
            CurrentState = PhoneState.AnimatingOut;
            _closeButton.SetActive(false);
            yield return _animController.PlayCloseAnim();
            _collapsedButtonRoot.SetActive(true);
            CurrentState = PhoneState.Collapsed;
        }

        private async void HandleUserMessage(string text)
        {
            if (!Core.ServiceLocator.TryResolve<IPetChatService>(out var chatService)) return;
            if (!Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence)) return;

            // Add user message bubble
            var userMsg = new ChatMessage(ChatRole.User, text);
            persistence.AddMessage(userMsg);
            _messageListView.AddBubble(ChatRole.User, text);

            // Call LLM — capture CTS locally to avoid stale field race
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            var cts = new CancellationTokenSource();
            _currentCts = cts;

            PetChatResult result;
            try
            {
                result = await chatService!.SendMessageAsync(text, persistence.History, cts.Token);
            }
            catch (OperationCanceledException)
            {
                _inputHandler.SetWaitingState(false);
                return;
            }

            _inputHandler.SetWaitingState(false);

            if (cts.IsCancellationRequested) return;

            if (result.IsCancelled)
            {
                Debug.Log("[PhoneChat] Request was cancelled");
                return;
            }

            // Add pet reply bubbles (respecting random order from PetChatService)
            if (!string.IsNullOrEmpty(result.AngelReply))
            {
                var angelMsg = new ChatMessage(ChatRole.Angel, result.AngelReply);
                persistence.AddMessage(angelMsg);
                _messageListView.AddBubble(ChatRole.Angel, result.AngelReply);
            }

            if (!string.IsNullOrEmpty(result.DevilReply))
            {
                var devilMsg = new ChatMessage(ChatRole.Devil, result.DevilReply);
                persistence.AddMessage(devilMsg);
                _messageListView.AddBubble(ChatRole.Devil, result.DevilReply);
            }

            _ = persistence.SaveAsync();
        }
    }
}
