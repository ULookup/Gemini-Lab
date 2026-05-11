#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Gateway;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗系统运行态宿主。挂在 Boot.unity 的 BootstrapRoot 上；DontDestroyOnLoad。
    /// 在 Inspector 拖入 `TarotDeckSO`；Awake 时注册 <see cref="ITarotService"/> 到 ServiceLocator。
    /// Gateway 已注册则使用 <see cref="GatewayTarotBackend"/>，否则回退到 <see cref="FallbackOnlyBackend"/>（全本地解读）。
    /// </summary>
    public sealed class TarotRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private TarotDeckSO? _deck;

        private IDisposable? _drawnSub;
        private EventBus? _eventBus;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_deck == null)
            {
                Debug.LogError("[TarotBootstrap] 未绑定 TarotDeckSO，塔罗服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out _eventBus);

            ITarotReadingBackend backend;
            if (ServiceLocator.TryResolve(out IGatewayClient? client) && client is not null)
            {
                backend = new GatewayTarotBackend(client);
            }
            else
            {
                backend = new FallbackOnlyBackend();
                Debug.Log("[TarotBootstrap] 未发现 IGatewayClient，塔罗解读走本地 fallback");
            }

            ServiceLocator.Register<ITarotService>(new TarotService(_deck, _eventBus, backend));
            Debug.Log("[TarotBootstrap] TarotService registered.");

            if (_eventBus is not null)
            {
                _drawnSub = _eventBus.Subscribe<TarotDrawnEvent>(OnTarotDrawn);
            }
        }

        private void OnDestroy()
        {
            _drawnSub?.Dispose();
        }

        private void OnTarotDrawn(TarotDrawnEvent evt)
        {
            if (_eventBus is null)
            {
                return;
            }

            string orientZh = evt.Result.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            string msg = $"今日塔罗：{evt.Result.Card.DisplayNameZh} · {orientZh}";
            _eventBus.Publish(new ToastRequestedEvent(msg, ToastKind.Success, 0f));
        }

        private sealed class FallbackOnlyBackend : ITarotReadingBackend
        {
            public System.Threading.Tasks.Task<TarotReading> RequestAsync(
                TarotDrawResult draw,
                GeminiLab.Modules.Pet.PetId petId,
                TarotOrientation orientation,
                System.Threading.CancellationToken cancellationToken)
            {
                return System.Threading.Tasks.Task.FromResult(LocalFallback.Build(draw, petId, orientation));
            }
        }
    }
}
