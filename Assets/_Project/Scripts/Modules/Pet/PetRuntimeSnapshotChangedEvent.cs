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
            bool workRequested,
            string targetFurnitureId,
            FurnitureCategory targetFurnitureCategory,
            FurnitureInteractionType targetFurnitureInteractionType,
            bool isTraveling,
            string lastInteractionFurnitureId,
            string lastInteractionSummary,
            PetId petId = PetId.Angel)
        {
            PetId = petId;
            CurrentState = currentState;
            Mood = mood;
            Energy = energy;
            Satiety = satiety;
            WorkRequested = workRequested;
            TargetFurnitureId = targetFurnitureId;
            TargetFurnitureCategory = targetFurnitureCategory;
            TargetFurnitureInteractionType = targetFurnitureInteractionType;
            IsTraveling = isTraveling;
            LastInteractionFurnitureId = lastInteractionFurnitureId;
            LastInteractionSummary = lastInteractionSummary;
        }

        public PetId PetId { get; }

        public string CurrentState { get; }

        public float Mood { get; }

        public float Energy { get; }

        public float Satiety { get; }

        public bool WorkRequested { get; }

        public string TargetFurnitureId { get; }

        public FurnitureCategory TargetFurnitureCategory { get; }

        public FurnitureInteractionType TargetFurnitureInteractionType { get; }

        public bool IsTraveling { get; }

        public string LastInteractionFurnitureId { get; }

        public string LastInteractionSummary { get; }
    }
}
