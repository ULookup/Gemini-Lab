#nullable enable
using System;

namespace GeminiLab.Modules.EmotionGarden
{
    public enum GrowthState
    {
        Growing = 0,
        Bloomed = 1
    }

    /// <summary>
    /// 一朵情绪花的全部数据。以 DateIso + Owner 确定唯一性（每天每个培育者最多一朵）。
    /// </summary>
    [Serializable]
    public struct EmotionFlowerData
    {
        public string FlowerId;
        public string DateIso;
        public int WeekId;
        public string EmotionType;
        public string EmotionDetail;
        public string Owner;
        public GrowthState State;
        public bool IsCollected;
        public long CreatedAtUtcTicks;
    }

    /// <summary>
    /// 按"情绪类型 + 培育者"累计的解锁进度。
    /// </summary>
    [Serializable]
    public struct ClusterProgress
    {
        public string EmotionType;
        public string Owner;
        public int TotalCount;
        public int UnlockedStage;
    }

    /// <summary>情绪花提交成功。</summary>
    public readonly struct EmotionFlowerSubmittedEvent
    {
        public EmotionFlowerSubmittedEvent(EmotionFlowerData flower) { Flower = flower; }
        public EmotionFlowerData Flower { get; }
    }

    /// <summary>情绪花开花。</summary>
    public readonly struct EmotionFlowerBloomedEvent
    {
        public EmotionFlowerBloomedEvent(string flowerId) { FlowerId = flowerId; }
        public string FlowerId { get; }
    }

    /// <summary>[调试] 花园数据已整体清空。</summary>
    public readonly struct EmotionGardenClearedEvent
    {
    }

    /// <summary>某个情绪+培育者组合达到新解锁阶段。</summary>
    public readonly struct ClusterUnlockedEvent
    {
        public ClusterUnlockedEvent(string emotionType, string owner, int newStage)
        {
            EmotionType = emotionType;
            Owner = owner;
            NewStage = newStage;
        }
        public string EmotionType { get; }
        public string Owner { get; }
        public int NewStage { get; }
    }
}
