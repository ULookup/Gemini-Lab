# Gemini-Lab Memory Main

Updated: 2026-05-11

## 定位
这份文档是 Gemini-Lab 的长期项目记忆总览。

`AGENTS.md` 已经作为总入口落地；本文件承担“第二入口 + 主记忆总览”的作用。

## 快速导航
- [AGENTS.md](../../AGENTS.md)
- [架构记忆](./gemini-lab-memory-architecture.md)
- [规则与历史](./gemini-lab-memory-rules-and-history.md)
- [开发手册](./gemini-lab-agent-development-playbook.md)
- [文件指南](./gemini-lab-project-file-guide.md)
- [项目结构总览](../project-structure-overview.md)
- [玩法规范](../gameplay-spec.md)
- [人工验证清单](../manual-validation-checklist.md)
- [美术替换工作流](../art-replacement-workflow.md)
- [Git 开发流程](../git-fork-upstream-pr-workflow.md)
- [项目 Skill 清单](../project-skill-catalog.md)
- [Skill 设计边界](../skill-design-boundary.md)
- [方法论对齐审计](../ai-workspace-bootstrap-alignment.md)
- [记忆索引](./memory-index.paths.txt)

## Workspace Identity
- 项目名称：Gemini-Lab
- 项目类型：Unity 2D 桌宠客户端（长期目标仍保留 AI 陪伴方向）
- Unity 版本：`2022.3.62f3c1`
- 当前阶段目标体验：让宠物在公寓场景中完成基础移动、家具展示、交互与状态显示；现阶段暂不接入大模型，桌宠主要由玩家直接控制移动
- 当前协作工具：Unity MCP、嵌入式 Unity Skills、`.cursor/skills/` 与 `.agents/skills/`

## 当前状态
- 当前仓库已经从“文档与工程骨架先行”推进到“文档 + 原型实现并行”阶段。
- `AGENTS.md`、`docs/` 与 `docs/ai-memory/` 于 2026-04-21 建立；2026-04-27 完成 fork 主线同步并补齐 Git 工作流文档。
- `Assets/_Project/` 下已经存在真实运行时代码、场景、asmdef、测试程序集、示例美术资源与动画资源。
- `2026-04-28` 已开始推进任务 1：现有场景家具接入 `FurnitureService`，并让 Apartment 场景里的状态/库存/概览面板显示真实运行时数据。
- `2026-04-28` 已完成任务 2 的首轮范围确认，并把现有 `Move` 动画 controller 显式绑定到 `Apartment_Main.unity` 中的 `Pet_Angel`。
- `2026-04-28` 已基于新增美术资源补上两个交互动画 clip：`Interact_Read` 与 `Interact_BesideDoor`，并把它们接进现有 `Pet_Angel.controller`。
- `2026-04-29` 已把 `公寓场景.psd` 备份到原目录，并把 PSD Importer 子资源转为独立 Sprite；当前第一轮实际用于家具整理的资源已开始放入 `Assets/_Project/Art/Sprites/Furniture/**/`，并按中文语义命名维护。
- `2026-04-29` 在 `Apartment_Main.unity` 中追加了一次“仅家具层补景”的试验：当前会在 `Furniture` 下额外挂一个 `StaticFurnitureDecorOnly`，用于承载与交互逻辑无关的纯静态补景家具。
- `2026-05-01` 已开始把公寓场景里真实存在的关键家具对象显式接入家具系统：
  - 新增 `SceneFurnitureDefinitionHint`
  - 新增 `ApartmentSceneFurnitureBindings`
  - `FurnitureService` 现已支持优先读取场景显式提示，而不是只靠名称推断
  - `Apartment_Main.unity` 的 `Furniture` 根当前已配置首批 8 个关键对象：天使床、天使床头柜、天使竖琴、天使工作桌、恶魔工作桌、天使书柜、天使花盆方桌、天使底部盆栽
- `2026-05-01` 已开始补“最优先的 3 个交互类型”代码：
  - `睡眠交互`
  - `装饰观察`
  - `休闲交互`
  当前交互类型已经进入 `FurnitureDefinitionSO`、`FurnitureInteractionTarget`、`PetRuntimeData` 和状态面板链路，不再只是口头分类。
- `2026-05-01` 已进一步把一部分家具从“类别级交互”推进到“对象级交互类型”：
  - `上床休息`
  - `看书柜`
  - `照镜子`
  - `整理床头柜`
  - `演奏竖琴`
  - `弹吉他`
  - `看画架`
  - `看照片板`
  - `观察植物`
  - `地毯休息`
  - `沙发休息`
  - `坐下休息`
