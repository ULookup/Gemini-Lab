#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Registers and hosts the runtime furniture service.
    /// </summary>
    public static class FurnitureBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureService()
        {
            if (ServiceLocator.TryResolve(out IFurnitureService? _))
            {
                return;
            }

            FurnitureService? existing = Object.FindFirstObjectByType<FurnitureService>();
            if (existing is null)
            {
                GameObject host = new(nameof(FurnitureService));
                Object.DontDestroyOnLoad(host);
                existing = host.AddComponent<FurnitureService>();
                host.AddComponent<BuildModeController>();
                host.AddComponent<FurnitureLayoutPersistence>();
            }

            ServiceLocator.Register<IFurnitureService>(existing);
            Debug.Log("[FurnitureBootstrap] FurnitureService registered.");
        }
    }
}
