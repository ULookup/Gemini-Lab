#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 室外桌宠动画调试入口。
    /// 数字键只用于当前动画联调，不代表最终策划触发条件。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class WorldMapPetAnimationTriggerController : MonoBehaviour
    {
        [Serializable]
        public sealed class DebugAnimationBinding
        {
            [SerializeField, Range(1, 9)] private int _keyNumber = 1;
            [SerializeField] private PetId _petId;
            [SerializeField] private string _label = string.Empty;
            [SerializeField] private string _animationStateName = string.Empty;
            [SerializeField, Min(0.1f)] private float _durationSeconds = 2f;

            public DebugAnimationBinding()
            {
            }

            public DebugAnimationBinding(
                int keyNumber,
                PetId petId,
                string label,
                string animationStateName,
                float durationSeconds)
            {
                _keyNumber = Mathf.Clamp(keyNumber, 1, 9);
                _petId = petId;
                _label = label;
                _animationStateName = animationStateName;
                _durationSeconds = Mathf.Max(0.1f, durationSeconds);
            }

            public int KeyNumber => Mathf.Clamp(_keyNumber, 1, 9);

            public PetId PetId => _petId;

            public string Label => _label;

            public string AnimationStateName => _animationStateName;

            public float DurationSeconds => Mathf.Max(0.1f, _durationSeconds);
        }

        private sealed class ActiveAnimation
        {
            public ActiveAnimation(
                PetController pet,
                Animator animator,
                string stateName,
                float durationSeconds,
                float clipLengthSeconds)
            {
                Pet = pet;
                Animator = animator;
                StateName = stateName;
                DurationSeconds = Mathf.Max(0.1f, durationSeconds);
                ClipLengthSeconds = Mathf.Max(0.01f, clipLengthSeconds);
            }

            public PetController Pet { get; }

            public Animator Animator { get; }

            public string StateName { get; }

            public float DurationSeconds { get; }

            public float ClipLengthSeconds { get; }

            public float ElapsedSeconds { get; set; }
        }

        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha0,
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };

        private static readonly KeyCode[] KeypadNumberKeys =
        {
            KeyCode.Keypad0,
            KeyCode.Keypad1,
            KeyCode.Keypad2,
            KeyCode.Keypad3,
            KeyCode.Keypad4,
            KeyCode.Keypad5,
            KeyCode.Keypad6,
            KeyCode.Keypad7,
            KeyCode.Keypad8,
            KeyCode.Keypad9
        };

        [SerializeField] private PetController? _angelPet;
        [SerializeField] private PetController? _devilPet;
        [SerializeField] private DebugAnimationBinding[] _bindings = Array.Empty<DebugAnimationBinding>();

        private ActiveAnimation? _activeAnimation;

        public IReadOnlyList<DebugAnimationBinding> Bindings => _bindings;

        private void OnDisable()
        {
            ReleaseActiveAnimation();
        }

        private void Update()
        {
            if (_activeAnimation != null)
            {
                TickActiveAnimation();
                return;
            }

            if (!TryReadNumberKey(out int keyNumber))
            {
                return;
            }

            TryTriggerForKey(keyNumber);
        }

        /// <summary>
        /// 供编辑器作者化脚本配置数字调试映射。映射本身保存为 Scene 中的序列化数据。
        /// </summary>
        public void ConfigureForAuthoring(PetController? angelPet, PetController? devilPet)
        {
            _angelPet = angelPet;
            _devilPet = devilPet;
            _bindings = new[]
            {
                new DebugAnimationBinding(1, PetId.Angel, "天使 - 坐地", "Outdoor_Sit", 2.5f),
                new DebugAnimationBinding(2, PetId.Angel, "天使 - 祈祷", "Outdoor_Pray", 2.5f),
                new DebugAnimationBinding(3, PetId.Angel, "天使 - 开心", "Outdoor_Happy", 2f),
                new DebugAnimationBinding(4, PetId.Angel, "天使 - 浇水", "Outdoor_Water", 2f),
                new DebugAnimationBinding(5, PetId.Devil, "恶魔 - 睡觉", "Outdoor_Sleep", 2.5f),
                new DebugAnimationBinding(6, PetId.Devil, "恶魔 - 施法", "Outdoor_Cast", 2f),
                new DebugAnimationBinding(7, PetId.Devil, "恶魔 - 得意", "Outdoor_Proud", 2f)
            };
        }

        public bool TryTriggerForKey(int keyNumber)
        {
            if (_activeAnimation != null)
            {
                return false;
            }

            DebugAnimationBinding? binding = FindBinding(keyNumber);
            if (binding == null)
            {
                Debug.LogWarning($"[WorldMapPetAnimation] 未配置数字键 {keyNumber} 的动画映射。", this);
                return false;
            }

            PetController? pet = binding.PetId == PetId.Angel ? _angelPet : _devilPet;
            if (pet == null)
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] 数字键 {binding.KeyNumber} 找不到 {binding.PetId} 的 WorldMap 桌宠。",
                    this);
                return false;
            }

            return TryTriggerForPet(pet, binding);
        }

        private bool TryTriggerForPet(PetController pet, DebugAnimationBinding binding)
        {
            Animator? animator = pet.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] 无法触发 '{binding.AnimationStateName}'：'{pet.name}' 缺少 Animator 或状态机。",
                    pet);
                return false;
            }

            if (!TryResolveStateName(animator, binding.AnimationStateName, out string resolvedStateName))
            {
                Debug.LogWarning(
                    $"[WorldMapPetAnimation] 状态 '{binding.AnimationStateName}' 不存在于 '{animator.runtimeAnimatorController.name}'。",
                    pet);
                return false;
            }

            animator.speed = 1f;
            animator.Play(resolvedStateName, 0, 0f);
            animator.Update(0f);
            float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
            pet.SetExternalMovementLock(true);
            _activeAnimation = new ActiveAnimation(
                pet,
                animator,
                resolvedStateName,
                binding.DurationSeconds,
                clipLength);

            Debug.Log(
                $"[WorldMapPetAnimation] 数字键 {binding.KeyNumber} 触发 '{binding.Label}'，状态='{binding.AnimationStateName}'。",
                pet);
            return true;
        }

        private void TickActiveAnimation()
        {
            ActiveAnimation? activeAnimation = _activeAnimation;
            if (activeAnimation == null)
            {
                return;
            }

            if (activeAnimation.Pet == null || activeAnimation.Animator == null)
            {
                ReleaseActiveAnimation();
                return;
            }

            activeAnimation.ElapsedSeconds += Time.deltaTime;
            if (activeAnimation.ElapsedSeconds >= activeAnimation.DurationSeconds)
            {
                activeAnimation.Animator.speed = 1f;
                ReleaseActiveAnimation();
                return;
            }

            // PetController 会在自己的 Update 中刷新待机/移动；本组件最后执行，持续维持调试动画。
            float normalizedTime = activeAnimation.ElapsedSeconds / activeAnimation.ClipLengthSeconds;
            activeAnimation.Animator.Play(activeAnimation.StateName, 0, normalizedTime);
            activeAnimation.Animator.speed = 1f;
        }

        private void ReleaseActiveAnimation()
        {
            ActiveAnimation? activeAnimation = _activeAnimation;
            if (activeAnimation?.Pet != null)
            {
                activeAnimation.Pet.SetExternalMovementLock(false);
            }

            _activeAnimation = null;
        }

        private DebugAnimationBinding? FindBinding(int keyNumber)
        {
            for (int index = 0; index < _bindings.Length; index++)
            {
                DebugAnimationBinding binding = _bindings[index];
                if (binding != null && binding.KeyNumber == keyNumber)
                {
                    return binding;
                }
            }

            return null;
        }

        private static bool TryReadNumberKey(out int keyNumber)
        {
            for (int number = 1; number <= 9; number++)
            {
                if (Input.GetKeyDown(NumberKeys[number]) || Input.GetKeyDown(KeypadNumberKeys[number]))
                {
                    keyNumber = number;
                    return true;
                }
            }

            keyNumber = 0;
            return false;
        }

        private static bool TryResolveStateName(Animator animator, string requestedStateName, out string resolvedStateName)
        {
            resolvedStateName = requestedStateName;
            if (string.IsNullOrWhiteSpace(requestedStateName))
            {
                return false;
            }

            int requestedHash = Animator.StringToHash(requestedStateName);
            if (animator.HasState(0, requestedHash))
            {
                return true;
            }

            string baseLayerStateName = $"Base Layer.{requestedStateName}";
            if (animator.HasState(0, Animator.StringToHash(baseLayerStateName)))
            {
                resolvedStateName = baseLayerStateName;
                return true;
            }

            return false;
        }
    }
}
