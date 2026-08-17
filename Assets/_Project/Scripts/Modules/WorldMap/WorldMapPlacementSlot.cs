#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    public sealed class WorldMapPlacementSlot : MonoBehaviour
    {
        [Serializable]
        public sealed class VisualBinding
        {
            [SerializeField] private string _key = string.Empty;
            [SerializeField] private GameObject? _visual;

            public VisualBinding() { }

            public VisualBinding(string key, GameObject visual)
            {
                _key = key;
                _visual = visual;
            }

            public string Key => _key;
            public GameObject? Visual => _visual;
        }

        [SerializeField] private Transform? _visualRoot;
        [SerializeField] private Collider2D? _occupancyCollider;
        [SerializeField] private WorldMapPlacedFlower? _metadata;
        [SerializeField] private List<VisualBinding> _visuals = new();
        [SerializeField] private int _placementSortingOrder;
        [SerializeField] private string _placementLayerId = string.Empty;

        [NonSerialized] private bool _isOccupied;

        public bool IsOccupied => _isOccupied;
        public bool HasVisualBindings
        {
            get
            {
                if (_visualRoot == null) return false;
                for (int i = 0; i < _visuals.Count; i++)
                {
                    if (_visuals[i].Visual != null) return true;
                }

                return false;
            }
        }
        public int VisualBindingCount => _visuals.Count;

        public void EnsureRuntimeBindings(IReadOnlyList<string> flowerIds)
        {
            if (_visualRoot == null) _visualRoot = transform;
            if (_occupancyCollider == null) _occupancyCollider = GetComponent<Collider2D>();
            if (_metadata == null) _metadata = GetComponent<WorldMapPlacedFlower>();
            if (_visualRoot == null) return;

            bool hasValidBinding = false;
            for (int i = 0; i < _visuals.Count; i++)
            {
                if (_visuals[i].Visual != null)
                {
                    hasValidBinding = true;
                    break;
                }
            }
            if (hasValidBinding) return;

            _visuals.Clear();
            Transform[] authoredVisuals = _visualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < authoredVisuals.Length; i++)
            {
                string name = authoredVisuals[i].name;
                const string prefix = "PlacedVisual_";
                if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;

                string suffix = name.Substring(prefix.Length);
                if (suffix.EndsWith("_Single", StringComparison.Ordinal))
                {
                    string flowerId = suffix.Substring(0, suffix.Length - "_Single".Length);
                    _visuals.Add(new VisualBinding(flowerId.Replace("_", "|", StringComparison.Ordinal) + "|Single", authoredVisuals[i].gameObject));
                }
                else if (suffix.EndsWith("_Cluster", StringComparison.Ordinal))
                {
                    string flowerId = suffix.Substring(0, suffix.Length - "_Cluster".Length);
                    _visuals.Add(new VisualBinding(flowerId.Replace("_", "|", StringComparison.Ordinal) + "|Cluster", authoredVisuals[i].gameObject));
                }
            }

            if (_visuals.Count > 0) return;

            for (int i = 0; i < flowerIds.Count; i++)
            {
                AddRuntimeBinding(flowerIds[i] + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Single);
                AddRuntimeBinding(flowerIds[i] + "|" + WorldMapFlowerPlacementController.PlacementVisualType.Cluster);
            }

            if (_visuals.Count == 0)
            {
                Debug.LogWarning($"[WorldMapFlowerPlacement] Slot {name} has no authored PlacedVisual children under {_visualRoot.name}; placement will still occupy the slot but cannot show a flower.");
            }
        }

        private void AddRuntimeBinding(string key)
        {
            if (_visualRoot == null) return;
            string nodeName = "PlacedVisual_" + key.Replace("|", "_").Replace(" ", "_");
            Transform? visual = _visualRoot.Find(nodeName);
            if (visual == null)
            {
                Transform[] candidates = _visualRoot.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (string.Equals(candidates[i].name, nodeName, StringComparison.Ordinal))
                    {
                        visual = candidates[i];
                        break;
                    }
                }
            }
            if (visual != null)
                _visuals.Add(new VisualBinding(key, visual.gameObject));
        }

        public void ClearVisualState()
        {
            _isOccupied = false;
            for (int i = 0; i < _visuals.Count; i++)
            {
                VisualBinding binding = _visuals[i];
                if (binding.Visual != null) binding.Visual.SetActive(false);
            }

            if (_occupancyCollider != null) _occupancyCollider.enabled = false;
            if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
        }

        public void Place(
            string flowerId,
            WorldMapFlowerPlacementController.PlacementVisualType visualType,
            Vector2 position,
            Vector2Int footprint,
            Vector2 cellSize,
            string placementLayerId,
            int sortingOrder,
            string sortingLayerName)
        {
            if (_visualRoot == null) _visualRoot = transform;
            if (_visualRoot == null) return;

            if (!HasVisualBindings)
                EnsureRuntimeBindings(Array.Empty<string>());

            _visualRoot.position = new Vector3(position.x, position.y, _visualRoot.position.z);
            _placementLayerId = placementLayerId;
            _placementSortingOrder = sortingOrder;
            string key = flowerId + "|" + visualType;
            for (int i = 0; i < _visuals.Count; i++)
            {
                VisualBinding binding = _visuals[i];
                if (binding.Visual != null)
                {
                    binding.Visual.SetActive(string.Equals(binding.Key, key, StringComparison.Ordinal));
                    SpriteRenderer[] renderers = binding.Visual.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        SpriteRenderer renderer = renderers[rendererIndex];
                        renderer.sortingLayerName = string.IsNullOrWhiteSpace(sortingLayerName)
                            ? "Default"
                            : sortingLayerName;
                        renderer.sortingOrder = sortingOrder;
                    }
                }
            }

            if (_metadata != null)
            {
                _metadata.FlowerId = flowerId;
                _metadata.VisualType = visualType;
                _metadata.GridFootprint = footprint;
                _metadata.CellSize = cellSize;
            }

            _visualRoot.gameObject.SetActive(true);
            _isOccupied = true;
            if (_occupancyCollider != null)
            {
                _occupancyCollider.enabled = true;
                if (_occupancyCollider is BoxCollider2D box)
                    box.size = Vector2.Scale((Vector2)footprint, cellSize);
            }
        }

        public Rect GetOccupiedRect()
        {
            if (_metadata == null) return new Rect();
            Vector2 size = Vector2.Scale((Vector2)_metadata.GridFootprint, _metadata.CellSize);
            Vector2 center = _visualRoot != null ? _visualRoot.position : Vector3.zero;
            return new Rect(new Vector2(center.x - size.x * 0.5f, center.y), size);
        }

        public bool IsOnSamePlacementLayer(string placementLayerId)
        {
            if (!_isOccupied) return false;
            return string.Equals(_placementLayerId, placementLayerId, StringComparison.Ordinal);
        }
    }
}
