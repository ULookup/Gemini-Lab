#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Modules.Apple;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>将 Scene 中已有的 TMP 文本绑定到苹果余额。</summary>
    public sealed class AppleBalanceDisplay : MonoBehaviour
    {
        private TMP_Text? _text;
        private IAppleService? _apple;
        private IDisposable? _subscription;
        private int _retryFrames;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryResolve(out EventBus? eventBus) && eventBus is not null)
            {
                _subscription = eventBus.Subscribe<AppleChangedEvent>(_ => Refresh());
            }

            _retryFrames = 8;
            Refresh();
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            _retryFrames = 0;
        }

        private void Update()
        {
            if (_retryFrames <= 0 || _apple is not null) return;
            _retryFrames--;
            Refresh();
        }

        private void Refresh()
        {
            if (_text == null) return;
            if (_apple == null) ServiceLocator.TryResolve(out _apple);
            // TopResource 已经作者化了苹果图标，沿用原 BalanceLabel 的纯数字样式。
            // 这样 Scene 中的原有文本和 Play 中的运行时文本保持一致，不再额外绘制“苹果”前缀。
            if (_apple != null) _text.text = _apple.Balance.ToString();
        }
    }
}
