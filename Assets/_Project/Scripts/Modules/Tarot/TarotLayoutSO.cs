#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗选牌 UI 布局参数。美术/策划在 Inspector 调整，脚本只读。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/Tarot Layout", fileName = "TarotLayout")]
    public sealed class TarotLayoutSO : ScriptableObject
    {
        [Header("弧形排列")]
        [Tooltip("弧形半径（像素）")]
        [SerializeField] private float _arcRadius = 400f;

        [Tooltip("弧形展开角度（度）")]
        [SerializeField] private float _arcSpanAngle = 160f;

        [Tooltip("展示牌数量")]
        [SerializeField] private int _cardSpreadCount = 11;

        [Header("Hover 效果")]
        [Tooltip("hover 放大倍率")]
        [SerializeField] private float _hoverScale = 1.15f;

        [Tooltip("hover 上浮距离（像素）")]
        [SerializeField] private float _hoverLift = 30f;

        [Header("动画")]
        [Tooltip("牌浮现动画时长（秒）")]
        [SerializeField] private float _cardAppearDuration = 0.4f;

        [Tooltip("牌飞入槽位动画时长（秒）")]
        [SerializeField] private float _cardFlyDuration = 0.5f;

        [Tooltip("每幕解读揭晓之间最小间隔（秒）")]
        [SerializeField] private float _revealIntervalSeconds = 1.5f;

        [Header("等待文案")]
        [Tooltip("等待解读时的情境文案池，按位置分组")]
        [SerializeField] private string[] _pastLoadingTexts = new string[]
        {
            "天使正在回望你的过去…",
            "恶魔翻开了昨日的账本…",
        };

        [SerializeField] private string[] _presentLoadingTexts = new string[]
        {
            "天使在凝视此刻的因果…",
            "恶魔端详着你现在的选择…",
        };

        [SerializeField] private string[] _futureLoadingTexts = new string[]
        {
            "天使在为你铺展前路…",
            "恶魔看到了你想要又不敢要的东西…",
        };

        public float ArcRadius => _arcRadius;
        public float ArcSpanAngle => _arcSpanAngle;
        public int CardSpreadCount => _cardSpreadCount;
        public float HoverScale => _hoverScale;
        public float HoverLift => _hoverLift;
        public float CardAppearDuration => _cardAppearDuration;
        public float CardFlyDuration => _cardFlyDuration;
        public float RevealIntervalSeconds => _revealIntervalSeconds;

        public string GetRandomLoadingText(TarotSlotPosition slot, bool isAngel)
        {
            var pool = slot switch
            {
                TarotSlotPosition.Past => _pastLoadingTexts,
                TarotSlotPosition.Present => _presentLoadingTexts,
                TarotSlotPosition.Future => _futureLoadingTexts,
                _ => _pastLoadingTexts
            };
            if (pool.Length == 0) return "…";
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
    }
}
