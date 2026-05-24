#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// LLM 直连配置：endpoint / key / model / prompt 模板。
    /// 在 Project 窗口右键 Create → GeminiLab → Tarot → LLM Config 创建。
    /// </summary>
    [CreateAssetMenu(menuName = "GeminiLab/Tarot/LLM Config", fileName = "LLMConfig")]
    public sealed class LLMConfigSO : ScriptableObject
    {
        [Tooltip("OpenAI 兼容 API 地址，如 https://api.openai.com/v1/chat/completions")]
        [SerializeField] private string _endpoint = "https://api.openai.com/v1/chat/completions";

        [Tooltip("API Key")]
        [SerializeField] private string _apiKey = string.Empty;

        [Tooltip("模型名，如 gpt-4o / claude-sonnet-4-6")]
        [SerializeField] private string _model = "gpt-4o";

        [Tooltip("天使 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _angelSystemTemplate =
            "你是一位天使 (Angel) —— 温柔、包容、愿意指出希望。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 80 个汉字。";

        [Tooltip("恶魔 System prompt。占位符: {personality}")]
        [TextArea(5, 20)]
        [SerializeField] private string _devilSystemTemplate =
            "你是一位恶魔 (Devil) —— 尖锐、坦白、敢把阴影讲透。\n当前的你：{personality}\n用 2-3 句中文给出塔罗解读，不超过 80 个汉字。回答要带戏剧性但不恶毒。";

        [Tooltip("User 消息模板。占位符: {cardName}, {slotName}, {question}, {keywords}")]
        [TextArea(3, 10)]
        [SerializeField] private string _userMessageTemplate =
            "玩家抽到了：{cardName}。这是代表「{slotName}」的牌。\n" +
            "玩家想问：{question}\n" +
            "关键词：{keywords}\n" +
            "请从你的人格视角给出「{slotName}」的解读。";

        [Tooltip("单次请求超时秒数")]
        [SerializeField] private float _timeoutSeconds = 30f;

        public string Endpoint => _endpoint;
        public string ApiKey => _apiKey;
        public string Model => _model;
        public string AngelSystemTemplate => _angelSystemTemplate;
        public string DevilSystemTemplate => _devilSystemTemplate;
        public string UserMessageTemplate => _userMessageTemplate;
        public float TimeoutSeconds => _timeoutSeconds;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_apiKey);
    }
}
