#nullable enable
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.UI
{
    /// <summary>
    /// Exposes build palette data for inventory UI binding.
    /// </summary>
    public sealed class FurnitureInventoryPanelController : MonoBehaviour
    {
        private readonly List<string> _items = new();

        public IReadOnlyList<string> Items => _items;

        private void Awake()
        {
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            _items.Clear();
            if (!ServiceLocator.TryResolve(out IFurnitureService? furnitureService) || furnitureService is null)
            {
                return;
            }

            IReadOnlyList<FurnitureDefinitionSO> palette = furnitureService.GetBuildPalette();
            for (int i = 0; i < palette.Count; i++)
            {
                FurnitureDefinitionSO definition = palette[i];
                _items.Add($"{definition.Category}:{definition.Id}");
            }
        }
    }
}
