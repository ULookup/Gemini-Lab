#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    public readonly struct FurniturePlacedEvent
    {
        public FurniturePlacedEvent(string furnitureId, string definitionId, Vector2 position)
        {
            FurnitureId = furnitureId;
            DefinitionId = definitionId;
            Position = position;
        }

        public string FurnitureId { get; }
        public string DefinitionId { get; }
        public Vector2 Position { get; }
    }

    public readonly struct FurnitureRemovedEvent
    {
        public FurnitureRemovedEvent(string furnitureId)
        {
            FurnitureId = furnitureId;
        }

        public string FurnitureId { get; }
    }

    public readonly struct FurnitureInteractionEvent
    {
        public FurnitureInteractionEvent(string furnitureId, EnvironmentalBuff buff)
        {
            FurnitureId = furnitureId;
            Buff = buff;
        }

        public string FurnitureId { get; }
        public EnvironmentalBuff Buff { get; }
    }
}
