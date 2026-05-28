#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using UnityEngine;

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
        /// <summary>总结轮结构化结果（第 7 次 LLM 调用返回）。</summary>
        public TarotSummaryResult? SummaryResult;

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

    /// <summary>总结轮幸运提示（LLM 结构化返回的子对象）。</summary>
    [Serializable]
    public sealed class LuckyHintData
    {
        public string color;
        public string number;
        public string time;
        public string action;
    }

    /// <summary>总结轮 LLM 返回的结构化数据。</summary>
    [Serializable]
    public sealed class TarotSummaryResult
    {
        public int fortuneLevel;
        public LuckyHintData luckyHint;
        public string advice;

        public static TarotSummaryResult FromJson(string json)
        {
            try
            {
                var result = JsonUtility.FromJson<TarotSummaryResult>(json);
                if (result == null) return Default();
                result.fortuneLevel = Mathf.Clamp(result.fortuneLevel, 1, 5);
                result.luckyHint ??= new LuckyHintData();
                result.advice ??= string.Empty;
                return result;
            }
            catch (Exception)
            {
                return Default();
            }
        }

        public static TarotSummaryResult Default()
        {
            return new TarotSummaryResult
            {
                fortuneLevel = 3,
                luckyHint = new LuckyHintData
                {
                    color = "蓝色",
                    number = "7",
                    time = "午后",
                    action = "保持平常心"
                },
                advice = "今日运势平稳，保持平常心，关注身边的小确幸。"
            };
        }
    }
}
