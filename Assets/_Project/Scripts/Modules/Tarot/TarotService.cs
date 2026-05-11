#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// <see cref="ITarotService"/> 默认实现。
    /// 日期来源统一走 <see cref="IGameClock"/>；PlayerPrefs 仍作为临时存档载体，
    /// C1 存档整合后会迁移到 SaveSlot.tarot.lastDrawDate。
    /// </summary>
    public sealed class TarotService : ITarotService
    {
        private const string LastDrawDateKey = "GeminiLab.Tarot.LastDrawDate";

        private readonly TarotDeckSO _deck;
        private readonly EventBus? _eventBus;
        private readonly ITarotReadingBackend _readingBackend;
        private readonly IGameClock _clock;

        public TarotService(
            TarotDeckSO deck,
            EventBus? eventBus,
            ITarotReadingBackend readingBackend,
            IGameClock? clock = null)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _eventBus = eventBus;
            _readingBackend = readingBackend ?? throw new ArgumentNullException(nameof(readingBackend));
            _clock = clock ?? ResolveClockOrFallback();
        }

        public string LastDrawDateIso => PlayerPrefs.GetString(LastDrawDateKey, string.Empty);

        public bool CanDrawToday()
        {
            return !_clock.IsToday(LastDrawDateIso);
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

            string today = _clock.TodayIso;
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

        private static IGameClock ResolveClockOrFallback()
        {
            if (ServiceLocator.TryResolve(out IGameClock? resolved) && resolved is not null)
            {
                return resolved;
            }

            Debug.LogWarning("[Tarot] 未在 ServiceLocator 找到 IGameClock，回退到 SystemGameClock");
            return new SystemGameClock();
        }
    }
}
