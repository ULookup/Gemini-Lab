#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class ChatInputHandler : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField = null!;
        [SerializeField] private GameObject _typingIndicator = null!;
        [SerializeField] private float _typingTimeoutSeconds = 15f;

        public event Action<string>? OnSubmitMessage;

        private bool _isWaitingReply;
        private Coroutine? _timeoutRoutine;

        private void Awake()
        {
            _inputField.onSubmit.AddListener(HandleSubmit);
            _typingIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            _inputField.onSubmit.RemoveListener(HandleSubmit);
            if (_timeoutRoutine != null) StopCoroutine(_timeoutRoutine);
        }

        private void HandleSubmit(string text)
        {
            if (_isWaitingReply) return;

            string trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            _inputField.text = string.Empty;
            SetWaitingState(true);
            OnSubmitMessage?.Invoke(trimmed);
        }

        public void SetWaitingState(bool waiting)
        {
            _isWaitingReply = waiting;
            _inputField.interactable = !waiting;
            _typingIndicator.SetActive(waiting);

            if (_timeoutRoutine != null)
            {
                StopCoroutine(_timeoutRoutine);
                _timeoutRoutine = null;
            }

            if (waiting)
            {
                _timeoutRoutine = StartCoroutine(TypingTimeoutRoutine());
            }
        }

        private IEnumerator TypingTimeoutRoutine()
        {
            yield return new WaitForSeconds(_typingTimeoutSeconds);
            if (_isWaitingReply)
            {
                Debug.LogWarning("[PhoneChat] Typing timeout reached, resetting input");
                SetWaitingState(false);
            }
        }

        public void Clear()
        {
            _inputField.text = string.Empty;
        }

        public void Focus()
        {
            _inputField.Select();
            _inputField.ActivateInputField();
        }
    }
}
