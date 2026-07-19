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
    /// 每日情绪输入面板。输入心情文本 → 提交生成情绪花。
    /// 当前阶段：方块 UI 占位，情绪固定为"悲伤"。
    /// 通过 payload 传入 owner ("angel" / "demon")。
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
        private string _owner = "angel";

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);

            if (payload is string owner) _owner = owner;

            _service ??= ServiceLocator.TryResolve(out IEmotionGardenService? s) ? s : null;

            if (_ownerText != null) _ownerText.text = $"培育者: {(_owner == "angel" ? "天使" : "恶魔")}";

            if (_service != null && !_service.CanSubmitToday())
            {
                SetInteractable(false);
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
            }
            else
            {
                SetInteractable(true);
                if (_statusText != null) _statusText.text = "";
            }
        }

        public void OnSubmitClick()
        {
            if (_service == null) return;
            if (!_service.CanSubmitToday())
            {
                if (_statusText != null) _statusText.text = "今天已经提交过心情了";
                return;
            }

            var detail = _inputField != null ? _inputField.text : "";
            if (string.IsNullOrWhiteSpace(detail))
            {
                if (_statusText != null) _statusText.text = "请输入心情";
                return;
            }

            // 当前阶段固定返回"悲伤"
            var flower = _service.SubmitEmotion("悲伤", detail, _owner);
            if (flower == null)
            {
                if (_statusText != null) _statusText.text = "提交失败";
                return;
            }

            SetInteractable(false);
            if (_statusText != null) _statusText.text = $"已生成: {_owner}·悲伤花 ({flower.Value.DateIso})";
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