- `2026-05-02` 已继续把第二批对象接入场景与交互链路：
  - 场景里已补进或绑定：镜子、恶魔地毯、恶魔沙发、恶魔凳子、天使凳子、恶魔画架、恶魔照片板、恶魔椅子
  - `ApartmentSceneFurnitureBindings` 当前已从最初的 5 个关键对象扩展到覆盖主要可交互家具与第二批静态装饰对象
- `2026-05-02` 已继续把第三轮装饰类对象补进对象级交互与场景绑定：
  - 纸张
  - 耳机
  - 音响
  - 音响和乐器
  - 柜子
  - 储物家具
- `2026-05-02` 已继续把第四轮剩余可做装饰对象接入对象级交互或场景绑定：
  - 窗台
  - 窗台上的盆栽
  - 床上玩偶
  - 沙发上枕头
- `2026-05-02` 已对 Apartment 家具交互链路做一轮精修：
  - 清理 `ApartmentSceneFurnitureBindings` 中的重复 `_target` 绑定
  - 把 `花盆方桌` 的场景定义从误写的装饰类修正为 `WorkDesk`
  - 为 `窗台 / 玩偶 / 枕头` 补上更贴近对象语义的对象级交互类型
  - 让 `小圆镜`、`园地毯`、`左下小家具`、`左下窄家具` 与当前脚本推断口径重新对齐
- `2026-05-03` 已继续把“已有资源但未完全落场景”的对象收口到 `Apartment_Main.unity`：
  - 修正 `照片板` 的空绑定 `_target`
  - 将 `园地毯` 接入场景显式绑定
  - 将 `小圆镜 / 羽翼边柜 / 恶魔盆栽 / 左下小家具 / 左下窄家具` 补入 `StaticFurnitureDecorOnly` 并接入交互绑定
- `2026-05-03` 已同步适配桌宠新版移动美术资源：
  - `Assets/_Project/Art/Sprites/Pet/Frames/Move/` 当前改为 `正面 / 背面 / 侧面` 三个子目录
  - `PetMoveAnimationSetupEditor` 现会优先读取这三个子目录，旧版 `Pet_Angel_Move_{Front|Back|Side}_0001...` 仍作为兜底
  - `Pet_Angel_Move_Front.anim`、`Pet_Angel_Move_Back.anim`、`Pet_Angel_Move_Side.anim` 已切换到新版移动帧引用
  - 运行时移动表现继续采用“前后分离、左右共用侧面并通过 `SpriteRenderer.flipX` 翻转”的四方向规则
- `2026-05-06` 已开始把 Apartment 家具交互链路从“场景直挂 + 运行时兜底”推进到“真实作者化资源”：
  - 已为当前 `Art/Sprites/Furniture/**/` 下的全部 `49` 个家具 Sprite 资源生成对应 `FurnitureDefinitionSO`
  - 已为同一批 `49` 个家具资源生成对应 `Furniture` Prefab
  - `Apartment_Main.unity` 已开始转成使用这些真实 Prefab 实例
  - `FurnitureService` 现会优先使用场景对象上已赋值的真实 `FurnitureDefinitionSO`
