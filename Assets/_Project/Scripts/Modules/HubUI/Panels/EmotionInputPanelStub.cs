#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.EmotionGarden;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每日情绪输入面板。读取心情文本，提交后生成对应的情绪花。
    /// </summary>
    public sealed class EmotionInputPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.EmotionInput;

        [Header("UI 控件")]
        [SerializeField] private TMP_InputField? _inputField;
        [SerializeField] private Button? _submitButton;
        [SerializeField] private TMP_Text? _statusText;
        [SerializeField] private TMP_Text? _ownerText;

        private IEmotionGardenService? _service;
        private IUIRouter? _router;
        private string _owner = EmotionFlowerCatalog.OwnerAngel;

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            if (payload is string owner)
            {
                _owner = EmotionFlowerCatalog.NormalizeOwner(owner);
            }

            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? service) ? service : null;
            _router ??= ServiceLocator.TryResolve(out IUIRouter? router) ? router : null;

            if (_service == null)
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "情绪花园服务未就绪";
                return;
            }

            if (_ownerText != null)
            {
                _ownerText.text = $"培育者: {EmotionFlowerCatalog.ResolveOwnerDisplayName(_owner)}";
            }

            if (!_service.CanSubmitToday())
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
            }
            else
            {
                SetInteractable(true);
                if (_statusText != null) _statusText.text = string.Empty;
            }
        }

        public void OnSubmitClick()
        {
            if (_service == null)
            {
                if (_statusText != null) _statusText.text = "情绪花园服务未就绪";
                return;
            }

            if (!_service.CanSubmitToday())
            {
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
                return;
            }

            var detail = _inputField != null ? _inputField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(detail))
            {
                if (_statusText != null) _statusText.text = "请输入心情";
                return;
            }

            var flower = _service.SubmitEmotion(string.Empty, detail, _owner);
            if (flower == null)
            {
                if (_statusText != null) _statusText.text = "提交失败";
                return;
            }

            SetInteractable(false);
            if (_statusText != null)
            {
                _statusText.text = $"已生成 {flower.Value.FlowerName}（{flower.Value.EmotionType}）";
            }

            _router?.Open(PanelId.WeeklyGardenView);
        }

        private void SetInteractable(bool interactable)
        {
            if (_inputField != null) _inputField.interactable = interactable;
            if (_submitButton != null) _submitButton.interactable = interactable;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_submitButton != null) _submitButton.onClick.AddListener(OnSubmitClick);
        }
    }
}
