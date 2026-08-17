#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    public sealed class WorldMapPlacedFlower : MonoBehaviour
    {
        [SerializeField] private string _flowerId = string.Empty;
        [SerializeField] private WorldMapFlowerPlacementController.PlacementVisualType _visualType;
        [SerializeField] private Vector2Int _gridFootprint = Vector2Int.one;
        [SerializeField] private Vector2 _cellSize = Vector2.one;

        public string FlowerId { get => _flowerId; set => _flowerId = value; }
        public WorldMapFlowerPlacementController.PlacementVisualType VisualType { get => _visualType; set => _visualType = value; }
        public Vector2Int GridFootprint { get => _gridFootprint; set => _gridFootprint = value; }
        public Vector2 CellSize { get => _cellSize; set => _cellSize = value; }
    }
}
