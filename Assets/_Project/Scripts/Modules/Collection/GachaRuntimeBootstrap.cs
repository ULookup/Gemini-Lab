#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Modules.Apple;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 挂 Boot.BootstrapRoot。Start 时注册 CoinService 和 GachaService
    /// （延迟到 Start 确保 CollectionRuntimeBootstrap.Awake 已注册 ICollectionService）。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GachaRuntimeBootstrap : MonoBehaviour
    {
        private EventBus? _eventBus;
        private CoinService? _coinService;
        private GachaService? _gachaService;

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ServiceLocator.TryResolve(out _eventBus);

            if (!ServiceLocator.TryResolve(out IAppleService? apple) || apple is null)
            {
                Debug.LogError("[GachaBootstrap] IAppleService 未注册，请先挂 AppleRuntimeBootstrap");
                return;
            }

            if (!ServiceLocator.TryResolve(out ICollectionService? collection) || collection is null)
            {
                Debug.LogError("[GachaBootstrap] ICollectionService 未注册，请先挂 CollectionRuntimeBootstrap");
                return;
            }

            _coinService = new CoinService(_eventBus);
            ServiceLocator.Register<ICoinService>(_coinService);

            _gachaService = new GachaService(apple, _coinService, collection, _eventBus);
            ServiceLocator.Register<IGachaService>(_gachaService);

            // 广播初始余额，确保已有的 CoinBalanceDisplay 能拿到当前余额
            _eventBus?.Publish(new CoinChangedEvent(_coinService.Balance));

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_coinService);
                registry.Register(_gachaService);
            }

            Debug.Log("[GachaBootstrap] CoinService + GachaService registered.");

            // 通过 Pet_Angel / Pet_Devil 找到 Pet 父节点并挂上 CoinDropController
            var petAngel = GameObject.Find("Pet_Angel");
            var petDevil = GameObject.Find("Pet_Devil");
            Transform? petParent = petAngel?.transform.parent ?? petDevil?.transform.parent;

            if (petParent != null && petParent.GetComponent<CoinDropController>() == null)
            {
                petParent.gameObject.AddComponent<CoinDropController>();
                Debug.Log("[GachaBootstrap] CoinDropController attached to Pet.");
            }
        }
    }
}
