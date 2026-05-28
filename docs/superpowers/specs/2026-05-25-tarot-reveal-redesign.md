# Tarot Reveal 阶段重设计 — 设计文档

**Date:** 2026-05-25
**Status:** Draft

## 概述

重设计 Reveal 阶段，将解读流程从"6 个 LLM 请求并行 + 点击继续逐槽揭示"改为 4 轮沉浸式体验：过去 → 当下 → 未来 → 总结。每轮在 LLM 等待期间显示占位图，结果返回后占位图渐隐、解读气泡渐显。总结轮返回结构化 JSON 数据（运势等级、幸运提示、今日建议）。

## 数据模型

### TarotSession — 新增字段

```csharp
public TarotSummaryResult? SummaryResult;  // 总结轮 LLM 返回的结构化数据
```

### 新增：TarotSummaryResult

```csharp
[Serializable]
public sealed class TarotSummaryResult
{
    public int FortuneLevel;       // 1-5 星
    public LuckyHintData LuckyHint; // 嵌套幸运提示
    public string Advice;           // 今日建议

    public static TarotSummaryResult FromJson(string json);
    public static TarotSummaryResult Default(); // fallback: 3星 + 通用建议
}

[Serializable]
public sealed class LuckyHintData
{
    public string Color;   // 幸运颜色
    public string Number;  // 幸运数字
    public string Time;    // 幸运时间
    public string Action;  // 幸运行动
}
```

### LLM 返回 JSON（总结轮）

```json
{
  "fortuneLevel": 4,
  "luckyHint": {
    "color": "紫色",
    "number": "7",
    "time": "黄昏",
    "action": "主动沟通"
  },
  "advice": "今日宜放下执念，顺其自然..."
}
```

### 不变

- `TarotCardSO` / `TarotDeckSO` — 不动
- `TarotDrawResult` / `TarotReading` — 不动
- `TarotSession` 其余字段 — 不动
- `TarotSlotPosition` 枚举 — 不动

## 服务层

### ITarotReadingBackend — 新增方法

```csharp
// 现有（不变）
Task<TarotReading> RequestAsync(TarotDrawResult draw, PetId petId,
    TarotOrientation orientation, CancellationToken ct);

// 新增：总结轮，过去/当下/未来全揭示后调用
Task<TarotSummaryResult> RequestSummaryAsync(
    TarotDrawResult past, TarotDrawResult present, TarotDrawResult future,
    string? question, CancellationToken ct);
```

### DirectLLMBackend — RequestSummaryAsync 实现

- 构建 system prompt（从 `LLMConfigSO.SummarySystemTemplate`），要求返回严格 JSON
- 注入三张牌信息（`{pastCard}`, `{presentCard}`, `{futureCard}`）和用户问题（`{question}`）
- 解析 JSON → `TarotSummaryResult`
- 超时/解析失败 → `TarotSummaryResult.Default()`

### LLMConfigSO — 新增字段

| 字段 | 类型 | 说明 |
|------|------|------|
| SummarySystemTemplate | string | 总结轮 system prompt，含 `{pastCard}`, `{presentCard}`, `{futureCard}`, `{question}` 占位符 |

## UI 架构

### 新增：TarotRevealController

挂到 Reveal 根节点上。TarotPanelStub 进入 Reveal 时把 session 传给它，Controller 接管全部 Reveal 流程。

**公开接口：**

```csharp
public sealed class TarotRevealController : MonoBehaviour
{
    public void BeginReveal(TarotSession session, ITarotService tarot);
    public event Action OnRevealComplete;   // 再抽一次 → 回到 Select
    public event Action OnOpenGuide;        // 查看图鉴 → 切到图鉴子视图
}
```

**4 个子阶段状态机：** Past → Present → Future → Summary

### 每槽位子阶段时序（Past/Present/Future）

```
1. 显示卡面（已知） + 底图 + 占位图（无气泡、无继续按钮）
2. 等待该槽位 Angel + Devil 结果就绪（并行请求已缓存）
3. 结果就绪 → 占位图渐隐(0.5s) + 气泡渐显(0.5s)
4. 继续按钮渐显(0.3s)
5. 用户点击继续 → 进入下一子阶段
```

- 6 个 Angel/Devil 请求在进入 Reveal 时全并行发出，结果缓存于 TarotSession.Readings
- 每槽位内部 Angel + Devil 同时揭示
- LLM 返回前不显示继续按钮

### Summary 子阶段时序

