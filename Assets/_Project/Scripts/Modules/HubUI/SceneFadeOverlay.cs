#nullable enable
using System;
using System.Collections;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// 切场景淡入淡出。
    /// 挂 Boot.BootstrapRoot，DontDestroyOnLoad。
    /// 订阅 <see cref="SceneLoadStartedEvent"/> 进入全黑遮罩，
    /// 收到 <see cref="SceneLoadCompletedEvent"/> 后淡出恢复。
    /// </summary>
    public sealed class SceneFadeOverlay : MonoBehaviour
    {
        [SerializeField] private float _fadeInSeconds = 0.25f;
        [SerializeField] private float _fadeOutSeconds = 0.4f;
        [SerializeField] private Color _fadeColor = Color.black;

        private CanvasGroup? _group;
        private IDisposable? _startSub;
        private IDisposable? _endSub;
        private Coroutine? _current;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildOverlay();

            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                _startSub = eventBus.Subscribe<SceneLoadStartedEvent>(OnSceneLoadStarted);
                _endSub = eventBus.Subscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);
            }
        }

        private void OnDestroy()
        {
            _startSub?.Dispose();
            _endSub?.Dispose();
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("SceneFadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            int uiLayer = LayerMask.NameToLayer("UI");
            canvasGo.layer = uiLayer >= 0 ? uiLayer : 5;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000; // 高于 Toast

            canvasGo.AddComponent<CanvasScaler>();

            var maskGo = new GameObject("FadeMask");
            maskGo.transform.SetParent(canvasGo.transform, false);
            maskGo.layer = canvasGo.layer;
            var rt = maskGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = maskGo.AddComponent<Image>();
            img.color = _fadeColor;
            img.raycastTarget = false;

            _group = maskGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        private void OnSceneLoadStarted(SceneLoadStartedEvent evt)
        {
            StartTween(targetAlpha: 1f, duration: _fadeInSeconds, blockWhenDone: true);
        }

        private void OnSceneLoadCompleted(SceneLoadCompletedEvent evt)
        {
            StartTween(targetAlpha: 0f, duration: _fadeOutSeconds, blockWhenDone: false);
        }

        private void StartTween(float targetAlpha, float duration, bool blockWhenDone)
        {
            if (_group == null)
            {
                return;
            }

            if (_current != null)
            {
                StopCoroutine(_current);
            }

            _current = StartCoroutine(TweenAlpha(targetAlpha, Mathf.Max(0.01f, duration), blockWhenDone));
        }

        private IEnumerator TweenAlpha(float target, float duration, bool blockWhenDone)
        {
            if (_group == null)
            {
                yield break;
            }

            // 淡入时立刻阻挡点击；淡出完成后放行
            _group.blocksRaycasts = true;

            float start = _group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            _group.alpha = target;
            _group.blocksRaycasts = blockWhenDone;
            _current = null;
        }
    }
}
