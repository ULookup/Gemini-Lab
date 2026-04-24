#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Central placement validation for V-Decor mode.
    /// </summary>
    public sealed class PlacementValidator2D
    {
        public bool IsPlacementValid(FurnitureDefinitionSO definition, Vector2 position, LayerMask obstacleMask, out string reason)
        {
            Vector2 boxSize = new(
                Mathf.Max(0.2f, definition.OccupiedCells.x),
                Mathf.Max(0.2f, definition.OccupiedCells.y));

            Collider2D? hit = Physics2D.OverlapBox(position, boxSize, 0f, obstacleMask);
            if (hit is not null)
            {
                reason = "Placement overlaps existing collider.";
                return false;
            }

            if (definition.PlacementType == FurniturePlacementType.Wall && position.y < 0.5f)
            {
                reason = "Wall furniture must be placed on upper wall area.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
