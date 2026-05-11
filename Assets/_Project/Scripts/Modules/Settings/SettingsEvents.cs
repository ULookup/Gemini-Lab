#nullable enable

namespace GeminiLab.Modules.Settings
{
    /// <summary>
    /// 全局设置变更事件。订阅者按需读取 <see cref="Snapshot"/> 做对应调整：
    /// AudioMixer 改音量 / 全屏切换 / Overlay 切换 / 语言切换等。
    /// </summary>
    public readonly struct SettingsChangedEvent
    {
        public SettingsChangedEvent(GameSettings snapshot) { Snapshot = snapshot; }
        public GameSettings Snapshot { get; }
    }
}
