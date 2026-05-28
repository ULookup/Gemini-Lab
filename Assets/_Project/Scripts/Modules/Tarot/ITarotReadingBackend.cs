#nullable enable
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗解读后端。默认由 Gateway 实现；测试时可注入 mock。
    /// </summary>
    public interface ITarotReadingBackend
    {
        Task<TarotReading> RequestAsync(
            TarotDrawResult draw,
            PetId petId,
            TarotOrientation orientation,
            CancellationToken cancellationToken);

        /// <summary>
        /// 请求总结轮结构化运势解读。传入三张已选牌 + 用户问题。
        /// </summary>
        Task<TarotSummaryResult> RequestSummaryAsync(
            TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
            string? question, CancellationToken cancellationToken);
    }
}
