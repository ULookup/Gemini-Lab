#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.SceneFlow;
using UnityEngine;

namespace GeminiLab.Modules.DesktopOverlay
{
    /// <summary>
    /// Controls apartment/overlay mode switching and scene bridging.
    /// 切场景统一走 <see cref="ISceneFlowService"/>；自己不直接调 SceneManager。
    /// </summary>
    public sealed class DesktopOverlayManager : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKey = KeyCode.F10;
        [SerializeField] private SceneId _apartmentScene = SceneId.Apartment;
        [SerializeField] private SceneId _overlayScene = SceneId.DesktopOverlay;

        private IWindowModeAdapter _windowAdapter = new WindowModeAdapter();
        private EventBus? _eventBus;
        private ISceneFlowService? _sceneFlow;

        public DesktopMode CurrentMode => _windowAdapter.CurrentMode;

        private void Awake()
        {
            ServiceLocator.Register<IWindowModeAdapter>(_windowAdapter);
            ServiceLocator.TryResolve(out _eventBus);
            ServiceLocator.TryResolve(out _sceneFlow);
        }

        private void Update()
        {
            if (_eventBus is null)
            {
                _ = ServiceLocator.TryResolve(out _eventBus);
            }

            if (_sceneFlow is null)
            {
                _ = ServiceLocator.TryResolve(out _sceneFlow);
            }

            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleMode();
            }
        }

        public void ToggleMode()
        {
            DesktopMode nextMode = _windowAdapter.CurrentMode == DesktopMode.Apartment
                ? DesktopMode.Overlay
                : DesktopMode.Apartment;
            ApplyMode(nextMode);
        }

        public void ApplyMode(DesktopMode mode)
        {
            if (_sceneFlow is null && !ServiceLocator.TryResolve(out _sceneFlow))
            {
                Debug.LogError("[DesktopOverlay] 未找到 ISceneFlowService，无法切换模式");
                return;
            }

            DesktopMode previousMode = _windowAdapter.CurrentMode;
            _windowAdapter.SetMode(mode);
            _windowAdapter.SetClickThrough(mode == DesktopMode.Overlay);

            SceneId target = mode == DesktopMode.Overlay ? _overlayScene : _apartmentScene;
            AsyncOperation? op = _sceneFlow!.LoadAsync(target);

            if (op is null && _sceneFlow.CurrentScene != target)
            {
                Debug.LogWarning($"[DesktopOverlay] 切换失败，回滚到 {previousMode}");
                _windowAdapter.SetMode(previousMode);
                _windowAdapter.SetClickThrough(previousMode == DesktopMode.Overlay);
                return;
            }

            _eventBus?.Publish(new OverlayModeChangedEvent(mode));
        }
    }
}
