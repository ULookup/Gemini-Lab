#nullable enable
using System;

namespace GeminiLab.Modules.Pet.Social
{
    /// <summary>
    /// 双宠物交流系统（数值规则文档 §14-19）。
    /// 亲密度为“对子”级数据（Angel ↔ Devil 共享一个值），初始 30，无自然衰减。
    /// </summary>
    public interface IPetSocialService
    {
        /// <summary>当前亲密度 0-100（§14）。</summary>
        float Friendship { get; }

        /// <summary>亲密度阶段标签：疏远/普通/熟悉/亲近/高亲密（§14）。</summary>
        string FriendshipStageLabel { get; }

        /// <summary>发起者精力是否足够发起交流（§16：Initiator.Energy &lt; 10 禁止发起）。</summary>
        bool CanInitiate(PetId initiator);

        /// <summary>仅计算对方回应类型，不产生任何数值变化（§15）。</summary>
        SocialResponseType ResolveResponseType(PetId target);

        /// <summary>
        /// 尝试发起一次交流并完整结算（§15-18）：
        /// 发起者精力不足时交流不发生（Initiated=false）；
        /// 否则按回应类型结算双方精力/心情，亲密度受 300s 防刷冷却限制。
        /// 玩家选择“不打扰了”时不要调用本方法（所有变化为 0）。
        /// </summary>
        PetSocialOutcome TrySocialize(PetId initiator, PetId target);

        /// <summary>特殊事件直接增减亲密度（§19），不受防刷冷却限制，也不刷新冷却时间。</summary>
        void ApplySpecialEventFriendship(float delta);

        /// <summary>交流完成结算后广播（交流未发生时不触发）。</summary>
        event Action<PetSocialInteractionEvent>? SocialInteractionCompleted;

        /// <summary>亲密度实际变化后触发（含特殊事件）；参数为变化后的值。</summary>
        event Action<float>? FriendshipChanged;
    }
}
