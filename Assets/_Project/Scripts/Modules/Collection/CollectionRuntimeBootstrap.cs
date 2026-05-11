#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 挂 Boot.BootstrapRoot。Awake 时注册 CollectionService；
    /// 同时订阅 <see cref="TarotDrawnEvent"/> 把每日塔罗记录自动落入收藏。
    /// 旅行/花园等系统后续加订阅即可（解耦：不动它们的代码）。
    /// </summary>
    public sealed class CollectionRuntimeBootstrap : MonoBehaviour
    {
        private EventBus? _eventBus;
        private IDisposable? _tarotDrawnSub;
        private IGameClock? _clock;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            ServiceLocator.TryResolve(out _eventBus);
            ServiceLocator.TryResolve(out _clock);

            var service = new CollectionService(_eventBus);
            ServiceLocator.Register<ICollectionService>(service);

            if (_eventBus is not null)
            {
                _tarotDrawnSub = _eventBus.Subscribe<TarotDrawnEvent>(OnTarotDrawn);
            }

            Debug.Log("[CollectionBootstrap] CollectionService registered.");
        }

        private void OnDestroy() { _tarotDrawnSub?.Dispose(); }

        private void OnTarotDrawn(TarotDrawnEvent evt)
        {
            if (!ServiceLocator.TryResolve(out ICollectionService? collection) || collection is null) return;

            var card = evt.Result.Card;
            string orientZh = evt.Result.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            string dateIso = _clock?.TodayIso ?? evt.Result.DrawDateIso;

            collection.Add(new CollectionEntry
            {
                Id = $"tarot_{evt.Result.DrawDateIso}_{card.Id}_{orientZh}",
                Category = CollectionCategory.Tarot,
                Title = $"{card.DisplayNameZh} · {orientZh}",
                Description = $"{dateIso} 抽得 {card.DisplayNameZh}（{card.DisplayNameEn}）·{orientZh}",
                AcquiredDateIso = dateIso,
                IconKey = $"tarot_{card.Id}"
            });
        }
    }
}
