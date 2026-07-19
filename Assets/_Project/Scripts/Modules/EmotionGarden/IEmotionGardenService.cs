#nullable enable
using System;
using System.Collections.Generic;

namespace GeminiLab.Modules.EmotionGarden
{
    /// <summary>
    /// 情绪花园对外门面。
    /// 存档走 <see cref="GeminiLab.Core.Persistence.IPersistentService"/>（Key=<c>emotion-garden</c>）。
    /// </summary>
    public interface IEmotionGardenService
    {
        /// <summary>今天是否已经提交过心情。</summary>
        bool CanSubmitToday();

        /// <summary>
        /// 提交今天的情绪，生成情绪花。同日重复提交返回 null。
        /// owner: "angel" / "demon"
        /// 当前阶段 emotionType 固定为 "悲伤"（占位）。
        /// </summary>
        EmotionFlowerData? SubmitEmotion(string emotionType, string emotionDetail, string owner);

        /// <summary>获取今天的情绪花（已提交则返回，否则 null）。</summary>
        EmotionFlowerData? GetTodayFlower();

        /// <summary>获取当前周编号（年份限定格式：年份*100+周号，如 202629）。</summary>
        int GetCurrentWeekId();

        /// <summary>周编号偏移 N 周（可为负），正确处理跨年和 52/53 周年份。</summary>
        int OffsetWeekId(int weekId, int deltaWeeks);

        /// <summary>获取指定周的周一日期（本地）。非法 weekId 返回 <see cref="DateTime.MinValue"/>。</summary>
        DateTime GetWeekStartDate(int weekId);

        /// <summary>获取指定周的 7 天情绪花数组（索引 0=周一 … 6=周日），无花的格子为 null。</summary>
        EmotionFlowerData?[] GetWeekFlowers(int weekId);

        /// <summary>将指定花设为已开花，并收入图鉴（幂等）。</summary>
        bool SetBloomed(string flowerId);

        /// <summary>获取所有情绪+培育者组合的累计进度。</summary>
        IReadOnlyList<ClusterProgress> GetAllClusters();

        /// <summary>检查所有 Growing 状态的花，跨天则自动开花（幂等）。</summary>
        void RefreshBlooming();

        /// <summary>[调试] 清空全部花园数据（花、图鉴进度、每日限制），发布 <see cref="EmotionGardenClearedEvent"/>。</summary>
        void ClearAllData();
    }
}