- `2026-05-11` 已落地「Phase D：面板全建 + Settings/Inventory/Collection 服务」：
  - **Settings**：新模块 `Modules/Settings`，`ISettingsService` + `SettingsService`（实现 `IPersistentService`），包含主/BGM/SFX 音量、全屏、Overlay 开关、语言 ISO；PlayerPrefs 持久化 + `SettingsChangedEvent` 广播；MainMenu.Canvas 下新增 Panel_Settings（滑条 + 开关 + 重置 + 关闭）。
  - **Inventory**：新模块 `Modules/Inventory`，`ItemCategory`、`ItemDefSO`、`ItemCatalogSO`、`IInventoryService` + `InventoryService`（实现 `IPersistentService`）、`ItemStack` / `InventoryChangedEvent`；已生成 10 个占位 `ItemDefSO`（种子/作物/塔罗券/旅行补给/纪念物/金币）+ `ItemCatalog.asset`；Apartment `Panel_Inventory` 升级为 Grid + Tooltip 真实面板。
  - **Collection**：新模块 `Modules/Collection`，`CollectionCategory`、`CollectionEntry`、`ICollectionService` + `CollectionService`（实现 `IPersistentService`）；`CollectionRuntimeBootstrap` 订阅 `TarotDrawnEvent` 自动把抽卡记录归入 Tarot 类别；Apartment `Panel_Collection` 升级为 3 Tab（旅行 / 塔罗 / 花园）+ Grid。
  - **SaveSlots 骨架**：MainMenu.Canvas 下新增 Panel_SaveSlots，3 个槽位读写 `Application.persistentDataPath/saves/slot_N.json`；仅槽位元数据，真正 SaveSystem 整合留给 Phase E。
  - **Boot.BootstrapRoot** 新挂三件：`SettingsRuntimeBootstrap`、`InventoryRuntimeBootstrap`、`CollectionRuntimeBootstrap`。
  - Editor 新增 authoring：`Tools/Gemini-Lab/Author Item Catalog (10 placeholders)`、`Author Boot Phase D Bootstraps`、`Author Settings + SaveSlots Panels (MainMenu)`、`Author Inventory + Collection Panels (Apartment)`。
  - 顺手恢复：PR #3 merge 中被退回的 `PetRuntimeData.PetId` / `PetRuntimeSnapshotChangedEvent.PetId` / `PetController._petId + Roster 注册` 重新接上，使 Phase C PetStatus 面板的双宠页签继续正常工作。
- `2026-05-11` 已落地「Phase C 第一轮：底座 + PetStatus Panel」：
  - **GameClock**：`Core/Time/IGameClock` + `SystemGameClock`（默认）+ `FakeGameClock`（测试用）；GameBootstrap 以 `IGameClock` 注册到 ServiceLocator，是业务侧取时间的**唯一入口**。
  - **塔罗每日限制迁移**：`TarotService` 从直接 `DateTime.Now` 改走 `IGameClock.TodayIso` / `IsToday`，构造参数里 `Func<DateTime>` 改为 `IGameClock?`；PlayerPrefs 仍作为临时载体，C1 存档整合时迁移到 SaveSlot。
  - **Toast 通知系统**：`Core/UI/ToastKind` + `ToastRequestedEvent` + `IToastService`；`Modules/HubUI/Toast/ToastOverlayController` 挂 Boot、DontDestroyOnLoad，既是 IToastService 实现也订阅 EventBus；TarotRuntimeBootstrap 订阅 `TarotDrawnEvent` 自动发 Success Toast。
  - **ESC 关栈 + Scene 淡入淡出**：`Modules/HubUI/UIInputRouter`（ESC → IUIRouter.CloseTop）、`Modules/HubUI/SceneFadeOverlay`（订阅 SceneLoadStarted/Completed 事件，黑幕 CanvasGroup 淡入淡出）都挂在 Boot.BootstrapRoot。
  - **IPersistentService 契约**：`Core/Persistence/IPersistentService`（Key + CaptureJson + RestoreJson），当前只是空接口 + 协议文档，C1 阶段 SaveSystem 整合时启用。
  - **PetStatus Panel 真实化**：`PersonalityRadarGraphic`（手绘 UI Mesh 7 维雷达）+ `PetStatusPanelStub` 升级为真实控制器（Angel/Devil 页签 + 心情/精力/饱食进度条 + 当前状态 + 雷达图）；数据源 `IPetRoster` + `PetRuntimeSnapshotChangedEvent` 实时刷新。
  - Editor 新增 3 个 authoring：`Tools/Gemini-Lab/Author Boot ToastOverlay`、`Author Boot InputRouter + Fade`、`Author Pet Status Panel UI`。
- `2026-05-10` 已落地「B1 塔罗垂直切片」：
  - 新模块 `Modules/Tarot`（asmdef `GeminiLab.Modules.Tarot`）：`TarotOrientation`、`TarotCardSO`、`TarotDeckSO`、`TarotModels`（DrawResult/Reading/事件）、`ITarotService` + `TarotService`（每日一次，`yyyy-MM-dd` PlayerPrefs 记录）、`ITarotReadingBackend` + `LocalFallback` + `GatewayTarotBackend`、`TarotRuntimeBootstrap`
  - 22 张大阿卡那 `TarotCardSO` + 1 张 `TarotDeckSO` 已生成到 `ScriptableObjects/TarotConfig/`；占位卡面 PNG 在 `Art/Sprites/Tarot/Majors/`（256x384 色块 + 卡框十字基准），美术交付后替换 `TarotCardSO._artwork`
  - Apartment `Panel_Tarot` 已升级成真实面板：左侧卡面 + 正/逆位标识、底部"抽今日塔罗"按钮、右上 `AngelBubble`（天使·正位）、右下 `DevilBubble`（恶魔·逆位）；所有 TMP 自动挂 `TMPFontBinder`
  - 塔罗解读路径：抽卡后**并行**发起 Angel 正位 / Devil 逆位两个 `RequestReadingAsync`；后端优先走 Gateway，失败或超时回退到 `LocalFallback`（关键词 + 人格模板拼接）
  - `TarotRuntimeBootstrap` 已挂在 Boot.unity 的 BootstrapRoot 上，绑定 `TarotDeck.asset`；Gateway 未就绪时自动走 Fallback-only backend
  - Editor 工具：`Tools/Gemini-Lab/Author Tarot Deck (22 Majors)`、`Author Tarot Panel UI`、`Author Boot TarotBootstrap`
