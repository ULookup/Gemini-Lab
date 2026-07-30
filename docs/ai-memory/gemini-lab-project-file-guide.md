# Gemini-Lab Project File Guide

Updated: 2026-07-29

## 入口文件
- `AGENTS.md`
  - 当前项目总入口。任何智能体进入项目后都应先读它。
- `docs/current-task-card.md`
  - 当前这一轮任务的轻量任务卡（L1）。
- `docs/current-task-card.json`
  - 当前任务卡的机器可检查版本。
- `docs/ai-memory/gemini-lab-memory-main.md`
  - 当前主记忆总览与第二入口。
- `docs/workflow-context-packages.md`
  - 不同任务类型应优先加载哪些上下文文件。
- `docs/context-compression-and-knowledge-plan.md`
  - 第二部分工作方式升级：上下文压缩、做梦整理、L2/L3 知识沉淀计划。
- `docs/dream-maintenance-checklist.md`
  - 当前人工版“做梦整理”执行清单。
- `tools/check-task-gate.ps1`
  - 当前最小闭环执行闸门脚本。
- `tools/run-unity-editor-method.ps1`
  - 项目本地 Unity batchmode runner，可定位 Unity Editor 并执行 editor static method；当前用于在没有完整 MCP 的情况下落地场景 authoring，并带 `-nographics`、启动日志超时、总执行超时和子进程 watchdog，避免 Unity 启动卡住时无期限阻塞。
- `README.md`
  - 项目总说明，面向产品、技术栈、整体架构和路线图。
- `Assets/README.md`
  - 更偏业务与 FSM 的设计说明。
- `Assets/plan.md`
  - 阶段里程碑、Sprint 拆解、DoD 与风险表。

## 先看哪里

### 想知道项目现在处于什么状态
- `AGENTS.md`
- `docs/current-task-card.md`
- `docs/ai-memory/gemini-lab-memory-main.md`
- `docs/ai-memory/gemini-lab-memory-rules-and-history.md`
- `docs/project-structure-overview.md`
- `docs/context-compression-and-knowledge-plan.md`
- `docs/current-task-card.json`

### 想知道项目真正要做成什么
- `docs/gameplay-spec.md`
- `README.md`
- `Assets/README.md`

### 想知道代码和模块应当怎么分层
- `docs/ai-memory/gemini-lab-memory-architecture.md`
- `Assets/_Project/Scripts/README.md`
- `Assets/_Project/Scripts/Core/README.md`
- `Assets/_Project/Scripts/Modules/README.md`
- 各模块 README

### 想知道场景、Prefab、SO 怎么组织
- `docs/project-structure-overview.md`
- `Assets/_Project/Scenes/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想从真实运行时入口开始看
- `Assets/_Project/Scenes/Boot.unity`
- `Assets/_Project/Scripts/Core/GameBootstrap.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetController.cs`
- `Assets/_Project/Scripts/Modules/Pet/WalkableSurface.cs`
- `Assets/_Project/Scripts/Modules/Pet/RandomWander.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetPlayerInputController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetPlayerFurnitureInteractionController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetClickReactionController.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetClickResponseLibrary.cs`
- `Assets/_Project/Scripts/Modules/Pet/PetRuntimeSnapshotChangedEvent.cs`
- `Assets/_Project/Scripts/Editor/Pet/PetMoveAnimationSetupEditor.cs`
- `Assets/_Project/Scripts/Editor/Furniture/ApartmentFurnitureAuthoringBootstrapEditor.cs`
- `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`
- `Assets/_Project/Scripts/Editor/Tools/DebugDisplayWindow.cs`
- `Assets/_Project/Scripts/Editor/Tools/ReadingBubbleLayoutSync.cs`
- `Assets/_Project/Scripts/Editor/Tools/SaveSlotTemplateCreator.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/SettingsAndSaveSlotsPanelAuthoring.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/AutoSetup.cs`
- `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapEmotionGardenUIPatch.cs`
- `Assets/_Project/Scripts/Modules/HubUI/Panels/FlowerCollectionPanelStub.cs`
- `Assets/_Project/Scripts/Modules/Furniture/FurnitureService.cs`
- `Assets/_Project/Scripts/Modules/Furniture/ApartmentSceneFurnitureBindings.cs`
- `Assets/_Project/Scripts/Modules/Furniture/SceneFurnitureDefinitionHint.cs`
- `Assets/_Project/Scripts/Modules/UI/StatusPanelController.cs`
- `Assets/_Project/Animations/Pet/Pet_Angel.controller`
- `Assets/_Project/Scripts/Modules/Gateway/GatewayBootstrap.cs`
- `Assets/_Project/Tests/EditMode/GeminiLab.Tests.EditMode.asmdef`

### 想做美术替换
- `docs/art-replacement-workflow.md`
- `docs/apartment-scene-sprite-naming-guide.md`
- `Assets/_Project/Art/README.md`
- `Assets/_Project/Art/WorldMap/flowerCodex`
- `Assets/_Project/Art/WorldMap/flower_info`
- `Assets/_Project/Art/Sprites/Furniture/README.md`
- `Assets/_Project/Art/Sprites/Pet/README.md`
- `Assets/_Project/Prefabs/README.md`
- `Assets/_Project/ScriptableObjects/README.md`

### 想看包依赖和工具链
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Packages/SkillsForUnity/`
- `PackageBackups/MCP-disabled-2026-06-02/`
- `PackageBackups/NuGet-disabled-2026-06-02/`
- `ProjectSettings/ProjectVersion.txt`
- `Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs`
- `.cursor/mcp.json`
- `.cursor/skills/`
- `.agents/skills/`
- `docs/project-skill-catalog.md`
- `docs/skill-design-boundary.md`
- `.agents/skills/git-sync-upstream-main/SKILL.md`
- `.agents/skills/unity-clear-generated-cache/SKILL.md`
- `.agents/skills/apartment-scene-rollback-to-commit/SKILL.md`
- `.agents/skills/pet-animation-reference-rebuild/SKILL.md`
- `.agents/skills/furniture-binding-check/SKILL.md`

