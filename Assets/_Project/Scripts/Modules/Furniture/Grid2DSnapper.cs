#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Converts world positions into coarse placement cells.
    /// </summary>
    public sealed class Grid2DSnapper
    {
        private readonly float _cellSize;

        public Grid2DSnapper(float cellSize = 1f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        public Vector2 Snap(Vector2 worldPosition)
        {
            float x = Mathf.Round(worldPosition.x / _cellSize) * _cellSize;
            float y = Mathf.Round(worldPosition.y / _cellSize) * _cellSize;
            return new Vector2(x, y);
        }
    }
}
