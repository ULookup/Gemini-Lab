#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    public enum FurniturePlacementType
    {
        Floor = 0,
        Wall = 1
    }

    [Serializable]
    public struct EnvironmentalBuff
    {
        public float MoodDelta;
        public float EnergyDelta;
    }

    [Serializable]
    public sealed class FurnitureLayoutSnapshot
    {
        public int SchemaVersion = 1;
        public FurnitureLayoutEntry[] Entries = Array.Empty<FurnitureLayoutEntry>();
    }

    [Serializable]
    public sealed class FurnitureLayoutEntry
    {
        public string FurnitureId = string.Empty;
        public string DefinitionId = string.Empty;
        public Vector2 Position;
        public float RotationZ;
    }

    public readonly struct FurnitureInteractionTarget
    {
        public FurnitureInteractionTarget(string furnitureId, Vector2 interactionPoint, float score)
        {
            FurnitureId = furnitureId;
            InteractionPoint = interactionPoint;
            Score = score;
        }

        public string FurnitureId { get; }

        public Vector2 InteractionPoint { get; }

        public float Score { get; }
    }
}
