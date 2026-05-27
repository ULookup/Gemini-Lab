#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public abstract class StubPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;
        [SerializeField] private Button? _closeButton;

        private IUIRouter? _router;
        private bool _registered;
        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content is not null)
            {
                _content.SetActive(false);
            }

            TryRegister();

            if (_closeButton is not null)
            {
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        protected virtual void OnEnable()
        {
            TryRegister();
        }

        private void TryRegister()
        {
            if (_registered) return;
            if (!ServiceLocator.TryResolve(out IUIRouter? router) || router is null) return;
            _router = router;
            _router.Register(this);
            _registered = true;
        }

        protected virtual void OnDestroy()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router))
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
    }
}