- `2026-05-10` 已落地「A1 双宠改造 + A2 CJK 字体 / Catalog 真接入」：
  - **A1 双宠改造**：`Modules/Pet/` 新增 `PetId` 枚举（Angel / Devil）、`IPetRoster` + `PetRoster`；`PetRuntimeData`、`PetContext`、`PetStateChangedEvent`、`PetRuntimeSnapshotChangedEvent` 都带上 PetId（默认 Angel 保持老代码零改动）。`PetController` 增加 `[SerializeField] _petId` 并在 Awake 时向 Roster 注册。`PetRuntimeBootstrap` 现会先保证 `IPetRoster` 已注册。Apartment / WorldMap 各有 Pet_Angel + Pet_Devil 两只宠物（Devil 用暖色染色占位，真实美术后续替换）。
  - **A2 CJK 字体 + Catalog 接入**：`Art/Fonts/` 落地 `NotoSansSC-VF.ttf`（17MB）+ 动态 SDF `NotoSansSC_SDF.asset`（~3KB）；`ScriptableObjects/UIArt/UIFontCatalog.asset` + `UIArtCatalog.asset` 落地，FontCatalog 的 default/title/bubble 三个 key 全部指向 NotoSansSC_SDF。`Modules/UI/Catalogs/` 新增 `IUIFontService`、`IUIArtService`、`UICatalogHost`（挂 Boot.unity，DontDestroyOnLoad，把 Catalog 以服务形式注册）、`TMPFontBinder`（挂每个 TMP_Text 上 Awake 时自动从 Catalog 取字体）。MainMenu / Apartment / WorldMap 三个场景的占位英文标签已替换为中文（开始/存档/设置、收藏/塔罗/花园/…），TMP Binder 已回填到 18 个 TMP 对象。
  - Editor 新增 authoring 菜单：`Tools/Gemini-Lab/Author Dual Pets (Apartment + WorldMap)`、`Generate CJK TMP Font Asset`、`Author UI Catalogs`、`Author Boot UICatalogHost`、`Author TMP Binder Backfill`
- `2026-05-10` 已落地「框架搭建 + 场景切换」承重层（P0）：
  - Core 新增 `SceneFlow/` 命名空间：`SceneId` 枚举（Boot / MainMenu / Apartment / WorldMap / DesktopOverlay）、`ISceneCatalog` 与 `DefaultSceneCatalog`、`ISceneFlowService` 与 `SceneFlowService`、`SceneTransitionPayload`、`SceneLoadStartedEvent` / `SceneLoadCompletedEvent`
  - Core 新增 `UI/` 命名空间：`PanelId`、`IUIPanel`、`IUIRouter` 与 `UIRouter`（含 `UIPanelOpenedEvent` / `UIPanelClosedEvent`）
  - `GameBootstrap` 升级：注册 EventBus / CommandDispatcher / SceneFlowService / UIRouter；Boot 场景启动后自动加载 `MainMenu`；在非 Boot 场景（直开 Editor 调试）跳过自动跳转并同步当前场景 id
  - `DesktopOverlayManager` 改造：切场景走 `ISceneFlowService.LoadAsync`，不再直接调用 `SceneManager`；仍由 `DesktopOverlayRuntimeBootstrap` 在首次场景加载后挂到 DontDestroyOnLoad 的 `DesktopOverlaySystem` 上
  - 新增模块 asmdef：`GeminiLab.Modules.MainMenu`、`GeminiLab.Modules.HubUI`、`GeminiLab.Modules.WorldMap`
  - 新增 `UIArtCatalogSO` / `UIFontCatalogSO`（放在 `Modules/UI/Catalogs/`）：静态 UI 走 Sprite key、动态文本走 TMP key；美术替换只改 `.asset`，不改 Prefab / 代码
  - 新增场景骨架：`Scenes/MainMenu/MainMenu.unity`（开始 / 存档 / 设置 三个占位按钮）、`Scenes/WorldMap/WorldMap_Main.unity`（横板摄像头 + 返回公寓按钮 + Garden Zone 标记）
  - 公寓场景注入 `UI_Sidebar`（展开/收起 + 4 个占位 Panel：PetStatus / Tarot / Collection / Inventory）与右上 `UI_WorldMapPortal` 按钮
  - EditorBuildSettings 整理：`Boot(0) / MainMenu / Apartment_Main / WorldMap_Main / Desktop_Overlay`，移除 Unity 默认 SampleScene
  - Editor 新增一次性 authoring 入口：`Tools/Gemini-Lab/Author MainMenu Scene`、`Author WorldMap Scene`、`Author Apartment Sidebar`
  - UI 静态文本统一走 Sprite（美术交付后替换）、动态文本走 TMP；当前占位 TMP 使用 LiberationSans SDF，中文字符显示为 □，待补中文 CJK TMP Font Asset
