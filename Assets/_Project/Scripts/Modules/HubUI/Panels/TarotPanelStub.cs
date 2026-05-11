#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Tarot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 每日塔罗面板。打开后显示：牌背 + "抽今日塔罗"按钮 ↦ 翻牌 ↦ 天使正位 / 恶魔逆位解读气泡。
    /// 当天已抽过则直接显示上次抽卡结果的 UI 不重复抽。
    /// </summary>
    public sealed class TarotPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.Tarot;

        [Header("卡面")]
        [SerializeField] private Image? _cardImage;
        [SerializeField] private TMP_Text? _cardTitleText;
        [SerializeField] private TMP_Text? _cardOrientationText;

        [Header("操作")]
        [SerializeField] private Button? _drawButton;
        [SerializeField] private TMP_Text? _drawButtonLabel;

        [Header("解读气泡")]
        [SerializeField] private TMP_Text? _angelReadingText;
        [SerializeField] private TMP_Text? _devilReadingText;

        private ITarotService? _tarot;
        private CancellationTokenSource? _cts;
        private TarotDrawResult? _currentDraw;

        protected override void Awake()
        {
            base.Awake();

            if (_drawButton != null)
            {
                _drawButton.onClick.AddListener(OnDrawClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveListener(OnDrawClicked);
            }
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            EnsureService();
            RefreshForEnterPanel();
        }

        public override void OnClose()
        {
            base.OnClose();
            _cts?.Cancel();
        }

        private void EnsureService()
        {
            if (_tarot == null)
            {
                ServiceLocator.TryResolve(out _tarot);
            }
        }

        private void RefreshForEnterPanel()
        {
            if (_tarot == null)
            {
                SetDrawButton("塔罗未就绪", interactable: false);
                return;
            }

            if (_tarot.CanDrawToday())
            {
                SetDrawButton("抽今日塔罗", interactable: true);
                ClearCardDisplay();
                ClearReadings();
            }
            else
            {
                SetDrawButton("今天已抽过，明天再来", interactable: false);
            }
        }

        private void OnDrawClicked()
        {
            if (_tarot == null) return;
            if (!_tarot.CanDrawToday()) return;

            var draw = _tarot.DrawDaily();
            if (draw == null)
            {
                SetDrawButton("抽卡失败", interactable: false);
                return;
            }

            _currentDraw = draw.Value;
            ShowCard(_currentDraw.Value);
            SetDrawButton("今天已抽过，明天再来", interactable: false);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = RequestDualReadingsAsync(_currentDraw.Value, _cts.Token);
        }

        private async Task RequestDualReadingsAsync(TarotDrawResult draw, CancellationToken token)
        {
            if (_tarot == null) return;

            // 天使 = 正位人格；恶魔 = 逆位人格。双轨并行。
            var angelTask = _tarot.RequestReadingAsync(draw, PetId.Angel, TarotOrientation.Upright, token);
            var devilTask = _tarot.RequestReadingAsync(draw, PetId.Devil, TarotOrientation.Reversed, token);

            SetReadingText(_angelReadingText, "（天使正在看牌…）");
            SetReadingText(_devilReadingText, "（恶魔正在看牌…）");

            try
            {
                var angel = await angelTask.ConfigureAwait(true);
                if (token.IsCancellationRequested) return;
                SetReadingText(_angelReadingText, angel.Text);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotPanel] angel reading failed: {ex.Message}");
                SetReadingText(_angelReadingText, "（天使暂时无法开口。）");
            }

            try
            {
                var devil = await devilTask.ConfigureAwait(true);
                if (token.IsCancellationRequested) return;
                SetReadingText(_devilReadingText, devil.Text);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TarotPanel] devil reading failed: {ex.Message}");
                SetReadingText(_devilReadingText, "（恶魔扭过脸不说话。）");
            }
        }

        private void ShowCard(TarotDrawResult draw)
        {
            if (_cardImage != null && draw.Card.Artwork != null)
            {
                _cardImage.sprite = draw.Card.Artwork;
                _cardImage.color = Color.white;
            }
            if (_cardTitleText != null)
            {
                _cardTitleText.text = $"{draw.Card.DisplayNameZh} · {draw.Card.DisplayNameEn}";
            }
            if (_cardOrientationText != null)
            {
                _cardOrientationText.text = draw.Orientation == TarotOrientation.Upright ? "正位" : "逆位";
            }
        }

        private void ClearCardDisplay()
        {
            if (_cardTitleText != null) _cardTitleText.text = "—";
            if (_cardOrientationText != null) _cardOrientationText.text = "";
        }

        private void ClearReadings()
        {
            SetReadingText(_angelReadingText, "");
            SetReadingText(_devilReadingText, "");
        }

        private void SetDrawButton(string label, bool interactable)
        {
            if (_drawButtonLabel != null) _drawButtonLabel.text = label;
            if (_drawButton != null) _drawButton.interactable = interactable;
        }

        private static void SetReadingText(TMP_Text? tmp, string text)
        {
            if (tmp != null) tmp.text = text;
        }
    }
}
