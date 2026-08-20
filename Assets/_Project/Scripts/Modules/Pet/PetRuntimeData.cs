#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet.Behavior;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Runtime mutable pet stats used by FSM decisions.
    /// </summary>
    [Serializable]
    public sealed class PetRuntimeData
    {
        /// <summary>该运行态数据属于哪只宠物。默认 Angel 兼容单宠历史代码。</summary>
        public PetId PetId = PetId.Angel;

        [Range(0f, 100f)]
        public float Mood = 50f;

        [Range(0f, 100f)]
        public float Energy = 100f;

        [Range(0f, 100f)]
        public float Satiety = 75f;

        [Range(0f, 100f)]
        public float Relation = 50f;

        public float TimeInCurrentState;

        public float RuntimeTimeSeconds;

        /// <summary>心情回归中性的累计计时（§13），秒。</summary>
        public float NeutralReturnTimerSeconds;

        public string CurrentState = "None";

        /// <summary>当前正在执行的行为 Id（行为权重系统，§25）；无行为时为 Empty。</summary>
        public string CurrentBehaviorId = string.Empty;

        /// <summary>行为运行态：当前行为、最近 3 个行为、冷却表（§22 BehaviorRuntimeState）。仅运行期，不持久化。</summary>
        public BehaviorRuntimeState BehaviorState = new();

        public bool WorkRequested;

        public Vector2 Position;

        public Vector2 TargetPosition;

        public string TargetFurnitureId = string.Empty;

        public FurnitureCategory TargetFurnitureCategory = FurnitureCategory.Unknown;

        public FurnitureInteractionType TargetFurnitureInteractionType = FurnitureInteractionType.Unknown;

        public float TargetInteractionDurationSeconds = 1f;

        public bool TargetReached;

        public int PathIndex;

        public List<Vector2> ActivePath = new();

        public float PreventSleepBeforeTime;

        public string LastTraceId = string.Empty;

        public string ActiveWorkTraceId = string.Empty;

        public string ActiveWorkMessage = string.Empty;

        public PetWorkTargetType RequiredWorkTargetType = PetWorkTargetType.Any;

        public bool IsAtRequiredWorkTarget;

        public bool IsTraveling;

        public string ActiveTravelTraceId = string.Empty;

        public string ActiveTravelTopic = string.Empty;

        public float TravelEndAtSeconds;

        public int TravelCompletedCount;

        public string LastInteractionFurnitureId = string.Empty;

        public string LastInteractionSummary = string.Empty;

        public bool IsPlayerInteractionActive;

        public float PlayerInteractionRemainingSeconds;

        public string PlayerInteractionAnimationVariant = string.Empty;

        public string PlayerInteractionAnimatorStateName = string.Empty;

        public string PlayerInteractionLabel = string.Empty;
    }
}
