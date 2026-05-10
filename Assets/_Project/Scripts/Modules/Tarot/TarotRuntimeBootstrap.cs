#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_deck == null)
            {
                Debug.LogError("[TarotBootstrap] 未绑定 TarotDeckSO，塔罗服务无法初始化");
                return;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus = null;
            }

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

            ServiceLocator.Register<ITarotService>(new TarotService(_deck, eventBus, backend));
            Debug.Log("[TarotBootstrap] TarotService registered.");
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