- `2026-05-07` 已完成 `Interact` 资源命名规范收口：
  - `read/` 组重命名为 `Pet_Angel_Interact_Read_0001...0006.png`
  - `beside door/` 组重命名为 `Pet_Angel_Interact_BesideDoor_0001...0005.png`
  - `Assets/_Project/Art/Sprites/Pet/README.md` 已改为“方向型帧保留方向字段，非方向型交互帧允许使用变体名”的真实规则
- `2026-05-08` 已把 Apartment 原型里的桌宠行动入口调整为玩家直接控制：
  - 新增 `PetPlayerInputController`
  - 新增 `PetPlayerFurnitureInteractionController`
  - 新增 `PetClickReactionController`
  - 新增 `PetClickResponseLibrary`
  - `Pet_Angel` 现支持 `WASD` 与方向键移动
  - `Pet_Angel` 现支持靠近指定家具或交互点时按 `F` 触发玩家手动交互
  - `Pet_Angel` 现支持鼠标左键点击后输出当前表情 Debug 信息，并弹出本地语料气泡回复
  - `PetController` 在检测到玩家输入组件后，会停止自主移动链路，改由玩家驱动 `Idle / Moving`
  - 当前文档口径同步收口为：现阶段暂不接入大模型，Gateway / Travel / AI 对话仍保留为后续规划
  - 同期修正 `FurnitureLayoutPersistence` 与 `ApartmentSceneFurnitureBindings`：
    - 当前默认禁用家具布局自动恢复
    - 家具存档只记录运行时摆放家具，不再清掉场景预摆家具
    - `ApartmentSceneFurnitureBindings` 现可按 `definitionId / Sprite 名 / 现有 hint` 自动找回缺失 `_target`
  - 同期补入 `Idle` 三视图与 `Sleep` 动画资源接线：
    - 新增 `Pet_Angel_Idle_Front.anim`
    - 新增 `Pet_Angel_Idle_Back.anim`
    - 新增 `Pet_Angel_Idle_Side.anim`
    - 新增 `Pet_Angel_Sleep.anim`
    - `Pet_Angel.controller` 新增 `Idle_Front / Idle_Back / Idle_Side / Sleep`
    - `PetController` 当前会在静止时切到 `Idle_*`，在 `SleepingState` 时切到 `Sleep`
- `Assets/_Project/Prefabs/` 与 `Assets/_Project/ScriptableObjects/` 现在都不再是完全空目录，且 `Furniture` / `FurnitureConfig` 这条线已覆盖当前全部家具 Sprite 资源；其他模块仍未完成资产作者化。
- README 系列文档描述的目标状态仍然大于当前实现范围，阅读时必须显式区分“已实现事实”和“规划目标”。
- 项目本地 skill 目录当前仍保持 `.agents/skills/` 与 `.cursor/skills/` 镜像关系，当前统计为 `72` 项。

