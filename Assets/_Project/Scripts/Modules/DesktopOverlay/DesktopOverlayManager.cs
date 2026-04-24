#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeminiLab.Modules.DesktopOverlay
{
    /// <summary>
    /// Controls apartment/overlay mode switching and scene bridging.
    /// </summary>
    public sealed class DesktopOverlayManager : MonoBehaviour
    {
        [SerializeField] private string _apartmentScenePath = "Assets/_Project/Scenes/Apartment/Apartment_Main.unity";
        [SerializeField] private string _overlayScenePath = "Assets/_Project/Scenes/Desktop/Desktop_Overlay.unity";
        [SerializeField] private KeyCode _toggleKey = KeyCode.F10;

        private IWindowModeAdapter _windowAdapter = new WindowModeAdapter();
        private EventBus? _eventBus;
        private Func<string, AsyncOperation?> _sceneLoader = path => SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);

        public DesktopMode CurrentMode => _windowAdapter.CurrentMode;

        private void Awake()
        {
            ServiceLocator.Register<IWindowModeAdapter>(_windowAdapter);
            if (!ServiceLocator.TryResolve(out _eventBus))
            {
                _eventBus = null;
            }
        }

        private void Update()
        {
            if (_eventBus is null)
            {
                _ = ServiceLocator.TryResolve(out _eventBus);
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
            DesktopMode previousMode = _windowAdapter.CurrentMode;
            _windowAdapter.SetMode(mode);
            _windowAdapter.SetClickThrough(mode == DesktopMode.Overlay);

            string targetPath = mode == DesktopMode.Overlay ? _overlayScenePath : _apartmentScenePath;
            AsyncOperation? load = _sceneLoader(targetPath);
            if (load is null)
            {
                Debug.LogWarning($"[DesktopOverlay] Failed to load scene: {targetPath}");
                _windowAdapter.SetMode(previousMode);
                _windowAdapter.SetClickThrough(previousMode == DesktopMode.Overlay);
                return;
            }

            _eventBus?.Publish(new OverlayModeChangedEvent(mode));
        }

        public void SetSceneLoader(Func<string, AsyncOperation?> loader)
        {
            _sceneLoader = loader ?? throw new ArgumentNullException(nameof(loader));
        }
    }
}
