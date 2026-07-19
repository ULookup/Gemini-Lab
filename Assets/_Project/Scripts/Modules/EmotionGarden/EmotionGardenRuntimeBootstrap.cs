#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.EmotionGarden
{
    /// <summary>
    /// 情绪花园引导器。挂载到 BootstrapRoot，在 Awake 时创建 EmotionGardenService 并注册。
    /// </summary>
    public sealed class EmotionGardenRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private float _refreshIntervalSeconds = 1f;

        private EmotionGardenService? _service;
        private float _refreshTimer;

        private void Awake()
        {
            if (!ServiceLocator.TryResolve(out IGameClock? clock))
            {
                Debug.LogError("[EmotionGardenBootstrap] IGameClock 未注册");
                return;
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                Debug.LogError("[EmotionGardenBootstrap] EventBus 未注册");
                return;
            }

            var go = new GameObject("EmotionGardenService");
            go.transform.SetParent(transform, false);
            DontDestroyOnLoad(go);

            _service = go.AddComponent<EmotionGardenService>();
            _service.Initialize(clock, eventBus);

            ServiceLocator.Register<IEmotionGardenService>(_service);

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry))
            {
                registry.Register((IPersistentService)_service);
            }

            Debug.Log("[EmotionGardenBootstrap] EmotionGardenService 已注册");
        }

        private void Update()
        {
            if (_service == null) return;

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < _refreshIntervalSeconds) return;
            _refreshTimer = 0f;

            _service.RefreshBlooming();
        }
    }
}
