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
        private const string SortingAnchorName = "SortingAnchor";

        [SerializeField] private string _instanceId = Guid.NewGuid().ToString("N");
        [SerializeField] private FurnitureDefinitionSO? _definition;
        [SerializeField] private InteractionAnchor? _anchor;
        [SerializeField] private bool _isSceneFurniture;
        [SerializeField] private Transform? _sortingAnchor;
        [SerializeField] private int _sortingOrderOffset;
        [SerializeField] private bool _useDynamicSortingRule;

        public string InstanceId => _instanceId;

        public FurnitureDefinitionSO Definition => _definition!;

        public InteractionAnchor Anchor => _anchor!;

        public bool IsSceneFurniture => _isSceneFurniture;

        public string DefinitionId => _definition != null ? _definition.Id : string.Empty;

        public bool UseDynamicSortingRule => _useDynamicSortingRule;

        public int CurrentSortingOrder
        {
            get
            {
                if (TryGetComponent(out SortingGroup sortingGroup) && sortingGroup != null)
                {
                    return sortingGroup.sortingOrder;
                }

                if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
                {
                    return renderer.sortingOrder;
                }

                return 0;
            }
        }

        public float SortingAnchorY => ResolveSortingAnchorY();

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

            TryAutoBindSortingAnchor();
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

            TryAutoBindSortingAnchor();
            EnsurePresentation();
        }

        private void LateUpdate()
        {
            if (_useDynamicSortingRule || ShouldPreserveScenePresentation())
            {
                return;
            }

            UpdateSortingOrder();
        }

        public void SetSceneFurniture(bool isSceneFurniture)
        {
            _isSceneFurniture = isSceneFurniture;
        }

        private void EnsurePresentation()
        {
            if (TryGetComponent(out SortingGroup sg) && sg != null
                && TryGetComponent(out SpriteRenderer sr) && sr != null
                && sg.sortingOrder == sr.sortingOrder
                && sg.sortingOrder != 0)
            {
                sr.sortingOrder = 0;
            }

            bool preserveScenePresentation = ShouldPreserveScenePresentation();
            if (_useDynamicSortingRule || preserveScenePresentation)
            {
                EnsureCollisionShape();
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

            if (!preserveScenePresentation)
            {
                sortingGroup.sortingLayerName = "Furniture";
            }

            if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
            {
                if (!preserveScenePresentation)
                {
                    renderer.sortingLayerName = "Furniture";
                }
            }

            if (_useDynamicSortingRule)
            {
                UpdateSortingOrder(sortingGroup);
            }
            else
            {
                FurniturePlacementType placementType = _definition?.PlacementType ?? FurniturePlacementType.Floor;
                int sortOrder = CalculateSortingOrder(transform.position.y, placementType) + _sortingOrderOffset;
                sortingGroup.sortingOrder = sortOrder;
                if (TryGetComponent(out SpriteRenderer sortingRenderer) && sortingRenderer != null)
                {
                    sortingRenderer.sortingOrder = sortOrder;
                }
            }

            EnsureCollisionShape();
        }

        public bool TryGetOcclusionBounds(out Bounds bounds)
        {
            bounds = default;

            if (_useDynamicSortingRule && TryGetComponent(out SpriteRenderer dynamicRenderer) && dynamicRenderer != null)
            {
                bounds = dynamicRenderer.bounds;
                return true;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            bool hasBlockingCollider = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (!hasBlockingCollider)
                {
                    bounds = collider.bounds;
                    hasBlockingCollider = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBlockingCollider)
            {
                return true;
            }

            if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            return false;
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

        private void UpdateSortingOrder()
        {
            SortingGroup? sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                return;
            }

            UpdateSortingOrder(sortingGroup);
        }

        private void UpdateSortingOrder(SortingGroup sortingGroup)
        {
            FurniturePlacementType placementType = _definition?.PlacementType ?? FurniturePlacementType.Floor;
            int sortOrder = CalculateSortingOrder(ResolveSortingAnchorY(), placementType) + _sortingOrderOffset;
            sortingGroup.sortingOrder = sortOrder;

            if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
            {
                renderer.sortingOrder = sortOrder;
            }
        }

        private float ResolveSortingAnchorY()
        {
            if (_sortingAnchor != null)
            {
                return _sortingAnchor.position.y;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            bool hasBlockingCollider = false;
            float lowestY = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                hasBlockingCollider = true;
                lowestY = Mathf.Min(lowestY, collider.bounds.min.y);
            }

            if (hasBlockingCollider)
            {
                return lowestY;
            }

            if (TryGetComponent(out SpriteRenderer renderer) && renderer != null)
            {
                return renderer.bounds.min.y;
            }

            return transform.position.y;
        }

        private void TryAutoBindSortingAnchor()
        {
            if (_sortingAnchor != null)
            {
                return;
            }

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == transform)
                {
                    continue;
                }

                if (!string.Equals(candidate.name, SortingAnchorName, StringComparison.Ordinal))
                {
                    continue;
                }

                _sortingAnchor = candidate;
                return;
            }
        }

        private void EnsureCollisionShape()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length > 0)
            {
                return;
            }

            BoxCollider2D? collider = gameObject.AddComponent<BoxCollider2D>();
            if (collider == null)
            {
                return;
            }

            FurniturePlacementType placementType = _definition?.PlacementType ?? FurniturePlacementType.Floor;
            collider.isTrigger = placementType == FurniturePlacementType.Wall;

            Vector2Int occupiedCells = _definition?.OccupiedCells ?? Vector2Int.one;
            collider.size = new Vector2(
                Mathf.Max(0.5f, occupiedCells.x),
                Mathf.Max(0.5f, occupiedCells.y));
            collider.offset = Vector2.zero;
        }
    }
}
