#nullable enable
using System;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 收藏条目。category 驱动页签；iconSpritePath 暂用字符串 key（UIArtCatalog / 直接资源路径），
    /// 后续 Phase F 起可以换成 Sprite 引用 + 异步加载。
    /// </summary>
    [Serializable]
    public struct CollectionEntry
    {
        public string Id;
        public CollectionCategory Category;
        public string Title;
        public string Description;
        public string AcquiredDateIso;
        public string IconKey;

        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }

    /// <summary>收藏条目新增事件。</summary>
    public readonly struct CollectionAddedEvent
    {
        public CollectionAddedEvent(CollectionEntry entry) { Entry = entry; }
        public CollectionEntry Entry { get; }
    }

    /// <summary>收藏整体变化（删除 / 重置）。</summary>
    public readonly struct CollectionChangedEvent { }
}
