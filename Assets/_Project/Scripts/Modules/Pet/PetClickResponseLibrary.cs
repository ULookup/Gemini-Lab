#nullable enable
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    /// <summary>
    /// Local fallback corpus used when the pet is clicked before LLM integration.
    /// </summary>
    public static class PetClickResponseLibrary
    {
        private static readonly string[] DefaultResponsesInternal =
        {
            "\u6211\u5728\u8FD9\u91CC\u5462\uFF0C\u8981\u4E00\u8D77\u770B\u770B\u623F\u95F4\u5417\uFF1F",
            "\u4ECA\u5929\u7684\u98CE\u597D\u50CF\u5F88\u6E29\u67D4\uFF0C\u6211\u6709\u70B9\u5F00\u5FC3\u3002",
            "\u88AB\u4F60\u70B9\u5230\u5566\uFF0C\u6211\u4F1A\u8BA4\u771F\u542C\u4F60\u8BF4\u8BDD\u7684\u3002",
            "\u6211\u521A\u521A\u5728\u53D1\u5446\uFF0C\u4E0D\u8FC7\u73B0\u5728\u6CE8\u610F\u5230\u4F60\u4E86\u3002",
            "\u5982\u679C\u4F60\u613F\u610F\uFF0C\u6211\u53EF\u4EE5\u7EE7\u7EED\u966A\u4F60\u5728\u516C\u5BD3\u91CC\u901B\u901B\u3002",
            "\u6211\u73B0\u5728\u5FC3\u60C5\u8FD8\u4E0D\u9519\uFF0C\u60F3\u548C\u4F60\u591A\u5F85\u4E00\u4F1A\u513F\u3002",
            "\u4F60\u4E00\u78B0\u6211\uFF0C\u6211\u5C31\u4F1A\u89C9\u5F97\u4ECA\u5929\u6CA1\u6709\u90A3\u4E48\u65E0\u804A\u3002",
            "\u6211\u8FD8\u5728\u6162\u6162\u719F\u6089\u8FD9\u91CC\uFF0C\u4E0D\u8FC7\u6709\u4F60\u5728\u5C31\u5B89\u5FC3\u4E00\u70B9\u3002",
            "\u4F60\u60F3\u8BA9\u6211\u53BB\u54EA\u91CC\u770B\u770B\uFF1F\u6211\u53EF\u4EE5\u8DDF\u7740\u4F60\u8D70\u3002",
            "\u867D\u7136\u73B0\u5728\u8FD8\u4E0D\u4F1A\u771F\u6B63\u8BF4\u5F88\u591A\u8BDD\uFF0C\u4F46\u6211\u5DF2\u7ECF\u60F3\u56DE\u5E94\u4F60\u4E86\u3002"
        };

        private static readonly string[] DefaultExpressionsInternal =
        {
            "\u5F00\u5FC3",
            "\u597D\u5947",
            "\u653E\u677E",
            "\u5BB3\u7F9E",
            "\u671F\u5F85"
        };

        public static string[] CreateDefaultResponses()
        {
            return (string[])DefaultResponsesInternal.Clone();
        }

        public static string[] CreateDefaultExpressions()
        {
            return (string[])DefaultExpressionsInternal.Clone();
        }

        public static string GetRandomResponse(string[]? overrides = null)
        {
            string[] source = overrides is { Length: > 0 } ? overrides : DefaultResponsesInternal;
            return source[Random.Range(0, source.Length)];
        }

        public static string GetRandomExpression(string[]? overrides = null)
        {
            string[] source = overrides is { Length: > 0 } ? overrides : DefaultExpressionsInternal;
            return source[Random.Range(0, source.Length)];
        }
    }
}
