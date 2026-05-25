#nullable enable
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗对外服务。支持创建抽牌会话（从 22 张随机选 11 张备选），
    /// 玩家手动选 3 张后并发请求解读。
    /// </summary>
    public interface ITarotService
    {
        /// <summary>塔罗牌堆，供图鉴等 UI 遍历展示。</summary>
        TarotDeckSO Deck { get; }

        /// <summary>创建一次新的抽牌会话。从 Deck 随机取 11 张备选。</summary>
        TarotSession CreateSession(string? question);

        /// <summary>洗牌：重新随机 11 张备选，清空已选。</summary>
        TarotSession ShuffleCards(TarotSession session);

        /// <summary>将指定牌填入下一个可用槽位（Past→Present→Future）。</summary>
        TarotSession PickCard(TarotSession session, TarotCardSO card);

        /// <summary>确认选牌完成。</summary>
        TarotSession ConfirmSelection(TarotSession session);

        /// <summary>
        /// 请求一次解读：以指定 PetId 的人格 + 指定正/逆位对牌面进行解读。
        /// 完成后会通过 EventBus 广播 <see cref="TarotReadingReceivedEvent"/>。
        /// </summary>
        Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId,
            TarotOrientation orientation, CancellationToken cancellationToken = default);

        /// <summary>
        /// 请求总结轮结构化运势解读。
        /// </summary>
        Task<TarotSummaryResult> RequestSummaryAsync(
            TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
            string? question, CancellationToken cancellationToken = default);
    }
}
