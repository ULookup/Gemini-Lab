#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 收藏服务门面。增删改通过事件通知。
    /// </summary>
    public interface ICollectionService
    {
        IReadOnlyList<CollectionEntry> All { get; }

        IEnumerable<CollectionEntry> GetByCategory(CollectionCategory category);

        /// <summary>添加条目；id 冲突则覆盖。</summary>
        void Add(CollectionEntry entry);

        bool TryRemove(string id);

        void Clear();
    }
}
