#nullable enable
using System.Collections.Generic;
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

    /// <summary>三张牌的槽位位置。</summary>
    public enum TarotSlotPosition
    {
        Past = 0,
        Present = 1,
        Future = 2
    }

    /// <summary>
    /// 一次完整的抽牌会话。由 TarotPanelStub 持有和驱动。
    /// </summary>
    public sealed class TarotSession
    {
        public string Question;
        public string SessionDateIso;
        public List<TarotCardSO> CandidateCards = new();
        public TarotDrawResult? PastCard;
        public TarotDrawResult? PresentCard;
        public TarotDrawResult? FutureCard;
        public int PickedCount;
        /// <summary>key = "past_angel" / "past_devil" / "present_angel" 等</summary>
        public Dictionary<string, TarotReading> Readings = new();
        public int RevealedSlotIndex;

        public TarotDrawResult? GetCardAtSlot(TarotSlotPosition slot)
        {
            return slot switch
            {
                TarotSlotPosition.Past => PastCard,
                TarotSlotPosition.Present => PresentCard,
                TarotSlotPosition.Future => FutureCard,
                _ => null
            };
        }

        public void SetCardAtSlot(TarotSlotPosition slot, TarotDrawResult draw)
        {
            switch (slot)
            {
                case TarotSlotPosition.Past: PastCard = draw; break;
                case TarotSlotPosition.Present: PresentCard = draw; break;
                case TarotSlotPosition.Future: FutureCard = draw; break;
            }
        }

        public static string ReadingKey(TarotSlotPosition slot, PetId petId)
        {
            string slotName = slot switch
            {
                TarotSlotPosition.Past => "past",
                TarotSlotPosition.Present => "present",
                TarotSlotPosition.Future => "future",
                _ => "unknown"
            };
            string petName = petId == PetId.Angel ? "angel" : "devil";
            return $"{slotName}_{petName}";
        }
    }
}
