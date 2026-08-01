#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class WalkableSurface : MonoBehaviour
    {
        [Header("手动覆写")]
        [SerializeField] private bool _overrideBounds;
        [SerializeField] private float _minX;
        [SerializeField] private float _maxX;
        [SerializeField] private float _surfaceY;

        [Header("自动")]
        [SerializeField] private float _yOffset;

        private const float EdgeEpsilon = 0.0001f;

        private Collider2D? _collider;

        public float SurfaceY
        {
            get
            {
                return ResolveFallbackSurfaceY();
            }
        }

        public bool ContainsX(float x)
        {
            if (TryGetPolygonUpperSurfaceY(x, out _))
            {
                return true;
            }

            if (HasActivePolygonCollider())
            {
                return false;
            }

            return ContainsFallbackX(x);
        }

        public bool TryGetSurfaceY(float x, out float surfaceY)
        {
            if (TryGetPolygonUpperSurfaceY(x, out surfaceY))
            {
                return true;
            }

            if (HasActivePolygonCollider())
            {
                surfaceY = default;
                return false;
            }

            if (!ContainsFallbackX(x))
            {
                surfaceY = default;
                return false;
            }

            surfaceY = ResolveFallbackSurfaceY();
            return true;
        }

        private Collider2D? ResolveCollider()
        {
            if (_collider == null) _collider = GetComponent<Collider2D>();
            return _collider;
        }

        private PolygonCollider2D? ResolvePolygonCollider()
        {
            return ResolveCollider() as PolygonCollider2D ?? GetComponent<PolygonCollider2D>();
        }

        private bool HasActivePolygonCollider()
        {
            PolygonCollider2D? polygon = ResolvePolygonCollider();
            return polygon != null && polygon.enabled;
        }

        private float ResolveFallbackSurfaceY()
        {
            if (_overrideBounds) return _surfaceY;
            var c = ResolveCollider();
            return c != null ? c.bounds.max.y + _yOffset : transform.position.y;
        }

        private bool ContainsFallbackX(float x)
        {
            if (_overrideBounds)
            {
                float minX = Mathf.Min(_minX, _maxX);
                float maxX = Mathf.Max(_minX, _maxX);
                return x >= minX && x <= maxX;
            }

            var c = ResolveCollider();
            if (c == null) return false;
            var b = c.bounds;
            return x >= b.min.x && x <= b.max.x;
        }

        private bool TryGetPolygonUpperSurfaceY(float worldX, out float surfaceY)
        {
            surfaceY = default;
            PolygonCollider2D? polygon = ResolvePolygonCollider();
            if (polygon == null || !polygon.enabled)
            {
                return false;
            }

            bool found = false;
            float bestY = float.NegativeInfinity;
            int pathCount = polygon.pathCount;
            for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
            {
                Vector2[] path = polygon.GetPath(pathIndex);
                if (path.Length < 2)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < path.Length; pointIndex++)
                {
                    Vector2 localA = path[pointIndex] + polygon.offset;
                    Vector2 localB = path[(pointIndex + 1) % path.Length] + polygon.offset;
                    Vector3 worldA = polygon.transform.TransformPoint(localA);
                    Vector3 worldB = polygon.transform.TransformPoint(localB);

                    if (!TryIntersectEdgeAtX(worldA, worldB, worldX, out float candidateY))
                    {
                        continue;
                    }

                    if (candidateY > bestY)
                    {
                        bestY = candidateY;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return false;
            }

            surfaceY = bestY;
            return true;
        }

        private static bool TryIntersectEdgeAtX(Vector3 a, Vector3 b, float x, out float y)
        {
            y = default;
            float minX = Mathf.Min(a.x, b.x) - EdgeEpsilon;
            float maxX = Mathf.Max(a.x, b.x) + EdgeEpsilon;
            if (x < minX || x > maxX)
            {
                return false;
            }

            if (Mathf.Abs(a.x - b.x) <= EdgeEpsilon)
            {
                if (Mathf.Abs(x - a.x) > EdgeEpsilon)
                {
                    return false;
                }

                y = Mathf.Max(a.y, b.y);
                return true;
            }

            float t = Mathf.InverseLerp(a.x, b.x, x);
            y = Mathf.Lerp(a.y, b.y, t);
            return true;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (ResolvePolygonCollider() != null)
            {
                return;
            }

            float minX, maxX, sy;
            if (_overrideBounds)
            {
                minX = Mathf.Min(_minX, _maxX);
                maxX = Mathf.Max(_minX, _maxX);
                sy = _surfaceY;
            }
            else
            {
                var c = ResolveCollider();
                if (c == null) return;
                var b = c.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                sy = b.max.y + _yOffset;
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(minX, sy, 0),
                new Vector3(maxX, sy, 0));
        }
#endif
    }
}
