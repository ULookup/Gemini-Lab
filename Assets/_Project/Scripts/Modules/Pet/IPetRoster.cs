#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 维护天使 / 恶魔两只宠物运行态数据的只读门面。
    /// 业务代码需要查询宠物状态时（UI 面板、塔罗解读、Gateway 人格）从 <see cref="IPetRoster"/> 取，
    /// 禁止再依赖"场景里找得到的唯一 PetController"。
    /// </summary>
    public interface IPetRoster
    {
        /// <summary>按 PetId 取运行态数据；若该宠尚未注册返回 null。</summary>
        PetRuntimeData? TryGet(PetId id);

        /// <summary>当前已注册的宠物集合。</summary>
        IReadOnlyList<PetId> RegisteredPets { get; }

        /// <summary>
        /// 由 PetController 在 Awake 时调用，把自己的 RuntimeData 交给 Roster。
        /// 重复 Register 同一个 PetId 会覆盖（场景重入是允许的）。
        /// </summary>
        void Register(PetId id, PetRuntimeData runtime);

        /// <summary>OnDestroy 时调用，Roster 解除对该 RuntimeData 的持有。</summary>
        void Unregister(PetId id);
    }
}
