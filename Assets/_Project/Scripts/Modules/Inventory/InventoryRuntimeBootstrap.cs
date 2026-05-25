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

        [Tooltip("新存档首次启动时自动注入的初始种子。Inventory 首次为空时才触发。")]
        [SerializeField] private StarterItem[] _starterItems = System.Array.Empty<StarterItem>();

        private InventoryService? _service;

        [System.Serializable]
        public struct StarterItem
        {
            public string ItemId;
            [Min(1)] public int Count;
        }

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (_catalog == null)
            {
                Debug.LogError("[InventoryBootstrap] 未绑定 ItemCatalogSO，物品栏服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);
            _service = new InventoryService(_catalog, eventBus);
            ServiceLocator.Register<IInventoryService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_service);
            }

            Debug.Log("[InventoryBootstrap] InventoryService registered.");
        }

        private void Start()
        {
            // 首次启动（存档恢复前 Inventory 仍为空）注入初始礼包。
            // 若后续 SaveCoordinator.LoadAsync 覆盖了 stacks，这批种子会被替换成真实存档，不会叠加。
            if (_service == null || _starterItems == null || _starterItems.Length == 0) return;
            if (_service.GetAllStacks().Count > 0) return;

            foreach (var s in _starterItems)
            {
                if (string.IsNullOrEmpty(s.ItemId) || s.Count <= 0) continue;
                _service.Add(s.ItemId, s.Count);
            }
        }
    }
}
