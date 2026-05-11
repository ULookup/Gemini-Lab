#nullable enable
using System;

namespace GeminiLab.Modules.Settings
{
    /// <summary>
    /// 全局游戏设置快照。值域均已规范化（音量 0..1、语言 iso code）。
    /// </summary>
    [Serializable]
    public struct GameSettings
    {
        public float MasterVolume;
        public float BgmVolume;
        public float SfxVolume;
        public bool Fullscreen;
        public bool DesktopOverlayEnabled;
        public string LanguageIso;

        public static GameSettings Default => new()
        {
            MasterVolume = 1f,
            BgmVolume = 0.8f,
            SfxVolume = 0.9f,
            Fullscreen = true,
            DesktopOverlayEnabled = true,
            LanguageIso = "zh-CN"
        };

        public GameSettings Clamp()
        {
            return new GameSettings
            {
                MasterVolume = UnityEngine.Mathf.Clamp01(MasterVolume),
                BgmVolume = UnityEngine.Mathf.Clamp01(BgmVolume),
                SfxVolume = UnityEngine.Mathf.Clamp01(SfxVolume),
                Fullscreen = Fullscreen,
                DesktopOverlayEnabled = DesktopOverlayEnabled,
                LanguageIso = string.IsNullOrWhiteSpace(LanguageIso) ? "zh-CN" : LanguageIso
            };
        }
    }
}
