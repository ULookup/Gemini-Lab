#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GeminiLab.Core.Persistence;
using GeminiLab.Core.Time;
using GeminiLab.Modules.Pet.Social;
using UnityEngine;

namespace GeminiLab.Modules.RoomRelic
{
    /// <summary>
    /// 室内遗留物系统默认实现。
    /// 每个房间独立执行每日纸条/遗留物判定；永久赠礼按房间方向从尚未获得的赠礼池中抽取，全局去重。
    /// </summary>
    public sealed class RoomRelicService : IRoomRelicService, IPersistentService, IDisposable
    {
        private const float NoteProbability = 0.5f;
        private const float RelicProbability = 0.5f;
        private const float GiftProbability = 0.15f;
        private const float RelicUnlockFriendship = 45f;
        private const float GiftUnlockFriendship = 80f;

        private readonly IGameClock _clock;
        private readonly IPetSocialService _social;
        private readonly RoomRelicCatalogSO _catalog;
        private readonly System.Random _random;

        private readonly Dictionary<RoomId, RoomRollState> _states = new();
        private readonly Dictionary<RoomId, string> _lastEntryDateByRoom = new();
        private readonly List<string> _obtainedGiftIds = new();

        private RoomId? _currentRoomId;
        private float _lastFriendship;

        public RoomRelicService(
            IGameClock clock,
            IPetSocialService social,
            RoomRelicCatalogSO catalog,
            System.Random? random = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _social = social ?? throw new ArgumentNullException(nameof(social));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _random = random ?? new System.Random();

            foreach (RoomId roomId in Enum.GetValues(typeof(RoomId)))
            {
                _states[roomId] = new RoomRollState();
                _lastEntryDateByRoom[roomId] = string.Empty;
            }

            _lastFriendship = _social.Friendship;
            _social.FriendshipChanged += HandleFriendshipChanged;
        }

        public string Key => "room_relic";

        public event Action<RoomRelicStateChangedEvent>? StateChanged;
        public event Action<RoomGiftObtainedEvent>? GiftObtained;

        public RoomRelicSnapshot GetSnapshot(RoomId roomId)
        {
            return new RoomRelicSnapshot(
                roomId,
                GetCurrentNote(roomId),
                GetCurrentRelic(roomId),
                GetPlacedGifts(roomId));
        }

        public void ProcessRoomEntry(RoomId roomId)
        {
            string today = _clock.TodayIso;
            if (string.Equals(_lastEntryDateByRoom[roomId], today, StringComparison.Ordinal))
            {
                return;
            }

            _lastEntryDateByRoom[roomId] = today;
            SetCurrentRoom(roomId);

            ProcessNote(roomId, today);

            float friendship = _social.Friendship;
            RoomRollState state = GetState(roomId);

            if (friendship < RelicUnlockFriendship)
            {
                state.currentRelicId = string.Empty;
            }
            else if (friendship < GiftUnlockFriendship)
            {
                ProcessRelic(roomId, today);
            }
            else
            {
                state.currentRelicId = string.Empty;
                ProcessGift(roomId, today);
            }

            PublishStateChanged(roomId);
        }

        public void SetCurrentRoom(RoomId roomId)
        {
            _currentRoomId = roomId;
        }

        public void ClearCurrentRoom(RoomId roomId)
        {
            if (_currentRoomId == roomId)
            {
                _currentRoomId = null;
            }
        }

        public RoomNoteData? GetCurrentNote(RoomId roomId)
        {
            RoomRollState state = GetState(roomId);
            if (string.IsNullOrWhiteSpace(state.currentNoteId))
            {
                return null;
            }

            return _catalog.Notes.FirstOrDefault(note => note.id == state.currentNoteId);
        }

        public RoomRelicData? GetCurrentRelic(RoomId roomId)
        {
            RoomRollState state = GetState(roomId);
            if (string.IsNullOrWhiteSpace(state.currentRelicId))
            {
                return null;
            }

            return _catalog.Relics.FirstOrDefault(relic => relic.id == state.currentRelicId);
        }

        public IReadOnlyList<RoomGiftData> GetPlacedGifts(RoomId roomId)
        {
            string receiver = GetReceiverCharacter(roomId);
            string giver = GetSenderCharacter(roomId);

            return _catalog.Gifts
                .Where(gift => gift.receiverCharacter == receiver &&
                               gift.giverCharacter == giver &&
                               _obtainedGiftIds.Contains(gift.id))
                .OrderBy(gift => gift.displaySlotId, StringComparer.Ordinal)
                .ToArray();
        }

        private void ProcessNote(RoomId roomId, string today)
        {
            RoomRollState state = GetState(roomId);
            if (string.Equals(state.lastNoteRollDateIso, today, StringComparison.Ordinal))
            {
                return;
            }

            state.lastNoteRollDateIso = today;

            if (!RollChance(NoteProbability))
            {
                state.currentNoteId = string.Empty;
                return;
            }

            string sender = GetSenderCharacter(roomId);
            RoomNoteData? note = PickWeighted(
                _catalog.Notes.Where(item => item.senderCharacter == sender).ToArray(),
                item => item.weight);
            state.currentNoteId = note?.id ?? string.Empty;
        }

