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

        /// <summary>从弧心到给定 anchoredPosition 的径向方向（用于 hover 突出）。</summary>
        public Vector2 GetRadialDirection(Vector2 anchoredPosition)
        {
            // Arc center is at (0, _arcBottomOffset) in this container's coordinate
            Vector2 center = new Vector2(0, _arcBottomOffset);
            Vector2 dir = anchoredPosition - center;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
        }

        /// <summary>立即排列所有子对象。</summary>
        public void ArrangeImmediate()
        {
            if (_layoutConfig == null) return;

            var selectables = GetComponentsInChildren<TarotCardSelectable>();
            int childCount = selectables.Length;
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

                var rt = selectables[i].transform as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(x, y);
                    rt.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg * 0.3f);
                }
                selectables[i].RecalibrateOrigin();
            }
        }

        /// <summary>带扇形展开动画依次排列（从中心飞出到弧位）。</summary>
        public IEnumerator ArrangeWithAppear(float delayBetween = 0.05f)
        {
            if (_layoutConfig == null) yield break;

            var selectables = GetComponentsInChildren<TarotCardSelectable>();
            int childCount = selectables.Length;
            if (childCount == 0) yield break;

            float spanAngle = _layoutConfig.ArcSpanAngle;
            float radius = _layoutConfig.ArcRadius;
            float startAngle = -spanAngle / 2f;
            float duration = _layoutConfig.CardAppearDuration;

            // Pre-calculate target positions
            var targets = new Vector2[childCount];
            var targetRots = new Quaternion[childCount];

            for (int i = 0; i < childCount; i++)
            {
                float t = childCount > 1 ? (float)i / (childCount - 1) : 0.5f;
                float angle = (startAngle + spanAngle * t) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float y = Mathf.Cos(angle) * radius * 0.5f + _arcBottomOffset;
                targets[i] = new Vector2(x, y);
                targetRots[i] = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg * 0.3f);
            }

            for (int i = 0; i < childCount; i++)
            {
                var t = selectables[i].transform;
                t.gameObject.SetActive(true);
                StartCoroutine(FanIn(t, targets[i], targetRots[i], duration));
                yield return new WaitForSeconds(delayBetween);
            }
        }

        private static IEnumerator FanIn(Transform t, Vector2 endPos, Quaternion endRot, float duration)
        {
            var rt = t as RectTransform;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0, -200f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float e = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
                    rt.localRotation = Quaternion.Slerp(Quaternion.identity, endRot, e);
                }
                t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, e);
                yield return null;
            }

            if (rt != null)
            {
                rt.anchoredPosition = endPos;
                rt.localRotation = endRot;
            }
            t.localScale = Vector3.one;
            t.GetComponent<TarotCardSelectable>()?.RecalibrateOrigin();
        }
    }
}
