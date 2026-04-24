#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Travel
{
    public static class TravelRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureBootstrap()
        {
            if (Object.FindFirstObjectByType<TravelBootstrap>() is not null)
            {
                return;
            }

            GameObject host = new("TravelBootstrap");
            Object.DontDestroyOnLoad(host);
            _ = host.AddComponent<TravelBootstrap>();
        }
    }
}
