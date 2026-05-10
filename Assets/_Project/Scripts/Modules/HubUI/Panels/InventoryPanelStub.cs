#nullable enable
using GeminiLab.Core.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>物品栏面板占位。道具格子实现后续接入 InventoryService。</summary>
    public sealed class InventoryPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Inventory;
    }
}
