#nullable enable
using System;
using System.Collections.Generic;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Toast
{
    /// <summary>
    /// 全局 Toast 宿主，挂在 Boot.BootstrapRoot 上、DontDestroyOnLoad。
    /// - 自行创建 Canvas + VerticalLayoutGroup 容器
    /// - 同时实现 <see cref="IToastService"/> 并向 EventBus 订阅 <see cref="ToastRequestedEvent"/>
    /// - 业务代码既可以 `toast.Show(...)` 也可以 `eventBus.Publish(new ToastRequestedEvent(...))`
    /// </summary>
    public sealed class ToastOverlayController : MonoBehaviour, IToastService
    {
        [Header("布局")]
        [SerializeField] private int _maxVisible = 3;
        [SerializeField] private float _defaultDurationSeconds = 3f;
        [SerializeField] private float _fadeSeconds = 0.3f;

        private EventBus? _eventBus;
        private IDisposable? _subscription;
        private Transform? _container;
        private Canvas? _canvas;
        private readonly Queue<ActiveToast> _active = new();

        private void Awake()
        {
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            ServiceLocator.Register<IToastService>(this);

            if (ServiceLocator.TryResolve(out _eventBus) && _eventBus is not null)
            {
                _subscription = _eventBus.Subscribe<ToastRequestedEvent>(OnToastRequested);
            }

            if (Application.isPlaying) BuildCanvas();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }

        public void Show(string message, ToastKind kind = ToastKind.Info, float durationSeconds = 0f)
        {
            float duration = durationSeconds > 0f ? durationSeconds : _defaultDurationSeconds;
            SpawnToast(message ?? string.Empty, kind, duration);
        }

        private void OnToastRequested(ToastRequestedEvent evt)
        {
            Show(evt.Message, evt.Kind, evt.DurationSeconds);
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("ToastCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = LayerMask.NameToLayer("UI") is int ui && ui >= 0 ? ui : 5;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var containerGo = new GameObject("ToastContainer");
            containerGo.transform.SetParent(canvasGo.transform, false);
            containerGo.layer = canvasGo.layer;
            var rt = containerGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-32f, 32f);
            rt.sizeDelta = new Vector2(420f, 0f);

            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            _container = containerGo.transform;
        }

        private void SpawnToast(string message, ToastKind kind, float duration)
        {
            if (_container == null)
            {
                return;
            }

            TrimIfOverCapacity();

            var go = new GameObject($"Toast_{Time.frameCount}");
            go.transform.SetParent(_container, false);
            go.layer = _container.gameObject.layer;

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 72f);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var bg = go.AddComponent<Image>();
            bg.color = BackgroundFor(kind);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.layer = go.layer;
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(18f, 12f);
            lrt.offsetMax = new Vector2(-18f, -12f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;

            // 懒绑字体：等 UI Font Service 可用后替换
            labelGo.AddComponent<GeminiLab.Modules.UI.Catalogs.TMPFontBinder>();

            var active = new ActiveToast(go, cg, duration, _fadeSeconds);
            _active.Enqueue(active);
        }

        private void TrimIfOverCapacity()
        {
            while (_active.Count >= _maxVisible)
            {
                var expired = _active.Dequeue();
                if (expired.GameObject != null)
                {
                    Destroy(expired.GameObject);
                }
            }
        }

        private void Update()
        {
            if (_active.Count == 0)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            int count = _active.Count;
            for (int i = 0; i < count; i++)
            {
                var toast = _active.Dequeue();
                toast.Tick(dt);
                if (toast.IsFinished)
                {
                    if (toast.GameObject != null)
                    {
                        Destroy(toast.GameObject);
                    }
                }
                else
                {
                    _active.Enqueue(toast);
                }
            }
        }

        private static Color BackgroundFor(ToastKind kind)
        {
            return kind switch
            {
                ToastKind.Success => new Color(0.20f, 0.55f, 0.35f, 0.92f),
                ToastKind.Warning => new Color(0.75f, 0.55f, 0.20f, 0.92f),
                ToastKind.Error => new Color(0.70f, 0.25f, 0.28f, 0.92f),
                _ => new Color(0.18f, 0.22f, 0.30f, 0.92f)
            };
        }

        private sealed class ActiveToast
        {
            public GameObject GameObject { get; }
            private readonly CanvasGroup _cg;
            private readonly float _duration;
            private readonly float _fade;
            private float _elapsed;

            public ActiveToast(GameObject go, CanvasGroup cg, float duration, float fade)
            {
                GameObject = go;
                _cg = cg;
                _duration = duration;
                _fade = Mathf.Max(0.01f, fade);
            }

            public bool IsFinished => _elapsed >= _duration + _fade;

            public void Tick(float dt)
            {
                _elapsed += dt;

                if (_elapsed <= _fade)
                {
                    _cg.alpha = Mathf.Clamp01(_elapsed / _fade);
                }
                else if (_elapsed >= _duration)
                {
                    float fadeOut = (_elapsed - _duration) / _fade;
                    _cg.alpha = Mathf.Clamp01(1f - fadeOut);
                }
                else
                {
                    _cg.alpha = 1f;
                }
            }
        }
    }
}
