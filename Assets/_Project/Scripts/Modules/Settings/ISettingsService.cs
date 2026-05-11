#nullable enable

namespace GeminiLab.Modules.Settings
{
    /// <summary>
    /// 设置服务门面。读写通过 struct snapshot；任何修改都会通过 EventBus 广播
    /// <see cref="SettingsChangedEvent"/>。
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>当前设置快照（已 Clamp）。</summary>
        GameSettings Current { get; }

        /// <summary>覆盖全部字段并持久化。</summary>
        void Apply(GameSettings newSettings);

        /// <summary>重置为默认并持久化。</summary>
        void ResetToDefault();
    }
}
