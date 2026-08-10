#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Modules.EmotionGarden
{
    /// <summary>
    /// 情绪花美术目录。由 Scene 作者化流程绑定资源，运行时只按情绪类型、培育者和状态查询精灵。
    /// </summary>
    [CreateAssetMenu(fileName = "EmotionFlowerArtCatalog", menuName = "GeminiLab/Emotion Garden/Flower Art Catalog")]
    public sealed class EmotionFlowerArtCatalog : ScriptableObject
    {
        [Serializable]
#pragma warning disable CS0649
        private struct Entry
        {
            public string EmotionType;
            public string Owner;
            public Sprite? GrowingSprite;
            public Sprite? BloomedSprite;
        }
#pragma warning restore CS0649

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public Sprite? Resolve(string emotionType, string owner, GrowthState state)
        {
            string canonicalEmotion = EmotionFlowerCatalog.NormalizeEmotionType(emotionType);
            string canonicalOwner = EmotionFlowerCatalog.NormalizeOwner(owner);

            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (!string.Equals(entry.EmotionType, canonicalEmotion, StringComparison.Ordinal) ||
                    !string.Equals(entry.Owner, canonicalOwner, StringComparison.Ordinal))
                {
                    continue;
                }

                if (state == GrowthState.Bloomed)
                {
                    return entry.BloomedSprite;
                }

                return entry.GrowingSprite;
            }

            return null;
        }
    }
}