## 当前最重要事实
1. 这个仓库不再是“只有说明文档”的空骨架，已经有一轮可运行原型；但说明文档密度依然高于最终实现密度。
2. `_Project/` 继续是自研业务代码与资源的唯一正式落点。
3. 当前已真实落地的关键内容包括：
   - 场景：`Boot.unity`、`Apartment/Apartment_Main.unity`、`Desktop/Desktop_Overlay.unity`
   - Core：`ServiceLocator`、`EventBus`、`CommandDispatcher`、FSM、`GameBootstrap`
   - 业务模块：`Pet`、`Furniture`、`Navigation`、`Gateway`、`Travel`、`Persistence`、`UI`、`DesktopOverlay`
   - 测试：`EditMode` / `PlayMode` 测试程序集与多组核心模块测试
4. 当前 Apartment 原型里的桌宠主行动方式已经调整为“玩家直接控制移动优先”，不再把自主寻路 / 大模型驱动行为作为当前阶段默认验证目标。
5. `Packages/manifest.json` 当前已经包含：
   - `com.unity.ai.navigation`
   - `com.ivanmurzak.unity.mcp`
   - `com.ivanmurzak.unity.mcp.particlesystem`
   - `com.ivanmurzak.unity.mcp.animation`
6. 当前原型里仍存在多处“占位实现 / 运行时兜底”：
   - `NavigationService` 与 `NavMesh2DRebaker` 目前更接近占位导航层，不是完整 2D NavMesh 方案
   - `WindowModeAdapter` 目前只提供模式状态与点击穿透标记，没有真正的原生透明窗口实现
   - `GatewayRuntimeHost` 在缺少配置资产时会回退到运行时创建的 Mock 配置
   - `FurnitureService` 在缺少配置资产时会补运行时家具定义
7. 当前最明显的资源层缺口仍是：
   - `Furniture` 之外的大部分 `Prefab` / `ScriptableObject` 资产仍未作者化
   - 真实人格雷达、美术更完整的交互动画与更正式的 UI 资源仍未补齐
   - `Pet_Angel` 当前已有 `Move_Front / Move_Back / Move_Side / Interact_Read / Interact_BesideDoor`
   - 但 `Idle` 与更完整的 `Emotion` 仍缺少正式资源与状态链路
8. 当前工作树不是干净状态，执行任何修改前都要先看 `git status`，避免覆盖用户现有改动。
9. 当前版本控制协作基线已经固定为 `fork + upstream + feature branch + PR` 工作流。

## 长期目标
- 做成一个真正可持续演化的 AI 桌宠项目，而不是一次性 Demo。
- 让“玩法、架构、工具、验证、文档”同时成长，不把任何一块长期欠账。
- 保持多智能体可接手：新智能体进入项目后，能在较短时间内读懂上下文并安全推进。

## 长期约束
- 所有中文文档和中文注释必须保持 UTF-8 正常显示。
- 文档里必须显式区分“已存在事实”和“规划目标”。
- `UI` 不承载业务逻辑；跨模块通信只走接口、事件或服务定位。
- ScriptableObject 资产在运行期只读，运行态状态进入 Service 或 Snapshot。
- 优先 Scene / Inspector 友好与美术替换友好，不做只能靠硬编码维持的结构。
- `_Project/` 继续作为自研业务资产唯一落点；第三方资源不要混入其中。

## 阶段进度
| Phase | 目标 | 当前判断 |
| :--- | :--- | :--- |
| Phase 1 | 核心基础设施与 FSM 骨架 | Core、FSM、`Boot.unity`、存档骨架与测试程序集已落原型 |
| Phase 2 | V-Decor、2D NavMesh、家具交互 | 公寓场景、家具系统与导航抽象已落原型，真实导航实现仍需加强 |
| Phase 3 | OpenClaw 网关与对话/工作链路 | Gateway Client、事件路由、Mock 链路已落原型，真实配置资产与联调仍待补齐 |
| Phase 4 | UI、桌面 Overlay、旅行系统 | UI / Overlay / Travel 已有首轮代码与场景支撑，原生 Overlay 与完整用户旅程仍未收口 |

## 近期建议优先级
1. 补齐 Prefab 与 ScriptableObject 资产，把现在依赖运行时兜底的部分逐步转成真实资源作者化。
2. 把导航与桌面 Overlay 从“占位实现”推进到真实可验证实现。
3. 在 Unity 内补跑测试与场景验证，并把结果回写 `docs/manual-validation-checklist.md`。
4. 随着场景、Prefab、SO、脚本继续落地，持续更新本记忆体系与结构文档。

## 更新触发
出现以下任一变化时，必须同步更新记忆文档：
- 核心玩法规则变化
- 场景结构变化
- UI 层级变化
- 关键脚本或关键包变化
- 文件结构变化
- 已知问题状态变化
- 推荐开发顺序变化
