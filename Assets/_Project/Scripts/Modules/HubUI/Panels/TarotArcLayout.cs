#nullable enable
using System;
using System.Collections;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 弧形排列子对象。挂到选牌区域容器上，读取 TarotLayoutSO 参数，
    /// 将子对象按弧形排列。调用 Arrange() 触发重新排列。
    /// </summary>
    public sealed class TarotArcLayout : MonoBehaviour
    {
        [SerializeField] private TarotLayoutSO? _layoutConfig;
        [SerializeField] private float _arcBottomOffset = -150f; // Y offset for arc center

        public TarotLayoutSO? LayoutConfig => _layoutConfig;

        /// <summary>立即排列所有子对象。</summary>
        public void ArrangeImmediate()
        {
            if (_layoutConfig == null) return;

            int childCount = transform.childCount;
            if (childCount == 0) return;

            float spanAngle = _layoutConfig.ArcSpanAngle;
            float radius = _layoutConfig.ArcRadius;
            float startAngle = -spanAngle / 2f;

            for (int i = 0; i < childCount; i++)
            {
                float t = childCount > 1 ? (float)i / (childCount - 1) : 0.5f;
                float angle = (startAngle + spanAngle * t) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius * 0.5f + _arcBottomOffset;

                var child = transform.GetChild(i);
                var rt = child as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(x, y);
                    // Tilt cards toward center
                    rt.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg * 0.3f);
                }
            }
        }

        /// <summary>带浮现动画依次排列。</summary>
        public IEnumerator ArrangeWithAppear(float delayBetween = 0.05f)
        {
            ArrangeImmediate();

            if (_layoutConfig == null) yield break;
            float duration = _layoutConfig.CardAppearDuration;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(true);
                StartCoroutine(ScaleIn(child, duration));
                yield return new WaitForSeconds(delayBetween);
            }
        }

        private static IEnumerator ScaleIn(Transform target, float duration)
        {
            target.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
