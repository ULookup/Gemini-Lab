#nullable enable
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>单次抽卡结果。</summary>
    public readonly struct TarotDrawResult
    {
        public TarotDrawResult(TarotCardSO card, TarotOrientation orientation, string drawDateIso)
        {
            Card = card;
            Orientation = orientation;
            DrawDateIso = drawDateIso;
        }

        public TarotCardSO Card { get; }
        public TarotOrientation Orientation { get; }
        public string DrawDateIso { get; }
    }

    /// <summary>一次塔罗解读（来自某只宠物人格 + 正/逆位）。</summary>
    public readonly struct TarotReading
    {
        public TarotReading(PetId petId, TarotOrientation orientation, string text, bool isFromGateway)
        {
            PetId = petId;
            Orientation = orientation;
            Text = text;
            IsFromGateway = isFromGateway;
        }

        public PetId PetId { get; }
        public TarotOrientation Orientation { get; }
        /// <summary>解读正文；首期不做流式，整段返回。</summary>
        public string Text { get; }
        /// <summary>true = Gateway 真实返回；false = 本地占位（Gateway 未就绪 / 未配置）。</summary>
        public bool IsFromGateway { get; }
    }

    /// <summary>EventBus：抽卡结束（还未调 Gateway）。</summary>
    public readonly struct TarotDrawnEvent
    {
        public TarotDrawnEvent(TarotDrawResult result) { Result = result; }
        public TarotDrawResult Result { get; }
    }

    /// <summary>EventBus：某只宠物给出了解读。双宠各发一次。</summary>
    public readonly struct TarotReadingReceivedEvent
    {
        public TarotReadingReceivedEvent(TarotDrawResult draw, TarotReading reading)
        {
            Draw = draw;
            Reading = reading;
        }

        public TarotDrawResult Draw { get; }
        public TarotReading Reading { get; }
    }
}
