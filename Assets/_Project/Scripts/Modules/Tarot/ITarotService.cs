#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Pet;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 塔罗对外服务。每日最多抽一次；抽完后由调用方并行发起 Angel 正位 / Devil 逆位解读。
    /// </summary>
    public interface ITarotService
    {
        /// <summary>今日是否还能抽卡（基于本地日期）。</summary>
        bool CanDrawToday();

        /// <summary>上次抽卡日期（ISO yyyy-MM-dd），从未抽过返回空串。</summary>
        string LastDrawDateIso { get; }

        /// <summary>
        /// 随机抽一张牌 + 随机正/逆位。失败（今天已抽 / 牌堆为空）返回 null。
        /// 成功时会通过 EventBus 广播 <see cref="TarotDrawnEvent"/>，并把今天记为已抽。
        /// </summary>
        TarotDrawResult? DrawDaily(Func<int, int>? randomRange = null);

        /// <summary>
        /// 请求一次解读：以指定 PetId 的人格 + 指定正/逆位对牌面进行解读。
        /// 完成后会通过 EventBus 广播 <see cref="TarotReadingReceivedEvent"/>。
        /// Gateway 未接入或报错时会回退到本地关键词拼接，仍会广播事件（<see cref="TarotReading.IsFromGateway"/>=false）。
        /// </summary>
        Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId, TarotOrientation orientation, CancellationToken cancellationToken = default);
    }
}
