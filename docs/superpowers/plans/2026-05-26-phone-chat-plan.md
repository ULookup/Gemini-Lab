# Phone Chat System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在公寓场景构建手机宠物聊天系统——左下角手机按钮弹出居中、用户与 Angel/Devil 双宠对话、LLM 基于性格+状态独立生成回复。

**Architecture:** 6 个模块分层协作：`ChatPhoneController` 作为主控驱动动画状态机，`PetChatService` 处理 LLM Prompt 构造+HTTP 调用+Fallback，`ChatPersistenceService` 管理聊天记录持久化，`ChatMessageListView`/`ChatInputHandler`/`PhoneAnimController` 各管 UI 一层。通过直接方法调用协作，不经过 EventBus。

**Tech Stack:** Unity C# (UnityWebRequest for LLM), TMP_InputField, ScrollRect, Coroutine-based animation, JsonUtility serialization

---

## File Structure

| File | Path | Responsibility |
|------|------|----------------|
| ChatMessage.cs | `Modules/Pet/` | 数据模型 + 枚举 |
| ChatPersistenceService.cs | `Modules/Pet/` | 聊天记录 JSON 持久化，注册 ServiceLocator |
| PetChatService.cs | `Modules/Pet/` | LLM Prompt 构造 + HTTP 调用 + Fallback，注册 ServiceLocator |
| PhoneAnimController.cs | `Modules/HubUI/Panels/PhoneChat/` | 弹出/收起 Coroutine 动画 |
| ChatInputHandler.cs | `Modules/HubUI/Panels/PhoneChat/` | InputField 监听 + 发送 + 打字指示器 |
| ChatMessageListView.cs | `Modules/HubUI/Panels/PhoneChat/` | ScrollRect + 气泡列表渲染 |
| ChatPhoneController.cs | `Modules/HubUI/Panels/PhoneChat/` | 主控，状态机，串联各模块 |

---

### Task 1: 数据模型 ChatMessage + ChatRole

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Pet/ChatMessage.cs`

- [ ] **Step 1: 写入 ChatMessage.cs**

```csharp
#nullable enable
using System;

namespace GeminiLab.Modules.Pet
{
    public enum ChatRole
    {
        User = 0,
        Angel = 1,
        Devil = 2
    }

    [Serializable]
    public sealed class ChatMessage
    {
        public ChatRole Role;
        public string Text = string.Empty;
        public long Timestamp;

        public ChatMessage() { }

        public ChatMessage(ChatRole role, string text)
        {
            Role = role;
            Text = text;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [Serializable]
    internal sealed class ChatHistoryWrapper
    {
        public ChatMessage[] messages = Array.Empty<ChatMessage>();
    }
}
```

- [ ] **Step 2: 编译验证**

```bash
# 在 Unity Editor 中 Ctrl+R 编译，确认无报错
```

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Pet/ChatMessage.cs
git commit -m "feat(pet): add ChatMessage and ChatRole data models for phone chat"
```

---

### Task 2: ChatPersistenceService 聊天记录持久化

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Pet/ChatPersistenceService.cs`

- [ ] **Step 1: 写入 ChatPersistenceService.cs**

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeminiLab.Core;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    public interface IChatPersistenceService
    {
        IReadOnlyList<ChatMessage> History { get; }
        void AddMessage(ChatMessage message);
        Task SaveAsync();
        Task LoadAsync();
        void Clear();
    }

    public sealed class ChatPersistenceService : IChatPersistenceService
    {
        private const int MaxMessages = 200;
        private const string FileName = "chat_history.json";

        private readonly List<ChatMessage> _messages = new();
        public IReadOnlyList<ChatMessage> History => _messages;

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public ChatPersistenceService()
        {
            ServiceLocator.Register<IChatPersistenceService>(this);
        }

        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);
            while (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var wrapper = new ChatHistoryWrapper { messages = _messages.ToArray() };
                string json = JsonUtility.ToJson(wrapper, prettyPrint: false);
                await File.WriteAllTextAsync(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChatPersistence] Save failed: {ex.Message}");
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                string json = await File.ReadAllTextAsync(FilePath);
                var wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);
                if (wrapper?.messages != null)
                {
                    _messages.Clear();
                    _messages.AddRange(wrapper.messages);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChatPersistence] Load failed: {ex.Message}");
            }
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Pet/ChatPersistenceService.cs
git commit -m "feat(pet): add ChatPersistenceService for chat history save/load"
```

---

### Task 3: PetChatService LLM 调用服务

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Pet/PetChatService.cs`

- [ ] **Step 1: 写入 PetChatService.cs**

```csharp
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

