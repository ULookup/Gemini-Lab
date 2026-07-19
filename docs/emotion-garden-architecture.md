# 情绪花园系统架构

Updated: 2026-07-14

## 定位

情绪花园（EmotionGarden）是室外伊甸园的核心玩法模块。玩家每天输入一句心情，系统生成情绪花，记录到以周为单位的培育面板，开花后收入图鉴，按"情绪+培育者"累计解锁花丛。

当前阶段：最小流程验证（方块 UI + 固定情绪 + 无 AI）。

## 数据模型

```
EmotionFlowerData (每朵花)
├── FlowerId         "悲伤_angel_20260714"
├── DateIso          "2026-07-14"
├── WeekId           周年编号（ISO 8601）
├── EmotionType      "悲伤"
├── EmotionDetail    原始输入文本
├── Owner            "angel" / "demon"
├── GrowthState      Growing / Bloomed
├── IsCollected      是否已收入图鉴
└── CreatedAtUtcTicks

ClusterProgress (情绪+培育者 累计)
├── EmotionType + Owner  (联合 key)
├── TotalCount
└── UnlockedStage   0 / 1 / 3
```

## 服务接口

```
IEmotionGardenService
├── CanSubmitToday() → bool
├── SubmitEmotion(emotionType, detail, owner) → EmotionFlowerData
├── GetWeekFlowers(weekId) → 7 格数组（按周一~周日）
├── GetTodayFlower() → 可选
├── SetBloomed(flowerId)
├── GetAllClusters() → 累计列表
└── IPersistentService (Key: "emotion-garden")
```

## 每日提交限制

- 服务内部维护 `_lastSubmitDateIso`
- 通过 `IGameClock.TodayIso` 比对，同日重复调用拒绝
- 跨天自动重置（监听 `NewDayStartedEvent` 或 submit 时检查）
- 提交后当天培育者锁定，不可更改

## 文件清单

```
新增:
Assets/_Project/Scripts/Modules/EmotionGarden/
├── EmotionGarden.asmdef
├── EmotionFlowerModels.cs          # 数据结构 + 事件
├── IEmotionGardenService.cs        # 接口
├── EmotionGardenService.cs         # 实现 (MonoBehaviour, IPersistentService)
├── EmotionGardenRuntimeBootstrap.cs # 引导注册

Assets/_Project/Scripts/Modules/HubUI/Panels/
├── EmotionInputPanelStub.cs        # 每日输入 UI（方块占位）
├── WeeklyGardenPanelStub.cs        # 周一~周日 7 格 UI
├── FlowerCollectionPanelStub.cs    # 图鉴累计 UI

Assets/_Project/Scripts/Editor/SceneBootstrap/
└── EmotionGardenPanelAuthoring.cs  # Editor 工具：创建面板 + 挂载
```

## 与现有系统的关系

| 依赖 | 用途 |
|---|---|
| `IGameClock` | 判断今天日期、跨天 |
| `ServiceLocator` | 注册/获取服务 |
| `EventBus` | 广播提交/开花/解锁事件 |
| `IPersistentService` | 存档/读档 |
| `IPersistentServiceRegistry` | Bootstrap 里注册 |

**不改动**：Garden 模块、Pet 模块、Core 基础设施。

## UI 示意图

```
每周面板：
[< 上周]              第 28 周              [下周 >]
┌────────┬────────┬────────┬────────┬────────┬────────┬────────┐
│  周一  │  周二  │  周三  │  周四  │  周五  │  周六  │  周日  │
│  悲伤  │  愤怒  │        │        │        │        │        │
│  天使  │  恶魔  │        │        │        │        │        │
│  🌸    │  🌱    │        │        │        │        │        │
└────────┴────────┴────────┴────────┴────────┴────────┴────────┘

图鉴面板：
天使培育：
  悲伤花 × 2  还差 1 朵解锁小花丛
  疲惫花 × 1
恶魔培育：
  悲伤花 × 3  已解锁小花丛
```

## 实施顺序

| 步骤 | 内容 |
|---|---|
| 1 | 数据模型 + 接口 + asmdef |
| 2 | Service 实现（提交/周记录/开花/累计）+ Bootstrap |
| 3 | 存档读写（IPersistentService） |
| 4 | 每日输入 UI（EmotionInputPanelStub） |
| 5 | 每周面板 UI（WeeklyGardenPanelStub） |
| 6 | 图鉴面板 UI（FlowerCollectionPanelStub） |
| 7 | Editor 工具 + Boot 连线 |

## 验收标准

- 点击天使区域 → 输入心情 → 生成"天使·悲伤花"
- 花出现在本周当天位置
- 花开花后进入图鉴，数量累计
- 累计 1/3 时触发解锁
- 退出重进数据不丢
- 同一天不可重复提交
- 恶魔版本独立运行
