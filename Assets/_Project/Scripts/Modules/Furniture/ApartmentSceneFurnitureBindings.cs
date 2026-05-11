#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Furniture
{
    [DefaultExecutionOrder(-500)]
    public sealed class ApartmentSceneFurnitureBindings : MonoBehaviour
    {
        private static readonly string[] IgnoredStaticDecorNames =
        {
            "\u7A97\u53F0\u82B1_\u5929\u4F7F_\u9759\u6001",
            "\u5DE6\u4E0B\u5C0F\u5BB6\u5177_\u6076\u9B54_\u9759\u6001"
        };

        private static readonly string[] IgnoredDefinitionIds =
        {
            "\u5BB6\u5177_\u88C5\u9970_\u7A97\u53F0\u4E0A\u7684\u76C6\u683D_\u5929\u4F7F_01",
            "\u5BB6\u5177_\u88C5\u9970_\u5DE6\u4E0B\u5C0F\u5BB6\u5177_\u6076\u9B54_01"
        };

        [SerializeField] private BindingEntry[] _bindings = Array.Empty<BindingEntry>();

        private void Awake()
        {
            ApplyBindings();
        }

        [ContextMenu("Apply Bindings")]
        public void ApplyBindings()
        {
            for (int i = 0; i < _bindings.Length; i++)
            {
                BindingEntry? entry = _bindings[i];
                if (entry is null || ShouldIgnore(entry))
                {
                    continue;
                }

                GameObject? target = ResolveTarget(entry);
                if (target == null)
                {
                    Debug.LogWarning($"[ApartmentSceneFurnitureBindings] Skip binding '{entry.DefinitionId}' because target could not be resolved.");
                    continue;
                }

                if (!target.TryGetComponent(out SpriteRenderer _))
                {
                    Debug.LogWarning($"[ApartmentSceneFurnitureBindings] Skip '{target.name}' because it has no SpriteRenderer.", target);
                    continue;
                }

                InteractionAnchor anchor = target.GetComponent<InteractionAnchor>() ?? target.AddComponent<InteractionAnchor>();
                SceneFurnitureDefinitionHint hint = target.GetComponent<SceneFurnitureDefinitionHint>() ?? target.AddComponent<SceneFurnitureDefinitionHint>();

                hint.Configure(
                    entry.DefinitionId,
                    entry.Category,
                    entry.InteractionType,
                    entry.InteractionDurationSeconds,
                    entry.PlacementType,
                    entry.OccupiedCells,
                    entry.Buff,
                    entry.IncludeInBuildPalette);

                anchor.SetAvailable(entry.IsAvailable);

                Furniture furniture = target.GetComponent<Furniture>() ?? target.AddComponent<Furniture>();
                furniture.SetSceneFurniture(true);
                if (furniture.isActiveAndEnabled)
                {
                    furniture.Initialize(furniture.InstanceId, furniture.Definition);
                }
            }
        }

        private static GameObject? ResolveTarget(BindingEntry entry)
        {
            if (entry.Target != null)
            {
                if (ShouldIgnore(entry.Target.name))
                {
                    return null;
                }

                return entry.Target;
            }

            if (string.IsNullOrWhiteSpace(entry.DefinitionId))
            {
                return null;
            }

            GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (ShouldIgnore(candidate.name))
                {
                    continue;
                }

                if (candidate.TryGetComponent(out SceneFurnitureDefinitionHint hint) &&
                    string.Equals(hint.DefinitionId, entry.DefinitionId, StringComparison.Ordinal))
                {
                    entry.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (ShouldIgnore(candidate.name))
                {
                    continue;
                }

                if (string.Equals(candidate.name, entry.DefinitionId, StringComparison.Ordinal))
                {
                    entry.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (ShouldIgnore(candidate.name))
                {
                    continue;
                }

                if (candidate.TryGetComponent(out SpriteRenderer renderer) &&
                    renderer != null &&
                    renderer.sprite != null &&
                    string.Equals(renderer.sprite.name, entry.DefinitionId, StringComparison.Ordinal))
                {
                    entry.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (ShouldIgnore(candidate.name))
                {
                    continue;
                }

                if (candidate.name.Contains(entry.DefinitionId, StringComparison.Ordinal))
                {
                    entry.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            return null;
        }

        private static bool ShouldIgnore(BindingEntry entry)
        {
            return Array.Exists(IgnoredDefinitionIds, id => string.Equals(id, entry.DefinitionId, StringComparison.Ordinal));
        }

        private static bool ShouldIgnore(string objectName)
        {
            return Array.Exists(IgnoredStaticDecorNames, ignoredName => string.Equals(ignoredName, objectName, StringComparison.Ordinal));
        }

        [Serializable]
        private sealed class BindingEntry
        {
            [SerializeField] private GameObject? _target;
            [SerializeField] private bool _isAvailable = true;
            [SerializeField] private bool _includeInBuildPalette;
            [SerializeField] private string _definitionId = string.Empty;
            [SerializeField] private FurnitureCategory _category = FurnitureCategory.Unknown;
            [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.Unknown;
            [SerializeField] private float _interactionDurationSeconds = -1f;
            [SerializeField] private FurniturePlacementType _placementType = FurniturePlacementType.Floor;
            [SerializeField] private Vector2Int _occupiedCells = Vector2Int.one;
            [SerializeField] private EnvironmentalBuff _buff;

            public GameObject? Target => _target;
            public bool IsAvailable => _isAvailable;
            public bool IncludeInBuildPalette => _includeInBuildPalette;
            public string DefinitionId => _definitionId;
            public FurnitureCategory Category => _category;
            public FurnitureInteractionType InteractionType => _interactionType;
            public float InteractionDurationSeconds => _interactionDurationSeconds;
            public FurniturePlacementType PlacementType => _placementType;
            public Vector2Int OccupiedCells => _occupiedCells;
            public EnvironmentalBuff Buff => _buff;

            public void SetResolvedTarget(GameObject target)
            {
                _target = target;
            }
        }
    }
}