    public sealed class PetChatResult
    {
        public string AngelReply = string.Empty;
        public string DevilReply = string.Empty;
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

            // 检查是否有宠物数据
            bool hasAngel = false;
            bool hasDevil = false;
            if (ServiceLocator.TryResolve<IPetRoster>(out var roster))
            {
                hasAngel = roster?.TryGet(PetId.Angel) != null;
                hasDevil = roster?.TryGet(PetId.Devil) != null;
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

            // 随机决定回复顺序
            bool angelFirst = UnityEngine.Random.value >= 0.5f;

            var result = new PetChatResult();

            var firstPet = angelFirst ? PetId.Angel : PetId.Devil;
            var secondPet = angelFirst ? PetId.Devil : PetId.Angel;

            string? firstReply = null;

            // 调第一只宠物
            if (TryGetPet(firstPet, out _))
            {
                (string reply, bool isFallback) = await RequestPetReplyAsync(
                    firstPet, userMessage, history, null, cancellationToken);
                firstReply = reply;
                SetResult(result, firstPet, reply, isFallback);
            }

            // 调第二只宠物，追加第一只的回复
            if (TryGetPet(secondPet, out _))
            {
                (string reply, bool isFallback) = await RequestPetReplyAsync(
                    secondPet, userMessage, history, firstReply, cancellationToken);
                SetResult(result, secondPet, reply, isFallback);
            }

            return result;
        }

        private async Task<(string reply, bool isFallback)> RequestPetReplyAsync(
            PetId petId,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            string? otherPetReply,
            CancellationToken cancellationToken)
        {
            string systemPrompt = BuildSystemPrompt(petId);
            string userPrompt = BuildUserPrompt(userMessage, otherPetReply);

            if (!_config.IsConfigured)
            {
                return (GetFallback(petId), true);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                string response = await SendLLMRequestAsync(systemPrompt, userPrompt, cts.Token);
                string cleaned = CleanResponse(response);
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    return (GetFallback(petId), true);
                }
                return (cleaned, false);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                return (GetFallback(petId), true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PetChat] LLM request failed for {petId}: {ex.Message}");
                return (GetFallback(petId), true);
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
            sb.AppendLine("- 不要使用任何 markup 或 JSON，只输出纯文本回复");

            return sb.ToString();
        }

        private static string BuildUserPrompt(string userMessage, string? otherPetReply)
        {
            if (string.IsNullOrEmpty(otherPetReply))
            {
                return $"用户说：{userMessage}";
            }
            return $"[另一个宠物刚对你说：{otherPetReply}]\n用户说：{userMessage}";
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

        private static string CleanResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            // 去掉常见的 markdown 包裹和首尾空白
            string trimmed = raw.Trim();
            // 去掉 ``` 标记
            if (trimmed.StartsWith("```")) trimmed = trimmed[3..];
            if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
            return trimmed.Trim();
        }

