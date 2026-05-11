#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.FSM;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Runtime host that ticks pet FSM and stat service.
    /// </summary>
    public sealed class PetController : MonoBehaviour
    {
        private const string MoveFrontStateName = "Move_Front";
        private const string IdleFrontStateName = "Idle_Front";
        private const string IdleBackStateName = "Idle_Back";
        private const string IdleSideStateName = "Idle_Side";
        private const string SleepStateName = "Sleep";
        private const string InteractReadStateName = "Interact_Read";
        private const string InteractBesideDoorStateName = "Interact_BesideDoor";

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveDirHash = Animator.StringToHash("MoveDir");
        // Squared-distance threshold for movement direction updates.
        private const float DirectionEpsilonSqr = 0.000001f;

        [SerializeField] private PetStateValueSO? _config;
        [SerializeField] private PersonalityMatrixSO? _personality;
        [SerializeField] private RuntimeAnimatorController? _movementController;
        [SerializeField] private bool _sideFramesFaceLeft = true;
        [SerializeField] private PetId _petId = PetId.Angel;

        public PetId PetId => _petId;

        private PetContext? _context;
        private StateMachine<PetContext>? _stateMachine;
        private StatTickService? _tickService;
        private IPetCommandLinkService? _commandLinkService;
        private PetPlayerInputController? _playerInputController;
        private Animator? _animator;
        private SpriteRenderer? _spriteRenderer;
        private Vector2 _lastAnimationPosition;
        private Vector2 _lastMoveDirection = Vector2.down;
        private Vector2 _playerAnimationDirection = Vector2.down;
        private bool _hasPlayerAnimationDirection;
        private string _lastForcedAnimatorStateName = string.Empty;
        private PetRuntimeSnapshotChangedEvent? _lastPublishedSnapshot;
        private readonly List<SpriteRenderer> _hiddenInteractionRenderers = new();
        private readonly List<bool> _hiddenInteractionRendererStates = new();
        private bool _hasStoredInteractionPose;
        private Vector3 _storedInteractionPosition;
        private Vector3 _storedInteractionScale;
        private bool _hasStoredInteractionSorting;
        private int _storedInteractionSortingLayerId;
        private int _storedInteractionSortingOrder;

        public string CurrentState => _context?.RuntimeData.CurrentState ?? "None";

        public PetRuntimeData? RuntimeData => _context?.RuntimeData;

        public bool IsPlayerControlEnabled => IsPlayerControlled();

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _playerInputController = GetComponent<PetPlayerInputController>();
            EnsureAnimatorBinding();
            _lastAnimationPosition = transform.position;

            PetStateValueSO config = _config ?? ScriptableObject.CreateInstance<PetStateValueSO>();
            _ = _personality; // Reserved for Phase 3 prompt adaptation.

            PetRuntimeData runtime = new()
            {
                PetId = _petId,
                Mood = config.InitialMood,
                Energy = config.InitialEnergy,
                Satiety = config.InitialSatiety,
                Position = transform.position
            };

            if (ServiceLocator.TryResolve(out IPetRoster? roster) && roster is not null)
            {
                roster.Register(_petId, runtime);
            }

            if (!ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                eventBus = null;
            }

            if (!ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                navigationService = null;
            }

            if (!ServiceLocator.TryResolve(out IFurnitureService? furnitureService))
            {
                furnitureService = null;
            }

            if (!ServiceLocator.TryResolve(out _commandLinkService))
            {
                _commandLinkService = new PetCommandLinkService();
                ServiceLocator.Register<IPetCommandLinkService>(_commandLinkService);
            }

            _context = new PetContext(runtime, config, navigationService, furnitureService, eventBus, _commandLinkService)
            {
                ApplyPosition = position =>
                {
                    transform.position = new Vector3(position.x, position.y, transform.position.z);
                }
            };
            _tickService = new StatTickService();
            _stateMachine = PetStateMachineBuilder.Build(_context);
            _stateMachine.StateChanged += PublishStateChanged;
        }

        private void Update()
        {
            if (_context is null || _stateMachine is null || _tickService is null)
            {
                return;
            }

            _context.RuntimeData.Position = transform.position;
            RefreshLateBoundServices(_context);
            if (IsPlayerControlled())
            {
                TickPlayerControlled(_context, Time.deltaTime);
                _context.ApplyPosition?.Invoke(_context.RuntimeData.Position);
                UpdateMovementAnimation();
                PublishSnapshotIfChanged(_context);
                return;
            }

            HandleDebugCommandInput(_context);
            ProcessCommands(_context, _stateMachine);
            _tickService.Tick(_context, Time.deltaTime);
            _stateMachine.Tick(Time.deltaTime);
            _context.ApplyPosition?.Invoke(_context.RuntimeData.Position);
            UpdateMovementAnimation();
            PublishSnapshotIfChanged(_context);
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            RestoreHiddenInteractionVisuals();
            RestoreInteractionSorting();
            if (_stateMachine is not null)
            {
                _stateMachine.StateChanged -= PublishStateChanged;
            }
            if (ServiceLocator.TryResolve(out IPetRoster? roster) && roster is not null)
            {
                roster.Unregister(_petId);
            }
        }

        public static void PublishStateChanged(string from, string to)
        {
            Debug.Log($"[PetFSM] {from} -> {to}");
            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                eventBus.Publish(new PetStateChangedEvent(from, to));
            }
        }

        private static void ProcessCommands(PetContext context, StateMachine<PetContext> stateMachine)
        {
            if (context.CommandLinkService is null)
            {
                return;
            }

            while (context.CommandLinkService.TryDequeue(out PetCommand command))
            {
                PetCommandRequest request = command.Request;
                context.RuntimeData.LastTraceId = request.TraceId;
                if (context.IsSleeping && request.ForceWake && request.CommandType == PetCommandType.WorkRequest)
                {
                    float moodPenalty = context.Config.ForceWakeMoodPenalty;
                    context.RuntimeData.Mood = Mathf.Clamp(context.RuntimeData.Mood - moodPenalty, 0f, 100f);
                    context.RuntimeData.PreventSleepBeforeTime = context.RuntimeData.RuntimeTimeSeconds + 3f;
                    stateMachine.ForceChangeState<IdleState>();
                    context.EventBus?.Publish(new PetWakePenaltyAppliedEvent(request.TraceId, -moodPenalty));
                }

                switch (request.CommandType)
                {
                    case PetCommandType.WorkRequest:
                        if (!context.IsSleeping || request.ForceWake)
                        {
                            if (request.TargetType == PetWorkTargetType.WorkDesk &&
                                (context.FurnitureService is null ||
                                 !context.FurnitureService.TryGetBestInteractionTarget(context.RuntimeData.Position, FurnitureInteractionQuery.WorkDeskOnly, out FurnitureInteractionTarget _)))
                            {
                                context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "No available WorkDesk target."));
                                break;
                            }

                            context.RuntimeData.WorkRequested = true;
                            context.RuntimeData.ActiveWorkTraceId = request.TraceId;
                            context.RuntimeData.ActiveWorkMessage = request.Message;
                            context.RuntimeData.RequiredWorkTargetType = request.TargetType;
                            context.RuntimeData.IsAtRequiredWorkTarget = false;
                            context.RuntimeData.TargetFurnitureId = string.Empty;
                            context.RuntimeData.TargetReached = false;
                            context.RuntimeData.ActivePath.Clear();
                            context.EventBus?.Publish(new PetCommandAcceptedEvent(request.TraceId, request.ForceWake, request.CommandType, request.Source));
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "Pet is sleeping and command is not force-wake."));
                        }

                        break;
                    case PetCommandType.WorkCompleted:
                        if (request.Source == PetCommandSource.Gateway &&
                            string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkCompletedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "WorkCompleted ignored: source is not Gateway or traceId mismatch."));
                        }

                        break;
                    case PetCommandType.WorkFailed:
                        if (request.Source == PetCommandSource.Gateway &&
                            string.Equals(context.RuntimeData.ActiveWorkTraceId, request.TraceId, System.StringComparison.Ordinal))
                        {
                            context.RuntimeData.WorkRequested = false;
                            context.EventBus?.Publish(new PetWorkFailedEvent(request.TraceId, request.Message));
                            ResetWorkRuntime(context);
                        }
                        else
                        {
                            context.EventBus?.Publish(new PetCommandRejectedEvent(request.TraceId, "WorkFailed ignored: source is not Gateway or traceId mismatch."));
                        }

                        break;
                }
            }
        }

        private void HandleDebugCommandInput(PetContext context)
        {
            if (_commandLinkService is null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                bool forceWake = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                string traceId = _commandLinkService.RequestWork(forceWake);
                context.RuntimeData.LastTraceId = traceId;
                Debug.Log($"[PetCommand] Debug work requested traceId={traceId}, forceWake={forceWake}");
            }
        }

        private static void RefreshLateBoundServices(PetContext context)
        {
            if (context.NavigationService is null && ServiceLocator.TryResolve(out INavigationService? navigationService))
            {
                context.NavigationService = navigationService;
            }

            if (context.FurnitureService is null && ServiceLocator.TryResolve(out IFurnitureService? furnitureService))
            {
                context.FurnitureService = furnitureService;
            }

            if (context.EventBus is null && ServiceLocator.TryResolve(out EventBus? eventBus))
            {
                context.EventBus = eventBus;
            }

            if (context.CommandLinkService is null && ServiceLocator.TryResolve(out IPetCommandLinkService? commandLinkService))
            {
                context.CommandLinkService = commandLinkService;
            }
        }

        private bool IsPlayerControlled()
        {
            if (_playerInputController == null)
            {
                _playerInputController = GetComponent<PetPlayerInputController>();
            }

            return _playerInputController != null && _playerInputController.InputEnabled;
        }

        private void TickPlayerControlled(PetContext context, float deltaTime)
        {
            if (TickPlayerInteraction(context, deltaTime))
            {
                return;
            }

            Vector2 movementInput = default;
            Vector2 rawInput = default;
            bool canMove = !context.RuntimeData.IsTraveling &&
                           _playerInputController != null &&
                           _playerInputController.TryGetMovementInput(out movementInput, out rawInput);

            _hasPlayerAnimationDirection = canMove;
            if (canMove)
            {
                _playerAnimationDirection = ResolvePlayerAnimationDirection(rawInput, _lastMoveDirection);
            }

            SetPlayerControlledState(context, canMove ? MovingState.StateName : IdleState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            ResetPlayerControlledRuntime(context);

            if (!canMove || _playerInputController == null)
            {
                return;
            }

            context.RuntimeData.Position += movementInput * _playerInputController.MoveSpeed * deltaTime;
            context.RuntimeData.TargetPosition = context.RuntimeData.Position;
            context.RuntimeData.TargetReached = true;
        }

        public bool TryStartPlayerInteraction(PetPlayerInteractionRequest request)
        {
            if (_context is null || !IsPlayerControlled() || _context.RuntimeData.IsPlayerInteractionActive)
            {
                return false;
            }

            _context.RuntimeData.IsPlayerInteractionActive = true;
            _context.RuntimeData.PlayerInteractionRemainingSeconds = Mathf.Max(0.1f, request.InteractionDurationSeconds);
            _context.RuntimeData.PlayerInteractionAnimationVariant = request.AnimationVariant;
            _context.RuntimeData.PlayerInteractionAnimatorStateName = request.AnimatorStateNameOverride;
            _context.RuntimeData.PlayerInteractionLabel = request.TargetName;
            _context.RuntimeData.TargetFurnitureId = request.TargetName;
            _context.RuntimeData.TargetFurnitureCategory = request.Category;
            _context.RuntimeData.TargetFurnitureInteractionType = request.InteractionType;
            _context.RuntimeData.TargetInteractionDurationSeconds = Mathf.Max(0.1f, request.InteractionDurationSeconds);
            _context.RuntimeData.TargetReached = true;
            _context.RuntimeData.ActivePath.Clear();
            _context.RuntimeData.PathIndex = 0;
            ApplyInteractionVisualOverride(request);
            ApplyInteractionSortingOverride(request);
            ApplyInteractionPoseOverride(request);
            Debug.Log($"[PetInteraction] Triggered player interaction target='{request.TargetName}' variant='{request.AnimationVariant}'.");
            return true;
        }

        private bool TickPlayerInteraction(PetContext context, float deltaTime)
        {
            if (!context.RuntimeData.IsPlayerInteractionActive)
            {
                return false;
            }

            SetPlayerControlledState(context, InteractingState.StateName);
            context.Advance(deltaTime);
            _tickService?.Tick(context, deltaTime);
            context.RuntimeData.WorkRequested = false;
            context.RuntimeData.ActivePath.Clear();
            context.RuntimeData.PathIndex = 0;
            context.RuntimeData.TargetReached = true;
            context.RuntimeData.PlayerInteractionRemainingSeconds -= deltaTime;

            if (context.RuntimeData.PlayerInteractionRemainingSeconds > 0f)
            {
                return true;
            }

            context.RuntimeData.IsPlayerInteractionActive = false;
            context.RuntimeData.PlayerInteractionRemainingSeconds = 0f;
            context.RuntimeData.LastInteractionFurnitureId = context.RuntimeData.TargetFurnitureId;
            context.RuntimeData.LastInteractionSummary =
                $"玩家交互 / {context.RuntimeData.PlayerInteractionLabel} / {context.RuntimeData.PlayerInteractionAnimationVariant}";
            context.RuntimeData.PlayerInteractionAnimatorStateName = string.Empty;
            context.RuntimeData.PlayerInteractionAnimationVariant = string.Empty;
            context.RuntimeData.PlayerInteractionLabel = string.Empty;
            RestoreHiddenInteractionVisuals();
            RestoreInteractionSorting();
            RestoreInteractionPose();
            return true;
        }

        private static void ResetWorkRuntime(PetContext context)
        {
            context.RuntimeData.ActiveWorkTraceId = string.Empty;
            context.RuntimeData.ActiveWorkMessage = string.Empty;
            context.RuntimeData.RequiredWorkTargetType = PetWorkTargetType.Any;
            context.RuntimeData.IsAtRequiredWorkTarget = false;
            context.RuntimeData.TargetFurnitureId = string.Empty;
            context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
            context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
            context.RuntimeData.TargetInteractionDurationSeconds = 1f;
            context.RuntimeData.TargetReached = false;
            context.RuntimeData.ActivePath.Clear();
        }

        private static void ResetPlayerControlledRuntime(PetContext context)
        {
            context.RuntimeData.WorkRequested = false;
            ResetWorkRuntime(context);
            if (!context.RuntimeData.IsPlayerInteractionActive)
            {
                context.RuntimeData.TargetFurnitureId = string.Empty;
                context.RuntimeData.TargetFurnitureCategory = FurnitureCategory.Unknown;
                context.RuntimeData.TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;
                context.RuntimeData.TargetInteractionDurationSeconds = 1f;
                context.RuntimeData.TargetReached = true;
                context.RuntimeData.ActivePath.Clear();
                context.RuntimeData.PathIndex = 0;
            }
        }

        private static void SetPlayerControlledState(PetContext context, string stateName)
        {
            if (string.Equals(context.RuntimeData.CurrentState, stateName, System.StringComparison.Ordinal))
            {
                return;
            }

            context.EnterState(stateName);
        }

        private void ApplyInteractionVisualOverride(PetPlayerInteractionRequest request)
        {
            RestoreHiddenInteractionVisuals();
            if (!request.HideTargetWhileInteracting || request.VisualHideTarget == null)
            {
                return;
            }

            SpriteRenderer[] renderers = request.VisualHideTarget.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                _hiddenInteractionRenderers.Add(renderer);
                _hiddenInteractionRendererStates.Add(renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void RestoreHiddenInteractionVisuals()
        {
            for (int i = 0; i < _hiddenInteractionRenderers.Count; i++)
            {
                SpriteRenderer? renderer = _hiddenInteractionRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = _hiddenInteractionRendererStates[i];
            }

            _hiddenInteractionRenderers.Clear();
            _hiddenInteractionRendererStates.Clear();
        }

        private void ApplyInteractionPoseOverride(PetPlayerInteractionRequest request)
        {
            RestoreInteractionPose();
            if (!request.UsePetPoseOverride)
            {
                return;
            }

            _hasStoredInteractionPose = true;
            _storedInteractionPosition = transform.position;
            _storedInteractionScale = transform.localScale;
            transform.position = new Vector3(request.PetInteractionWorldPoint.x, request.PetInteractionWorldPoint.y, transform.position.z);
            transform.localScale = request.PetInteractionScale;
        }

        private void RestoreInteractionPose()
        {
            if (!_hasStoredInteractionPose)
            {
                return;
            }

            transform.position = _storedInteractionPosition;
            transform.localScale = _storedInteractionScale;
            _hasStoredInteractionPose = false;
        }

        private void ApplyInteractionSortingOverride(PetPlayerInteractionRequest request)
        {
            RestoreInteractionSorting();
            if (_spriteRenderer == null || !request.UseTargetSortingWhileInteracting || request.VisualSortingTarget == null)
            {
                return;
            }

            _hasStoredInteractionSorting = true;
            _storedInteractionSortingLayerId = _spriteRenderer.sortingLayerID;
            _storedInteractionSortingOrder = _spriteRenderer.sortingOrder;

            if (request.VisualSortingTarget.TryGetComponent(out SortingGroup sortingGroup))
            {
                _spriteRenderer.sortingLayerID = sortingGroup.sortingLayerID;
                _spriteRenderer.sortingOrder = sortingGroup.sortingOrder;
                return;
            }

            if (request.VisualSortingTarget.TryGetComponent(out SpriteRenderer targetRenderer))
            {
                _spriteRenderer.sortingLayerID = targetRenderer.sortingLayerID;
                _spriteRenderer.sortingOrder = targetRenderer.sortingOrder;
            }
        }

        private void RestoreInteractionSorting()
        {
            if (!_hasStoredInteractionSorting || _spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.sortingLayerID = _storedInteractionSortingLayerId;
            _spriteRenderer.sortingOrder = _storedInteractionSortingOrder;
            _hasStoredInteractionSorting = false;
        }

        private void UpdateMovementAnimation()
        {
            if (_animator == null)
            {
                return;
            }

            string? currentState = _context?.RuntimeData.CurrentState;
            if (currentState == SleepingState.StateName)
            {
                PlayForcedAnimatorState(SleepStateName);
                _animator.SetBool(IsMovingHash, false);
                _animator.speed = 1f;
                return;
            }

            if (currentState == InteractingState.StateName || currentState == WorkingState.StateName)
            {
                PlayForcedAnimatorState(ResolveInteractionStateName());
                _animator.SetBool(IsMovingHash, false);
                _animator.speed = 1f;
                return;
            }

            Vector2 currentPosition = transform.position;
            Vector2 delta = currentPosition - _lastAnimationPosition;
            _lastAnimationPosition = currentPosition;

            bool isMoving = string.Equals(currentState, MovingState.StateName, System.StringComparison.Ordinal);
            bool hasDelta = delta.sqrMagnitude > DirectionEpsilonSqr;

            if (!isMoving)
            {
                PlayForcedAnimatorState(ResolveIdleStateName(_lastMoveDirection));
            }
            else
            {
                _lastForcedAnimatorStateName = string.Empty;
            }

            if (IsPlayerControlled() && _hasPlayerAnimationDirection)
            {
                _lastMoveDirection = _playerAnimationDirection;
            }
            else if (hasDelta)
            {
                _lastMoveDirection = delta.normalized;
            }
            else if (isMoving && _context is not null)
            {
                // When frame-to-frame delta is tiny, keep direction aligned with
                // current movement target so transitions still choose correct clip.
                Vector2 targetDelta = _context.RuntimeData.TargetPosition - currentPosition;
                if (targetDelta.sqrMagnitude > DirectionEpsilonSqr)
                {
                    _lastMoveDirection = targetDelta.normalized;
                }
            }

            _animator.SetBool(IsMovingHash, isMoving);
            _animator.SetFloat(MoveXHash, _lastMoveDirection.x);
            _animator.SetFloat(MoveYHash, _lastMoveDirection.y);
            int moveDir = ResolveMoveDirection(_lastMoveDirection);
            _animator.SetInteger(MoveDirHash, moveDir);
            _animator.speed = 1f;

            UpdateSideMirror(moveDir, _lastMoveDirection);
        }

        private static Vector2 ResolvePlayerAnimationDirection(Vector2 rawInput, Vector2 previousDirection)
        {
            bool hasHorizontal = Mathf.Abs(rawInput.x) > 0.0001f;
            bool hasVertical = Mathf.Abs(rawInput.y) > 0.0001f;

            if (hasHorizontal && hasVertical)
            {
                if (Mathf.Abs(previousDirection.x) > Mathf.Abs(previousDirection.y))
                {
                    return new Vector2(Mathf.Sign(rawInput.x), 0f);
                }

                if (Mathf.Abs(previousDirection.y) > 0.0001f)
                {
                    return new Vector2(0f, Mathf.Sign(rawInput.y));
                }

                return new Vector2(Mathf.Sign(rawInput.x), 0f);
            }

            if (hasHorizontal)
            {
                return new Vector2(Mathf.Sign(rawInput.x), 0f);
            }

            if (hasVertical)
            {
                return new Vector2(0f, Mathf.Sign(rawInput.y));
            }

            return previousDirection;
        }

        private static int ResolveMoveDirection(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return 2; // Side
            }

            return direction.y >= 0f ? 1 : 0; // Back / Front
        }

        private void UpdateSideMirror(int moveDir, Vector2 direction)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            // Only side movement uses horizontal mirroring.
            if (moveDir != 2 || Mathf.Abs(direction.x) <= 0.0001f)
            {
                _spriteRenderer.flipX = false;
                return;
            }

            bool movingRight = direction.x > 0f;
            _spriteRenderer.flipX = _sideFramesFaceLeft ? movingRight : !movingRight;
        }

        private string ResolveInteractionStateName()
        {
            if (_context?.RuntimeData.IsPlayerInteractionActive == true)
            {
                if (!string.IsNullOrWhiteSpace(_context.RuntimeData.PlayerInteractionAnimatorStateName))
                {
                    return _context.RuntimeData.PlayerInteractionAnimatorStateName;
                }

                return ResolvePlayerInteractionStateName(_context.RuntimeData.PlayerInteractionAnimationVariant);
            }

            if (_context?.RuntimeData.RequiredWorkTargetType == PetWorkTargetType.WorkDesk ||
                _context?.RuntimeData.TargetFurnitureInteractionType == FurnitureInteractionType.WorkFocus ||
                _context?.RuntimeData.TargetFurnitureCategory == FurnitureCategory.WorkDesk)
            {
                return InteractReadStateName;
            }

            return _context?.RuntimeData.TargetFurnitureInteractionType switch
            {
                FurnitureInteractionType.PlayHarp => InteractReadStateName,
                FurnitureInteractionType.PlayGuitar => InteractReadStateName,
                FurnitureInteractionType.PaintAtEasel => InteractReadStateName,
                FurnitureInteractionType.ViewPhotoBoard => InteractReadStateName,
                FurnitureInteractionType.LeisureEngage => InteractReadStateName,
                FurnitureInteractionType.InspectBookshelf => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectMirror => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectNightstand => InteractBesideDoorStateName,
                FurnitureInteractionType.ObservePlant => InteractBesideDoorStateName,
                FurnitureInteractionType.ObserveWindow => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectToy => InteractBesideDoorStateName,
                FurnitureInteractionType.ArrangePillow => InteractBesideDoorStateName,
                FurnitureInteractionType.InspectPapers => InteractBesideDoorStateName,
                FurnitureInteractionType.ListenToAudio => InteractBesideDoorStateName,
                FurnitureInteractionType.OrganizeStorage => InteractBesideDoorStateName,
                FurnitureInteractionType.DecorInspect => InteractBesideDoorStateName,
                FurnitureInteractionType.RestOnRug => MoveFrontStateName,
                FurnitureInteractionType.SitOnSeat => MoveFrontStateName,
                FurnitureInteractionType.LoungeOnSofa => MoveFrontStateName,
                FurnitureInteractionType.SleepInBed => MoveFrontStateName,
                FurnitureInteractionType.SleepRest => MoveFrontStateName,
                _ => MoveFrontStateName
            };
        }

        private static string ResolvePlayerInteractionStateName(string variant)
        {
            return variant switch
            {
                "beside door" => InteractBesideDoorStateName,
                "flower" => InteractBesideDoorStateName,
                "playing music" => InteractReadStateName,
                "read" => InteractReadStateName,
                "sleep" => SleepStateName,
                _ => MoveFrontStateName
            };
        }

        private static string ResolveIdleStateName(Vector2 direction)
        {
            int moveDir = ResolveMoveDirection(direction);
            return moveDir switch
            {
                1 => IdleBackStateName,
                2 => IdleSideStateName,
                _ => IdleFrontStateName
            };
        }

        private void PlayForcedAnimatorState(string stateName)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (_lastForcedAnimatorStateName == stateName)
            {
                return;
            }

            _animator.Play(stateName, 0, 0f);
            _lastForcedAnimatorStateName = stateName;
        }

        private void PublishSnapshotIfChanged(PetContext context)
        {
            if (context.EventBus is null)
            {
                return;
            }

            PetRuntimeData runtime = context.RuntimeData;
            PetRuntimeSnapshotChangedEvent snapshot = new(
                runtime.CurrentState,
                runtime.Mood,
                runtime.Energy,
                runtime.Satiety,
                runtime.WorkRequested,
                runtime.TargetFurnitureId,
                runtime.TargetFurnitureCategory,
                runtime.TargetFurnitureInteractionType,
                runtime.IsTraveling,
                runtime.LastInteractionFurnitureId,
                runtime.LastInteractionSummary,
                petId: runtime.PetId);

            if (_lastPublishedSnapshot.HasValue && AreSnapshotsEquivalent(_lastPublishedSnapshot.Value, snapshot))
            {
                return;
            }

            _lastPublishedSnapshot = snapshot;
            context.EventBus.Publish(snapshot);
        }

        private static bool AreSnapshotsEquivalent(PetRuntimeSnapshotChangedEvent previous, PetRuntimeSnapshotChangedEvent current)
        {
            return previous.PetId == current.PetId &&
                   previous.CurrentState == current.CurrentState &&
                   Mathf.Abs(previous.Mood - current.Mood) < 0.01f &&
                   Mathf.Abs(previous.Energy - current.Energy) < 0.01f &&
                   Mathf.Abs(previous.Satiety - current.Satiety) < 0.01f &&
                   previous.WorkRequested == current.WorkRequested &&
                   previous.TargetFurnitureId == current.TargetFurnitureId &&
                   previous.TargetFurnitureCategory == current.TargetFurnitureCategory &&
                   previous.TargetFurnitureInteractionType == current.TargetFurnitureInteractionType &&
                   previous.IsTraveling == current.IsTraveling &&
                   previous.LastInteractionFurnitureId == current.LastInteractionFurnitureId &&
                   previous.LastInteractionSummary == current.LastInteractionSummary;
        }

        private void EnsureAnimatorBinding()
        {
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogWarning("[PetController] Failed to ensure Animator component on pet object.", this);
                return;
            }

            if (_movementController != null && _animator.runtimeAnimatorController != _movementController)
            {
                _animator.runtimeAnimatorController = _movementController;
            }
        }
    }
}
