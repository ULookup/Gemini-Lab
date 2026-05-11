#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// <see cref="ITarotService"/> 默认实现。
    /// 每日限制：当前阶段用 PlayerPrefs 记录上次抽卡日期（存档系统 C1 完成后迁移到 SaveSlot.tarot.lastDrawDate）。
    /// </summary>
    public sealed class TarotService : ITarotService
    {
        private const string LastDrawDateKey = "GeminiLab.Tarot.LastDrawDate";

        private readonly TarotDeckSO _deck;
        private readonly EventBus? _eventBus;
        private readonly ITarotReadingBackend _readingBackend;
        private readonly Func<DateTime> _nowProvider;

        public TarotService(
            TarotDeckSO deck,
            EventBus? eventBus,
            ITarotReadingBackend readingBackend,
            Func<DateTime>? nowProvider = null)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _eventBus = eventBus;
            _readingBackend = readingBackend ?? throw new ArgumentNullException(nameof(readingBackend));
            _nowProvider = nowProvider ?? (() => DateTime.Now);
        }

        public string LastDrawDateIso => PlayerPrefs.GetString(LastDrawDateKey, string.Empty);

        public bool CanDrawToday()
        {
            string today = _nowProvider().ToString("yyyy-MM-dd");
            return !string.Equals(LastDrawDateIso, today, StringComparison.Ordinal);
        }

        public TarotDrawResult? DrawDaily(Func<int, int>? randomRange = null)
        {
            if (!CanDrawToday())
            {
                return null;
            }

            if (_deck.Cards == null || _deck.Cards.Count == 0)
            {
                Debug.LogError("[Tarot] Deck 为空，无法抽卡");
                return null;
            }

            Func<int, int> range = randomRange ?? (upper => UnityEngine.Random.Range(0, upper));
            int cardIndex = range(_deck.Cards.Count);
            int orientationRoll = range(2);
            var card = _deck.Cards[cardIndex];
            var orientation = orientationRoll == 0 ? TarotOrientation.Upright : TarotOrientation.Reversed;

            string today = _nowProvider().ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(LastDrawDateKey, today);
            PlayerPrefs.Save();

            var result = new TarotDrawResult(card, orientation, today);
            _eventBus?.Publish(new TarotDrawnEvent(result));
            return result;
        }

        public async Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId, TarotOrientation orientation, CancellationToken cancellationToken = default)
        {
            TarotReading reading;
            try
            {
                reading = await _readingBackend.RequestAsync(draw, petId, orientation, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Tarot] Reading backend failed, falling back. {ex.Message}");
                reading = LocalFallback.Build(draw, petId, orientation);
            }

            _eventBus?.Publish(new TarotReadingReceivedEvent(draw, reading));
            return reading;
        }
    }
}
