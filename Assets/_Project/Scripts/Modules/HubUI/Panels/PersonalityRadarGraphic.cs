#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 7 维性格雷达图，手绘 UI Mesh。
    /// Values 取值 [-1, 1]；-1 = 中心，+1 = 外圈。
    /// 同时画：外圈网格（4 圈 + 7 根辐射线）+ 中心 0 线 + 数据多边形填充。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PersonalityRadarGraphic : Graphic
    {
        [Header("数据")]
        [SerializeField] private List<float> _values = new() { 0f, 0f, 0f, 0f, 0f, 0f, 0f };

        [Header("外观")]
        [SerializeField] private Color _gridColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _fillColor = new(0.45f, 0.65f, 0.9f, 0.55f);
        [SerializeField] private Color _strokeColor = new(0.85f, 0.95f, 1f, 0.95f);
        [SerializeField, Range(0.005f, 0.05f)] private float _strokeWidth = 0.015f;
        [SerializeField, Range(3, 12)] private int _gridRings = 4;

        public IReadOnlyList<float> Values => _values;

        public void SetValues(IReadOnlyList<float> values)
        {
            _values = new List<float>(values);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_values.Count < 3)
            {
                return;
            }

            int axes = _values.Count;
            Rect r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * 0.5f;
            Vector2 center = Vector2.zero; // rectTransform 的 pivot 默认 (0.5, 0.5)

            // 外圈网格
            for (int ring = 1; ring <= _gridRings; ring++)
            {
                float ratio = (float)ring / _gridRings;
                DrawPolygonOutline(vh, center, radius * ratio, axes, _gridColor, _strokeWidth * 0.5f);
            }

            // 辐射线
            for (int i = 0; i < axes; i++)
            {
                float angle = AngleFor(i, axes);
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(vh, center, center + dir * radius, _gridColor, _strokeWidth * 0.5f);
            }

            // 数据多边形（先填充，再描边）
            var polygon = new Vector2[axes];
            for (int i = 0; i < axes; i++)
            {
                float v = Mathf.Clamp(_values[i], -1f, 1f);
                float t = (v + 1f) * 0.5f; // -1..1 → 0..1
                float angle = AngleFor(i, axes);
                polygon[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * t);
            }

            DrawFilledPolygon(vh, center, polygon, _fillColor);
            DrawClosedPath(vh, polygon, _strokeColor, _strokeWidth);
        }

        private static float AngleFor(int index, int count)
        {
            // 从上方（+Y）开始顺时针分布：0 → 90°
            const float startAngle = Mathf.PI * 0.5f;
            return startAngle - (index * Mathf.PI * 2f / count);
        }

        private static void DrawPolygonOutline(VertexHelper vh, Vector2 center, float radius, int sides, Color color, float thickness)
        {
            var pts = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = AngleFor(i, sides);
                pts[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            DrawClosedPath(vh, pts, color, thickness);
        }

        private static void DrawFilledPolygon(VertexHelper vh, Vector2 center, Vector2[] pts, Color color)
        {
            int startIndex = vh.currentVertCount;
            vh.AddVert(new UIVertex { position = new Vector3(center.x, center.y, 0), color = color });
            for (int i = 0; i < pts.Length; i++)
            {
                vh.AddVert(new UIVertex { position = new Vector3(pts[i].x, pts[i].y, 0), color = color });
            }
            for (int i = 0; i < pts.Length; i++)
            {
                int next = (i + 1) % pts.Length;
                vh.AddTriangle(startIndex, startIndex + 1 + i, startIndex + 1 + next);
            }
        }

        private static void DrawClosedPath(VertexHelper vh, Vector2[] pts, Color color, float thickness)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                int next = (i + 1) % pts.Length;
                DrawLine(vh, pts[i], pts[next], color, thickness);
            }
        }

        private static void DrawLine(VertexHelper vh, Vector2 from, Vector2 to, Color color, float thickness)
        {
            Vector2 dir = (to - from).normalized;
            if (dir.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Vector2 perp = new(-dir.y, dir.x);
            Vector2 offset = perp * (thickness * 0.5f);
            int start = vh.currentVertCount;
            vh.AddVert(new UIVertex { position = new Vector3(from.x + offset.x, from.y + offset.y, 0), color = color });
            vh.AddVert(new UIVertex { position = new Vector3(from.x - offset.x, from.y - offset.y, 0), color = color });
            vh.AddVert(new UIVertex { position = new Vector3(to.x - offset.x, to.y - offset.y, 0), color = color });
            vh.AddVert(new UIVertex { position = new Vector3(to.x + offset.x, to.y + offset.y, 0), color = color });
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
