#nullable enable
using System;
using UnityEngine;

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// 场景切换服务：业务代码的唯一场景跳转入口。
    /// 禁止在模块内部直接调用 UnityEngine.SceneManagement.SceneManager。
    /// </summary>
    public interface ISceneFlowService
    {
        /// <summary>当前已激活的逻辑场景。</summary>
        SceneId CurrentScene { get; }

        /// <summary>是否有场景正在加载。</summary>
        bool IsLoading { get; }

        /// <summary>
        /// 发起异步切换请求。返回的 <see cref="AsyncOperation"/> 可以 yield。
        /// 返回 null 代表目标场景即为当前场景，不做切换。
        /// </summary>
        AsyncOperation? LoadAsync(SceneId target, SceneTransitionPayload? payload = null, Action? onCompleted = null);
    }
}
