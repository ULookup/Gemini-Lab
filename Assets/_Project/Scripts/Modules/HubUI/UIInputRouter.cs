#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 把 ESC 键映射到 <see cref="IUIRouter.CloseTop"/>。
    /// 挂 Boot.BootstrapRoot，DontDestroyOnLoad。
    /// </summary>
    public sealed class UIInputRouter : MonoBehaviour
    {
        [SerializeField] private KeyCode _closeTopKey = KeyCode.Escape;

        private IUIRouter? _router;

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            ServiceLocator.TryResolve(out _router);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(_closeTopKey))
            {
                return;
            }

            if (_router is null && !ServiceLocator.TryResolve(out _router))
            {
                return;
            }

            if (_router!.Top.HasValue)
            {
                _router.CloseTop();
            }
        }
    }
}
