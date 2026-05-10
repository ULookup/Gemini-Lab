#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// UI 面板路由。业务/侧边栏只发意图，不直接持有面板引用。
    /// </summary>
    public interface IUIRouter
    {
        /// <summary>当前栈顶面板；栈为空时返回 null。</summary>
        PanelId? Top { get; }

        /// <summary>
        /// 注册一个已实例化的面板实例。面板首次被 Open 之前必须先 Register。
        /// 常见做法：面板 MonoBehaviour 在 Awake / Start 里向 Router 注册自身。
        /// </summary>
        void Register(IUIPanel panel);

        /// <summary>
        /// 注销面板。面板 GameObject 被销毁时调用。
        /// </summary>
        void Unregister(PanelId id);

        /// <summary>
        /// 打开指定面板。若面板已在栈内则提升到栈顶，否则激活并入栈。
        /// </summary>
        bool Open(PanelId id, object? payload = null);

        /// <summary>
        /// 关闭栈顶面板；若栈顶不等于指定 id 则不关闭。
        /// </summary>
        bool Close(PanelId id);

        /// <summary>关闭栈顶面板（相当于按 ESC）。</summary>
        bool CloseTop();

        /// <summary>关闭所有面板。</summary>
        void CloseAll();
    }
}
