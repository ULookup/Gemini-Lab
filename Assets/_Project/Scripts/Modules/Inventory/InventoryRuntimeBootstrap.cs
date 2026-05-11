#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using UnityEngine;

namespace GeminiLab.Modules.Inventory
{
    /// <summary>
    /// Boot.BootstrapRoot 上挂一个本组件，Inspector 拖入 ItemCatalogSO。
    /// Awake 时把 InventoryService 注册到 ServiceLocator。
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
            ServiceLocator.Register<IInventoryService>(new InventoryService(_catalog, eventBus));
            Debug.Log("[InventoryBootstrap] InventoryService registered.");
        }
    }
}
