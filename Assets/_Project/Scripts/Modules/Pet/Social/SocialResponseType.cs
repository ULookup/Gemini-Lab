#nullable enable
namespace GeminiLab.Modules.Pet.Social
{
    /// <summary>
    /// 交流回应类型（数值规则文档 §15）。
    /// </summary>
    public enum SocialResponseType
    {
        /// <summary>对方需要空间：Target.Energy &lt; 30 或 Target.Mood &lt; 30。优先级最高。</summary>
        NeedSpace = 0,

        /// <summary>普通交流。</summary>
        Normal = 1,

        /// <summary>亲密交流：Friendship &gt;= 60。</summary>
        Warm = 2
    }
}
