#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Settings
{
    /// <summary>
    /// 挂 Boot.BootstrapRoot，Awake 时把 SettingsService 注册到 ServiceLocator。
    /// 同时在首次 Awake 后发一次 SettingsChangedEvent，让订阅方一上来就同步到当前值。
    /// </summary>
    public sealed class SettingsRuntimeBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            ServiceLocator.TryResolve(out EventBus? eventBus);
            var service = new SettingsService(eventBus);
            ServiceLocator.Register<ISettingsService>(service);
            eventBus?.Publish(new SettingsChangedEvent(service.Current));

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(service);
            }

            Debug.Log("[SettingsBootstrap] SettingsService registered.");
        }
    }
}
