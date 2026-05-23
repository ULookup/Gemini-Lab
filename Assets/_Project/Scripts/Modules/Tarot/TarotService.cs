#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// <see cref="ITarotService"/> 默认实现。
    /// 日期来源统一走 <see cref="IGameClock"/>；
    /// Phase E 起实现 <see cref="IPersistentService"/>，上次抽卡日期随 SaveSlot 走；
    /// PlayerPrefs 仍保留为 Cold-start（SaveSystem 未注册或未加载存档时）的兜底。
    /// </summary>
    public sealed class TarotService : ITarotService, IPersistentService
    {
        private const string LastDrawDateKey = "GeminiLab.Tarot.LastDrawDate";

        private readonly TarotDeckSO _deck;
        private readonly EventBus? _eventBus;
        private readonly ITarotReadingBackend _readingBackend;
        private readonly IGameClock _clock;

        private string _lastDrawDateIso;

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
            _lastDrawDateIso = PlayerPrefs.GetString(LastDrawDateKey, string.Empty);
        }

        public string Key => "tarot";

        public TarotDeckSO Deck => _deck;

        public string LastDrawDateIso => _lastDrawDateIso;

        public bool CanDrawToday()
        {
            return !_clock.IsToday(_lastDrawDateIso);
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
            _lastDrawDateIso = today;
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

        // ---- IPersistentService ----
        [Serializable]
        private struct SavePayload
        {
            public int version;
            public string lastDrawDateIso;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload { version = 1, lastDrawDateIso = _lastDrawDateIso ?? string.Empty });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _lastDrawDateIso = payload.lastDrawDateIso ?? string.Empty;
                // 同步回 PlayerPrefs，作为 SaveSystem 不可用时的 cold-start 兜底
                PlayerPrefs.SetString(LastDrawDateKey, _lastDrawDateIso);
                PlayerPrefs.Save();
                return true;
            }
            catch
            {
                return false;
            }
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
