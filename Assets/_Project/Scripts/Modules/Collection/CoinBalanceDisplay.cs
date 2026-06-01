#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    /// <summary>
    /// 挂到任意 TMP_Text 上，自动显示并监听 CoinService 余额变化。
    /// </summary>
    public sealed class CoinBalanceDisplay : MonoBehaviour
    {
        private TMP_Text? _text;
        private ICoinService? _coin;
        private IDisposable? _sub;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (!ServiceLocator.TryResolve(out _coin) || _coin == null) return;

            if (_text != null) _text.text = $"{_coin.Balance}";

            if (ServiceLocator.TryResolve(out EventBus? eb) && eb != null)
            {
                _sub = eb.Subscribe<CoinChangedEvent>(e =>
                {
                    if (_text != null) _text.text = $"{e.Balance}";
                });
            }
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
        }
    }
}
