#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 单张塔罗牌配置：编号、中英文名、正逆位关键词、卡面 Sprite。
    /// 运行期只读。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/Tarot Card", fileName = "TarotCard")]
    public sealed class TarotCardSO : ScriptableObject
    {
        [Tooltip("大阿卡那编号 0..21")]
        [SerializeField] private int _majorIndex;

        [Tooltip("英文标识，例如 the_fool / the_magician，供代码引用")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("中文名，例如 愚者 / 魔术师")]
        [SerializeField] private string _displayNameZh = string.Empty;

        [Tooltip("英文名，例如 The Fool / The Magician")]
        [SerializeField] private string _displayNameEn = string.Empty;

        [Tooltip("正位关键词，用于 Gateway prompt 与占位解读")]
        [SerializeField] private string[] _uprightKeywords = System.Array.Empty<string>();

        [Tooltip("逆位关键词，用于 Gateway prompt 与占位解读")]
        [SerializeField] private string[] _reversedKeywords = System.Array.Empty<string>();

        [Tooltip("卡面 Sprite；美术交付后替换")]
        [SerializeField] private Sprite? _artwork;

        public int MajorIndex => _majorIndex;
        public string Id => _id;
        public string DisplayNameZh => _displayNameZh;
        public string DisplayNameEn => _displayNameEn;
        public System.Collections.Generic.IReadOnlyList<string> UprightKeywords => _uprightKeywords;
        public System.Collections.Generic.IReadOnlyList<string> ReversedKeywords => _reversedKeywords;
        public Sprite? Artwork => _artwork;

        public System.Collections.Generic.IReadOnlyList<string> GetKeywords(TarotOrientation orientation)
        {
            return orientation == TarotOrientation.Upright ? _uprightKeywords : _reversedKeywords;
        }
    }
}
