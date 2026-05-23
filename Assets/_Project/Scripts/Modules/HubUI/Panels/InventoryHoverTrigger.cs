#nullable enable
using GeminiLab.Modules.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GeminiLab.Modules.HubUI.Panels
{
    /// <summary>
    /// 挂在物品栏格子上，hover 时通知 InventoryPanelStub 显示/隐藏 tooltip。
    /// </summary>
    public sealed class InventoryHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryPanelStub? _owner;
        private ItemStack _stack;
        private ItemDefSO? _def;

        public void Bind(InventoryPanelStub owner, ItemStack stack, ItemDefSO? def)
        {
            _owner = owner;
            _stack = stack;
            _def = def;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _owner?.ShowTooltip(_stack, _def);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _owner?.HideTooltip();
        }
    }
}