### 想看版本控制与 PR 流程
- `docs/git-fork-upstream-pr-workflow.md`
- `AGENTS.md`

### 想按任务类型加载最小必要上下文
- `docs/workflow-context-packages.md`
- `docs/current-task-card.md`
- `docs/context-compression-and-knowledge-plan.md`

## 当前真实存在的重要目录

### 文档与协作
- `AGENTS.md`
  - 当前项目总入口与执行规则入口
- `docs/current-task-card.md`
  - 当前轮任务的轻量任务卡
- `docs/current-task-card.json`
  - 当前轮任务卡的机器检查版
- `docs/workflow-context-packages.md`
  - 当前项目推荐的上下文装配方式
- `docs/context-compression-and-knowledge-plan.md`
  - 当前项目第二部分工作方式升级计划
- `docs/dream-maintenance-checklist.md`
  - 当前项目人工版做梦整理清单
- `tools/check-task-gate.ps1`
  - 当前项目最小闭环执行闸门
- `tools/run-unity-editor-method.ps1`
  - 当前项目本地 Unity batchmode 执行入口，带 watchdog 和超时保护
- `docs/`
  - 项目文档根目录
- `docs/ai-memory/`
  - 记忆文档与索引
- `docs/project-skill-catalog.md`
  - 当前项目本地 skill 清单与分类说明
- `docs/skill-design-boundary.md`
  - 当前项目 skill 的设计边界与组织方式
- `.agents/skills/git-sync-upstream-main/`
  - 第一批 workflow skill：安全同步本地 `main` 到 `ULookup:main`
- `.agents/skills/unity-clear-generated-cache/`
  - 第二批 workflow skill：只清理 Unity 生成缓存，不改源码
- `.agents/skills/apartment-scene-rollback-to-commit/`
  - 第三批 workflow skill：精准回退 `Apartment_Main.unity` 到指定 commit
- `.agents/skills/pet-animation-reference-rebuild/`
  - 第四批 workflow skill：重建 `Pet_Angel` 的动画资源与 controller 引用链
- `.agents/skills/furniture-binding-check/`
  - 第五批 workflow skill：只读巡检 Apartment 家具绑定状态
- `docs/git-fork-upstream-pr-workflow.md`
  - 当前项目标准 fork / upstream / feature branch / PR 工作流

### Unity 项目
- `Assets/_Project/`
  - 自研业务唯一正式根目录
- `Assets/_Project/Scripts/`
  - 已存在真实 C# 实现、asmdef 与少量编辑器工具
- `Assets/_Project/Scenes/`
  - 已存在 `Boot.unity`、`Apartment/Apartment_Main.unity`、`WorldMap/WorldMap_Main.unity`、`Desktop/Desktop_Overlay.unity`
- `Assets/_Project/Tests/`
  - 已存在 `EditMode` / `PlayMode` 测试程序集
