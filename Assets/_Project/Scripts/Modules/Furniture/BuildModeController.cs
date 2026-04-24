#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Minimal V-Decor build mode controller.
    /// </summary>
    public sealed class BuildModeController : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKey = KeyCode.V;

        private IFurnitureService? _furnitureService;
        private bool _isBuildMode;
        private int _selectedIndex;

        private void Awake()
        {
            _furnitureService = FindFirstObjectByType<FurnitureService>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _isBuildMode = !_isBuildMode;
            }

            if (!_isBuildMode || _furnitureService is null)
            {
                return;
            }

            IReadOnlyList<FurnitureDefinitionSO> palette = _furnitureService.GetBuildPalette();
            if (palette.Count == 0)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _selectedIndex = 0;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _selectedIndex = Mathf.Min(1, palette.Count - 1);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _selectedIndex = (_selectedIndex + 1) % palette.Count;
            }

            Vector2 world = Camera.main is null
                ? Vector2.zero
                : Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                FurnitureDefinitionSO selected = palette[Mathf.Clamp(_selectedIndex, 0, palette.Count - 1)];
                _ = _furnitureService.TryPlaceFurniture(selected, world, 0f, out Furniture? _, out string _);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                _ = _furnitureService.TryRemoveNearestFurniture(world, 1.2f, out string _);
            }
        }
    }
}
