#nullable enable
using System;

namespace GeminiLab.Modules.Pet.Behavior
{
    /// <summary>
    /// 行为标签（数值规则文档 §4）。新增行为时用标签而非硬编码名称参与硬规则过滤。
    /// </summary>
    [Flags]
    public enum BehaviorTag
    {
        None = 0,

        /// <summary>活跃行为：精力低于阈值（默认 10，文档 §4 "6~9 禁止活跃行为"）时被过滤。</summary>
        Active = 1 << 0
    }
}
