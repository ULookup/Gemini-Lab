#nullable enable
namespace GeminiLab.Modules.Pet.Social
{
    /// <summary>一次交流完成后的广播（UI/剧情系统可订阅）。</summary>
    public readonly struct PetSocialInteractionEvent
    {
        public PetSocialInteractionEvent(
            PetId initiator,
            PetId target,
            SocialResponseType responseType,
            float friendshipDelta,
            float friendship,
            bool friendshipGainApplied)
        {
            Initiator = initiator;
            Target = target;
            ResponseType = responseType;
            FriendshipDelta = friendshipDelta;
            Friendship = friendship;
            FriendshipGainApplied = friendshipGainApplied;
        }

        public PetId Initiator { get; }
        public PetId Target { get; }
        public SocialResponseType ResponseType { get; }

        /// <summary>实际生效的亲密度变化（防刷冷却中为 0，§18）。</summary>
        public float FriendshipDelta { get; }

        /// <summary>结算后的亲密度。</summary>
        public float Friendship { get; }

        /// <summary>本次是否真正写入了亲密度（false = 处于 300s 防刷冷却中）。</summary>
        public bool FriendshipGainApplied { get; }
    }
}
