#nullable enable
using GeminiLab.Modules.Pet.Personality;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 性格维度枚举，与 <see cref="PersonalityVector"/> 字段一一对应。
    /// 用于行为配置里的性格标签（数值规则文档 §2）。
    /// </summary>
    public enum PetTrait
    {
        Kindness = 0,
        Evilness = 1,
        Calmness = 2,
        Bravery = 3,
        Shyness = 4,
        Integrity = 5,
        Curiosity = 6
    }

    public static class PetTraitExtensions
    {
        /// <summary>取性格向量对应维度的值（-1..1，等价于文档 0~100 量表的 (Trait-50)/50）。</summary>
        public static float GetTrait(this PersonalityVector vector, PetTrait trait)
        {
            return trait switch
            {
                PetTrait.Kindness => vector.Kindness,
                PetTrait.Evilness => vector.Evilness,
                PetTrait.Calmness => vector.Calmness,
                PetTrait.Bravery => vector.Bravery,
                PetTrait.Shyness => vector.Shyness,
                PetTrait.Integrity => vector.Integrity,
                PetTrait.Curiosity => vector.Curiosity,
                _ => 0f
            };
        }
    }
}
