#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GeminiLab.Modules.HubUI
{
    public sealed class FurnitureDescriptionObserver : MonoBehaviour
    {
        private ApartmentViewportInputBridge? _viewportBridge;

        private EventSystem? _eventSystem;
        private PointerEventData? _pointerEvent;

        private readonly List<RaycastResult> _uiHits = new();

        private void Awake()
        {
            FindViewportBridge();
        }

        private void FindViewportBridge()
        {
            _viewportBridge =
                Object.FindFirstObjectByType<ApartmentViewportInputBridge>();

            if (_viewportBridge == null)
            {
                Debug.LogWarning(
                    "[FurnitureDescription] " +
                    "暂时找不到 ApartmentViewportInputBridge"
                );
            }
            else
            {
                Debug.Log(
                    "[FurnitureDescription] 找到 ViewportBridge：" +
                    _viewportBridge.gameObject.name
                );
            }
        }

        private void Update()
        {
            if (!TryGetLeftClick(out Vector2 screenPosition))
                return;

            Debug.Log(
                "[FurnitureDescription] 收到点击 screen=" +
                screenPosition
            );

            // =========================================
            // Bridge 如果因为初始化顺序没找到，重新找
            // =========================================
            if (_viewportBridge == null)
            {
                FindViewportBridge();

                if (_viewportBridge == null)
                {
                    Debug.LogWarning(
                        "[FurnitureDescription] " +
                        "点击失败：ViewportBridge 仍然为空"
                    );

                    FurnitureInfoPopup.Instance?.Hide();
                    return;
                }
            }

            // =========================================
            // 确认点击是在 Apartment Viewport
            // =========================================
            if (!TryGetViewportEventCamera(
                    screenPosition,
                    out Camera? eventCamera))
            {
                Debug.LogWarning(
                    "[FurnitureDescription] " +
                    "点击没有通过 Viewport UI 检查"
                );

                FurnitureInfoPopup.Instance?.Hide();
                return;
            }

            // =========================================
            // 屏幕坐标 -> 公寓世界坐标
            // =========================================
            if (!_viewportBridge.TryScreenPointToWorldPoint(
                    screenPosition,
                    eventCamera,
                    out Vector2 worldPoint,
                    out _))
            {
                Debug.LogWarning(
                    "[FurnitureDescription] " +
                    "Screen → World 转换失败"
                );

                FurnitureInfoPopup.Instance?.Hide();
                return;
            }

            Debug.Log(
                "[FurnitureDescription] worldPoint=" +
                worldPoint
            );

            // =========================================
            // 找所有可以显示描述的家具
            // =========================================
            FurnitureInspectable[] inspectables =
                Object.FindObjectsByType<FurnitureInspectable>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            Debug.Log(
                "[FurnitureDescription] 当前 FurnitureInspectable 数量=" +
                inspectables.Length
            );

            FurnitureInspectable? best = null;
            int bestOrder = int.MinValue;

            foreach (FurnitureInspectable inspectable in inspectables)
            {
                if (inspectable == null)
                    continue;

                if (!inspectable.TryHit(
                        worldPoint,
                        out int sortingOrder))
                {
                    continue;
                }

                Debug.Log(
                    "[FurnitureDescription] 命中：" +
                    inspectable.gameObject.name +
                    " sorting=" +
                    sortingOrder
                );

                if (best == null ||
                    sortingOrder > bestOrder)
                {
                    best = inspectable;
                    bestOrder = sortingOrder;
                }
            }

            // =========================================
            // 最终结果
            // =========================================
            if (best != null)
            {
                Debug.Log(
                    "[FurnitureDescription] 最终选择：" +
                    best.gameObject.name
                );

                best.ShowDescription(screenPosition);
            }
            else
            {
                Debug.Log(
                    "[FurnitureDescription] 没有命中可描述家具"
                );

                FurnitureInfoPopup.Instance?.Hide();
            }
        }

        private bool TryGetViewportEventCamera(
            Vector2 screenPosition,
            out Camera? eventCamera)
        {
            eventCamera = null;

            if (EventSystem.current == null)
            {
                Debug.LogWarning(
                    "[FurnitureDescription] EventSystem.current == null"
                );

                return false;
            }

            if (_viewportBridge == null)
                return false;

            if (_eventSystem != EventSystem.current)
            {
                _eventSystem = EventSystem.current;

                _pointerEvent =
                    new PointerEventData(_eventSystem);
            }

            _pointerEvent!.Reset();
            _pointerEvent.position = screenPosition;

            _uiHits.Clear();

            _eventSystem.RaycastAll(
                _pointerEvent,
                _uiHits
            );

            if (_uiHits.Count == 0)
            {
                Debug.LogWarning(
                    "[FurnitureDescription] UI Raycast 0 个结果"
                );

                return false;
            }

            RaycastResult topHit =
                _uiHits[0];

            Debug.Log(
                "[FurnitureDescription] UI TopHit=" +
                topHit.gameObject.name
            );

            ApartmentViewportInputBridge? hitBridge =
                topHit.gameObject
                    .GetComponentInParent<
                        ApartmentViewportInputBridge>();

            if (hitBridge != _viewportBridge)
            {
                Debug.LogWarning(
                    "[FurnitureDescription] " +
                    "TopHit 不是 ApartmentViewportInputBridge，" +
                    "当前挡住点击的是：" +
                    topHit.gameObject.name
                );

                return false;
            }

            eventCamera =
                topHit.module.eventCamera;

            return true;
        }

        private static bool TryGetLeftClick(
            out Vector2 screenPosition)
        {
            screenPosition = default;

#if ENABLE_INPUT_SYSTEM

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition =
                    Mouse.current.position.ReadValue();

                return true;
            }

#endif

#if ENABLE_LEGACY_INPUT_MANAGER

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition =
                    Input.mousePosition;

                return true;
            }

#endif

            return false;
        }
    }
}
