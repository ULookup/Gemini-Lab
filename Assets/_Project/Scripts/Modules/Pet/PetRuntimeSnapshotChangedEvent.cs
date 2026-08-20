#nullable enable
using GeminiLab.Modules.Furniture;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Event payload containing a UI-friendly snapshot of the pet runtime state.
    /// </summary>
    public readonly struct PetRuntimeSnapshotChangedEvent
    {
        public PetRuntimeSnapshotChangedEvent(
            string currentState,
            float mood,
            float energy,
            float satiety,
            float relation,
            bool workRequested,
            string targetFurnitureId,
            FurnitureCategory targetFurnitureCategory,
            FurnitureInteractionType targetFurnitureInteractionType,
            bool isTraveling,
            string lastInteractionFurnitureId,
            string lastInteractionSummary,
            PetId petId = PetId.Angel,
            string currentBehaviorId = "")
        {
            CurrentState = currentState;
            Mood = mood;
            Energy = energy;
            Satiety = satiety;
            Relation = relation;
            WorkRequested = workRequested;
            TargetFurnitureId = targetFurnitureId;
            TargetFurnitureCategory = targetFurnitureCategory;
            TargetFurnitureInteractionType = targetFurnitureInteractionType;
            IsTraveling = isTraveling;
            LastInteractionFurnitureId = lastInteractionFurnitureId;
            LastInteractionSummary = lastInteractionSummary;
            PetId = petId;
            CurrentBehaviorId = currentBehaviorId;
        }

        public string CurrentState { get; }

        public float Mood { get; }

        public float Energy { get; }

        public float Satiety { get; }

        public float Relation { get; }

        public bool WorkRequested { get; }

        public string TargetFurnitureId { get; }

        public FurnitureCategory TargetFurnitureCategory { get; }

        public FurnitureInteractionType TargetFurnitureInteractionType { get; }

        public bool IsTraveling { get; }

        public string LastInteractionFurnitureId { get; }

        public string LastInteractionSummary { get; }

        /// <summary>发布方所属宠物；默认 Angel 以兼容旧构造。</summary>
        public PetId PetId { get; }

        /// <summary>当前正在执行的行为 Id（行为权重系统）；无行为时为 Empty。</summary>
        public string CurrentBehaviorId { get; }
    }
}