        private static void SetResult(PetChatResult result, PetId petId, string reply, bool isFallback)
        {
            if (petId == PetId.Angel)
            {
                result.AngelReply = reply;
                result.IsAngelFallback = isFallback;
            }
            else
            {
                result.DevilReply = reply;
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
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Pet/PetChatService.cs
git commit -m "feat(pet): add PetChatService for independent LLM pet conversation"
```

---

### Task 4: PhoneAnimController 动画控制

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/PhoneAnimController.cs`

- [ ] **Step 1: 创建目录并写入 PhoneAnimController.cs**

```bash
mkdir -p Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat
```

```csharp
#nullable enable
using System;
using System.Collections;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class PhoneAnimController : MonoBehaviour
    {
        [SerializeField] private RectTransform _phoneRect = null!;
        [SerializeField] private CanvasGroup _canvasGroup = null!;
        [SerializeField] private Vector2 _collapsedAnchoredPosition = new(80f, 80f);
        [SerializeField] private Vector2 _centerAnchoredPosition = Vector2.zero;
        [SerializeField] private float _openDuration = 0.35f;
        [SerializeField] private float _closeDuration = 0.25f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public bool IsAnimating { get; private set; }

        private void Awake()
        {
            _phoneRect.localScale = Vector3.one * 0.3f;
            _phoneRect.anchoredPosition = _collapsedAnchoredPosition;
            _canvasGroup.alpha = 0f;
        }

        public IEnumerator PlayOpenAnim()
        {
            IsAnimating = true;
            _canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < _openDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _openDuration);
                _phoneRect.localScale = Vector3.Lerp(
                    Vector3.one * 0.3f, Vector3.one, _scaleCurve.Evaluate(p));
                _phoneRect.anchoredPosition = Vector2.Lerp(
                    _collapsedAnchoredPosition, _centerAnchoredPosition, _moveCurve.Evaluate(p));
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(p / 0.7f));
                yield return null;
            }
            _phoneRect.localScale = Vector3.one;
            _phoneRect.anchoredPosition = _centerAnchoredPosition;
            _canvasGroup.alpha = 1f;
            IsAnimating = false;
        }

        public IEnumerator PlayCloseAnim()
        {
            IsAnimating = true;
            float t = 0f;
            Vector3 startScale = _phoneRect.localScale;
            Vector2 startPos = _phoneRect.anchoredPosition;
            float startAlpha = _canvasGroup.alpha;
            while (t < _closeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _closeDuration);
                _phoneRect.localScale = Vector3.Lerp(
                    startScale, Vector3.one * 0.3f, _moveCurve.Evaluate(p));
                _phoneRect.anchoredPosition = Vector2.Lerp(
                    startPos, _collapsedAnchoredPosition, _moveCurve.Evaluate(p));
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(p / 0.8f));
                yield return null;
            }
            _phoneRect.localScale = Vector3.one * 0.3f;
            _phoneRect.anchoredPosition = _collapsedAnchoredPosition;
            _canvasGroup.alpha = 0f;
            IsAnimating = false;
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/PhoneAnimController.cs
git commit -m "feat(phone-chat): add PhoneAnimController for open/close coroutine animation"
```

---

### Task 5: ChatInputHandler 输入管理

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatInputHandler.cs`

- [ ] **Step 1: 写入 ChatInputHandler.cs**

```csharp
#nullable enable
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class ChatInputHandler : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField = null!;
        [SerializeField] private GameObject _typingIndicator = null!;
        [SerializeField] private float _typingTimeoutSeconds = 15f;

        public event Action<string>? OnSubmitMessage;

        private bool _isWaitingReply;

        private void Awake()
        {
            _inputField.onSubmit.AddListener(HandleSubmit);
            _typingIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            _inputField.onSubmit.RemoveListener(HandleSubmit);
        }

        private void HandleSubmit(string text)
        {
            if (_isWaitingReply) return;

            string trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            _inputField.text = string.Empty;
            SetWaitingState(true);
            OnSubmitMessage?.Invoke(trimmed);
        }

        public void SetWaitingState(bool waiting)
        {
            _isWaitingReply = waiting;
            _inputField.interactable = !waiting;
            _typingIndicator.SetActive(waiting);

            if (waiting)
            {
                StartCoroutine(TypingTimeoutRoutine());
            }
        }

        private IEnumerator TypingTimeoutRoutine()
        {
            yield return new WaitForSeconds(_typingTimeoutSeconds);
            if (_isWaitingReply)
            {
                Debug.LogWarning("[PhoneChat] Typing timeout reached, resetting input");
                SetWaitingState(false);
            }
        }

        public void Clear()
        {
            _inputField.text = string.Empty;
        }

        public void Focus()
        {
            _inputField.Select();
            _inputField.ActivateInputField();
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatInputHandler.cs
git commit -m "feat(phone-chat): add ChatInputHandler for input submission and typing indicator"
```

---

### Task 6: ChatMessageListView 气泡列表

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatMessageListView.cs`

- [ ] **Step 1: 写入 ChatMessageListView.cs**

```csharp
#nullable enable
using System.Collections.Generic;
using GeminiLab.Modules.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public sealed class ChatMessageListView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect = null!;
        [SerializeField] private RectTransform _contentRect = null!;
        [SerializeField] private GameObject _bubbleUserPrefab = null!;
        [SerializeField] private GameObject _bubbleAngelPrefab = null!;
        [SerializeField] private GameObject _bubbleDevilPrefab = null!;
        [SerializeField] private GameObject _emptyHint = null!;
        [SerializeField] private int _maxVisibleBubbles = 50;

        private readonly List<GameObject> _activeBubbles = new();

        public void AddBubble(ChatRole role, string text)
        {
            if (_emptyHint != null) _emptyHint.SetActive(false);

            var prefab = GetPrefab(role);
            if (prefab == null)
            {
                Debug.LogError($"[PhoneChat] Missing bubble prefab for role {role}");
                return;
            }

            var bubble = Instantiate(prefab, _contentRect);
            var tmpText = bubble.GetComponentInChildren<TMP_Text>();
            if (tmpText != null) tmpText.text = text;

            _activeBubbles.Add(bubble);
            RecycleOldBubbles();
            ScrollToBottom();
        }

        public void AddMessagesFromHistory(IReadOnlyList<ChatMessage> messages)
        {
            Clear();
            foreach (var msg in messages)
            {
                AddBubble(msg.Role, msg.Text);
            }
        }

        public void Clear()
        {
            foreach (var bubble in _activeBubbles)
            {
                if (bubble != null) Destroy(bubble);
            }
            _activeBubbles.Clear();
            if (_emptyHint != null) _emptyHint.SetActive(true);
        }

        private void RecycleOldBubbles()
        {
            while (_activeBubbles.Count > _maxVisibleBubbles)
            {
                var oldest = _activeBubbles[0];
                _activeBubbles.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            _scrollRect.normalizedPosition = Vector2.zero;
        }

        private GameObject? GetPrefab(ChatRole role)
        {
            return role switch
            {
                ChatRole.User => _bubbleUserPrefab,
                ChatRole.Angel => _bubbleAngelPrefab,
                ChatRole.Devil => _bubbleDevilPrefab,
                _ => null
            };
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatMessageListView.cs
git commit -m "feat(phone-chat): add ChatMessageListView for scrollable chat bubble rendering"
```

---

### Task 7: ChatPhoneController 主控

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatPhoneController.cs`

- [ ] **Step 1: 写入 ChatPhoneController.cs**

```csharp
#nullable enable
using System.Collections;
using System.Threading;
using GeminiLab.Modules.Pet;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels.PhoneChat
{
    public enum PhoneState
    {
        Collapsed,
        AnimatingIn,
        Open,
        AnimatingOut
    }

    public sealed class ChatPhoneController : MonoBehaviour
    {
        [SerializeField] private PhoneAnimController _animController = null!;
        [SerializeField] private ChatInputHandler _inputHandler = null!;
        [SerializeField] private ChatMessageListView _messageListView = null!;
        [SerializeField] private GameObject _collapsedButtonRoot = null!;
        [SerializeField] private GameObject _closeButton = null!;
        [SerializeField] private KeyCode _closeKey = KeyCode.Escape;

        public PhoneState CurrentState { get; private set; } = PhoneState.Collapsed;

        private CancellationTokenSource? _currentCts;

        private void Awake()
        {
            _collapsedButtonRoot.SetActive(true);
            _closeButton.SetActive(false);
            _inputHandler.OnSubmitMessage += HandleUserMessage;
        }

        private async void Start()
        {
            // 加载历史记录
            if (Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence))
            {
                await persistence!.LoadAsync();
                _messageListView.AddMessagesFromHistory(persistence.History);
            }
        }

        private void Update()
        {
            if (CurrentState == PhoneState.Open && Input.GetKeyDown(_closeKey))
            {
                ClosePhone();
            }
        }

        private void OnDestroy()
        {
            _inputHandler.OnSubmitMessage -= HandleUserMessage;
            _currentCts?.Cancel();
            _currentCts?.Dispose();

            // 保存聊天记录
            if (Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence))
            {
                _ = persistence!.SaveAsync();
            }
        }

        public void OnCollapsedButtonClicked()
        {
            if (CurrentState != PhoneState.Collapsed) return;
            StartCoroutine(OpenRoutine());
        }

        public void ClosePhone()
        {
            if (CurrentState != PhoneState.Open) return;
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            CurrentState = PhoneState.AnimatingIn;
            _collapsedButtonRoot.SetActive(false);
            _closeButton.SetActive(true);
            yield return _animController.PlayOpenAnim();
            CurrentState = PhoneState.Open;
            _inputHandler.Focus();
        }

        private IEnumerator CloseRoutine()
        {
            CurrentState = PhoneState.AnimatingOut;
            _closeButton.SetActive(false);
            yield return _animController.PlayCloseAnim();
            _collapsedButtonRoot.SetActive(true);
            CurrentState = PhoneState.Collapsed;
        }

        private async void HandleUserMessage(string text)
        {
            if (!Core.ServiceLocator.TryResolve<IPetChatService>(out var chatService)) return;
            if (!Core.ServiceLocator.TryResolve<IChatPersistenceService>(out var persistence)) return;

            // 添加用户消息到列表
            var userMsg = new ChatMessage(ChatRole.User, text);
            persistence.AddMessage(userMsg);
            _messageListView.AddBubble(ChatRole.User, text);

            // 调用 LLM
            _currentCts?.Cancel();
            _currentCts = new CancellationTokenSource();

            var result = await chatService!.SendMessageAsync(text, persistence.History, _currentCts.Token);

            _inputHandler.SetWaitingState(false);

            if (_currentCts.IsCancellationRequested) return;

            if (result.IsCancelled)
            {
                Debug.Log("[PhoneChat] Request was cancelled");
                return;
            }

            // 按回复顺序添加气泡（尊重随机顺序）
            if (!string.IsNullOrEmpty(result.AngelReply))
            {
                var angelMsg = new ChatMessage(ChatRole.Angel, result.AngelReply);
                persistence.AddMessage(angelMsg);
                _messageListView.AddBubble(ChatRole.Angel, result.AngelReply);
            }

            if (!string.IsNullOrEmpty(result.DevilReply))
            {
                var devilMsg = new ChatMessage(ChatRole.Devil, result.DevilReply);
                persistence.AddMessage(devilMsg);
                _messageListView.AddBubble(ChatRole.Devil, result.DevilReply);
            }

            _ = persistence.SaveAsync();
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/PhoneChat/ChatPhoneController.cs
git commit -m "feat(phone-chat): add ChatPhoneController main orchestrator with state machine"
```

---

### Task 8: PhoneChatRuntimeBootstrap 服务注册

**Files:**
- Create: `Assets/_Project/Scripts/Modules/Pet/PhoneChatRuntimeBootstrap.cs`

- [ ] **Step 1: 写入 PhoneChatRuntimeBootstrap.cs**

```csharp
#nullable enable
using GeminiLab.Core;
using GeminiLab.Modules.Tarot;
using UnityEngine;

namespace GeminiLab.Modules.Pet
{
    public static class PhoneChatRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 尝试从 Resources 加载 LLMConfigSO
            var config = Resources.Load<LLMConfigSO>("LLMConfig");
            if (config == null)
            {
                Debug.Log("[PhoneChat] LLMConfigSO not found in Resources, chat will use fallback only");
                config = ScriptableObject.CreateInstance<LLMConfigSO>();
            }

            // 注册 PetChatService（如果还没注册）
            if (!ServiceLocator.TryResolve<IPetChatService>(out _))
            {
                _ = new PetChatService(config);
            }

            // 注册 ChatPersistenceService（如果还没注册）
            if (!ServiceLocator.TryResolve<IChatPersistenceService>(out _))
            {
                _ = new ChatPersistenceService();
            }
        }
    }
}
```

- [ ] **Step 2: 编译验证**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Modules/Pet/PhoneChatRuntimeBootstrap.cs
git commit -m "feat(pet): add PhoneChatRuntimeBootstrap for phone chat service registration"
```

---

### Task 9: Prefab 制作与场景集成

**Files:**
- Create: Prefab `Assets/_Project/Prefabs/UI/PhoneChatRoot.prefab`
- Modify: `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`

- [ ] **Step 1: 在 Unity Editor 中创建 PhoneChatRoot Prefab**

在 Unity Editor 中按以下层级手动创建 Canvas 子对象（或在场景中创建后拖成 Prefab）：

```
PhoneChatRoot (RectTransform, CanvasGroup, PhoneAnimController, ChatPhoneController)
├── CollapsedButtonRoot (Image + Button, 左下角 anchor)
│   └── 美术素材 Image（暂用白色方块占位，60x90）
├── PhoneFrame (Image, 手机边框)
│   ├── ScreenArea (Image + Mask, 屏幕区域)
│   │   ├── ScrollView (ScrollRect)
│   │   │   ├── Viewport (Image + Mask)
│   │   │   │   └── Content (VerticalLayoutGroup + ContentSizeFitter)
│   │   │   │       └── EmptyHint (TMP_Text, "和你的宠物聊聊天吧~")
│   │   │   └── Scrollbar (可选)
│   │   └── TypingIndicator (TMP_Text "..." + 三个点动画子节点)
│   └── CloseButton (Button + Image, 右上角 X)
└── InputArea (Image)
    └── TMP_InputField (Placeholder: "输入消息...", characterLimit: 500)
```

**关键配置:**
- `PhoneChatRoot` CanvasGroup: Alpha=0, 挂 `PhoneAnimController` + `ChatPhoneController`
- `CollapsedButtonRoot`: anchor 左下角 (0,0), Pos(80,80)
- `PhoneFrame`: 初始 scale 0.3, anchor 居中
- `ScrollRect.viewport` -> `Viewport`, `ScrollRect.content` -> `Content`
- `Content`: VerticalLayoutGroup (ChildAlignment: UpperLeft, ChildForceExpandWidth: true), ContentSizeFitter (Vertical: PreferredSize)
- `CloseButton.onClick` -> `ChatPhoneController.ClosePhone`
- `CollapsedButtonRoot/Button.onClick` -> `ChatPhoneController.OnCollapsedButtonClicked`

**气泡 Prefab (Bubble_User, Bubble_Angel, Bubble_Devil):** 三个独立 Prefab，结构均如下但颜色不同：

```
Bubble_User:
├── Background (Image, color: #4A6FA5 蓝)
└── MessageText (TMP_Text, 右对齐, maxWidth: 260, fontSize: 14)

Bubble_Angel:
├── Background (Image, color: #2A2A3E 暗暖)
├── AvatarIcon (Image, 圆形, color: #FFCC80)
└── MessageText (TMP_Text, 左对齐)

Bubble_Devil:
├── Background (Image, color: #3E2A2A 暗红)
├── AvatarIcon (Image, 圆形, color: #EF9A9A)
└── MessageText (TMP_Text, 左对齐)
```

**ChatMessageListView 引用:**
- `_bubbleUserPrefab` -> Bubble_User prefab
- `_bubbleAngelPrefab` -> Bubble_Angel prefab
- `_bubbleDevilPrefab` -> Bubble_Devil prefab

**PhoneAnimController 引用:**
- `_phoneRect` -> PhoneFrame RectTransform
- `_canvasGroup` -> PhoneChatRoot CanvasGroup
- `_collapsedAnchoredPosition` -> (80, 80)
- `_centerAnchoredPosition` -> (0, 0)
- Scale Curve 配置为 EaseOut 风格（0→1 逐渐平缓）

**ChatInputHandler 引用:**
- `_inputField` -> TMP_InputField
- `_typingIndicator` -> TypingIndicator GameObject

**场景集成:**
- 将 PhoneChatRoot Prefab 拖入 `Apartment_Main.unity`
- PhoneChatRoot 的 Canvas 设置 sortingOrder = 102
- 确保其 Canvas 的 RenderMode 为 ScreenSpaceOverlay

- [ ] **Step 2: 在 Unity Editor 中 Play Mode 测试**

测试流程:
1. 进入公寓场景
2. 点击左下角按钮 → 手机弹出到屏幕中央
3. 输入消息回车 → 等待 → Angel/Devil 回复以气泡形式出现
4. 按 ESC → 手机收起回左下角
5. 重新打开手机 → 之前的聊天记录还在
6. 退出 Play Mode → 检查 PersistentDataPath 下是否生成 `chat_history.json`

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Prefabs/UI/PhoneChatRoot.prefab
git add Assets/_Project/Prefabs/UI/Bubble_User.prefab
git add Assets/_Project/Prefabs/UI/Bubble_Angel.prefab
git add Assets/_Project/Prefabs/UI/Bubble_Devil.prefab
git add Assets/_Project/Scenes/Apartment/Apartment_Main.unity
git commit -m "feat(phone-chat): add PhoneChatRoot prefab and integrate into apartment scene"
```
