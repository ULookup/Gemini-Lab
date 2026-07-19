#nullable enable

namespace GeminiLab.Core
{
    /// <summary>
    /// 全局开发模式开关。Editor 下通过 Tools → Gemini-Lab → Toggle Dev Mode 切换。
    /// 运行时只读——设置由 Editor 侧的 <c>[InitializeOnLoad]</c> 完成。
    /// 打包后强制 false。
    /// </summary>
    public static class DevMode
    {
        /// <summary>当前是否处于开发者模式（开发期默认 true，打包后强制 false）。</summary>
        public static bool Active { get; set; } = true;

#if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForcePlayerMode() => Active = false;
#endif
    }
}
