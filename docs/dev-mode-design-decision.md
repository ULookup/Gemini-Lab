# 设计决策：开发者模式 / 玩家模式

**日期**: 2026-07-18
**状态**: 已实施
**提出人**: rei

## 背景

情绪花园的每日循环（一天一次提交、跨天开花）导致开发调试必须"等到第二天"。早期做过两个绕过按钮（重置今日限制、立即开花），后整合为一个走真实跨天路径的"进入下一天"按钮。随之产生新问题：**调试用的时间快进不应该出现在真实游戏里**——玩家应当按真实世界时间游玩。

## 决策

引入轻量级双模式：

| 模式 | 时间判定 | 调试 UI | 适用 |
|---|---|---|---|
| 开发者模式 | 可用"进入下一天"拨快 `IGameClock` | 显示 | 日常开发调试 |
| 玩家模式 | 真实系统时间 | 隐藏 | 真实游玩 / 打包版本 |

### 实现要点

1. **模式标志**：`GeminiLab.Core.DevMode.Active`（静态 bool，默认 true）
   - Editor 下由 `DevModeToggle`（`[InitializeOnLoad]`）从 `EditorPrefs`（key: `GeminiLab.DevMode`）读入
   - 菜单 `Tools → Gemini-Lab → Toggle Dev Mode` 切换（**Play 中禁止切换**，见第 5 条）
   - **打包版本经 `[RuntimeInitializeOnLoadMethod]` 强制 false**，玩家无法开启
2. **调试 UI 统一父节点**：所有调试按钮集中在 `Canvas/DevTools` 下（不再散落在各面板 Content 里），由 `WorldMapEmotionGardenUIPatch.EnsureDevTools` 幂等创建
3. **双重防护**：
   - 可见性：`DevToolsActions.Awake` 在玩家模式下 `SetActive(false)` 整组隐藏
   - 行为：每个调试方法入口 `if (!DevMode.Active) return;` 门禁
4. **`IGameClock.DebugAdvanceDays` 保留在接口上**：它只被调试代码调用，运行时不会自行触发；玩家模式下按钮隐藏 + 门禁拦截，天然安全
5. **存档按模式隔离**（2026-07-19 新增）：`PersistenceBootstrap` 构造 `SaveSystem` 时按模式选根目录——开发者模式 `persistentDataPath/Saves-Dev/`，玩家模式 `persistentDataPath/Saves/`。所有走 `ISaveSystem` 的数据（autosave、手动槽位、furniture_layout）自动全部隔离，调试数据（时钟快进产生的未来日期花等）不会污染真实进度。因此 Play 中禁止切换模式（存档目录在启动时已固定，中途翻转会"读 A 写 B"）。历史遗留：模式隔离上线时，旧 `Saves/` 全部内容属开发期产物，已整体迁移到 `Saves-Dev/`
   - **不隔离的边界**：`chat_history.json`（ChatPersistenceService 直写文件，未走 ISaveSystem）、`DailyResetService` 的 PlayerPrefs 冷启动回退 key——两模式共用，目前影响可忽略，接入正式聊天存档时一并处理

## 刻意不做的（当前阶段）

| 方案 | 不采用的理由 |
|---|---|
| `#if DEV_BUILD` 编译宏区分两个包 | 调试流程是"同一份工程里切换"，不是"打两个包"；宏切换要重编译，慢 |
| `IDevModeService` 注册进 ServiceLocator | 一个静态 bool 就够，没到需要注入/mock 的复杂度 |
| 玩家模式下删除调试 GameObject | 删除需重跑补丁才能恢复；SetActive 反转快、幂等、场景 diff 小 |
| 玩家侧（PlayerPrefs）持久化模式选择 | 玩家永远不该能切到开发者模式，打包强制 false 已覆盖 |

## 已知取舍

- `DevTools` 下所有工具共享一个开关，颗粒度是整组（够用；将来需要单独开关再拆）
- `EditorPrefs` 是机器级配置，换机器/换用户默认回到开发者模式（开发期这是期望行为）
- ~~时钟快进只在内存中生效，重启 Play 还原为真实时间~~ **2026-07-19 修订**：此取舍已被实际调试流程证伪——快进后种下的"未来日期"花在重启后落入不可达的未来周，表现为数据消失。现改为：开发者模式下偏移经 `PlayerPrefs`（key: `GeminiLab.Debug.ClockOffsetDays`）持久化，虚拟日期跨 Play 会话连续；玩家模式忽略偏移、永远真实时间；DevTools 提供"重置时钟"按钮清零偏移

## 未来演进

- 新调试功能一律挂到 `Canvas/DevTools` + `DevToolsActions`（或同级新组件），自动继承模式开关
- 如果将来要给测试玩家发"可调试包"，再把 `DevMode.Active` 的强制逻辑改成读构建配置（一处改动）

## 相关文件

- `Assets/_Project/Scripts/Core/DevMode.cs` — 模式标志
- `Assets/_Project/Scripts/Editor/Tools/DevModeToggle.cs` — Editor 菜单开关
- `Assets/_Project/Scripts/Modules/DevTools/DevToolsActions.cs` — 调试按钮行为（含门禁）
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapEmotionGardenUIPatch.cs` — `EnsureDevTools` 场景搭建
- `Assets/_Project/Scripts/Core/Time/IGameClock.cs` — `DebugAdvanceDays` 调试快进
