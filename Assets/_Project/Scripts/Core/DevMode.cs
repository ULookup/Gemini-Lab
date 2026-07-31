#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace GeminiLab.Core
{
    /// <summary>
    /// 全局开发模式开关。Editor 下通过 Tools → Gemini-Lab → Toggle Dev Mode 切换。
    /// 运行时只读——设置由 Editor 侧的 <c>[InitializeOnLoad]</c> 完成。
    /// 打包后强制 false。
    /// </summary>
    public static class DevMode
    {
        /// <summary>当前是否处于开发者模式（开发期默认 true，打包后强制 false）。</summary>
        public static bool Active { get; set; } = true;

#if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForcePlayerMode() => Active = false;
#endif
    }

    /// <summary>
    /// 鼠标点选遮挡裁决工具。
    /// 用当前点击点的最上层 2D 碰撞体作为唯一可响应目标，避免被前景物体遮挡时仍误触后方对象。
    /// </summary>
    public static class ClickOcclusionUtility
    {
        public static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public static bool IsTopmostColliderUnderMouse(Collider2D? targetCollider)
        {
            Camera? camera = Camera.main;
            return camera != null && IsTopmostColliderUnderMouse(camera, targetCollider);
        }

        public static bool IsTopmostColliderUnderMouse(Camera camera, Collider2D? targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            Vector3 screenPoint = Input.mousePosition;
            Vector3 worldPoint = camera.ScreenToWorldPoint(screenPoint);
            return IsTopmostColliderAtWorldPoint(worldPoint, targetCollider);
        }

        public static bool IsTopmostColliderAtWorldPoint(Vector2 worldPoint, Collider2D? targetCollider)
        {
            if (targetCollider == null)
            {
                return false;
            }

            return TryGetTopmostColliderAtWorldPoint(worldPoint, out Collider2D? topmostCollider) &&
                   ReferenceEquals(topmostCollider, targetCollider);
        }

        public static bool TryGetTopmostColliderUnderMouse(Camera camera, out Collider2D? topmostCollider)
        {
            Vector3 screenPoint = Input.mousePosition;
            Vector3 worldPoint = camera.ScreenToWorldPoint(screenPoint);
            return TryGetTopmostColliderAtWorldPoint(worldPoint, out topmostCollider);
        }

        public static bool TryGetTopmostColliderAtWorldPoint(Vector2 worldPoint, out Collider2D? topmostCollider)
        {
            topmostCollider = null;
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            bool found = false;
            ClickPriority bestPriority = default;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || !hit.enabled || !hit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ClickPriority priority = ResolveClickPriority(hit.gameObject);
                if (!found || ComparePriority(priority, bestPriority) > 0)
                {
                    found = true;
                    bestPriority = priority;
                    topmostCollider = hit;
                }
            }

            return found && topmostCollider != null;
        }

        private static int ComparePriority(ClickPriority left, ClickPriority right)
        {
            int compare = left.SortingLayerValue.CompareTo(right.SortingLayerValue);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.SortingOrder.CompareTo(right.SortingOrder);
            if (compare != 0)
            {
                return compare;
            }

            return left.FrontDepth.CompareTo(right.FrontDepth);
        }

        private static ClickPriority ResolveClickPriority(GameObject target)
        {
            if (TryResolveSortingReference(target, out int sortingLayerValue, out int sortingOrder))
            {
                return new ClickPriority(sortingLayerValue, sortingOrder, -target.transform.position.z);
            }

            return new ClickPriority(0, 0, -target.transform.position.z);
        }

        private static bool TryResolveSortingReference(GameObject target, out int sortingLayerValue, out int sortingOrder)
        {
            if (target.TryGetComponent(out SortingGroup directSortingGroup))
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(directSortingGroup.sortingLayerID);
                sortingOrder = directSortingGroup.sortingOrder;
                return true;
            }

            if (target.TryGetComponent(out Renderer directRenderer))
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(directRenderer.sortingLayerID);
                sortingOrder = directRenderer.sortingOrder;
                return true;
            }

            SortingGroup? parentSortingGroup = target.GetComponentInParent<SortingGroup>(true);
            if (parentSortingGroup != null)
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(parentSortingGroup.sortingLayerID);
                sortingOrder = parentSortingGroup.sortingOrder;
                return true;
            }

            Renderer? parentRenderer = target.GetComponentInParent<Renderer>(true);
            if (parentRenderer != null)
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(parentRenderer.sortingLayerID);
                sortingOrder = parentRenderer.sortingOrder;
                return true;
            }

            SortingGroup? childSortingGroup = target.GetComponentInChildren<SortingGroup>(true);
            if (childSortingGroup != null)
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(childSortingGroup.sortingLayerID);
                sortingOrder = childSortingGroup.sortingOrder;
                return true;
            }

            Renderer? childRenderer = target.GetComponentInChildren<Renderer>(true);
            if (childRenderer != null)
            {
                sortingLayerValue = SortingLayer.GetLayerValueFromID(childRenderer.sortingLayerID);
                sortingOrder = childRenderer.sortingOrder;
                return true;
            }

            sortingLayerValue = 0;
            sortingOrder = 0;
            return false;
        }

        private readonly struct ClickPriority
        {
            public ClickPriority(int sortingLayerValue, int sortingOrder, float frontDepth)
            {
                SortingLayerValue = sortingLayerValue;
                SortingOrder = sortingOrder;
                FrontDepth = frontDepth;
            }

            public int SortingLayerValue { get; }

            public int SortingOrder { get; }

            public float FrontDepth { get; }
        }
    }
}
