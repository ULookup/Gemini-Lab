#nullable enable

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// SceneId 到 Unity 真实 scene name 的映射。
    /// 当前阶段提供硬编码默认实现 <see cref="DefaultSceneCatalog"/>。
    /// 若后续需要在 Inspector 配置，可替换为 ScriptableObject 实现。
    /// </summary>
    public interface ISceneCatalog
    {
        /// <summary>
        /// 返回该 <see cref="SceneId"/> 在 Build Settings 中的 scene name。
        /// </summary>
        string GetSceneName(SceneId id);
    }
}
