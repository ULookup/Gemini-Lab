# Tarot Multi-Card Reading — 设计文档

**Date:** 2026-05-24
**Status:** Draft

## 概述

将塔罗抽牌从"每日随机抽 1 张"改为"从 11 张备选中手动选 3 张（过去/当下/未来）+ LLM 逐牌双人格解读"的沉浸式体验。

## 数据模型

### 新增：TarotSession（单次抽牌会话）

```csharp
public sealed class TarotSession
{
    public string Question;                          // 玩家输入的问题（可为空）
    public string SessionDateIso;                    // 会话日期 yyyy-MM-dd
    public List<TarotCardSO> CandidateCards;          // 11 张备选牌
    public TarotDrawResult? PastCard;                // 过去槽位
    public TarotDrawResult? PresentCard;             // 当下槽位
    public TarotDrawResult? FutureCard;              // 未来槽位
    public int PickedCount;                          // 已选数量 (0-3)
    public Dictionary<string, TarotReading> Readings; // key="位置_人格" e.g. "past_angel"
    public int RevealedSlotIndex;                    // 已揭晓到第几个槽位 (0-2)
}
```

### 新增：LLMConfigSO

ScriptableObject，存 LLM 连接配置 + prompt 模板：

| 字段 | 类型 | 说明 |
|------|------|------|
| Endpoint | string | OpenAI 兼容 API 地址 |
| ApiKey | string | API Key |
| Model | string | 模型名（如 gpt-4o / claude-sonnet-4-6） |
| AngelSystemTemplate | string | 天使人格 system prompt，含 `{personality}` 占位符 |
| DevilSystemTemplate | string | 恶魔人格 system prompt，含 `{personality}` 占位符 |
| UserMessageTemplate | string | 用户消息模板，含 `{cardName}`, `{slotName}`, `{question}`, `{keywords}` 占位符 |
| TimeoutSeconds | float | 请求超时秒数 |

### 新增：TarotLayoutSO

ScriptableObject，存 UI 布局参数：

| 字段 | 类型 | 说明 |
|------|------|------|
| ArcRadius | float | 弧形排列半径 |
| ArcSpanAngle | float | 弧形展开角度（如 160°） |
| CardSpreadCount | int | 展示牌数 (11) |
| HoverScale | float | hover 放大倍率 |
| HoverLift | float | hover 上浮距离(px) |
| CardAppearDuration | float | 牌浮现动画时长 |
| CardFlyDuration | float | 牌飞入槽位动画时长 |
| RevealIntervalSeconds | float | 每幕之间最小间隔 |
| LoadingTexts | string[] | 等待解读时的情境文案池 |

### 不变

- `TarotCardSO` / `TarotDeckSO` — 牌面数据不动
- `TarotDrawResult` — 结构不变，Orientation 字段保留但解读不再强绑定（Angel 恒正位、Devil 恒逆位）
- `TarotReading` / `TarotReadingReceivedEvent` — 保持不变
- `EventBus` 事件体系 — 保留

### 移除

- `CanDrawToday()` / `LastDrawDateIso` / `DrawDaily()` — 不再限制每日
- TarotService 中的 PlayerPrefs 日期持久化逻辑

## 服务层

### ITarotService 变更

```csharp
public interface ITarotService
{
    TarotDeckSO Deck { get; }

    // 新增
    TarotSession CreateSession(string? question);
    TarotSession ShuffleCards(TarotSession session);
    TarotSession PickCard(TarotSession session, TarotCardSO card);
    TarotSession ConfirmSelection(TarotSession session);

    // 保留（调用方式不变，内部通过 DirectLLMBackend 执行）
    Task<TarotReading> RequestReadingAsync(TarotDrawResult draw, PetId petId,
        TarotOrientation orientation, CancellationToken cancellationToken = default);
}
```

- `CreateSession`: 从 Deck 中随机取 11 张牌组成备选池，返回新 session
- `ShuffleCards`: 重新随机选 11 张，清空已选，返回更新后的 session
- `PickCard`: 按顺序填入 slot[index]，index = PickedCount
- `ConfirmSelection`: 标记选牌完成，进入解读阶段

### TarotService 变更

- 移除 PlayerPrefs 日期逻辑、`CanDrawToday`、`DrawDaily`
- 移除 `IPersistentService` 实现（日期持久化不再需要）
- 新增上述 session 管理方法
- `RequestReadingAsync` 改为走 `DirectLLMBackend`

### DirectLLMBackend : ITarotReadingBackend

```
RequestAsync(draw, petId, orientation, ct)
  │
  ├─ 1. 从 ServiceLocator 获取 IPetRoster
  │     获取对应 petId 的 PetRuntimeData / PersonalityVector
  │
  ├─ 2. 读取 LLMConfigSO
  │     构建 System prompt: AngelSystemTemplate 或 DevilSystemTemplate
  │     注入 personality 数值文本（7 维性格的当前值）
  │     构建 User prompt: 牌名 + 位置名 + 问题 + 关键词
  │
  ├─ 3. UnityWebRequest.Post → OpenAI 兼容 endpoint
  │     解析 choices[0].message.content
  │
  └─ 4. 成功 → TarotReading(..., isFromGateway: true)
        失败/超时 → 回退到 LocalFallback.Build()
```

