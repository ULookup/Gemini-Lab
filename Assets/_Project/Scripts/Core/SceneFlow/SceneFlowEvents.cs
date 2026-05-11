#nullable enable

namespace GeminiLab.Core.SceneFlow
{
    /// <summary>
    /// 场景加载开始时通过 EventBus 广播。
    /// </summary>
    public readonly struct SceneLoadStartedEvent
    {
        public SceneId From { get; }
        public SceneId To { get; }
        public SceneTransitionPayload? Payload { get; }

        public SceneLoadStartedEvent(SceneId from, SceneId to, SceneTransitionPayload? payload)
        {
            From = from;
            To = to;
            Payload = payload;
        }
    }

    /// <summary>
    /// 场景加载完成、已 activate 后通过 EventBus 广播。
    /// </summary>
    public readonly struct SceneLoadCompletedEvent
    {
        public SceneId From { get; }
        public SceneId To { get; }
        public SceneTransitionPayload? Payload { get; }

        public SceneLoadCompletedEvent(SceneId from, SceneId to, SceneTransitionPayload? payload)
        {
            From = from;
            To = to;
            Payload = payload;
        }
    }
}
