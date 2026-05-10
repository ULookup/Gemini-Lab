#nullable enable
using System;
using GeminiLab.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// <see cref="ISceneFlowService"/> 的默认实现。
    /// 通过 <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> 切换场景，
    /// 并在 EventBus 上广播 <see cref="SceneLoadStartedEvent"/> / <see cref="SceneLoadCompletedEvent"/>。
    /// </summary>
    public sealed class SceneFlowService : ISceneFlowService
    {
        private readonly ISceneCatalog _catalog;
        private readonly EventBus _eventBus;
        private readonly Func<string, LoadSceneMode, AsyncOperation?> _loader;

        public SceneId CurrentScene { get; private set; }
        public bool IsLoading { get; private set; }

        public SceneFlowService(ISceneCatalog catalog, EventBus eventBus)
            : this(catalog, eventBus, DefaultLoader)
        {
        }

        public SceneFlowService(ISceneCatalog catalog, EventBus eventBus, Func<string, LoadSceneMode, AsyncOperation?> loader)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            CurrentScene = SceneId.Boot;
        }

        public AsyncOperation? LoadAsync(SceneId target, SceneTransitionPayload? payload = null, Action? onCompleted = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneFlow] 已有场景加载进行中，忽略 → {target}");
                return null;
            }

            if (target == CurrentScene)
            {
                onCompleted?.Invoke();
                return null;
            }

            SceneId from = CurrentScene;
            string sceneName = _catalog.GetSceneName(target);

            AsyncOperation? op = _loader(sceneName, LoadSceneMode.Single);
            if (op is null)
            {
                Debug.LogError($"[SceneFlow] 加载失败：{sceneName}（未登记在 Build Settings 或 catalog 错配）");
                return null;
            }

            IsLoading = true;
            _eventBus.Publish(new SceneLoadStartedEvent(from, target, payload));

            op.completed += _ =>
            {
                CurrentScene = target;
                IsLoading = false;
                _eventBus.Publish(new SceneLoadCompletedEvent(from, target, payload));
                onCompleted?.Invoke();
            };

            return op;
        }

        /// <summary>
        /// 外部场景切换 / 手动开场景完成时，用于同步当前场景 id。
        /// 通常由 Bootstrap 在启动阶段调用。
        /// </summary>
        public void SetCurrentScene(SceneId id)
        {
            CurrentScene = id;
        }

        private static AsyncOperation? DefaultLoader(string sceneName, LoadSceneMode mode)
        {
            return SceneManager.LoadSceneAsync(sceneName, mode);
        }
    }
}
