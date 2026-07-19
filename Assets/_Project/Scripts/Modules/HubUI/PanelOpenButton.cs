#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 挂到 Button 所在 GameObject 上，点击时打开指定 PanelId。
    /// 若 IUIRouter 尚未注册则自动创建。
    /// </summary>
    public sealed class PanelOpenButton : MonoBehaviour
    {
        [SerializeField] private PanelId _panelId;

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            var router = ResolveOrCreateRouter();
            router?.CloseAll();
            router?.Open(_panelId);
        }

        private static IUIRouter? ResolveOrCreateRouter()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router))
                return router;

            var eventBus = new EventBus();
            ServiceLocator.Register(eventBus);
            router = new UIRouter(eventBus);
            ServiceLocator.Register<IUIRouter>(router);
            return router;
        }
    }
}
