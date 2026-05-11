#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 占位面板基类：负责 Register/Unregister 到 UIRouter、统一开/关显示。
    /// 真实内容由具体子类在美术资源到位后填充。
    /// </summary>
    public abstract class StubPanelBase : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GameObject? _content;

        public abstract PanelId Id { get; }

        protected virtual void Awake()
        {
            if (_content is not null)
            {
                _content.SetActive(false);
            }

            if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                router!.Register(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router))
            {
                router!.Unregister(Id);
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
    }
}
