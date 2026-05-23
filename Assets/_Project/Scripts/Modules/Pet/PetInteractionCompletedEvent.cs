#nullable enable
using GeminiLab.Modules.Furniture;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 宠物完成一次家具交互后广播。
    /// 塔罗 / 性格演化 / 收藏 / 日志等订阅者都能用。
    /// </summary>
    public readonly struct PetInteractionCompletedEvent
    {
        public PetInteractionCompletedEvent(
            PetId petId,
            string furnitureId,
            FurnitureCategory category,
            FurnitureInteractionType interactionType)
        {
            PetId = petId;
            FurnitureId = furnitureId;
            Category = category;
            InteractionType = interactionType;
        }

        public PetId PetId { get; }
        public string FurnitureId { get; }
        public FurnitureCategory Category { get; }
        public FurnitureInteractionType InteractionType { get; }
    }
}
