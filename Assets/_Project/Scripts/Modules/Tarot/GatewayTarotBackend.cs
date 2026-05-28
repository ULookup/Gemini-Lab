#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeminiLab.Modules.Gateway;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.Tarot
{
    /// <summary>
    /// 走 OpenClaw Gateway 的塔罗解读后端。
    /// 按 PetId 切 persona（Angel / Devil），prompt 带上正/逆位与关键词；
    /// Gateway 当前 API 只返回 SendAsync ACK（真正回复走 GatewayEventRouter），
    /// MVP 阶段这里走本地兜底文本生成，同时把请求发给 Gateway 供日志追踪与未来事件挂接。
    /// 后续 Gateway 扩展"同步问答"接口后，这里可以直接 return 真实内容。
    /// </summary>
    public sealed class GatewayTarotBackend : ITarotReadingBackend
    {
        private readonly IGatewayClient _gatewayClient;
        private readonly TimeSpan _softTimeout;

        public GatewayTarotBackend(IGatewayClient gatewayClient, TimeSpan? softTimeout = null)
        {
            _gatewayClient = gatewayClient ?? throw new ArgumentNullException(nameof(gatewayClient));
            _softTimeout = softTimeout ?? TimeSpan.FromSeconds(6);
        }

        public async Task<TarotReading> RequestAsync(
            TarotDrawResult draw,
            PetId petId,
            TarotOrientation orientation,
            CancellationToken cancellationToken)
        {
            string traceId = Guid.NewGuid().ToString("N");
            string prompt = BuildPrompt(draw, petId, orientation);

            var request = new GatewayRequest
            {
                TraceId = traceId,
                RequestType = GatewayRequestType.Chat,
                Message = prompt,
                PlayerId = "local_player",
                PetState = string.Empty,
                Personality = JsonUtility.ToJson(new PersonaHint(petId, orientation)),
                ContentJson = "{}",
                IsAck = false
            };

            GatewaySendResult result;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_softTimeout);
                result = await _gatewayClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return LocalFallback.Build(draw, petId, orientation);
            }

            if (!result.Success)
            {
                return LocalFallback.Build(draw, petId, orientation);
            }

            // Gateway 当前没有同步响应内容通道；MVP 先走本地兜底，保留 traceId 供 Router 后续事件挂接。
            var fallback = LocalFallback.Build(draw, petId, orientation);
            return new TarotReading(petId, orientation, fallback.Text, isFromGateway: false);
        }

        private static string BuildPrompt(TarotDrawResult draw, PetId petId, TarotOrientation orientation)
        {
            string persona = petId == PetId.Angel
                ? "You are 天使 (Angel) —— 温柔、包容、愿意指出希望。回答要明亮且带鼓励。"
                : "You are 恶魔 (Devil) —— 尖锐、坦白、敢把阴影讲透。回答要带戏剧性但不恶毒。";
            string orient = orientation == TarotOrientation.Upright ? "正位 (upright)" : "逆位 (reversed)";
            string kws = string.Join("、", draw.Card.GetKeywords(orientation));

            StringBuilder sb = new();
            sb.AppendLine("[System]");
            sb.AppendLine(persona);
            sb.AppendLine("用 2-3 句中文给出今日塔罗解读，不超过 80 个汉字。");
            sb.AppendLine();
            sb.AppendLine("[User]");
            sb.Append("我今天抽到了：").Append(draw.Card.DisplayNameZh).Append("（").Append(draw.Card.DisplayNameEn).Append("），").Append(orient).AppendLine("。");
            if (!string.IsNullOrEmpty(kws))
            {
                sb.Append("关键词：").AppendLine(kws);
            }
            sb.AppendLine("请从你的人格视角给出解读。");
            return sb.ToString();
        }

        public Task<TarotSummaryResult> RequestSummaryAsync(
            TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
            string? question, CancellationToken cancellationToken)
        {
            return Task.FromResult(TarotSummaryResult.Default());
        }

        [Serializable]
        private struct PersonaHint
        {
            public string petId;
            public string orientation;
            public PersonaHint(PetId pet, TarotOrientation orient)
            {
                petId = pet.ToString();
                orientation = orient.ToString();
            }
        }
    }
}
