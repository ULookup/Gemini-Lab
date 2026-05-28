# Phone Chat System Design

**Date:** 2026-05-26
**Status:** Approved
**Context:** 在公寓场景左下角新增手机聊天入口，用户可与双宠（Angel/Devil）对话，LLM 基于宠物性格+状态生成回复。

---

## 1. 核心决策

| 决策点 | 选择 | 理由 |
|--------|------|------|
| LLM 路径 | 独立调用，复用 `LLMConfigSO` | 不走 Gateway，简化调试 |
| 面板关系 | 独立于 `IUIPanel`/`UIRouter` | 交互模式与侧边栏面板完全不同 |
| 双宠回复 | 同时回复，串行调用，随机先后顺序 | Devil 可接 Angel 话茬，更有对话感 |
| 动画 | 弹出 + 缩放 + 位移，Coroutine + AnimationCurve | 项目已有此模式，无额外依赖 |
| 聊天记录 | 持久化到存档 | 用户需求 |
| 布局 | 用户消息右对齐，宠物消息左对齐（同侧上下排列） | 简洁清晰 |
| 滚动 | 标准 ScrollRect | 实现简单 |
| 回复展示 | 等完整结果一次性显示 | 避免半截话体验差 |

## 2. 模块架构

```
ChatPhoneController          ← 主控 + 动画状态机
├── PhoneAnimController      ← 弹出/收起动画（Coroutine）
├── ChatMessageListView      ← ScrollRect + 消息气泡渲染
├── PetChatService           ← LLM 调用 + Prompt 构造 + Fallback
├── ChatPersistenceService   ← 聊天记录存档/读档
└── ChatInputHandler         ← InputField + 回车发送 + 打字指示器
```

每个模块职责单一，通过直接方法调用协作（不通过 EventBus，因为模块都在同一 GameObject 上下文内）。

## 3. 模块详述

### 3.1 ChatPhoneController

MonoBehaviour，挂在 PhoneChatRoot 上。管理生命周期和动画状态转换。

**状态机:** `Collapsed → AnimatingIn → Open → AnimatingOut → Collapsed`

**职责:**
- 监听点击收起态按钮 → `PlayOpenAnimation()`
- 监听 ESC / 关闭按钮 → `PlayCloseAnimation()`
- 接收 `ChatInputHandler` 的用户消息 → 调用 `PetChatService` → 将结果交给 `ChatMessageListView`
- `Awake` 时初始化子模块，`OnDestroy` 时触发持久化保存

### 3.2 PhoneAnimController

纯动画模块，只控制 RectTransform。

**接口:**
- `IEnumerator PlayOpenAnim()` — scale 0.3→1.0 (EaseOutBack)，position 左下角→中央 (EaseOutCubic)，alpha 0→1 (0.25s)
- `IEnumerator PlayCloseAnim()` — scale 1.0→0.3 (EaseInCubic)，position 中央→左下角 (EaseInCubic)，alpha 1→0 (0.2s)

动画锁 `_isAnimating` 阻止重复触发。

### 3.3 ChatMessageListView

管理 ScrollRect 中的消息气泡列表。

**职责:**
- 添加消息气泡（用户右对齐，宠物左对齐带小头像）
- 维护 Content RectTransform 高度，新消息自动滚到底部
- 支持对象池回收旧气泡（消息超过 50 条时）

**气泡样式:** 用户蓝底，Angel 暖色底（#ffcc80 头像），Devil 暗红底（#ef9a9a 头像）

### 3.4 PetChatService

无 MonoBehaviour 依赖，注册到 ServiceLocator。

**接口:** `Task<PetChatResult> SendMessageAsync(string userMessage, List<ChatMessage> history)`

`PetChatResult` 包含 `string AngelReply` 和 `string DevilReply`，以及 `bool IsFallback`。

**LLM 调用流程:**
1. 随机决定 Angel/Devil 谁先回复
2. 构造宠物 A 的 System + User Prompt → HTTP POST 到 `LLMConfigSO` endpoint
3. 拿到回复 A
4. 构造宠物 B 的 System + User Prompt（追加宠物 A 的回复作为 context）→ HTTP POST
5. 返回两个回复

**错误处理:** 超时 10s / HTTP 错误 / 网络不可达 → Fallback。单个宠物失败不影响另一个。

**Fallback:** 预设 5-8 句中文回复，基于宠物性格随机挑选。

### 3.5 ChatPersistenceService

**接口:**
- `void AddMessages(params ChatMessage[])` — 追加到内存列表
- `List<ChatMessage> LoadHistory()` — 返回完整历史
- `Task SaveAsync()` — JSON 序列化写入 PersistentDataPath
- `Task LoadAsync()` — 从文件反序列化

**数据模型:**
```csharp
[Serializable]
public class ChatMessage
{
    public ChatRole Role;       // User, Angel, Devil
    public string Text;
    public long Timestamp;
}

public enum ChatRole { User, Angel, Devil }
```

