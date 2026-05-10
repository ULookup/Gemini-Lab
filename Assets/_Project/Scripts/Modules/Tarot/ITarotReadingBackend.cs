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
    }
}
