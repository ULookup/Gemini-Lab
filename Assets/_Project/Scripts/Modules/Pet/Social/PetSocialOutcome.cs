#nullable enable
namespace GeminiLab.Modules.Pet.Social
{
    /// <summary>一次交流的结算结果（数值规则文档 §15-18）。</summary>
    public readonly struct PetSocialOutcome
    {
        public PetSocialOutcome(
            bool initiated,
            SocialResponseType responseType,
            float initiatorEnergyDelta,
            float initiatorMoodDelta,
            float targetEnergyDelta,
            float targetMoodDelta,
            float friendshipDelta,
            bool friendshipGainApplied)
        {
            Initiated = initiated;
            ResponseType = responseType;
            InitiatorEnergyDelta = initiatorEnergyDelta;
            InitiatorMoodDelta = initiatorMoodDelta;
            TargetEnergyDelta = targetEnergyDelta;
            TargetMoodDelta = targetMoodDelta;
            FriendshipDelta = friendshipDelta;
            FriendshipGainApplied = friendshipGainApplied;
        }

        /// <summary>交流是否实际发生（false = 发起者精力不足，§16）。</summary>
        public bool Initiated { get; }

        public SocialResponseType ResponseType { get; }

        public float InitiatorEnergyDelta { get; }
        public float InitiatorMoodDelta { get; }
        public float TargetEnergyDelta { get; }
        public float TargetMoodDelta { get; }

        /// <summary>实际生效的亲密度变化（防刷冷却中为 0）。</summary>
        public float FriendshipDelta { get; }

        /// <summary>本次是否真正写入了亲密度。</summary>
        public bool FriendshipGainApplied { get; }

        /// <summary>交流未发生时的空结果。</summary>
        public static PetSocialOutcome NotInitiated { get; } =
            new PetSocialOutcome(false, SocialResponseType.NeedSpace, 0f, 0f, 0f, 0f, 0f, false);
    }
}
