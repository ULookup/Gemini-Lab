#nullable enable
using System.Collections.Generic;
using GeminiLab.Modules.Furniture;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Personality
{
    /// <summary>
    /// 性格演化规则。Inspector 可编辑：每一条 → "某事件发生时，给哪只宠物的哪个维度加/减多少"。
    /// MVP 阶段只支持两大事件源：塔罗抽牌、家具交互；旅行/工作结果后续扩展。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Pet/Personality Evolution Rules", fileName = "PersonalityEvolutionRules")]
    public sealed class PersonalityEvolutionRulesSO : ScriptableObject
    {
        [System.Serializable]
        public sealed class TarotRule
        {
            [Tooltip("对应 TarotCardSO.Id；空串 = 任意牌")]
            public string CardId = string.Empty;

            [Tooltip("正位 / 逆位 / 都触发（默认都触发）")]
            public OrientationFilter Filter = OrientationFilter.Both;

            [Tooltip("作用的人格（Angel 或 Devil）；若为 Any 则根据塔罗正位归 Angel / 逆位归 Devil")]
            public PetIdFilter TargetPet = PetIdFilter.Any;

            public PersonalityVector Delta;
        }

        [System.Serializable]
        public sealed class FurnitureInteractionRule
        {
            [Tooltip("Any = 忽略类型")]
            public FurnitureInteractionType Type = FurnitureInteractionType.Unknown;

            public PetIdFilter TargetPet = PetIdFilter.Any;

            public PersonalityVector Delta;
        }

        public enum OrientationFilter { Both, UprightOnly, ReversedOnly }
        public enum PetIdFilter { Any, Angel, Devil }

        [Header("塔罗抽卡 → 性格演化")]
        public List<TarotRule> TarotRules = new();

        [Header("家具交互完成 → 性格演化")]
        public List<FurnitureInteractionRule> FurnitureRules = new();

        [Tooltip("一条规则未指定 CardId / InteractionType 时视为通配；同一事件匹配多条时按顺序全部累加")]
        public float GlobalDeltaScale = 0.02f;
    }
}
