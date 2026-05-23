#nullable enable

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// PetController.Awake 完成后广播。
    /// 订阅者常见用途：
    /// - PersonalityEvolutionService 用 <see cref="PersonalityMatrix"/> 作为初始向量
    /// - 其他依赖 PetController 生命周期的观察者
    /// </summary>
    public readonly struct PetControllerInitializedEvent
    {
        public PetControllerInitializedEvent(PetId petId, PersonalityMatrixSO? personalityMatrix)
        {
            PetId = petId;
            PersonalityMatrix = personalityMatrix;
        }

        public PetId PetId { get; }
        public PersonalityMatrixSO? PersonalityMatrix { get; }
    }
}
