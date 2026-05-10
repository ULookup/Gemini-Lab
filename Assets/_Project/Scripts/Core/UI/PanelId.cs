#nullable enable

namespace GeminiLab.Core.UI
{
    /// <summary>
    /// UI 面板的逻辑标识。面板是覆盖在当前场景之上的 Canvas 子对象，不是场景。
    /// </summary>
    public enum PanelId
    {
        None = 0,

        // 主菜单相关
        SaveSlots = 10,
        Settings = 11,

        // 侧边栏四件套
        PetStatus = 20,
        Tarot = 21,
        Collection = 22,
        Inventory = 23,

        // 通用
        ConfirmDialog = 90
    }
}
