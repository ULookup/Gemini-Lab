#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.Pet.Personality;
using GeminiLab.Modules.Tarot;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.Pet
{
    public interface IPetChatService
    {
        Task<PetChatResult> SendMessageAsync(
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default);
    }

    [Serializable]
    public sealed class ChatStatChanges
    {
        public float mood;
        public float energy;
        public float relation;
        public float kindness;
        public float evilness;
        public float calmness;
        public float bravery;
        public float shyness;
        public float integrity;
        public float curiosity;
    }

    public sealed class PetChatResult
    {
        public string AngelReply = string.Empty;
        public string DevilReply = string.Empty;
        public ChatStatChanges? AngelStatChanges;
        public ChatStatChanges? DevilStatChanges;
        public bool IsAngelFallback;
        public bool IsDevilFallback;
        public bool IsCancelled;
    }

    public sealed class PetChatService : IPetChatService
    {
        private readonly LLMConfigSO _config;
        private readonly float _timeoutSeconds;

        private static readonly string[] AngelFallbacks =
        {
            "我一直都在呢~有什么想聊的？🌸",
            "今天也请保持好心情哦。",
            "虽然不太确定，但我觉得一切都会好起来的~",
            "你想听听我的想法吗？☀️",
            "温柔对待自己，你已经做得很好了。"
        };

        private static readonly string[] DevilFallbacks =
        {
            "啧，又来找我了？行吧，陪你聊聊。",
            "说实话，这事有点无聊，不过你开心就好。",
            "哼，我可不是为了你才回答的。",
            "看在你主动找我的份上，勉为其难告诉你吧。",
            "直接说吧，别拐弯抹角的。"
        };

        public PetChatService(LLMConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _timeoutSeconds = config.TimeoutSeconds > 0f ? config.TimeoutSeconds : 10f;
            ServiceLocator.Register<IPetChatService>(this);
        }

        public async Task<PetChatResult> SendMessageAsync(
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new PetChatResult();
            }

            bool hasAngel = false;
            bool hasDevil = false;
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                hasAngel = roster!.TryGet(PetId.Angel) != null;
                hasDevil = roster!.TryGet(PetId.Devil) != null;
            }

            if (!hasAngel && !hasDevil)
            {
                return new PetChatResult
                {
                    AngelReply = "（宠物不在家...）",
                    DevilReply = "（宠物不在家...）",
                    IsAngelFallback = true,
                    IsDevilFallback = true
                };
            }

            bool angelFirst = UnityEngine.Random.value >= 0.5f;

            var result = new PetChatResult();

            var firstPet = angelFirst ? PetId.Angel : PetId.Devil;
            var secondPet = angelFirst ? PetId.Devil : PetId.Angel;

            string? firstReply = null;

            if (TryGetPet(firstPet, out _))
            {
                (string reply, ChatStatChanges? stats, bool isFallback) = await RequestPetReplyAsync(
                    firstPet, userMessage, history, null, cancellationToken);
                firstReply = reply;
                SetResult(result, firstPet, reply, stats, isFallback);
                ApplyStatChanges(firstPet, stats);
            }

            if (TryGetPet(secondPet, out _))
            {
                (string reply, ChatStatChanges? stats, bool isFallback) = await RequestPetReplyAsync(
                    secondPet, userMessage, history, firstReply, cancellationToken);
                SetResult(result, secondPet, reply, stats, isFallback);
                ApplyStatChanges(secondPet, stats);
            }

            return result;
        }

        private async Task<(string reply, ChatStatChanges? stats, bool isFallback)> RequestPetReplyAsync(
            PetId petId,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            string? otherPetReply,
            CancellationToken cancellationToken)
        {
            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(userMessage, otherPetReply, history);

            if (!_config.IsConfigured)
            {
                return (GetFallback(petId), null, true);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                string response = await SendLLMRequestAsync(systemPrompt, userPrompt, cts.Token);
                var (cleaned, stats) = ExtractStatChanges(response);
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    return (GetFallback(petId), null, true);
                }
                return (cleaned, stats, false);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                return (GetFallback(petId), null, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PetChat] LLM request failed for {petId}: {ex.Message}");
                return (GetFallback(petId), null, true);
            }
        }

        private string BuildSystemPrompt(PetId petId)
        {
            string petName = petId == PetId.Angel ? "Angel" : "Devil";

            PersonalityVector personality = default;
            if (ServiceLocator.TryResolve<IPersonalityEvolutionService>(out var personalityService))
            {
                personality = personalityService!.GetMatrix(petId);
            }

            PetRuntimeData? runtime = null;
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                runtime = roster!.TryGet(petId);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"你是 {petName}，住在玩家的公寓里。");
            sb.AppendLine();
            sb.AppendLine("## 你的性格");
            sb.AppendLine($"- 善良: {personality.Kindness:F2}  /  邪恶: {personality.Evilness:F2}  /  冷静: {personality.Calmness:F2}");
            sb.AppendLine($"- 勇敢: {personality.Bravery:F2}  /  害羞: {personality.Shyness:F2}  /  正直: {personality.Integrity:F2}");
            sb.AppendLine($"- 好奇心: {personality.Curiosity:F2}");

            if (runtime != null)
            {
                sb.AppendLine();
                sb.AppendLine("## 你当前的状态");
                sb.AppendLine($"- 心情: {runtime.Mood:F0}/100");
                sb.AppendLine($"- 精力: {runtime.Energy:F0}/100");
                sb.AppendLine($"- 饱腹: {runtime.Satiety:F0}/100");
                sb.AppendLine($"- 当前动作: {runtime.CurrentState}");
                if (runtime.IsTraveling)
                {
                    sb.AppendLine("- 正在旅行中");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## 回复规则");
            sb.AppendLine("- 回复要简短自然，2-4句话，不要超过 80 个字");
            sb.AppendLine("- 回复要符合你的性格，保持角色一致性");
            sb.AppendLine("- 回复可以提及你当前的状态（比如累了就说累）");
            sb.AppendLine("- 用口语化中文回复");
            sb.AppendLine("- 可以适当接上一句话茬（如果另一个宠物刚说过话）");
            sb.AppendLine();
            sb.AppendLine("## 数值变化（必须输出）");
            sb.AppendLine("根据用户说的话对你产生的影响，在回复末尾附上数值变化 JSON。");
            sb.AppendLine("格式：---STATS---换行后接一个 JSON 对象，再换行---END---");
            sb.AppendLine("示例：");
            sb.AppendLine("---STATS---");
            sb.AppendLine("{\"mood\":3,\"energy\":-2,\"relation\":1,\"kindness\":0,\"evilness\":0,\"calmness\":0,\"bravery\":0,\"shyness\":0,\"integrity\":0,\"curiosity\":0}");
            sb.AppendLine("---END---");
            sb.AppendLine("规则：");
            sb.AppendLine("- mood/energy/relation 每次变化不超过 ±8，通常在 ±3 以内");
            sb.AppendLine("- 性格维度（kindness/evilness/calmness/bravery/shyness/integrity/curiosity）变化在 ±0.1 以内，通常为 0");
            sb.AppendLine("- 只在对话内容与性格相关时才修改性格维度（如用户赞扬善良 → kindness +0.05）");
            sb.AppendLine("- 如果用户的话对你完全没有影响，所有值填 0");
            sb.AppendLine("- 不要输出任何格式以外的内容，不要输出 markdown 代码块");

            return sb.ToString();
        }

        private static string BuildUserPrompt(string userMessage, string? otherPetReply,
            IReadOnlyList<ChatMessage> history)
        {
            var sb = new StringBuilder();

            // Include recent conversation history (last 10 turns) for context
            int start = Math.Max(0, history.Count - 10);
            for (int i = start; i < history.Count; i++)
            {
                var msg = history[i];
                string roleLabel = msg.Role switch
                {
                    ChatRole.User => "用户",
                    ChatRole.Angel => "Angel",
                    ChatRole.Devil => "Devil",
                    _ => "?"
                };
                sb.AppendLine($"{roleLabel}：{msg.Text}");
            }

            if (!string.IsNullOrEmpty(otherPetReply))
            {
                sb.AppendLine($"[另一个宠物刚对你说：{otherPetReply}]");
            }

            sb.AppendLine($"用户说：{userMessage}");
            return sb.ToString();
        }

        private async Task<string> SendLLMRequestAsync(
            string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            var body = new LLMRequest
            {
                model = _config.Model,
                messages = new[]
                {
                    new LLMMessage { role = "system", content = systemPrompt },
                    new LLMMessage { role = "user", content = userPrompt }
                },
                max_tokens = 200
            };

            string json = JsonUtility.ToJson(body);
            using var req = new UnityWebRequest(_config.Endpoint, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");

            var operation = req.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    req.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"LLM request failed: {req.error} — {req.downloadHandler?.text}");
            }

            string responseJson = req.downloadHandler?.text ?? string.Empty;
            var response = JsonUtility.FromJson<LLMResponse>(responseJson);
            if (response.choices == null || response.choices.Length == 0)
            {
                throw new Exception("LLM response has no choices");
            }

            return response.choices[0].message?.content ?? string.Empty;
        }

        private static string GetFallback(PetId petId)
        {
            var pool = petId == PetId.Angel ? AngelFallbacks : DevilFallbacks;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private static (string cleaned, ChatStatChanges? stats) ExtractStatChanges(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return (string.Empty, null);

            // Remove markdown code blocks first
            string trimmed = raw.Trim();
            if (trimmed.StartsWith("```json")) trimmed = trimmed[7..];
            else if (trimmed.StartsWith("```")) trimmed = trimmed[3..];
            trimmed = trimmed.TrimEnd();
            if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
            trimmed = trimmed.Trim();

            const string marker = "---STATS---";
            const string endMarker = "---END---";
            int statsStart = trimmed.LastIndexOf(marker, StringComparison.Ordinal);
            int statsEnd = trimmed.LastIndexOf(endMarker, StringComparison.Ordinal);

            if (statsStart >= 0 && statsEnd > statsStart)
            {
                string jsonPart = trimmed[(statsStart + marker.Length)..statsEnd].Trim();
                string reply = trimmed[..statsStart].Trim();

                try
                {
                    var stats = JsonUtility.FromJson<ChatStatChanges>(jsonPart);
                    return (reply, stats);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PetChat] Failed to parse stat changes: {ex.Message}");
                    return (reply, null);
                }
            }

            return (trimmed, null);
        }

        private static void ApplyStatChanges(PetId petId, ChatStatChanges? stats)
        {
            if (stats == null) return;

            // Apply runtime stat deltas via IPetRoster
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                var data = roster!.TryGet(petId);
                if (data != null)
                {
                    data.Mood = Mathf.Clamp(data.Mood + stats.mood, 0f, 100f);
                    data.Energy = Mathf.Clamp(data.Energy + stats.energy, 0f, 100f);
                    data.Relation = Mathf.Clamp(data.Relation + stats.relation, 0f, 100f);
                }
            }

            // Apply personality deltas via IPersonalityEvolutionService
            var personalityDelta = new PersonalityVector
            {
                Kindness = stats.kindness,
                Evilness = stats.evilness,
                Calmness = stats.calmness,
                Bravery = stats.bravery,
                Shyness = stats.shyness,
                Integrity = stats.integrity,
                Curiosity = stats.curiosity
            };

            bool hasPersonalityDelta =
                Mathf.Abs(personalityDelta.Kindness) > 0.0001f ||
                Mathf.Abs(personalityDelta.Evilness) > 0.0001f ||
                Mathf.Abs(personalityDelta.Calmness) > 0.0001f ||
                Mathf.Abs(personalityDelta.Bravery) > 0.0001f ||
                Mathf.Abs(personalityDelta.Shyness) > 0.0001f ||
                Mathf.Abs(personalityDelta.Integrity) > 0.0001f ||
                Mathf.Abs(personalityDelta.Curiosity) > 0.0001f;

            if (hasPersonalityDelta &&
                ServiceLocator.TryResolve<IPersonalityEvolutionService>(out var evolution))
            {
                evolution!.ApplyDelta(petId, personalityDelta);
            }
        }

        private static void SetResult(PetChatResult result, PetId petId, string reply, ChatStatChanges? stats, bool isFallback)
        {
            if (petId == PetId.Angel)
            {
                result.AngelReply = reply;
                result.AngelStatChanges = stats;
                result.IsAngelFallback = isFallback;
            }
            else
            {
                result.DevilReply = reply;
                result.DevilStatChanges = stats;
                result.IsDevilFallback = isFallback;
            }
        }

        private static bool TryGetPet(PetId petId, out PetRuntimeData? data)
        {
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                data = roster!.TryGet(petId);
                return data != null;
            }
            data = null;
            return false;
        }

        [Serializable]
        private sealed class LLMRequest
        {
            public string model = string.Empty;
            public LLMMessage[] messages = Array.Empty<LLMMessage>();
            public int max_tokens;
        }

        [Serializable]
        private sealed class LLMMessage
        {
            public string role = string.Empty;
            public string content = string.Empty;
        }

        [Serializable]
        private sealed class LLMResponse
        {
            public LLMChoice[] choices = Array.Empty<LLMChoice>();
        }

        [Serializable]
        private sealed class LLMChoice
        {
            public LLMMessage? message;
        }
    }
}
