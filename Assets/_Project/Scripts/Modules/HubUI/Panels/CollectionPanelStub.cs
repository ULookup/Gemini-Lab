#nullable enable
using GeminiLab.Core.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>收藏面板占位。旅行照片与纪念物展示的真实实现后续接入 CollectionService。</summary>
    public sealed class CollectionPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Collection;
    }
}
