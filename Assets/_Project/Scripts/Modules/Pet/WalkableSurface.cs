#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// 标记一个物体为可步行表面。桌宠移动到此物体上方时会自动站到表面上。
    /// 使用 Collider2D.bounds.max.y 作为表面高度。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WalkableSurface : MonoBehaviour
    {
        /// <summary>表面 Y 偏移（正值向上）。实际表面高度 = collider.bounds.max.y + offset。</summary>
        [SerializeField] private float _yOffset;

        private Collider2D? _collider;

        public Bounds Bounds =>
            _collider != null ? _collider.bounds : GetComponent<Collider2D>().bounds;

        public float SurfaceY => Bounds.max.y + _yOffset;

        /// <summary>检查给定 X 是否在此表面的水平范围内。</summary>
        public bool ContainsX(float x)
        {
            if (_collider == null) _collider = GetComponent<Collider2D>();
            var b = _collider.bounds;
            return x >= b.min.x && x <= b.max.x;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_collider == null) _collider = GetComponent<Collider2D>();
            var b = _collider.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(b.min.x, b.max.y + _yOffset, 0),
                new Vector3(b.max.x, b.max.y + _yOffset, 0));
        }
#endif
    }
}
