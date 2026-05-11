#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.Inventory;
using UnityEngine;

namespace GeminiLab.Modules.Garden
{
    /// <summary>
    /// Boot.BootstrapRoot 上挂一个本组件，Inspector 拖入 SeedCatalogSO。
    /// Awake 创建 <see cref="GardenService"/> 并注册到 ServiceLocator + Registry。
    /// </summary>
    public sealed class GardenRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private SeedCatalogSO? _seedCatalog;

        private GardenService? _service;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_seedCatalog == null)
            {
                Debug.LogError("[GardenBootstrap] 未绑定 SeedCatalogSO，花园服务无法初始化");
                return;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                Debug.LogError("[GardenBootstrap] IGameClock 未注册");
                return;
            }

            if (!ServiceLocator.TryResolve(out IInventoryService? inventory) || inventory is null)
            {
                Debug.LogError("[GardenBootstrap] IInventoryService 未注册，请先挂 InventoryRuntimeBootstrap");
                return;
            }

            ServiceLocator.TryResolve(out ICollectionService? collection);
            ServiceLocator.TryResolve(out EventBus? eventBus);

            _service = new GardenService(clock, inventory, _seedCatalog, collection, eventBus);
            ServiceLocator.Register<IGardenService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_service);
            }

            Debug.Log("[GardenBootstrap] GardenService registered.");
        }

        private void Update()
        {
            _service?.Refresh();
        }
    }
}
