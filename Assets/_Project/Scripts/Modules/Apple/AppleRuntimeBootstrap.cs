#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Apple
{
    /// <summary>Boot.BootstrapRoot 上的苹果资源宿主。</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class AppleRuntimeBootstrap : MonoBehaviour
    {
        private AppleService? _service;

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                Debug.LogError("[AppleBootstrap] IGameClock 未注册，苹果服务无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);
            _service = new AppleService(clock, eventBus);
            ServiceLocator.Register<IAppleService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null)
            {
                registry.Register(_service);
            }

            Debug.Log($"[AppleBootstrap] AppleService 已注册，初始余额 {_service.Balance}");
        }
    }
}
