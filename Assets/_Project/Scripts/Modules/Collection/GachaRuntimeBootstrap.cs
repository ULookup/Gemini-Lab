#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 挂 Boot.BootstrapRoot。Start 时注册 CoinService 和 GachaService
    /// （延迟到 Start 确保 CollectionRuntimeBootstrap.Awake 已注册 ICollectionService）。
    /// </summary>
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

            if (!ServiceLocator.TryResolve(out ICollectionService? collection) || collection is null)
            {
                Debug.LogError("[GachaBootstrap] ICollectionService 未注册，请先挂 CollectionRuntimeBootstrap");
                return;
            }

            _coinService = new CoinService(_eventBus);
            ServiceLocator.Register<ICoinService>(_coinService);

            _gachaService = new GachaService(_coinService, collection, _eventBus);
            ServiceLocator.Register<IGachaService>(_gachaService);

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
