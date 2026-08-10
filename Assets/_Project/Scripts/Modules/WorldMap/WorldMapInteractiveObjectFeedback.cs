#nullable enable
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// WorldMap 场景可交互物体的悬停缩放反馈。
    /// 基准缩放取自 Scene 中当前的 localScale，不会在运行时累计放大。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WorldMapInteractiveObjectFeedback : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _hoverScaleMultiplier = 1.06f;
        [SerializeField, Min(0f)] private float _transitionSeconds = 0.08f;
        [SerializeField] private bool _requireTopmostCollider;

        private Collider2D? _collider;
        private Camera? _camera;
        private Vector3 _baseLocalScale;
        private Vector3 _hoverLocalScale;
        private bool _pointerInside;
        private float _hoverWeight;
        private bool _baseScaleCaptured;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _camera = Camera.main;
            CaptureBaseScale();
        }

        private void OnEnable()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (!_baseScaleCaptured)
            {
                CaptureBaseScale();
            }

            transform.localScale = _baseLocalScale;
            _hoverWeight = 0f;
            _pointerInside = false;
        }

        private void Update()
        {
            // 不依赖 OnMouseEnter 的发送顺序，直接按鼠标世界坐标检测自身碰撞体。
            // 这样即使大树下方被花丛等场景碰撞体覆盖，也不会丢失悬停反馈。
            _pointerInside = IsPointerOverSelf();
            float targetWeight = _pointerInside && IsPointerEligible() ? 1f : 0f;
            if (_transitionSeconds <= 0f)
            {
                _hoverWeight = targetWeight;
            }
            else
            {
                _hoverWeight = Mathf.MoveTowards(
                    _hoverWeight,
                    targetWeight,
                    Time.unscaledDeltaTime / _transitionSeconds);
            }

            transform.localScale = Vector3.LerpUnclamped(_baseLocalScale, _hoverLocalScale, _hoverWeight);
        }

        private void OnMouseEnter()
        {
            _pointerInside = true;
        }

        private void OnMouseExit()
        {
            _pointerInside = false;
        }

        private void OnDisable()
        {
            _pointerInside = false;
            _hoverWeight = 0f;
            transform.localScale = _baseLocalScale;
        }

        private void CaptureBaseScale()
        {
            _baseLocalScale = transform.localScale;
            _hoverLocalScale = _baseLocalScale * Mathf.Max(1f, _hoverScaleMultiplier);
            _baseScaleCaptured = true;
        }

        private bool IsPointerOverSelf()
        {
            if (_collider == null || !_collider.enabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            Camera? camera = _camera != null ? _camera : Camera.main;
            if (camera == null)
            {
                return false;
            }

            Vector2 worldPoint = camera.ScreenToWorldPoint(Input.mousePosition);
            return _collider.OverlapPoint(worldPoint);
        }

        private bool IsPointerEligible()
        {
            if (ClickOcclusionUtility.IsPointerOverUI())
            {
                return false;
            }

            return !_requireTopmostCollider ||
                   ClickOcclusionUtility.IsTopmostColliderUnderMouse(_collider);
        }
    }
}
