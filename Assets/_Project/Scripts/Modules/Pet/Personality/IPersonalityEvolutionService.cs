#nullable enable
using System;

namespace GeminiLab.Modules.Pet.Personality
{
    /// <summary>
    /// 运行态性格演化服务。
    /// - 初值来源：`PersonalityMatrixSO`（挂在宠物对象上）
    /// - 演化来源：EventBus 上的 <see cref="TarotDrawnEvent"/> / <see cref="PetInteractionCompletedEvent"/>
    /// - 查询：按 PetId 取当前 7 维快照；PetStatusPanel 改走这里
    /// </summary>
    public interface IPersonalityEvolutionService
    {
        /// <summary>当前性格快照（已叠加所有演化增量并 Clamp 到 [-1,1]）。</summary>
        PersonalityVector GetMatrix(PetId petId);

        /// <summary>性格向量变化事件。</summary>
        event Action<PetId, PersonalityVector>? MatrixChanged;

        /// <summary>
        /// 初始化一只宠物的初始值。通常由 PetController.Awake 调。
        /// 重复调用会覆盖。
        /// </summary>
        void SetInitialMatrix(PetId petId, PersonalityVector initial);
    }
}
