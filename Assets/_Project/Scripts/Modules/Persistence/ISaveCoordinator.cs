#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiLab.Modules.Persistence
{
    /// <summary>
    /// 业务层操作"整盘存档"的门面。
    /// - 内部聚合 <see cref="IPersistentServiceRegistry"/> + <see cref="ISaveSystem"/>
    /// - 业务代码只认 slotId，不直接操作文件/JSON
    /// </summary>
    public interface ISaveCoordinator
    {
        /// <summary>当前阶段固定 3 个槽位 id：slot_1 / slot_2 / slot_3。</summary>
        IReadOnlyList<string> DefaultSlotIds { get; }

        /// <summary>查询所有槽位摘要（不加载完整 bundle）。</summary>
        Task<IReadOnlyList<SlotSummary>> ListSlotsAsync(CancellationToken cancellationToken = default);

        /// <summary>把 Registry 里所有服务的 CaptureJson 打包写入指定槽位。</summary>
        Task SaveAsync(string slotId, CancellationToken cancellationToken = default);

        /// <summary>读取指定槽位，按 key 路由回各服务 RestoreJson；返回成功/失败。</summary>
        Task<bool> LoadAsync(string slotId, CancellationToken cancellationToken = default);

        /// <summary>删除指定槽位。</summary>
        Task DeleteAsync(string slotId, CancellationToken cancellationToken = default);
    }
}