**限制:** 最多保留 200 条，超出裁剪最早的消息。文件路径: `{PersistentDataPath}/chat_history.json`

### 3.6 ChatInputHandler

**职责:**
- 监听 TMP_InputField.onSubmit → 非空校验 → 通知 ChatPhoneController
- 发送后灰掉输入框，显示打字指示器（三个点循环缩放动画）
- 打字超时 15s → 强制显示 Fallback 回复
- 收到回复后恢复输入框，隐藏指示器

## 4. LLM Prompt 构造

### System Prompt 模板

```
你是 {{PetName}}，住在玩家的公寓里。

## 你的性格
- 善良: {{Kindness}}  /  邪恶: {{Evilness}}  /  冷静: {{Calmness}}
- 勇敢: {{Bravery}}  /  害羞: {{Shyness}}  /  正直: {{Integrity}}
- 好奇心: {{Curiosity}}

## 你当前的状态
- 心情: {{Mood}}/100
- 精力: {{Energy}}/100
- 饱腹: {{Satiety}}/100
- 当前动作: {{CurrentState}}
- 正在旅行: {{IsTraveling}}

## 回复规则
- 回复要简短自然，2-4句话，不要超过 80 个字
- 回复要符合你的性格，保持角色一致性
- 回复可以提及你当前的状态
- 用口语化中文回复
- 可以适当接上一句话茬（如果另一个宠物刚说过话）
```

### User Message 格式

第二个宠物调用时，追加 context：
```
[{{FirstName}} 刚对你说：{{firstReply}}]
用户说：{{userMessage}}
```

### 参数来源

| 参数 | 来源 |
|------|------|
| 性格 7 轴 | `IPersonalityEvolutionService.GetMatrix(PetId)` |
| 状态数据 | `IPetRoster.TryGet(PetId)` → `PetRuntimeData` |
| LLM 配置 | `LLMConfigSO` (endpoint, apiKey, model, timeout) |

## 5. Prefab 层级

```
PhoneChatRoot (ChatPhoneController, CanvasGroup)
├── PhoneFrame              ← 美术手机边框素材 (Image)
│   ├── ScreenArea          ← 屏幕区域 (Image + Mask)
│   │   ├── ScrollView      ← ScrollRect
│   │   │   └── Viewport (Mask)
│   │   │       └── Content ← ChatMessageListView 管理
│   │   │           ├── EmptyHint ("和你的宠物聊聊天吧~")
│   │   │           ├── Bubble_User  (模板)
│   │   │           └── Bubble_Pet   (模板, 通过颜色/头像区分)
│   │   └── TypingIndicator ← "..." 跳动动画
│   └── CloseButton         ← 关闭按钮
├── InputArea
│   └── TMP_InputField      ← ChatInputHandler
└── CollapsedButtonRoot     ← 收起态按钮（美术素材）
```

**气泡模板:**
- `Bubble_User`: 蓝色背景 Image + TMP 文字，右对齐
- `Bubble_Pet`: 底色可 tint Image + 圆形头像 Image + TMP 文字，左对齐

**场景集成:**
- 放在 `Apartment_Main.unity` Canvas 层级下，sortingOrder 102（高于 Sidebar 的 100）
- `CollapsedButtonRoot` anchor 在左下角
- PhoneFrame 初始 scale 设为 0.3

## 6. 错误处理

| 场景 | 处理 |
|------|------|
| LLM 超时 (>10s) | Toast "宠物们正在发呆"，输入框恢复 |
| LLM HTTP 错误 | 本地 Fallback 回复 |
| 网络不可达 | 本地 Fallback 回复 |
| 第一个宠物成功，第二个失败 | 成功的气泡正常显示，失败的显示 Fallback |
| 存档读写失败 | 静默失败，不影响聊天 |
| 动画中重复点击 | 动画锁忽略 |
| 空消息 | InputField 校验阻止 |
| 消息过长 | characterLimit 500 |
| 聊天记录超量 | 裁剪到最近 200 条 |
| 打字指示器超时 15s | 强制 Fallback + Toast |
| 场景切换 | OnDestroy 触发自动保存 |
| 无历史记录 | 显示空状态引导语 "和你的宠物聊聊天吧~" |
| 只有一只宠物有数据 | 只让有数据的那只回复 |

## 7. 实现范围

### 新建文件

| 文件 | 路径 |
|------|------|
| PhoneChatController.cs | `Modules/HubUI/Panels/PhoneChat/` |
| PhoneAnimController.cs | 同上 |
| ChatMessageListView.cs | 同上 |
| PetChatService.cs | `Modules/Pet/` |
| ChatPersistenceService.cs | `Modules/Pet/` |
| ChatInputHandler.cs | `Modules/HubUI/Panels/PhoneChat/` |
| ChatMessage.cs + ChatRole.cs | `Modules/Pet/` |

### 新建 Prefab

- `PhoneChatRoot.prefab` (Asset 路径待定)

### 修改文件

- `Apartment_Main.unity` — 添加 PhoneChatRoot 实例