**性格注入方式：** `LLMConfigSO` 的 system prompt 模板中使用 `{personality}` 占位符，运行时由 `DirectLLMBackend` 替换为当前宠物的 PersonalityVector 数值描述文本（如 "Kindness: 0.7, Evilness: -0.5, Calmness: 0.3..."）。

## UI 架构

### 3 阶段状态机

**Stage.Idle（初始界面）**
- 3 个按钮：DrawButton / GuideButton / HistoryButton
- TMP_InputField（可选问题输入）
- 点击 DrawButton → Stage.Select
- 点击 GuideButton → 切换到图鉴子视图（收集册式）
- 点击 HistoryButton → 切换到历史记录子视图

**Stage.Select（选牌）**
- 3 个按钮隐藏
- 11 张牌弧形排列（由 TarotArcLayout 组件驱动，参数来自 TarotLayoutSO）
  - hover: 放大 + 上浮
  - 点击: 牌飞入当前槽位动画
- 底部 3 个槽位（TarotSlot prefab × 3），标签分别为"过去"/"当下"/"未来"
- 按顺序填入：第一张 → 过去，第二张 → 当下，第三张 → 未来
- [洗牌] 按钮 → 重新随机 11 张，清空已选
- 选满 3 张后，显示确认状态 → 进入 Stage.Reveal

**Stage.Reveal（解读揭晓）**
- 顶部：3 张已选牌 + 对应槽位标签
- 3 个槽位下方各有 2 个 ReadingBubble 槽位（Angel + Devil）
- 6 个 LLM 请求并行发出
- 响应存入对应槽位缓冲区
- 揭晓顺序：过去 → 当下 → 未来（手动点击 [继续] 推进）
- 每幕揭晓时：对应槽位的 Angel + Devil 气泡弹出
- 等待中：显示情境加载文案 + 动画
- 全部揭晓后：[完成] → 保存历史记录 → 回到 Stage.Idle

### 新增 Prefab

| Prefab | 组件 | 描述 |
|--------|------|------|
| TarotCardSelectable | Image, Button, Animator | 可点击的牌。卡背/卡面 sprite 可换，hover/选中/飞行动画可换 |
| TarotSlot | Image, TMP_Text | 牌位槽。空槽 sprite、标签文本、占位光效可调 |
| ReadingBubble | Image, TMP_Text, Animator | 解读气泡。背景色（天使/恶魔不同）、弹出动画 clip |
| TarotArcLayout | MonoBehaviour | 挂到容器父节点。读 TarotLayoutSO 参数，自动计算子对象位置形成弧形排列 |

### 图鉴视图改动

- 从"遍历 Deck 全 22 张"改为"遍历 CollectionService 的 Tarot 条目"
- 抽过的：显示卡面 + 名称
- 未抽过的：显示剪影/问号 + "未收集"
- `CollectionService` 在每次抽牌会话完成时写入 3 条 `CollectionEntry`

### 历史记录视图改动

- 不再展示单张牌记录，改为展示 `TarotSession` 记录
- 每条记录显示：日期 + 3 张牌缩略图 + 问题摘要
- 点击展开查看完整解读

## 数据流

```
用户进入塔罗面板
  │
  ├─ [输入问题(可选)] → 点击"开始抽牌"
  │
  ├─ TarotService.CreateSession(question)
  │   └─ 随机从 22 张中选 11 张 → TarotSession.CandidateCards
  │
  ├─ Stage.Select: UI 展示 11 张牌
  │   ├─ 用户点击牌 → PickCard(session, card) → 填入 slot[i]
  │   ├─ 用户点 [洗牌] → ShuffleCards(session) → 重新 11 张
  │   └─ 选满 3 张 → ConfirmSelection(session)
  │
  ├─ Stage.Reveal: 6 个 RequestReadingAsync() 并行
  │   ├─ 每对 (past_angel, past_devil) → 缓冲
  │   ├─ 每对 (present_angel, present_devil) → 缓冲
  │   ├─ 每对 (future_angel, future_devil) → 缓冲
  │   ├─ 用户点 [继续] → 揭晓下一幕
  │   └─ 全部揭晓 → 写入 CollectionService + 保存历史
  │
  └─ 回到 Stage.Idle
```

## 边界情况

| 场景 | 处理 |
|------|------|
| LLM 请求超时 | 显示对应人格的占位文案 + 本地 fallback |
| ApiKey 未配置 | 全部走 LocalFallback，UI 标注"本地解读" |
| 选牌不足 3 张时退出 | 提示"牌还未选满" |
| 洗牌后重新选 | 清空已选，重新 3 个槽位 |
| Deck 不足 11 张牌 | 实际展示 min(11, deckCount) 张 |
| 同一张牌在不同 session 重复出现 | 允许 |
| 用户点完某张牌后立刻出现 LLM 报错 | 已经缓存的状态先显示 loading，超时后再 fallback |

## 配置结构总结

通过 ScriptableObject 实现"UI 不写死"：

- `LLMConfigSO` — LLM endpoint / key / model / prompt 模板（含 `{personality}` 占位符）
- `TarotLayoutSO` — 弧形参数 / 动画时长 / hover 效果 / loading 文案
- `TarotCardSO`（已有） — 卡面 Sprite、关键词
- `TarotDeckSO`（已有） — 22 张牌列表、卡背 Sprite
- `PersonalityMatrixSO`（已有） — 天使/恶魔的 7 维性格基础值

美术/策划可直接在 Inspector 调整这些 SO 改变表现，不需碰代码。
