#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Data-driven furniture definition.
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Furniture/Definition", fileName = "FurnitureDefinition")]
    public sealed class FurnitureDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id = "Furniture.Definition";
        [SerializeField] private Sprite? _sprite;
        [SerializeField] private FurnitureCategory _category = FurnitureCategory.Decoration;
        [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.Unknown;
        [SerializeField] private float _interactionDurationSeconds = 1f;
        [SerializeField] private FurniturePlacementType _placementType = FurniturePlacementType.Floor;
        [SerializeField] private Vector2Int _occupiedCells = Vector2Int.one;
        [SerializeField] private EnvironmentalBuff _buff;

        public string Id => _id;

        public Sprite? Sprite => _sprite;

        public FurnitureCategory Category => _category;

        public FurnitureInteractionType InteractionType => _interactionType;

        public float InteractionDurationSeconds => _interactionDurationSeconds;

        public FurniturePlacementType PlacementType => _placementType;

        public Vector2Int OccupiedCells => _occupiedCells;

        public EnvironmentalBuff Buff => _buff;

        internal void ConfigureRuntime(
            string id,
            FurnitureCategory category,
            FurniturePlacementType placementType,
            Vector2Int occupiedCells,
            EnvironmentalBuff buff,
            Sprite? sprite = null,
            FurnitureInteractionType interactionType = FurnitureInteractionType.Unknown,
            float interactionDurationSeconds = 1f)
        {
            _id = id;
            _category = category;
            _interactionType = interactionType;
            _interactionDurationSeconds = interactionDurationSeconds;
            _placementType = placementType;
            _occupiedCells = occupiedCells;
            _buff = buff;
            _sprite = sprite;
        }
    }
}
