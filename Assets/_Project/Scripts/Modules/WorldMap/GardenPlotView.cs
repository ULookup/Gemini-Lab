#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.EmotionGarden;
using GeminiLab.Modules.Garden;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 九宫格花圃可视化。优先展示情绪花园本周数据（Mon-Sun → Plot 0-6），
    /// 情绪花园未注册时回退到通用 IGardenService 数据。
    /// </summary>
    public sealed class GardenPlotView : MonoBehaviour
    {
        [Header("格子尺寸")]
        [SerializeField] private float _cellSize = 1.4f;
        [SerializeField] private float _cellGap = 0.2f;

        [Header("状态颜色")]
        [SerializeField] private Color _emptyColor = new(0.35f, 0.3f, 0.25f, 0.7f);
        [SerializeField] private Color _seededColor = new(0.55f, 0.4f, 0.25f, 0.8f);
        [SerializeField] private Color _growingColor = new(0.3f, 0.7f, 0.3f, 0.8f);
        [SerializeField] private Color _readyColor = new(0.95f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color _witheredColor = new(0.2f, 0.15f, 0.1f, 0.8f);

        private IGardenService? _garden;
        private IEmotionGardenService? _emotion;
        private EventBus? _eventBus;
        private IDisposable? _plotSub;
        private IDisposable? _emotionSub;
        private IDisposable? _bloomedSub;
        private IDisposable? _clearedSub;
        private readonly SpriteRenderer?[] _cells = new SpriteRenderer?[9];

        private void Start()
        {
            ServiceLocator.TryResolve(out _garden);
            ServiceLocator.TryResolve(out _emotion);

            if (ServiceLocator.TryResolve(out EventBus? eb))
            {
                _eventBus = eb;
                _plotSub = _eventBus.Subscribe<GardenPlotChangedEvent>(_ => Refresh());
                _emotionSub = _eventBus.Subscribe<EmotionFlowerSubmittedEvent>(_ => Refresh());
                _bloomedSub = _eventBus.Subscribe<EmotionFlowerBloomedEvent>(_ => Refresh());
                _clearedSub = _eventBus.Subscribe<EmotionGardenClearedEvent>(_ => Refresh());
            }

            BuildCells();
            Refresh();
        }

        private void OnDestroy()
        {
            _plotSub?.Dispose();
            _emotionSub?.Dispose();
            _bloomedSub?.Dispose();
            _clearedSub?.Dispose();
        }

        private void BuildCells()
        {
            for (int i = 0; i < 9; i++)
            {
                int col = i % 3;
                int row = i / 3;

                var existing = transform.Find($"Plot_{i}");
                if (existing != null)
                {
                    var sr = existing.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = existing.gameObject.AddComponent<SpriteRenderer>();
                    _cells[i] = sr;
                    continue;
                }

                float cellTotal = _cellSize + _cellGap;
                float startX = -(cellTotal * 2f) / 2f;

                float x = startX + col * cellTotal;
                float y = -row * cellTotal;

                var cellGo = new GameObject($"Plot_{i}");
                cellGo.transform.SetParent(transform, false);
                cellGo.transform.localPosition = new Vector3(x, y, 0);

                var srNew = cellGo.AddComponent<SpriteRenderer>();
                srNew.sprite = CreateWhitePixelSprite();
                srNew.drawMode = SpriteDrawMode.Simple;
                srNew.size = new Vector2(_cellSize, _cellSize);
                srNew.sortingLayerName = "Furniture";
                srNew.sortingOrder = 5;
                srNew.color = _emptyColor;

                _cells[i] = srNew;
            }
        }

        private void Refresh()
        {
            if (_emotion != null)
            {
                RefreshFromEmotionGarden();
                return;
            }

            if (_garden == null) return;

            var plots = _garden.GetAllPlots();
            for (int i = 0; i < _cells.Length && i < plots.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null) continue;

                cell.color = plots[i].Stage switch
                {
                    GardenStage.Empty => _emptyColor,
                    GardenStage.Seeded => _seededColor,
                    GardenStage.Growing => _growingColor,
                    GardenStage.Ready => _readyColor,
                    GardenStage.Withered => _witheredColor,
                    _ => _emptyColor
                };
            }
        }

        private void RefreshFromEmotionGarden()
        {
            if (_emotion == null) return;

            var weekId = _emotion.GetCurrentWeekId();
            var flowers = _emotion.GetWeekFlowers(weekId);

            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i];
                if (cell == null) continue;

                if (i < flowers.Length && flowers[i].HasValue)
                {
                    cell.color = flowers[i].Value.State switch
                    {
                        GrowthState.Growing => _growingColor,
                        GrowthState.Bloomed => _readyColor,
                        _ => _emptyColor
                    };
                }
                else
                {
                    cell.color = _emptyColor;
                }
            }
        }

        private static Sprite? s_whitePixelSprite;
        private static Sprite CreateWhitePixelSprite()
        {
            if (s_whitePixelSprite != null) return s_whitePixelSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;

            s_whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            s_whitePixelSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_whitePixelSprite;
        }
    }
}
