#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 在第一次场景加载后保证：
    /// - <see cref="IPetRoster"/> 已注册（默认 <see cref="PetRoster"/>）
    /// - <see cref="PetRuntimeSaveService"/> 已挂进 <see cref="IPersistentServiceRegistry"/>，让双宠运行态随 SaveBundle 走
    /// - 完全空场景下兜底生成一个 PetPlaceholder，方便脚本验证
    /// </summary>
    public static class PetRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePetServices()
        {
            bool rosterWasMissing = false;
            if (!ServiceLocator.TryResolve(out IPetRoster? roster) || roster is null)
            {
                roster = new PetRoster();
                ServiceLocator.Register(roster);
                rosterWasMissing = true;
                Debug.Log("[PetRuntimeBootstrap] PetRoster registered.");
            }

            if (rosterWasMissing)
            {
                var controllers = Object.FindObjectsByType<PetController>(FindObjectsSortMode.None);
                foreach (var ctrl in controllers)
                {
                    if (ctrl.RuntimeData != null)
                    {
                        roster!.Register(ctrl.PetId, ctrl.RuntimeData);
                        Debug.Log($"[PetRuntimeBootstrap] Late-registered {ctrl.PetId} (missed roster during Awake)");
                    }
                }
            }

            if (ServiceLocator.TryResolve(out IPersistentServiceRegistry? registry) && registry is not null
                && registry.TryGet("pet_runtime") is null)
            {
                registry.Register(new PetRuntimeSaveService(roster!));
                Debug.Log("[PetRuntimeBootstrap] PetRuntimeSaveService registered.");
            }

            EnsurePlaceholderPet();
        }

        private static void EnsurePlaceholderPet()
        {
            if (Object.FindFirstObjectByType<PetController>() is not null)
            {
                return;
            }

            GameObject pet = new("PetPlaceholder");
            pet.transform.position = Vector3.zero;
            pet.AddComponent<PetController>();
        }
    }
}
