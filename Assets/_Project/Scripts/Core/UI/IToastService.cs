#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// Toast 通知服务的门面。
    /// 实现层可直接挂 MonoBehaviour 或通过 EventBus 广播 <see cref="ToastRequestedEvent"/>。
    /// </summary>
    public interface IToastService
    {
        /// <summary>弹出一条通知。<paramref name="durationSeconds"/> &lt;=0 使用默认时长。</summary>
        void Show(string message, ToastKind kind = ToastKind.Info, float durationSeconds = 0f);
    }
}