```
1. 3 张已揭示的牌保留显示
2. 底图 + 总结占位图（"正在生成运势总结…"）
3. 发起第7次 LLM 请求（总结）
4. 返回 → 占位图渐隐(0.5s) + 运势内容渐显(0.5s) + 按钮渐显(0.3s)
    - [再抽一次] → 触发 OnRevealComplete
    - [查看图鉴] → 触发 OnOpenGuide
```

### Inspector 绑定（TarotRevealController）

| 字段 | 类型 | 数量 | 说明 |
|------|------|------|------|
| 底图 | Image | × 3 | 过去/当下/未来的解读区域背景（美术资源） |
| 占位图 | Image | × 4 | 过去/当下/未来/总结 占卜中占位（美术资源） |
| 卡面 | Image | × 3 | 已选牌面显示 |
| Angel/Devil 气泡 | ReadingBubble | × 6 | 已有组件，从 TarotPanelStub 迁移 |
| 运势星级 | TMP_Text | × 1 | 总结轮 1-5 星显示 |
| 幸运字段 | TMP_Text | × 4 | 颜色/数字/时间/行动 |
| 今日建议 | TMP_Text | × 1 | 建议文案 |
| 继续按钮 | Button | × 1 | 显示/隐藏控制 |
| 再抽一次按钮 | Button | × 1 | 总结轮显示 |
| 查看图鉴按钮 | Button | × 1 | 总结轮显示 |

### TarotPanelStub 变更

**移除（迁移到 TarotRevealController）：**

| 移除项 |
|--------|
| `_revealRoot` 及其下所有序列化字段 |
| `SetupRevealStage()` |
| `FireAllReadingsAsync()` / `GetReadingForSlot()` |
| `OnContinueReveal()` / `RevealSlotReadings()` |
| `ShowSlotLoading()` / `GetSlotBubbles()` |
| `ShowCardInRevealSlot()` / `HideAllBubbles()` |
| `OnFinish()` |

**保留/新增：**

```csharp
[Header("Reveal")]
[SerializeField] private TarotRevealController? _revealController;
```

EnterStage(Stage.Reveal) 简化为：

```csharp
_revealController?.BeginReveal(_session!, _tarot!);
```

Awake 中绑定事件：

```csharp
_revealController.OnRevealComplete += () => {
    SaveToCollection(_session!);
    // 开始新一轮：创建新 session → 进入 Select
    _session = _tarot!.CreateSession(_questionInput?.text);
    EnterStage(Stage.Select);
};
_revealController.OnOpenGuide += () => SwitchTab(SubView.Guide);
```

## 数据流

```
进入 Reveal
  │
  ├─ TarotRevealController.BeginReveal(session, tarot)
  │
  ├─ 6 个 RequestAsync() 并行发出 → 缓存到 session.Readings
  │
  ├─ 子阶段 Past：
  │   ├─ 显示卡面 + 底图 + 占位图（无气泡、无按钮）
  │   ├─ 等待 past_angel + past_devil 结果
  │   ├─ 占位图渐隐 + 气泡渐显 → 继续按钮渐显
  │   └─ 用户点击继续
  │
  ├─ 子阶段 Present（同上模式）
  │
  ├─ 子阶段 Future（同上模式）
  │
  └─ 子阶段 Summary：
      ├─ 底图 + 总结占位图（"正在生成运势总结…"）
      ├─ RequestSummaryAsync() → TarotSummaryResult
      ├─ 占位图渐隐 + 运势内容渐显 + 按钮渐显
      └─ 用户点击 [再抽一次] 或 [查看图鉴]
```

## 动画参数

| 动画 | 时长 | 方式 |
|------|------|------|
| 占位图 → 气泡（每槽位） | 0.5s | CanvasGroup alpha crossfade |
| 继续按钮出现 | 0.3s | alpha fade-in |
| 总结内容 + 按钮出现 | 0.5s / 0.3s | alpha fade-in |

## 边界情况

| 场景 | 处理 |
|------|------|
| 某槽位 LLM 超时 | LocalFallback 兜底，气泡标注"本地解读" |
| 总结 LLM 超时/JSON 解析失败 | `TarotSummaryResult.Default()`（3星 + 通用文案） |
| 用户快速连点继续 | 按钮点击后立即隐藏，防止重复触发 |
| ApiKey 未配置 | 全部走 LocalFallback，总结用默认值 |
| 占卜中途退出面板 | CancelToken 取消所有进行中的请求 |
| 查看图鉴 | 当前 session 保存后切换到图鉴子视图，Guide Back 回到 Idle |
