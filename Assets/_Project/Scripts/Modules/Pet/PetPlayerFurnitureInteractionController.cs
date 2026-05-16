#nullable enable
using System;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Allows player-triggered furniture interactions when the pet is close enough
    /// to a configured scene object and the player presses F.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PetPlayerFurnitureInteractionController : MonoBehaviour
    {
        [Serializable]
        public sealed class InteractionAnimationOption
        {
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animatorStateName = string.Empty;
            [SerializeField] private string _variantKey = string.Empty;

            public string Label => _label;
            public string AnimatorStateName => _animatorStateName;
            public string VariantKey => _variantKey;
        }

        [Serializable]
        private sealed class InteractionBinding
        {
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private GameObject? _target;
            [SerializeField] private string _fallbackTargetName = string.Empty;
            [SerializeField] private bool _useFallbackWorldPoint;
            [SerializeField] private Vector2 _fallbackWorldPoint;
            [SerializeField] private float _activationDistance = 1.5f;
            [SerializeField] private bool _hideTargetWhileInteracting;
            [SerializeField] private GameObject[] _additionalHideTargets = Array.Empty<GameObject>();
            [SerializeField] private bool _useTargetSortingWhileInteracting;
            [SerializeField] private bool _usePetPoseOverride;
            [SerializeField] private bool _useTargetPositionForPetPose;
            [SerializeField] private Vector2 _petInteractionLocalOffset;
            [SerializeField] private Vector2 _petInteractionWorldPoint;
            [SerializeField] private Vector3 _petInteractionScale = Vector3.one;
            [SerializeField] private FurnitureCategory _category = FurnitureCategory.Decoration;
            [SerializeField] private FurnitureInteractionType _interactionType = FurnitureInteractionType.DecorInspect;
            [SerializeField] private PetSelfInteractionVariant _animationVariant = PetSelfInteractionVariant.Read;
            [SerializeField] private float _interactionDurationSeconds = 1.5f;
            [SerializeField] private InteractionAnimationOption[] _animationOptions = Array.Empty<InteractionAnimationOption>();

            public string Label => _label;
            public GameObject? Target => _target;
            public string FallbackTargetName => _fallbackTargetName;
            public bool UseFallbackWorldPoint => _useFallbackWorldPoint;
            public Vector2 FallbackWorldPoint => _fallbackWorldPoint;
            public float ActivationDistance => Mathf.Max(0.1f, _activationDistance);
            public bool HideTargetWhileInteracting => _hideTargetWhileInteracting;
            public GameObject[] AdditionalHideTargets => _additionalHideTargets;
            public bool UseTargetSortingWhileInteracting => _useTargetSortingWhileInteracting;
            public bool UsePetPoseOverride => _usePetPoseOverride;
            public bool UseTargetPositionForPetPose => _useTargetPositionForPetPose;
            public Vector2 PetInteractionLocalOffset => _petInteractionLocalOffset;
            public Vector2 PetInteractionWorldPoint => _petInteractionWorldPoint;
            public Vector3 PetInteractionScale => _petInteractionScale;
            public FurnitureCategory Category => _category;
            public FurnitureInteractionType InteractionType => _interactionType;
            public PetSelfInteractionVariant AnimationVariant => _animationVariant;
            public float InteractionDurationSeconds => Mathf.Max(0.1f, _interactionDurationSeconds);
            public InteractionAnimationOption[] AnimationOptions => _animationOptions;

            public void SetResolvedTarget(GameObject target)
            {
                _target = target;
            }

            public bool TryGetPreferredAnimation(out string animatorStateName, out string variantKey)
            {
                for (int i = 0; i < _animationOptions.Length; i++)
                {
                    InteractionAnimationOption option = _animationOptions[i];
                    if (string.IsNullOrWhiteSpace(option.AnimatorStateName) &&
                        string.IsNullOrWhiteSpace(option.VariantKey))
                    {
                        continue;
                    }

                    animatorStateName = option.AnimatorStateName;
                    variantKey = option.VariantKey;
                    return true;
                }

                animatorStateName = string.Empty;
                variantKey = string.Empty;
                return false;
            }
        }

        [SerializeField] private bool _enableInteraction = true;
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        [SerializeField] private InteractionBinding[] _bindings = Array.Empty<InteractionBinding>();

        private PetController? _petController;

        private void Awake()
        {
            _petController = GetComponent<PetController>();
        }

        private void Update()
        {
            if (!_enableInteraction || !isActiveAndEnabled || !Input.GetKeyDown(_interactKey))
            {
                return;
            }

            if (_petController == null)
            {
                _petController = GetComponent<PetController>();
            }

            if (_petController == null || !_petController.IsPlayerControlEnabled)
            {
                return;
            }

            if (!TryGetClosestBinding(transform.position, out InteractionBinding? binding, out string targetName, out GameObject? targetObject))
            {
                return;
            }

            PetPlayerInteractionRequest request = new(
                targetName: !string.IsNullOrWhiteSpace(binding.Label) ? binding.Label : targetName,
                category: binding.Category,
                interactionType: binding.InteractionType,
                animationVariant: ResolveAnimationVariant(binding),
                animatorStateNameOverride: ResolveAnimatorStateOverride(binding),
                hideTargetWhileInteracting: binding.HideTargetWhileInteracting,
                visualHideTarget: binding.HideTargetWhileInteracting ? targetObject : null,
                additionalVisualHideTargets: binding.AdditionalHideTargets,
                useTargetSortingWhileInteracting: binding.UseTargetSortingWhileInteracting,
                visualSortingTarget: binding.UseTargetSortingWhileInteracting ? targetObject : null,
                usePetPoseOverride: binding.UsePetPoseOverride,
                useTargetPositionForPetPose: binding.UseTargetPositionForPetPose,
                petInteractionLocalOffset: binding.PetInteractionLocalOffset,
                petInteractionWorldPoint: binding.PetInteractionWorldPoint,
                petInteractionScale: binding.PetInteractionScale,
                interactionDurationSeconds: binding.InteractionDurationSeconds);

            Debug.Log($"[PetPlayerFurnitureInteraction] Selected binding label='{binding.Label}' target='{targetName}' variant='{request.AnimationVariant}' animatorState='{request.AnimatorStateNameOverride}'.");
            _ = _petController.TryStartPlayerInteraction(request);
        }

        private bool TryGetClosestBinding(Vector2 petPosition, out InteractionBinding? bestBinding, out string bestTargetName, out GameObject? bestTargetObject)
        {
            bestBinding = null;
            bestTargetName = string.Empty;
            bestTargetObject = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _bindings.Length; i++)
            {
                InteractionBinding binding = _bindings[i];
                if (!TryResolveInteractionPoint(binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject))
                {
                    continue;
                }

                float distance = Vector2.Distance(petPosition, interactionPoint);
                if (distance > binding.ActivationDistance || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestBinding = binding;
                bestTargetName = targetName;
                bestTargetObject = targetObject;
            }

            return bestBinding != null && !string.IsNullOrWhiteSpace(bestTargetName);
        }

        private static string ResolveAnimationVariant(InteractionBinding binding)
        {
            return binding.TryGetPreferredAnimation(out _, out string variantKey) &&
                   !string.IsNullOrWhiteSpace(variantKey)
                ? variantKey
                : binding.AnimationVariant.ToVariantKey();
        }

        private static string ResolveAnimatorStateOverride(InteractionBinding binding)
        {
            return binding.TryGetPreferredAnimation(out string animatorStateName, out _) &&
                   !string.IsNullOrWhiteSpace(animatorStateName)
                ? animatorStateName
                : string.Empty;
        }

        private static GameObject? ResolveTarget(InteractionBinding binding)
        {
            if (binding.Target != null)
            {
                return binding.Target;
            }

            if (string.IsNullOrWhiteSpace(binding.FallbackTargetName))
            {
                return null;
            }

            GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (candidate.TryGetComponent(out SceneFurnitureDefinitionHint hint) &&
                    string.Equals(hint.DefinitionId, binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }

                if (candidate.TryGetComponent(out SpriteRenderer renderer) &&
                    renderer.sprite != null &&
                    string.Equals(renderer.sprite.name, binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }

                if (string.Equals(candidate.name, binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                GameObject candidate = sceneObjects[i];
                if (candidate.TryGetComponent(out SceneFurnitureDefinitionHint hint) &&
                    hint.DefinitionId.Contains(binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }

                if (candidate.TryGetComponent(out SpriteRenderer renderer) &&
                    renderer.sprite != null &&
                    renderer.sprite.name.Contains(binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }

                if (candidate.name.Contains(binding.FallbackTargetName, StringComparison.Ordinal))
                {
                    binding.SetResolvedTarget(candidate);
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryResolveInteractionPoint(InteractionBinding binding, out Vector2 interactionPoint, out string targetName, out GameObject? targetObject)
        {
            if (binding.UseFallbackWorldPoint)
            {
                interactionPoint = binding.FallbackWorldPoint;
                targetObject = ResolveTarget(binding);
                if (targetObject != null)
                {
                    targetName = targetObject.name;
                    return true;
                }

                targetName = !string.IsNullOrWhiteSpace(binding.Label) ? binding.Label : binding.FallbackTargetName;
                return true;
            }

            targetObject = ResolveTarget(binding);
            if (targetObject != null)
            {
                interactionPoint = targetObject.transform.position;
                targetName = targetObject.name;
                return true;
            }

            interactionPoint = default;
            targetName = string.Empty;
            targetObject = null;
            return false;
        }
    }

    public readonly struct PetPlayerInteractionRequest
    {
        public PetPlayerInteractionRequest(
            string targetName,
            FurnitureCategory category,
            FurnitureInteractionType interactionType,
            string animationVariant,
            string animatorStateNameOverride,
            bool hideTargetWhileInteracting,
            GameObject? visualHideTarget,
            GameObject[]? additionalVisualHideTargets,
            bool useTargetSortingWhileInteracting,
            GameObject? visualSortingTarget,
            bool usePetPoseOverride,
            bool useTargetPositionForPetPose,
            Vector2 petInteractionLocalOffset,
            Vector2 petInteractionWorldPoint,
            Vector3 petInteractionScale,
            float interactionDurationSeconds)
        {
            TargetName = targetName;
            Category = category;
            InteractionType = interactionType;
            AnimationVariant = animationVariant;
            AnimatorStateNameOverride = animatorStateNameOverride;
            HideTargetWhileInteracting = hideTargetWhileInteracting;
            VisualHideTarget = visualHideTarget;
            AdditionalVisualHideTargets = additionalVisualHideTargets ?? Array.Empty<GameObject>();
            UseTargetSortingWhileInteracting = useTargetSortingWhileInteracting;
            VisualSortingTarget = visualSortingTarget;
            UsePetPoseOverride = usePetPoseOverride;
            UseTargetPositionForPetPose = useTargetPositionForPetPose;
            PetInteractionLocalOffset = petInteractionLocalOffset;
            PetInteractionWorldPoint = petInteractionWorldPoint;
            PetInteractionScale = petInteractionScale;
            InteractionDurationSeconds = interactionDurationSeconds;
        }

        public string TargetName { get; }
        public FurnitureCategory Category { get; }
        public FurnitureInteractionType InteractionType { get; }
        public string AnimationVariant { get; }
        public string AnimatorStateNameOverride { get; }
        public bool HideTargetWhileInteracting { get; }
        public GameObject? VisualHideTarget { get; }
        public GameObject[] AdditionalVisualHideTargets { get; }
        public bool UseTargetSortingWhileInteracting { get; }
        public GameObject? VisualSortingTarget { get; }
        public bool UsePetPoseOverride { get; }
        public bool UseTargetPositionForPetPose { get; }
        public Vector2 PetInteractionLocalOffset { get; }
        public Vector2 PetInteractionWorldPoint { get; }
        public Vector3 PetInteractionScale { get; }
        public float InteractionDurationSeconds { get; }
    }

    public enum PetSelfInteractionVariant
    {
        BesideDoor = 0,
        Flower = 1,
        PlayingMusic = 2,
        Read = 3,
        Sleep = 4
    }

    public static class PetSelfInteractionVariantExtensions
    {
        public static string ToVariantKey(this PetSelfInteractionVariant variant)
        {
            return variant switch
            {
                PetSelfInteractionVariant.BesideDoor => "beside door",
                PetSelfInteractionVariant.Flower => "flower",
                PetSelfInteractionVariant.PlayingMusic => "playing music",
                PetSelfInteractionVariant.Read => "read",
                PetSelfInteractionVariant.Sleep => "sleep",
                _ => "read"
            };
        }
    }
}