        private void ProcessRelic(RoomId roomId, string today)
        {
            RoomRollState state = GetState(roomId);
            if (!state.relicUnlocked)
            {
                state.relicUnlocked = true;
                state.currentRelicId = RollRelicId(roomId, forced: true);
                return;
            }

            if (string.Equals(state.lastRelicRollDateIso, today, StringComparison.Ordinal))
            {
                return;
            }

            state.lastRelicRollDateIso = today;
            state.currentRelicId = RollChance(RelicProbability)
                ? RollRelicId(roomId, forced: false)
                : string.Empty;
        }

        private string RollRelicId(RoomId roomId, bool forced)
        {
            string owner = GetSenderCharacter(roomId);
            RoomRelicData? relic = PickWeighted(
                _catalog.Relics.Where(item => item.targetRoom == roomId &&
                                              item.ownerCharacter == owner).ToArray(),
                item => item.weight);
            return relic?.id ?? string.Empty;
        }

        private void ProcessGift(RoomId roomId, string today)
        {
            RoomRollState state = GetState(roomId);
            if (!state.giftUnlocked)
            {
                state.giftUnlocked = true;
                TryRollGift(roomId, forced: true);
                return;
            }

            if (string.Equals(state.lastGiftRollDateIso, today, StringComparison.Ordinal))
            {
                return;
            }

            state.lastGiftRollDateIso = today;
            if (RollChance(GiftProbability))
            {
                TryRollGift(roomId, forced: false);
            }
        }

        private void TryRollGift(RoomId roomId, bool forced)
        {
            string receiver = GetReceiverCharacter(roomId);
            string giver = GetSenderCharacter(roomId);

            RoomGiftData[] unowned = _catalog.Gifts
                .Where(gift => gift.receiverCharacter == receiver &&
                               gift.giverCharacter == giver &&
                               !_obtainedGiftIds.Contains(gift.id))
                .ToArray();

            if (unowned.Length == 0)
            {
                return;
            }

            RoomGiftData? gift = PickWeighted(unowned, item => item.weight);
            if (gift == null)
            {
                return;
            }

            _obtainedGiftIds.Add(gift.id);
            GiftObtained?.Invoke(new RoomGiftObtainedEvent(roomId, gift));
            PublishStateChanged(roomId);
        }

        private void HandleFriendshipChanged(float newFriendship)
        {
            if (_currentRoomId is not RoomId roomId)
            {
                _lastFriendship = newFriendship;
                return;
            }

            RoomRollState state = GetState(roomId);

            if (_lastFriendship < RelicUnlockFriendship &&
                newFriendship >= RelicUnlockFriendship &&
                !state.relicUnlocked)
            {
                state.relicUnlocked = true;
                state.currentRelicId = RollRelicId(roomId, forced: true);
                PublishStateChanged(roomId);
            }

            if (_lastFriendship < GiftUnlockFriendship &&
                newFriendship >= GiftUnlockFriendship)
            {
                state.currentRelicId = string.Empty;
                if (!state.giftUnlocked)
                {
                    state.giftUnlocked = true;
                    TryRollGift(roomId, forced: true);
                }
                else
                {
                    PublishStateChanged(roomId);
                }
            }

            _lastFriendship = newFriendship;
        }

        private RoomRollState GetState(RoomId roomId)
        {
            if (!_states.TryGetValue(roomId, out RoomRollState? state) || state == null)
            {
                state = new RoomRollState();
                _states[roomId] = state;
            }

            return state;
        }

        private void PublishStateChanged(RoomId roomId)
        {
            StateChanged?.Invoke(new RoomRelicStateChangedEvent(roomId, GetSnapshot(roomId)));
        }

        private static string GetSenderCharacter(RoomId roomId)
        {
            return roomId == RoomId.AngelRoom ? "Demon" : "Angel";
        }

        private static string GetReceiverCharacter(RoomId roomId)
        {
            return roomId == RoomId.AngelRoom ? "Angel" : "Demon";
        }

        private bool RollChance(float probability)
        {
            return _random.NextDouble() < probability;
        }

        private T? PickWeighted<T>(T[] items, Func<T, float> weightSelector)
        {
            if (items == null || items.Length == 0)
            {
                return default;
            }

            float total = 0f;
            for (int i = 0; i < items.Length; i++)
            {
                total += Mathf.Max(0f, weightSelector(items[i]));
            }

            if (total <= 0f)
            {
                return items[0];
            }

            float cursor = (float)_random.NextDouble() * total;
            for (int i = 0; i < items.Length; i++)
            {
                cursor -= Mathf.Max(0f, weightSelector(items[i]));
                if (cursor <= 0f)
                {
                    return items[i];
                }
            }

            return items[items.Length - 1];
        }

        public void Dispose()
        {
            _social.FriendshipChanged -= HandleFriendshipChanged;
        }

        [Serializable]
        private struct SavePayload
        {
            public int version;
            public RoomRollState angelRoom;
            public RoomRollState devilRoom;
            public List<string> obtainedGiftIds;
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(new SavePayload
            {
                version = 1,
                angelRoom = GetState(RoomId.AngelRoom),
                devilRoom = GetState(RoomId.DevilRoom),
                obtainedGiftIds = _obtainedGiftIds
            });
        }

        public bool RestoreJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                SavePayload payload = JsonUtility.FromJson<SavePayload>(json);
                _states[RoomId.AngelRoom] = payload.angelRoom ?? new RoomRollState();
                _states[RoomId.DevilRoom] = payload.devilRoom ?? new RoomRollState();
                _obtainedGiftIds.Clear();
                if (payload.obtainedGiftIds != null)
                {
                    _obtainedGiftIds.AddRange(payload.obtainedGiftIds);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
