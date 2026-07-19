#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.Events;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.Collection
{
    public sealed class CoinBalanceDisplay : MonoBehaviour
    {
        private TMP_Text? _text;
        private ICoinService? _coin;
        private IDisposable? _sub;
        private int _retryFrames;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryResolve(out EventBus? eb) && eb != null)
                _sub = eb.Subscribe<CoinChangedEvent>(_ => RefreshBalance());

            RefreshBalance();
            _retryFrames = 5;
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
            _retryFrames = 0;
        }

        private void Update()
        {
            if (_retryFrames <= 0 || _coin != null) return;
            _retryFrames--;
            RefreshBalance();
        }

        private void RefreshBalance()
        {
            if (_text == null) return;
            if (_coin == null) ServiceLocator.TryResolve(out _coin);
            if (_coin != null) _text.text = $"{_coin.Balance}";
        }
    }
}
