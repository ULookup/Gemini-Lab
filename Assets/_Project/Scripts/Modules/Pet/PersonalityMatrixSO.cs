#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Base personality weights used for future prompt shaping.
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Pet/PersonalityMatrix", fileName = "PersonalityMatrix")]
    public sealed class PersonalityMatrixSO : ScriptableObject
    {
        [Range(-1f, 1f)] public float Kindness = 0.5f;
        [Range(-1f, 1f)] public float Evilness = -0.2f;
        [Range(-1f, 1f)] public float Calmness = 0.2f;
        [Range(-1f, 1f)] public float Bravery = 0.1f;
        [Range(-1f, 1f)] public float Shyness = 0f;
        [Range(-1f, 1f)] public float Integrity = 0.4f;
        [Range(-1f, 1f)] public float Curiosity = 0.3f;
    }
}
