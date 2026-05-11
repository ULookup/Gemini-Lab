#nullable enable

namespace GeminiLab.Core.Persistence
{
    /// <summary>
    /// 所有需要进存档的业务服务（Tarot / Inventory / Collection / Garden / Pet …）
    /// 统一实现此接口，由未来 <c>SaveSystem</c> 在保存/加载时按 <see cref="Key"/> 路由。
    ///
    /// 当前阶段（Phase C）这是一个**空契约 + 协议文档**：
    /// - 接口已落地，但 SaveSystem 还没改造成用它
    /// - 各服务可以按自己节奏实现此接口，先把 CaptureJson / RestoreJson 写出来
    /// - C1 阶段 SaveSystem 整合时，会扫描 ServiceLocator 里所有 IPersistentService 并统一序列化
    ///
    /// 约束：
    /// 1. <see cref="Key"/> 在整个项目唯一（例："tarot" / "inventory" / "pet.angel"）。
    /// 2. <see cref="CaptureJson"/> 必须是纯函数，不改运行态。
    /// 3. <see cref="RestoreJson"/> 接受任意字符串（含空串 / 损坏 JSON），失败时保持运行态不变并返回 false。
    /// 4. 序列化格式约定为 UTF-8 JSON；版本字段放在 JSON 内部，由各服务自行处理向前兼容。
    /// </summary>
    public interface IPersistentService
    {
        /// <summary>项目唯一的存档键；推荐小写下划线 + 点分隔（模块.子项）。</summary>
        string Key { get; }

        /// <summary>序列化当前运行态为 JSON 字符串。</summary>
        string CaptureJson();

        /// <summary>
        /// 从 JSON 字符串恢复运行态。
        /// 返回 true = 成功；false = 失败（SaveSystem 应记录并保持默认状态）。
        /// </summary>
        bool RestoreJson(string json);
    }
}
