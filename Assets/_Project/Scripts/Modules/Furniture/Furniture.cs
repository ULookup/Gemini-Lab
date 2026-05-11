#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Modules.Furniture
{
    /// <summary>
    /// Runtime furniture component.
    /// </summary>
    public sealed class Furniture : MonoBehaviour, IFurniture
    {
        [SerializeField] private string _instanceId = Guid.NewGuid().ToString("N");
        [SerializeField] private FurnitureDefinitionSO? _definition;
        [SerializeField] private InteractionAnchor? _anchor;
        [SerializeField] private bool _isSceneFurniture;

        public string InstanceId => _instanceId;

        public FurnitureDefinitionSO Definition => _definition!;

        public InteractionAnchor Anchor => _anchor!;

        public bool IsSceneFurniture => _isSceneFurniture;

        private void Awake()
        {
            if (_anchor is null)
            {
                _anchor = gameObject.GetComponent<InteractionAnchor>() ?? gameObject.AddComponent<InteractionAnchor>();
            }

            if (_definition is null)
            {
                FurnitureDefinitionSO fallback = ScriptableObject.CreateInstance<FurnitureDefinitionSO>();
                fallback.ConfigureRuntime(
                    "Furniture.Fallback",
                    FurnitureCategory.Decoration,
                    FurniturePlacementType.Floor,
                    Vector2Int.one,
                    default);
                _definition = fallback;
            }

            EnsurePresentation();
        }

        public void Initialize(string instanceId, FurnitureDefinitionSO definition)
        {
            _instanceId = instanceId;
            _definition = definition;

            if (_anchor is null)
            {
                _anchor = gameObject.GetComponent<InteractionAnchor>() ?? gameObject.AddComponent<InteractionAnchor>();
            }

            EnsurePresentation();
        }

        public void SetSceneFurniture(bool isSceneFurniture)
        {
            _isSceneFurniture = isSceneFurniture;
        }

        private void EnsurePresentation()
        {
            if (ShouldPreserveScenePresentation())
            {
                return;
            }

            SortingGroup? sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
            }

            if (sortingGroup == null)
            {
                Debug.LogWarning($"[Furniture] Failed to ensure SortingGroup on '{gameObject.name}'.", this);
                return;
            }

            sortingGroup.sortingLayerName = "Furniture";

            if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
            {
                renderer.sortingLayerName = "Furniture";
                renderer.sortingOrder = CalculateSortingOrder(transform.position.y, _definition?.PlacementType ?? FurniturePlacementType.Floor);
                sortingGroup.sortingOrder = renderer.sortingOrder;
            }
        }

        private bool ShouldPreserveScenePresentation()
        {
            if (_isSceneFurniture)
            {
                return true;
            }

            return TryGetComponent(out SceneFurnitureDefinitionHint hint) && hint.EnabledHint;
        }

        private static int CalculateSortingOrder(float y, FurniturePlacementType placementType)
        {
            int sortOrder = -(int)(y * 100f);
            if (placementType == FurniturePlacementType.Wall)
            {
                sortOrder += 500;
            }

            return sortOrder;
        }
    }
}
