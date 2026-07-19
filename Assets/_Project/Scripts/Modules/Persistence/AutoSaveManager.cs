#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// 自动存档/读档管理器。退出时自动保存到 "autosave"，启动时自动恢复。
    /// 由 PersistenceBootstrap 创建，DontDestroyOnLoad。
    /// </summary>
    public sealed class AutoSaveManager : MonoBehaviour
    {
        private const string AutoSlot = "autosave";

        private async void Start()
        {
            // 等待一帧，确保所有模块 Bootstrap 完成 IPersistentService 注册
            await System.Threading.Tasks.Task.Yield();

            if (!ServiceLocator.TryResolve(out ISaveCoordinator? coordinator) || coordinator is null)
            {
                Debug.LogWarning("[AutoSave] ISaveCoordinator 未注册，跳过自动读档");
                return;
            }

            var loaded = await coordinator.LoadAsync(AutoSlot);
            if (loaded)
                Debug.Log("[AutoSave] 自动读档成功");
            else
                Debug.Log("[AutoSave] 未找到 autosave 存档，使用全新数据");
        }

        private async void OnApplicationQuit()
        {
            if (!ServiceLocator.TryResolve(out ISaveCoordinator? coordinator) || coordinator is null)
            {
                Debug.LogWarning("[AutoSave] ISaveCoordinator 未注册，跳过自动存档");
                return;
            }

            await coordinator.SaveAsync(AutoSlot);
            Debug.Log("[AutoSave] 自动存档成功");
        }
    }
}
