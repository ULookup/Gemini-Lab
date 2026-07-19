#nullable enable
using System;
using System.Collections;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class PhoneAnimController : MonoBehaviour
    {
        [SerializeField] private RectTransform _phoneRect = null!;
        [SerializeField] private CanvasGroup _canvasGroup = null!;
        [SerializeField] private Vector2 _collapsedAnchoredPosition = new(80f, 80f);
        [SerializeField] private Vector2 _centerAnchoredPosition = Vector2.zero;
        [SerializeField] private float _openDuration = 0.35f;
        [SerializeField] private float _closeDuration = 0.25f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public bool IsAnimating { get; private set; }

        private void Awake()
        {
            _phoneRect.localScale = Vector3.one * 0.3f;
            _phoneRect.anchoredPosition = _collapsedAnchoredPosition;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public IEnumerator PlayOpenAnim()
        {
            IsAnimating = true;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < _openDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _openDuration);
                _phoneRect.localScale = Vector3.Lerp(
                    Vector3.one * 0.3f, Vector3.one, _scaleCurve.Evaluate(p));
                _phoneRect.anchoredPosition = Vector2.Lerp(
                    _collapsedAnchoredPosition, _centerAnchoredPosition, _moveCurve.Evaluate(p));
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(p / 0.7f));
                yield return null;
            }
            _phoneRect.localScale = Vector3.one;
            _phoneRect.anchoredPosition = _centerAnchoredPosition;
            _canvasGroup.alpha = 1f;
            IsAnimating = false;
        }

        public IEnumerator PlayCloseAnim()
        {
            IsAnimating = true;
            float t = 0f;
            Vector3 startScale = _phoneRect.localScale;
            Vector2 startPos = _phoneRect.anchoredPosition;
            float startAlpha = _canvasGroup.alpha;
            while (t < _closeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _closeDuration);
                _phoneRect.localScale = Vector3.Lerp(
                    startScale, Vector3.one * 0.3f, _scaleCurve.Evaluate(p));
                _phoneRect.anchoredPosition = Vector2.Lerp(
                    startPos, _collapsedAnchoredPosition, _moveCurve.Evaluate(p));
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(p / 0.8f));
                yield return null;
            }
            _phoneRect.localScale = Vector3.one * 0.3f;
            _phoneRect.anchoredPosition = _collapsedAnchoredPosition;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            IsAnimating = false;
        }
    }
}
