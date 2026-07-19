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
                // 开发者模式与玩家模式的存档完全隔离：
                // 调试数据（时钟快进产生的未来日期等）不会污染真实进度，反之亦然
                string saveRoot = System.IO.Path.Combine(
                    Application.persistentDataPath, DevMode.Active ? "Saves-Dev" : "Saves");
                saveSystem = new SaveSystem(saveRootPath: saveRoot);
                ServiceLocator.Register(saveSystem);
                Debug.Log($"[PersistenceBootstrap] SaveSystem registered. 存档目录: {(DevMode.Active ? "Saves-Dev (开发者)" : "Saves (玩家)")}");
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

            // 创建自动存档/读档管理器
            var autoSaveGo = new GameObject("AutoSaveManager");
            Object.DontDestroyOnLoad(autoSaveGo);
            autoSaveGo.AddComponent<AutoSaveManager>();
            Debug.Log("[PersistenceBootstrap] AutoSaveManager created.");
        }
    }
}
