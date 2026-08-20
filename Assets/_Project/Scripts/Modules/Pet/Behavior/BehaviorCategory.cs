#nullable enable
namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 行为类型（数值规则文档 §6）。心情倍率按此分类查表。
    /// </summary>
    public enum BehaviorCategory
    {
        /// <summary>中性：任何心情档位倍率都是 1.0。</summary>
        Neutral = 0,

        /// <summary>休息：心情差时更倾向（1.4），心情好时降低（0.7）。</summary>
        Rest = 1,

        /// <summary>安静：心情差时略增（1.15），心情好时略降（0.95）。</summary>
        Quiet = 2,

        /// <summary>活跃：心情差时降低（0.7），心情好时提高（1.25）。</summary>
        Active = 3,

        /// <summary>社交/探索：心情差时明显降低（0.6），心情好时明显提高（1.3）。</summary>
        Social = 4
    }
}
