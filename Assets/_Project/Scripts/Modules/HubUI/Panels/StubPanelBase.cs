#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public abstract class StubPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;
        [SerializeField] protected Button? _closeButton;

        private IUIRouter? _router;
        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content is not null)
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
        }

        public virtual void OnClose()
        {
            if (_content is not null)
            {
                _content.SetActive(false);
            }
        }

        protected void CloseSelf()
        {
            _router?.Close(Id);
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
