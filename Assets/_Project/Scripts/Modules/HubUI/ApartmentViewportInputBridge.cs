#nullable enable
using GeminiLab.Modules.Furniture;
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI
{
    /// <summary>
    /// Bridges pointer clicks from a RawImage viewport into Apartment world-space interaction.
    /// Current version forwards pet clicks first, then falls back to pet furniture interaction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ApartmentViewportInputBridge : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RawImage? _viewportImage;
        [SerializeField] private Camera? _viewportCamera;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_viewportCamera == null)
            {
                return;
            }

            RectTransform targetRect = _viewportImage != null
                ? _viewportImage.rectTransform
                : (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Rect rect = targetRect.rect;
            if (rect.width <= 0.001f || rect.height <= 0.001f)
            {
                return;
            }

            float viewportX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float viewportY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
            Vector3 worldPoint3 = _viewportCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, 0f));
            Vector2 worldPoint = new(worldPoint3.x, worldPoint3.y);

            if (TryHandleBuildMode(worldPoint, eventData.button))
            {
                return;
            }

            PetClickReactionController[] petClicks = Object.FindObjectsByType<PetClickReactionController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < petClicks.Length; i++)
            {
                PetClickReactionController petClick = petClicks[i];
                if (petClick != null && petClick.TryHandleWorldPoint(worldPoint))
                {
                    return;
                }
            }

            PetPlayerFurnitureInteractionController[] furnitureInteractions = Object.FindObjectsByType<PetPlayerFurnitureInteractionController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < furnitureInteractions.Length; i++)
            {
                PetPlayerFurnitureInteractionController interaction = furnitureInteractions[i];
                if (interaction != null && interaction.TryHandleWorldPoint(worldPoint))
                {
                    return;
                }
            }
        }

        private static bool TryHandleBuildMode(Vector2 worldPoint, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
            {
                return false;
            }

            BuildModeController[] buildControllers = Object.FindObjectsByType<BuildModeController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < buildControllers.Length; i++)
            {
                BuildModeController buildController = buildControllers[i];
                if (buildController == null || !buildController.IsBuildModeEnabled)
                {
                    continue;
                }

                bool isPrimaryAction = button == PointerEventData.InputButton.Left;
                if (buildController.TryHandleViewportWorldPoint(worldPoint, isPrimaryAction))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