- `Assets/_Project/Art/` 与 `Assets/_Project/Animations/`
  - 已存在宠物、家具、环境示例资源与宠物动画
  - `Assets/_Project/Art/Sprites/Furniture/**/` 当前已开始承接从 `公寓场景.psd` 派生出来、准备用于家具系统接线的独立 Sprite，后续按中文语义命名维护
- `Packages/`
  - 包依赖与嵌入式包
  - 当前保留的关键本地包是 `SkillsForUnity`
- `PackageBackups/`
  - 当前用于临时停用并备份 4 个 Unity MCP 包
  - 当前还用于暂存从 `Assets/Plugins/NuGet` 软移出的 MCP NuGet DLL 目录
- `ProjectSettings/`
  - Unity 项目级设置

## 当前真实存在但容易误判的情况
1. `Assets/_Project/Scenes/` 已经有真实 `.unity` 场景文件，但它们当前更接近原型场景，不等于所有 README 里的目标场景集合都已完成。
2. `Assets/_Project/Prefabs/` 已开始落地真实 `.prefab` 资源，当前 `Furniture/` 已覆盖全部家具 Sprite 资源。
3. `Assets/_Project/Scripts/` 已经有大量真实 C# 业务实现与 asmdef，不再是只有 README 的空目录。
4. `Assets/_Project/ScriptableObjects/` 已开始落地实际 `.asset` 配置，当前 `FurnitureConfig/` 已覆盖全部家具 Sprite 资源；很多其他 SO 类型仍只存在于代码层。
5. `Assets/_Project/Scripts/Modules/Desktop/README.md` 仍承载桌面模块的设计说明，但当前真实运行时代码目录是 `Assets/_Project/Scripts/Modules/DesktopOverlay/`。
6. 多个系统当前依赖运行时兜底或 Mock 配置，看到“能跑起来”不等于“资产作者化已完成”。
7. `2026-05-26` 起，Apartment 场景里的旧占位 UI 残留（`TopLeft_StatusPanel`、`Right_InventoryPanel`、`BottomRight_PersonalityRadar`）已从 `Apartment_Main.unity` 真实移除；旧的 `SpaceSystemPrototypeRoot` 原型 UI 也不再作为后续 UI 制作基础。当前保留的新主界面骨架为 `Panel_PetStatus`、`Panel_SpaceSys`、`Sidebar`、`SidebarOverlay`、`ApartmentViewportHost`、`ApartmentViewportImage` 与 `ApartmentViewportCamera`。此前尝试绑定到 `Panel_PetStatus` 的 `profile` 贴图、宠物正面待机预览 Sprite、雷达配色与尺寸微调已撤回；当前公寓 viewport 已改挂到 `Panel_SpaceSys`，`Profile` 重新只承担双宠资料展示。具体 UI 美术资源选择、贴图映射与最终视觉作者化后续由人工完成，AI 后续只继续承接弱视觉或非美术资源相关的逻辑、结构、输入桥接、验证与文档任务。同日已完成一轮非美术 UI 技术收口：`ApartmentViewportInputBridge` 增加矩形内点击判定与可测试坐标转换，`SidebarController` / `StubPanelBase` 增加 `IUIRouter` / `EventBus` 兜底注册，`ProfilePanelStub` / `TarotPanelStub` / `InventoryPanelStub` 增加服务缺失或空数据兜底；`Assets/_Project/Prefabs/UI/Panels` 与 `Assets/_Project/Prefabs/UI/Widgets` 已建立目录说明但尚无正式 UI prefab。
8. `2026-05-22` 起，Apartment 场景中的 `Pet` 根节点已同时包含 `Pet_Angel` 与 `Pet_Devil`；当前玩家控制方式为“默认天使可控，点击恶魔后切换恶魔主控”，不再是简单复制输入组件后让双宠同时吃同一套方向键；未被选中的桌宠当前会保持待机，不再继续跑自动睡觉链路。当前 `Pet_Angel` 与 `Pet_Devil` 也不再共用同一个移动边界：恶魔已切到左侧专用 `PetMovementBounds_Devil`。
9. `Assets/_Project/Animations/Pet/` 当前除 3 个 move clip 外，已新增 `Pet_Angel_Interact_Read.anim` 与 `Pet_Angel_Interact_BesideDoor.anim`，并新增 `Pet_Devil_Move_* / Pet_Devil_Idle_* / Pet_Devil_Sleep.anim`、`Pet_Devil_Interact_BesideDoor.anim`、`Pet_Devil_Interact_Write.anim` 与 `Pet_Devil_Interact_PlayingMusic.anim`；但恶魔其余完整交互动画仍未补齐。
10. `2026-05-25` 起，Apartment 场景已开始搭第一版 `viewport` 结构：当前 `ApartmentViewportHost` 与 `ApartmentViewportImage` 已归属 `Panel_SpaceSys/Content`，并新增 `ArtGenerated/ApartmentViewportCamera`；当前 `RenderTexture` 资产路径为 `Assets/_Project/Settings/RenderTextures/ApartmentViewport_RT.renderTexture`。同日已补 `ApartmentViewportInputBridge`，当前可把 viewport 内点击先桥接到桌宠点击，再桥接到当前宠物的家具交互链路；当 `BuildModeController` 开启时，也会优先桥接到建造模式的放置/删除家具入口。
11. `2026-07-28` 起，`WorldMap_Main.unity` 中桥对象 `桥` 的过桥移动轮廓由同物体 `PolygonCollider2D` 上侧轮廓直接提供。`WalkableSurface` 是运行时读取入口，`WorldMapSceneObjectsPatch` 只确保桥对象有 `WalkableSurface` 并保留现有 collider 点位，不再维护 `_profileLocalPoints` 独立折线轨道。`PetController.ResolveGroundY` 会把桥面 surface Y 当作脚底/行走锚点高度，再换算成 transform Y。相关 PlayMode 表现仍需按 `docs/manual-validation-checklist.md` 的 B9 章节人工补验。
12. `2026-07-29` 起，WorldMap 情绪花图鉴列表页与详情页的美术资源入口为 `Assets/_Project/Art/WorldMap/flowerCodex` 与 `Assets/_Project/Art/WorldMap/flower_info`。运行时入口是 `FlowerCollectionPanelStub`，编辑器作者化入口是 `WorldMapEmotionGardenUIPatch.SetupFlowerCollectionBookContent`。设计要求是 Scene/Inspector 可调：`Panel_EmotionCollection/Content` 下应由 `CodexView`、`DetailView`、书本背景、卡槽、按钮和文本字段这些真实 UI 子节点组成；运行时只填数据和切换视图。本轮已通过 `tools/run-unity-editor-method.ps1` 在 Unity batchmode 中执行 `WorldMapEmotionGardenUIPatch.Patch()` 并落盘 `WorldMap_Main.unity`，场景 YAML 已能命中 `CodexView`、`DetailView`、`CodexCardSlot_00`、`StockPlate` 等节点；PlayMode 点击和最终视觉微调仍需在 Unity 中补验。
13. `Assets/_Project/Art/Sprites/Pet/Frames/Move/` 当前已经从旧的平铺命名，切换为 `正面 / 背面 / 侧面` 三个子目录；对应导入链路由 `PetMoveAnimationSetupEditor` 兼容新旧两套来源。
14. `Assets/_Project/Art/Sprites/Pet/Frames/Interact/` 当前两组交互帧已经统一改为规范命名：`Pet_Angel_Interact_Read_0001...` 与 `Pet_Angel_Interact_BesideDoor_0001...`，不再使用 `IMG_986x.PNG`。
14. `2026-06-02` 起，项目已补一条 Windows 构建防护：`Assets/_Project/Scripts/Editor/Build/McpNuGetPlayerImportGuard.cs` 会把 `Assets/Plugins/NuGet` 下由 Unity MCP 依赖解析器解压出的 `McpPlugin / SignalR / Microsoft.Extensions.*` DLL 统一校正为 `Editor-only`，避免它们以 Player 插件身份进入 Bee / Burst 构建链。
15. `2026-06-02` 同日，项目又把 4 个 Unity MCP 包临时从 `Packages/` 移到了 `PackageBackups/MCP-disabled-2026-06-02/`，并从 `Packages/manifest.json` 取消引用；当前保留不动的是 `Packages/SkillsForUnity`。
16. `2026-06-02` 同日，项目还把 `Assets/Plugins/NuGet` 整个目录及其 `.meta` 软移出到了 `PackageBackups/NuGet-disabled-2026-06-02/`，并清空了 `ProjectSettings/ProjectSettings.asset` 里的 `Standalone` `UNITY_MCP_READY`，用于让 Windows Build 不再继续命中这批 MCP NuGet 残留 DLL。
17. `Apartment_Main.unity` 当前并不是所有“看起来像家具”的对象都天然进入家具逻辑；首轮显式接线通过 `ApartmentSceneFurnitureBindings` 给关键对象补 `Furniture` / `InteractionAnchor` / `SceneFurnitureDefinitionHint`。
18. `ApartmentSceneFurnitureBindings` 当前已经覆盖公寓场景里主要可交互对象，但仍要注意两类现实区别：
   - 有些对象已经进入对象级交互类型，却未必已经摆进 `Apartment_Main.unity`
   - 场景绑定里的定义 ID、类别和交互类型需要持续与真实 Sprite 资源名保持一致，不能把 `WorkDesk` 类资源误绑成装饰类
