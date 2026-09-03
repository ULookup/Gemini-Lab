#nullable enable
using System;
using GeminiLab.Core;
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 房间级遗留物视觉刷新器。
    /// 监听服务状态变化，只切换 Scene 中已作者化的槽位和变体。
    /// </summary>
    public sealed class RoomRelicRoomView : MonoBehaviour
    {
        [SerializeField] private RoomId _roomId = RoomId.AngelRoom;
        [SerializeField] private RoomRelicView[] _noteSlots = Array.Empty<RoomRelicView>();
        [SerializeField] private RoomRelicView[] _relicSlots = Array.Empty<RoomRelicView>();
        [SerializeField] private RoomRelicView[] _giftSlots = Array.Empty<RoomRelicView>();

        private IRoomRelicService? _service;

        private void Awake()
        {
            if (!ServiceLocator.TryResolve(out IRoomRelicService? service) || service is null)
            {
                return;
            }

            _service = service;
            _service.StateChanged += HandleStateChanged;
            _service.GiftObtained += HandleGiftObtained;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_service == null)
            {
                return;
            }

            _service.StateChanged -= HandleStateChanged;
            _service.GiftObtained -= HandleGiftObtained;
        }

        private void HandleStateChanged(RoomRelicStateChangedEvent evt)
        {
            if (evt.RoomId == _roomId)
            {
                Refresh();
            }
        }

        private void HandleGiftObtained(RoomGiftObtainedEvent evt)
        {
            if (evt.RoomId != _roomId)
            {
                return;
            }

            Refresh();
            OpenGiftPopup(evt.Gift);
        }

        private void Refresh()
        {
            if (_service == null)
            {
                return;
            }

            RoomRelicSnapshot snapshot = _service.GetSnapshot(_roomId);
            ApplySlots(_noteSlots, snapshot.CurrentNote?.id);
            ApplySlots(_relicSlots, snapshot.CurrentRelic?.id);
            ApplyGiftSlots(snapshot.PlacedGifts);
        }

        private static void ApplySlots(RoomRelicView[] slots, string? currentId)
        {
            if (slots.Length == 0 || string.IsNullOrWhiteSpace(currentId))
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i]?.Apply(null);
                }

                return;
            }

            int preferredIndex = Math.Abs(StringComparer.Ordinal.GetHashCode(currentId)) % slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]?.Apply(i == preferredIndex ? currentId : null);
            }
        }

        private void ApplyGiftSlots(System.Collections.Generic.IReadOnlyList<RoomGiftData> gifts)
        {
            for (int i = 0; i < _giftSlots.Length; i++)
            {
                string? giftId = i < gifts.Count ? gifts[i].id : null;
                _giftSlots[i]?.Apply(giftId);
            }
        }

        private static void OpenGiftPopup(RoomGiftData gift)
        {
            if (ServiceLocator.TryResolve(out IUIRouter? router) && router is not null)
            {
                router.Open(PanelId.RoomGiftObtained, gift);
            }
        }
    }
}
