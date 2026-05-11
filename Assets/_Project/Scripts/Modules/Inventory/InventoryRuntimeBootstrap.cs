#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// Boot.BootstrapRoot 上挂一个本组件，Inspector 拖入 ItemCatalogSO。
    /// Awake 时把 InventoryService 注册到 ServiceLocator + IPersistentServiceRegistry。
    /// </summary>
    public sealed class InventoryRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private ItemCatalogSO? _catalog;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (_catalog == null)
            {
                Debug.LogError("[InventoryBootstrap] 未绑定 ItemCatalogSO，物品栏服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);
            var service = new InventoryService(_catalog, eventBus);
            ServiceLocator.Register<IInventoryService>(service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(service);
            }

            Debug.Log("[InventoryBootstrap] InventoryService registered.");
        }
    }
}
