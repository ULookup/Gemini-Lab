#nullable enable

using UnityEngine;
using UnityEngine.Rendering;

namespace GeminiLab.Modules.HubUI
{
    [DisallowMultipleComponent]
    public sealed class FurnitureInspectable : MonoBehaviour
    {
        [TextArea(2, 6)]
        [SerializeField]
        private string _description = "";

        private Collider2D? _hitArea;

        private void Awake()
        {
            _hitArea = GetComponent<Collider2D>();

            if (_hitArea == null)
                _hitArea = GetComponentInChildren<Collider2D>();
        }

        public bool TryHit(
            Vector2 worldPoint,
            out int sortingOrder)
        {
            sortingOrder = 0;

            if (!isActiveAndEnabled ||
                _hitArea == null ||
                !_hitArea.enabled ||
                !_hitArea.OverlapPoint(worldPoint))
            {
                return false;
            }

            SortingGroup? group =
                GetComponentInParent<SortingGroup>();

            if (group != null)
            {
                sortingOrder = group.sortingOrder;
                return true;
            }

            SpriteRenderer? renderer =
                GetComponentInChildren<SpriteRenderer>();

            if (renderer != null)
                sortingOrder = renderer.sortingOrder;

            return true;
        }

        public void ShowDescription(Vector2 screenPosition)
        {
            Debug.Log("[FurnitureDescription] " + gameObject.name);

            if (FurnitureInfoPopup.Instance == null)
            {
                Debug.LogError("FurnitureInfoPopup.Instance == null");
                return;
            }

            FurnitureInfoPopup.Instance.ShowAtScreenPosition(
                _description,
                screenPosition
            );
        }

    }
}
