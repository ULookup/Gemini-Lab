#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// 请求弹出一条 Toast；由表现层订阅。
    /// 业务代码发布此事件即可，无需直接依赖具体 Toast 实现。
    /// </summary>
    public readonly struct ToastRequestedEvent
    {
        public ToastRequestedEvent(string message, ToastKind kind, float durationSeconds)
        {
            Message = message;
            Kind = kind;
            DurationSeconds = durationSeconds;
        }

        public string Message { get; }
        public ToastKind Kind { get; }
        public float DurationSeconds { get; }
    }
}
