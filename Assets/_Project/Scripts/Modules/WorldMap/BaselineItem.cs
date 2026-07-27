#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.WorldMap
{
    /// <summary>
    /// 基准线物品：锁死 Y 坐标在指定基准线上，仅允许水平拖动，X 受限在 [minX, maxX] 范围内。
    /// 自动管理 SpriteRenderer.sortingOrder，确保同一基准线上的物体深度一致。
    /// 挂到场景中每个基准线上的可移动物体（花圃装饰、邮箱、草丛等）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BaselineItem : MonoBehaviour
    {
        [Header("基准线")]
        [SerializeField] private float _baselineY;

        [Header("X 移动范围")]
        [SerializeField] private float _minX = -10f;
        [SerializeField] private float _maxX = 10f;

        [Header("深度排序")]
        [SerializeField] private int _sortingOrder;

        [Header("拖拽")]
        [SerializeField] private bool _allowDrag;

        [Header("物理")]
        [SerializeField] private bool _solidCollider;

        private SpriteRenderer? _sprite;
        private Collider2D? _collider;
        private Vector3 _dragOffset;
        private bool _isDragging;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null && !_solidCollider) _collider.isTrigger = true;
        }

        private SpriteRenderer Sprite
        {
            get
            {
                if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
                return _sprite;
            }
        }

        private void ApplySortingOrder()
        {
            if (Sprite != null) Sprite.sortingOrder = _sortingOrder;
        }

        private void OnMouseDown()
        {
            if (!_allowDrag) return;
            _dragOffset = transform.position - GetMouseWorldPoint();
            _isDragging = true;
        }

        private void OnMouseDrag()
        {
            if (!_allowDrag || !_isDragging) return;
            Vector3 target = GetMouseWorldPoint() + _dragOffset;
            target.y = _baselineY;
            target.z = transform.position.z;
            target.x = Mathf.Clamp(target.x, _minX, _maxX);
            transform.position = target;
        }

        private void OnMouseUp()
        {
            _isDragging = false;
        }

        private static Vector3 GetMouseWorldPoint()
        {
            Vector3 screen = Input.mousePosition;
            screen.z = -Camera.main!.transform.position.z;
            return Camera.main.ScreenToWorldPoint(screen);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            ApplySortingOrder();
        }
#endif
    }
}
