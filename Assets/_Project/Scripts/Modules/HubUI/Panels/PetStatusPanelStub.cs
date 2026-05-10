#nullable enable
using GeminiLab.Core.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>宠物状态面板占位。天使/恶魔分页、性格雷达图的真实实现后续接入。</summary>
    public sealed class PetStatusPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.PetStatus;
    }
}
