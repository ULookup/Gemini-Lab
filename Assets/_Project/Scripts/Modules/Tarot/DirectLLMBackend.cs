#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Core;
using GeminiLab.Modules.Pet;
using UnityEngine;
using UnityEngine.Networking;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// UnityWebRequest 直连 OpenAI 兼容 LLM API 的塔罗解读后端。
    /// 需要 LLMConfigSO 配置 endpoint + key；未配置时回退到 LocalFallback。
    /// </summary>
    public sealed class DirectLLMBackend : ITarotReadingBackend
    {
        private readonly LLMConfigSO _config;
        private readonly Func<PetId, string>? _personalityResolver;

        public DirectLLMBackend(LLMConfigSO config, Func<PetId, string>? personalityResolver = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _personalityResolver = personalityResolver;
        }

        public async Task<TarotReading> RequestAsync(
            TarotDrawResult draw,
            PetId petId,
            TarotOrientation orientation,
            CancellationToken cancellationToken)
        {
            if (!_config.IsConfigured)
            {
                return LocalFallback.Build(draw, petId, orientation);
            }

            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(draw, petId);

            string responseText;
            try
            {
                responseText = await SendRequestAsync(systemPrompt, userPrompt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DirectLLM] Request failed: {ex.Message}");
                return LocalFallback.Build(draw, petId, orientation);
            }

            return new TarotReading(petId, orientation, responseText, isFromGateway: true);
        }

        private string BuildSystemPrompt(PetId petId)
        {
            string template = petId == PetId.Angel
                ? _config.AngelSystemTemplate
                : _config.DevilSystemTemplate;

            string personalityText = ResolvePersonality(petId);

            return template.Replace("{personality}", personalityText);
        }

        private string BuildUserPrompt(TarotDrawResult draw, PetId petId)
        {
            string template = _config.UserMessageTemplate;
            return template
                .Replace("{cardName}", $"{draw.Card.DisplayNameZh} ({draw.Card.DisplayNameEn})")
                .Replace("{slotName}", "")
                .Replace("{question}", "")
                .Replace("{keywords}", string.Join("、", draw.Card.GetKeywords(draw.Orientation)));
        }

        private async Task<string> SendRequestAsync(string systemPrompt, string userPrompt,
            CancellationToken cancellationToken)
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

        private string ResolvePersonality(PetId petId)
        {
            if (_personalityResolver != null)
            {
                return _personalityResolver(petId);
            }
            return "性格数据未加载";
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