19. `Pet` 模块当前需要区分“长期规划”和“现阶段入口”：
   - 长期规划仍保留 HFSM、自主行为、Gateway / Travel / AI 对话方向
   - 当前 Apartment 原型里，`Pet_Angel` 的主要行动入口已经切换为 `PetPlayerInputController`，由玩家通过 `WASD` / 方向键直接控制移动
   - 当前 Apartment 原型还新增了 `PetPlayerFurnitureInteractionController`，用于靠近特定家具或交互点时按 `F` 触发玩家手动交互
   - 当前 Apartment 原型还新增了 `PetClickReactionController`，用于鼠标左键点击桌宠后输出表情 Debug，并显示本地语料气泡回复
   - 当场景里同时存在 `Pet_Angel` 与 `Pet_Devil` 时，点击桌宠不仅会触发气泡回应，也会显式切换当前键盘控制对象
   - 当前恶魔的 `门边` 交互已沿用天使同一条 `Interact_BesideDoor` 触发链路，但 controller 已切到恶魔自己的 `Pet_Devil_Interact_BesideDoor.anim`
   - `2026-05-27` 起，恶魔当前还额外拥有两条玩家自交互接线：`画画` 会对着 `家具_休闲_画架_恶魔_01` 触发并坐到 `家具_装饰_椅子_恶魔_01`，`玩掌机` 会坐到 `家具_装饰_沙发_恶魔_02`
