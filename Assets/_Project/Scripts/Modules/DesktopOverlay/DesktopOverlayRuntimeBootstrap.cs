#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    public static class DesktopOverlayRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureDesktopServices()
        {
            if (Object.FindFirstObjectByType<DesktopOverlayManager>() is not null)
            {
                return;
            }

            GameObject host = new("DesktopOverlaySystem");
            Object.DontDestroyOnLoad(host);
            _ = host.AddComponent<DesktopOverlayManager>();
            _ = host.AddComponent<ForegroundWindowProbe>();
            _ = host.AddComponent<AppContextRuleEngine>();
        }
    }
}
