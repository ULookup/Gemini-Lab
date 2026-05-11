#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 设置面板：4 个音量/Overlay 切换 + 1 个重置按钮。
    /// 绑定 <see cref="ISettingsService"/>；修改即写入 PlayerPrefs 并广播事件。
    /// </summary>
    public sealed class SettingsPanel : StubPanelBase
    {
        public override PanelId Id => PanelId.Settings;

        [Header("音量滑条 0..1")]
        [SerializeField] private Slider? _master;
        [SerializeField] private TMP_Text? _masterValue;
        [SerializeField] private Slider? _bgm;
        [SerializeField] private TMP_Text? _bgmValue;
        [SerializeField] private Slider? _sfx;
        [SerializeField] private TMP_Text? _sfxValue;

        [Header("开关")]
        [SerializeField] private Toggle? _fullscreen;
        [SerializeField] private Toggle? _desktopOverlay;

        [Header("操作")]
        [SerializeField] private Button? _resetButton;
        [SerializeField] private Button? _closeButton;

        private ISettingsService? _service;
        private bool _suppressCallbacks;

        protected override void Awake()
        {
            base.Awake();

            if (_master != null) _master.onValueChanged.AddListener(_ => OnChanged());
            if (_bgm != null) _bgm.onValueChanged.AddListener(_ => OnChanged());
            if (_sfx != null) _sfx.onValueChanged.AddListener(_ => OnChanged());
            if (_fullscreen != null) _fullscreen.onValueChanged.AddListener(_ => OnChanged());
            if (_desktopOverlay != null) _desktopOverlay.onValueChanged.AddListener(_ => OnChanged());
            if (_resetButton != null) _resetButton.onClick.AddListener(OnReset);
            if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            if (_service == null)
            {
                ServiceLocator.TryResolve(out _service);
            }

            if (_service != null)
            {
                Bind(_service.Current);
            }
        }

        private void OnCloseClicked()
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
            {
                router.Close(Id);
            }
        }

        private void OnReset()
        {
            _service?.ResetToDefault();
            if (_service != null) Bind(_service.Current);
        }

        private void OnChanged()
        {
            if (_suppressCallbacks || _service == null) return;

            var next = new GameSettings
            {
                MasterVolume = _master != null ? _master.value : _service.Current.MasterVolume,
                BgmVolume = _bgm != null ? _bgm.value : _service.Current.BgmVolume,
                SfxVolume = _sfx != null ? _sfx.value : _service.Current.SfxVolume,
                Fullscreen = _fullscreen != null ? _fullscreen.isOn : _service.Current.Fullscreen,
                DesktopOverlayEnabled = _desktopOverlay != null ? _desktopOverlay.isOn : _service.Current.DesktopOverlayEnabled,
                LanguageIso = _service.Current.LanguageIso
            };
            _service.Apply(next);
            BindTextsOnly(_service.Current);
        }

        private void Bind(GameSettings s)
        {
            _suppressCallbacks = true;
            if (_master != null) _master.SetValueWithoutNotify(s.MasterVolume);
            if (_bgm != null) _bgm.SetValueWithoutNotify(s.BgmVolume);
            if (_sfx != null) _sfx.SetValueWithoutNotify(s.SfxVolume);
            if (_fullscreen != null) _fullscreen.SetIsOnWithoutNotify(s.Fullscreen);
            if (_desktopOverlay != null) _desktopOverlay.SetIsOnWithoutNotify(s.DesktopOverlayEnabled);
            _suppressCallbacks = false;

            BindTextsOnly(s);
        }

        private void BindTextsOnly(GameSettings s)
        {
            if (_masterValue != null) _masterValue.text = Mathf.RoundToInt(s.MasterVolume * 100f) + "%";
            if (_bgmValue != null) _bgmValue.text = Mathf.RoundToInt(s.BgmVolume * 100f) + "%";
            if (_sfxValue != null) _sfxValue.text = Mathf.RoundToInt(s.SfxVolume * 100f) + "%";
        }
    }
}
