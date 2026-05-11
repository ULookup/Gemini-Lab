#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 一副塔罗牌。MVP 阶段只装 22 张大阿卡那。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/Tarot Deck", fileName = "TarotDeck")]
    public sealed class TarotDeckSO : ScriptableObject
    {
        [SerializeField] private List<TarotCardSO> _cards = new();
        [SerializeField] private Sprite? _cardBack;

        public IReadOnlyList<TarotCardSO> Cards => _cards;
        public Sprite? CardBack => _cardBack;

#if UNITY_EDITOR
        public void SetCardsEditorOnly(IEnumerable<TarotCardSO> cards, Sprite? cardBack)
        {
            _cards = new List<TarotCardSO>(cards);
            _cardBack = cardBack;
        }
#endif
    }
}
