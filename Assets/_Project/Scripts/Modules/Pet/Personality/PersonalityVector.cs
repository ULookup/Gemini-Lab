#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.Pet.Personality
{
    /// <summary>
    /// 7 维性格运行态（值域 -1..1）。
    /// 与静态 <see cref="PersonalityMatrixSO"/> 的字段一一对应；
    /// 后者提供初始值，本结构承载运行期演化增量。
    /// </summary>
    [Serializable]
    public struct PersonalityVector
    {
        [Range(-1f, 1f)] public float Kindness;
        [Range(-1f, 1f)] public float Evilness;
        [Range(-1f, 1f)] public float Calmness;
        [Range(-1f, 1f)] public float Bravery;
        [Range(-1f, 1f)] public float Shyness;
        [Range(-1f, 1f)] public float Integrity;
        [Range(-1f, 1f)] public float Curiosity;

        public static PersonalityVector FromSO(PersonalityMatrixSO? so)
        {
            if (so == null) return default;
            return new PersonalityVector
            {
                Kindness = so.Kindness,
                Evilness = so.Evilness,
                Calmness = so.Calmness,
                Bravery = so.Bravery,
                Shyness = so.Shyness,
                Integrity = so.Integrity,
                Curiosity = so.Curiosity
            };
        }

        public PersonalityVector Clamp()
        {
            return new PersonalityVector
            {
                Kindness = Mathf.Clamp(Kindness, -1f, 1f),
                Evilness = Mathf.Clamp(Evilness, -1f, 1f),
                Calmness = Mathf.Clamp(Calmness, -1f, 1f),
                Bravery = Mathf.Clamp(Bravery, -1f, 1f),
                Shyness = Mathf.Clamp(Shyness, -1f, 1f),
                Integrity = Mathf.Clamp(Integrity, -1f, 1f),
                Curiosity = Mathf.Clamp(Curiosity, -1f, 1f)
            };
        }

        public static PersonalityVector operator +(PersonalityVector a, PersonalityVector b)
        {
            return new PersonalityVector
            {
                Kindness = a.Kindness + b.Kindness,
                Evilness = a.Evilness + b.Evilness,
                Calmness = a.Calmness + b.Calmness,
                Bravery = a.Bravery + b.Bravery,
                Shyness = a.Shyness + b.Shyness,
                Integrity = a.Integrity + b.Integrity,
                Curiosity = a.Curiosity + b.Curiosity
            };
        }
    }
}
