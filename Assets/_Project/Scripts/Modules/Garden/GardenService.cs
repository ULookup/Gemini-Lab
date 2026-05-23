#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core.Events;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Collection;
using GeminiLab.Modules.Inventory;
using UnityEngine;

namespace GeminiLab.Modules.Garden
{
    /// <summary>
    /// 默认花园实现。9 格地块，实时生长（离线补算）。
    /// 生长时间轴：Seeded(0..GrowingStart) → Growing(GrowingStart..Total) → Ready(Total..)。
    /// </summary>
    public sealed class GardenService : IGardenService, IPersistentService
    {
        private const int DefaultPlotCount = 9;

        private readonly IGameClock _clock;
        private readonly IInventoryService _inventory;
        private readonly ICollectionService? _collection;
        private readonly SeedCatalogSO _seeds;
        private readonly EventBus? _eventBus;
        private readonly GardenPlot[] _plots;
        private readonly HashSet<string> _alreadyCollected = new();

        public GardenService(
            IGameClock clock,
            IInventoryService inventory,
            SeedCatalogSO seeds,
            ICollectionService? collection,
            EventBus? eventBus)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _seeds = seeds != null ? seeds : throw new ArgumentNullException(nameof(seeds));
            _collection = collection;
            _eventBus = eventBus;

            _plots = new GardenPlot[DefaultPlotCount];
            for (int i = 0; i < _plots.Length; i++)
            {
                _plots[i] = new GardenPlot { Index = i, Stage = GardenStage.Empty };
            }
        }

        public string Key => "garden";

        public int PlotCount => _plots.Length;

        public IReadOnlyList<GardenPlot> GetAllPlots() => _plots;

        public GardenPlot Get(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plots.Length) return default;
            return _plots[plotIndex];
        }

        public bool Plant(int plotIndex, string seedItemId)
        {
            if (plotIndex < 0 || plotIndex >= _plots.Length) return false;
            if (_plots[plotIndex].Stage != GardenStage.Empty) return false;

            var seed = _seeds.FindBySeedId(seedItemId);
            if (seed == null) return false;
            if (!_inventory.TryRemove(seedItemId, 1)) return false;

            _plots[plotIndex] = new GardenPlot
            {
                Index = plotIndex,
                Stage = GardenStage.Seeded,
                SeedItemId = seedItemId,
                CropItemId = seed.CropItemId,
                PlantedAtUtcTicks = _clock.UtcNow.Ticks,
                TotalGrowSeconds = seed.TotalGrowSeconds
            };

            _eventBus?.Publish(new GardenPlotChangedEvent(_plots[plotIndex]));
            return true;
        }

        public bool Harvest(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plots.Length) return false;
            RefreshPlot(plotIndex);
            if (_plots[plotIndex].Stage != GardenStage.Ready) return false;

            var plot = _plots[plotIndex];
            var seed = _seeds.FindBySeedId(plot.SeedItemId);
            int count = seed?.HarvestCount ?? 1;
            string cropId = plot.CropItemId;

            _inventory.Add(cropId, count);

            if (_collection is not null && !string.IsNullOrEmpty(cropId) && _alreadyCollected.Add(cropId))
            {
                _collection.Add(new CollectionEntry
                {
                    Id = $"garden_{cropId}",
                    Category = CollectionCategory.GardenHarvest,
                    Title = cropId,
                    Description = "花园首次收获",
                    AcquiredDateIso = _clock.TodayIso,
                    IconKey = cropId
                });
            }

            _plots[plotIndex] = new GardenPlot { Index = plotIndex, Stage = GardenStage.Empty };

            _eventBus?.Publish(new GardenHarvestedEvent(plotIndex, cropId, count));
            _eventBus?.Publish(new GardenPlotChangedEvent(_plots[plotIndex]));
            return true;
        }

        public void Refresh()
        {
            for (int i = 0; i < _plots.Length; i++)
            {
                RefreshPlot(i);
            }
        }

        public int GetRemainingSeconds(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= _plots.Length) return 0;
            var p = _plots[plotIndex];
            if (p.Stage == GardenStage.Empty || p.Stage == GardenStage.Ready || p.Stage == GardenStage.Withered) return 0;

            int elapsed = ComputeElapsedSeconds(p);
            int remain = p.TotalGrowSeconds - elapsed;
            return Mathf.Max(0, remain);
        }

        private void RefreshPlot(int plotIndex)
        {
            var p = _plots[plotIndex];
            if (p.Stage == GardenStage.Empty || p.Stage == GardenStage.Withered || p.Stage == GardenStage.Ready) return;

            var seed = _seeds.FindBySeedId(p.SeedItemId);
            if (seed == null) return;

            int elapsed = ComputeElapsedSeconds(p);
            GardenStage next = elapsed >= p.TotalGrowSeconds
                ? GardenStage.Ready
                : (elapsed >= seed.GrowingStartSeconds ? GardenStage.Growing : GardenStage.Seeded);

            if (next == p.Stage) return;
            p.Stage = next;
            _plots[plotIndex] = p;
            _eventBus?.Publish(new GardenPlotChangedEvent(p));
        }

        private int ComputeElapsedSeconds(GardenPlot p)
        {
            if (p.PlantedAtUtcTicks <= 0) return 0;
            var plantedUtc = new DateTime(p.PlantedAtUtcTicks, DateTimeKind.Utc);
            var elapsed = _clock.ElapsedSinceUtc(plantedUtc);
            if (elapsed < TimeSpan.Zero) return 0;
            return (int)elapsed.TotalSeconds;
        }

        // ---- IPersistentService ----
        [Serializable]
        private struct PlotEntry
        {
            public int index;
            public int stage;
            public string seedItemId;
            public string cropItemId;
            public long plantedAtUtcTicks;
            public int totalGrowSeconds;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public PlotEntry[] plots;
            public string[] alreadyCollected;
        }

        public string CaptureJson()
        {
            var entries = new PlotEntry[_plots.Length];
            for (int i = 0; i < _plots.Length; i++)
            {
                var p = _plots[i];
                entries[i] = new PlotEntry
                {
                    index = p.Index,
                    stage = (int)p.Stage,
                    seedItemId = p.SeedItemId ?? string.Empty,
                    cropItemId = p.CropItemId ?? string.Empty,
                    plantedAtUtcTicks = p.PlantedAtUtcTicks,
                    totalGrowSeconds = p.TotalGrowSeconds
                };
            }

            var collected = new string[_alreadyCollected.Count];
            _alreadyCollected.CopyTo(collected);

            return JsonUtility.ToJson(new SavePayload
            {
                version = 1,
                plots = entries,
                alreadyCollected = collected
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var payload = JsonUtility.FromJson<SavePayload>(json);
                if (payload.plots != null)
                {
                    foreach (var e in payload.plots)
                    {
                        if (e.index < 0 || e.index >= _plots.Length) continue;
                        _plots[e.index] = new GardenPlot
                        {
                            Index = e.index,
                            Stage = (GardenStage)e.stage,
                            SeedItemId = e.seedItemId ?? string.Empty,
                            CropItemId = e.cropItemId ?? string.Empty,
                            PlantedAtUtcTicks = e.plantedAtUtcTicks,
                            TotalGrowSeconds = e.totalGrowSeconds > 0 ? e.totalGrowSeconds : 7200
                        };
                    }
                }

                _alreadyCollected.Clear();
                if (payload.alreadyCollected != null)
                {
                    foreach (var c in payload.alreadyCollected)
                    {
                        if (!string.IsNullOrEmpty(c)) _alreadyCollected.Add(c);
                    }
                }

                Refresh();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
