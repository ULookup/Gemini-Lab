#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// 在场景加载完成后补齐 SaveSystem + SaveCoordinator。
    /// 用 AfterSceneLoad 是为了等其他业务 Bootstrap（Settings / Inventory / Collection / Tarot）
    /// 完成自身 <see cref="IPersistentServiceRegistry"/> 注册；Coordinator 只在调用时实时查 Registry，
    /// 所以顺序只影响"第一次 List / Save"能看到哪些服务。
    /// </summary>
    public static class PersistenceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (!ServiceLocator.TryResolve(out ISaveSystem? saveSystem) || saveSystem is null)
            {
                saveSystem = new SaveSystem();
                ServiceLocator.Register(saveSystem);
                Debug.Log("[PersistenceBootstrap] SaveSystem registered.");
            }

            if (ServiceLocator.TryResolve(out ISaveCoordinator? _))
            {
                return;
            }

            if (!ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) || registry is null)
            {
                Debug.LogError("[PersistenceBootstrap] IPersistentServiceRegistry 未注册，Coordinator 无法初始化");
                return;
            }

            if (!ServiceLocator.TryResolve(out IGameClock? clock) || clock is null)
            {
                Debug.LogError("[PersistenceBootstrap] IGameClock 未注册，Coordinator 无法初始化");
                return;
            }

            ServiceLocator.TryResolve(out EventBus? eventBus);

            ServiceLocator.Register<ISaveCoordinator>(new SaveCoordinator(saveSystem, registry, clock, eventBus));
            Debug.Log("[PersistenceBootstrap] SaveCoordinator registered.");
        }
    }
}
