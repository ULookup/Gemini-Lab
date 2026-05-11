#nullable enable
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Modules.Settings
{
    /// <summary>
    /// <see cref="ISettingsService"/> 默认实现。
    /// 当前阶段用 PlayerPrefs 持久化；C1 存档整合后可把存档载体改为 SaveSlot 全局块，
    /// 但 IPersistentService 接口已经实现好，SaveSystem 启用后可直接扫描。
    /// </summary>
    public sealed class SettingsService : ISettingsService, IPersistentService
    {
        private const string MasterKey = "GeminiLab.Settings.MasterVolume";
        private const string BgmKey = "GeminiLab.Settings.BgmVolume";
        private const string SfxKey = "GeminiLab.Settings.SfxVolume";
        private const string FullscreenKey = "GeminiLab.Settings.Fullscreen";
        private const string OverlayKey = "GeminiLab.Settings.DesktopOverlayEnabled";
        private const string LangKey = "GeminiLab.Settings.LanguageIso";

        private readonly EventBus? _eventBus;

        public SettingsService(EventBus? eventBus)
        {
            _eventBus = eventBus;
            Current = Load();
        }

        public GameSettings Current { get; private set; }

        public string Key => "settings";

        public void Apply(GameSettings newSettings)
        {
            Current = newSettings.Clamp();
            Save(Current);
            _eventBus?.Publish(new SettingsChangedEvent(Current));
        }

        public void ResetToDefault()
        {
            Apply(GameSettings.Default);
        }

        private static GameSettings Load()
        {
            var def = GameSettings.Default;
            return new GameSettings
            {
                MasterVolume = PlayerPrefs.GetFloat(MasterKey, def.MasterVolume),
                BgmVolume = PlayerPrefs.GetFloat(BgmKey, def.BgmVolume),
                SfxVolume = PlayerPrefs.GetFloat(SfxKey, def.SfxVolume),
                Fullscreen = PlayerPrefs.GetInt(FullscreenKey, def.Fullscreen ? 1 : 0) == 1,
                DesktopOverlayEnabled = PlayerPrefs.GetInt(OverlayKey, def.DesktopOverlayEnabled ? 1 : 0) == 1,
                LanguageIso = PlayerPrefs.GetString(LangKey, def.LanguageIso)
            }.Clamp();
        }

        private static void Save(GameSettings s)
        {
            PlayerPrefs.SetFloat(MasterKey, s.MasterVolume);
            PlayerPrefs.SetFloat(BgmKey, s.BgmVolume);
            PlayerPrefs.SetFloat(SfxKey, s.SfxVolume);
            PlayerPrefs.SetInt(FullscreenKey, s.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(OverlayKey, s.DesktopOverlayEnabled ? 1 : 0);
            PlayerPrefs.SetString(LangKey, s.LanguageIso);
            PlayerPrefs.Save();
        }

        // IPersistentService 当前桥接：等 SaveSystem 整合时 Capture/Restore 会被调用。
        public string CaptureJson() => JsonUtility.ToJson(Current);

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var parsed = JsonUtility.FromJson<GameSettings>(json);
                Apply(parsed);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