20. `Apartment_Main.unity` 当前会用 `StaticFurnitureDecorOnly` 承载一部分“已有独立 Sprite、但不直接走原始关卡对象”的静态家具；这类对象进入交互系统时，也要同步补进 `ApartmentSceneFurnitureBindings`，避免出现“场景有图但无交互绑定”或“绑定有定义但 `_target` 为空”。
21. 当前工作流已开始显式区分三层记忆：
   - `L1`：`docs/current-task-card.md`
   - `L2`：`docs/ai-memory/`
   - `L3`：git / PR / 长文档历史
22. 当前工作流已开始显式区分任务上下文包，入口在 `docs/workflow-context-packages.md`。
23. 涉及视觉、布局、UI、相机、装饰层的任务时，默认要求：
   - `Scene` 视图可直接看到
   - `Inspector` 可直接调整
   - `Play` 视图与 `Scene` 视图效果一致
   - 不依赖运行时脚本临时拼出最终视觉
24. `2026-07-12` 起，HubUI 面板（SaveSlotsPanel、ReadingBubble、TarotSummaryPreview）的编辑器预览系统已收口：
   - `SaveSlotsPanel` 现在使用 `[ExecuteAlways]` + 模板克隆模式：场景中的 `SlotTemplate` 为 inactive 模板，运行时和编辑器预览均通过 `Instantiate` 克隆，用户只需编辑模板即可统一修改所有槽位的美术资源
   - `DebugDisplayWindow` 的 Tarot Preview 开关现在会联动刷新场景中 `ReadingBubble` / `TarotSummaryPreview` 的 active 状态
   - 新增 `ReadingBubbleLayoutSync` 工具用于按 Angel/Devil 分组同步气泡布局
   - 新增 `SaveSlotTemplateCreator` 工具用于在场景中创建/更新 SlotTemplate
   - 新增长期规则 #12：禁止在未经用户确认的情况下修改 Unity scene 文件或场景对象（含编辑器回调中的隐式修改）

## 模块 README 导航
- `Assets/_Project/Scripts/Modules/Pet/README.md`
- `Assets/_Project/Scripts/Modules/Furniture/README.md`
- `Assets/_Project/Scripts/Modules/Navigation/README.md`
- `Assets/_Project/Scripts/Modules/Gateway/README.md`
- `Assets/_Project/Scripts/Modules/Travel/README.md`
- `Assets/_Project/Scripts/Modules/Desktop/README.md`
- `Assets/_Project/Scripts/Modules/Persistence/README.md`

## 什么时候刷新这个文件
出现以下任一情况就要更新：
- 新增或删除关键目录
- 场景、Prefab、SO 的真实落地状态变化
- 工具入口变化
- `AGENTS.md` 入口协议变化
- 目录或文件名改动导致现有导航失效
