#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Apple;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public abstract class StubPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;
        [SerializeField] protected Button? _closeButton;
        [SerializeField] protected TMP_Text? _balanceText;

        private IUIRouter? _router;
        private IAppleService? _apple;
        private EventBus? _eventBus;
        private IDisposable? _appleChangedSub;
        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content is not null && Application.isPlaying)
            {
                _content.SetActive(false);
            }

            _router = ResolveOrCreateRouter();
            _router.Register(this);

            if (_closeButton is not null)
            {
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        protected virtual void OnDestroy()
        {
            _appleChangedSub?.Dispose();
            if (_router is not null)
            {
                _router.Unregister(Id);
            }
            else if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                router.Unregister(Id);
            }
        }

        public virtual void OnOpen(object? payload)
        {
            if (_content is not null)
            {
                _content.SetActive(true);
            }

            EnsureAppleService();
            RefreshBalance();
        }

        public virtual void OnClose()
        {
            _appleChangedSub?.Dispose();
            _appleChangedSub = null;

            if (_content is not null)
            {
                _content.SetActive(false);
            }
        }

        protected void CloseSelf()
        {
            _router?.Close(Id);
        }

        private void EnsureAppleService()
        {
            if (_eventBus == null) ServiceLocator.TryResolve(out _eventBus);
            if (_apple == null) ServiceLocator.TryResolve(out _apple);

            if (_eventBus != null && _appleChangedSub == null)
            {
                _appleChangedSub = _eventBus.Subscribe<AppleChangedEvent>(_ => RefreshBalance());
            }
        }

        private void RefreshBalance()
        {
            if (_apple == null) ServiceLocator.TryResolve(out _apple);
            if (_apple == null || _balanceText == null) return;
            _balanceText.text = $"{_apple.Balance}";
        }

        private static IUIRouter ResolveOrCreateRouter()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
            {
                return router;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus) || eventBus is null)
            {
                eventBus = new EventBus();
                ServiceLocator.Register(eventBus);
            }

            router = new UIRouter(eventBus);
            ServiceLocator.Register<IUIRouter>(router);
            return router;
        }
    }
}
