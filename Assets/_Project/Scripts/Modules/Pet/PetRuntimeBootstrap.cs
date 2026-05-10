#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 保证 IPetRoster 已经注册；在完全空场景下兜底创建一只 Angel 占位宠物。
    /// </summary>
    public static class PetRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRoster()
        {
            if (!ServiceLocator.TryResolve(out IPetRoster? _))
            {
                ServiceLocator.Register<IPetRoster>(new PetRoster());
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePetHost()
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
