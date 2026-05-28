#nullable enable

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗抽卡事件（轻量版）。由 TarotBootstrap 在抽卡时发布，
    /// 供 Personality 等在 Core 层的模块订阅，避免循环程序集依赖。
    /// </summary>
    public readonly struct CardDrawnEvent
    {
        public CardDrawnEvent(string cardId, TarotOrientation orientation)
        {
            CardId = cardId;
            Orientation = orientation;
        }

        public string CardId { get; }
        public TarotOrientation Orientation { get; }
    }
}
