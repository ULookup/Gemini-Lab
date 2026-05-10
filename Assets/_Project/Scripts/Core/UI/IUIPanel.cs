#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// 所有可被 <see cref="IUIRouter"/> 管理的面板必须实现此接口。
    /// 真实实现通常为 MonoBehaviour，挂在面板 Prefab 根节点上。
    /// </summary>
    public interface IUIPanel
    {
        PanelId Id { get; }

        /// <summary>
        /// 面板被 Router 打开时调用。
        /// </summary>
        void OnOpen(object? payload);

        /// <summary>
        /// 面板被 Router 关闭时调用，Router 随后会将其 GameObject 隐藏或销毁。
        /// </summary>
        void OnClose();
    }
}
