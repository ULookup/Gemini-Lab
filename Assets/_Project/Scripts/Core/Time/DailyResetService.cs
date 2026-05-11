#nullable enable
using System;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using UnityEngine;

namespace GeminiLab.Core.Time
{
    /// <summary>
    /// 默认 <see cref="IDailyResetService"/>。
    /// - 日期来源：<see cref="IGameClock"/>
    /// - 持久化：实现 <see cref="IPersistentService"/>，lastRecordedDate 进 SaveBundle；
    ///   冷启动（未加载存档）回退到 PlayerPrefs，保证跨次启动也能正确判跨天
    /// </summary>
    public sealed class DailyResetService : IDailyResetService, IPersistentService
    {
        private const string PlayerPrefsKey = "GeminiLab.DailyReset.LastDate";

        private readonly IGameClock _clock;
        private readonly EventBus? _eventBus;

        private string _lastRecordedDateIso;

        public DailyResetService(IGameClock clock, EventBus? eventBus)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventBus = eventBus;
            _lastRecordedDateIso = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        }

        public string Key => "daily_reset";

        public string LastRecordedDateIso => _lastRecordedDateIso;

        public bool CheckAndReset()
        {
            string today = _clock.TodayIso;
            if (string.Equals(today, _lastRecordedDateIso, StringComparison.Ordinal))
            {
                return false;
            }

            string previous = _lastRecordedDateIso;
            _lastRecordedDateIso = today;
            PlayerPrefs.SetString(PlayerPrefsKey, today);
            PlayerPrefs.Save();

            _eventBus?.Publish(new NewDayStartedEvent(previous, today));
            return true;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public string lastRecordedDateIso;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload
            {
                version = 1,
                lastRecordedDateIso = _lastRecordedDateIso ?? string.Empty
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                _lastRecordedDateIso = payload.lastRecordedDateIso ?? string.Empty;
                PlayerPrefs.SetString(PlayerPrefsKey, _lastRecordedDateIso);
                PlayerPrefs.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
