# Gemini-Lab Memory Main

Updated: 2026-05-08

## 定位
这份文档是 Gemini-Lab 的长期项目记忆总览。

`AGENTS.md` 已经作为总入口落地；本文件承担“第二入口 + 主记忆总览”的作用。

## 快速导航
- [AGENTS.md](../../AGENTS.md)
- [当前任务卡](../current-task-card.md)
- [当前任务卡 JSON](../current-task-card.json)
- [上下文包](../workflow-context-packages.md)
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
- [上下文压缩与知识沉淀计划](../context-compression-and-knowledge-plan.md)
- [做梦整理清单](../dream-maintenance-checklist.md)
- [记忆索引](./memory-index.paths.txt)

## Workspace Identity
- 项目名称：Gemini-Lab
- 项目类型：Unity 2D 桌宠客户端（长期目标仍保留 AI 陪伴方向）
- Unity 版本：`2022.3.62f3c1`
- 当前阶段目标体验：让宠物在公寓场景中完成基础移动、家具展示、交互与状态显示；现阶段暂不接入大模型，桌宠主要由玩家直接控制移动
- 当前协作工具：Unity MCP、嵌入式 Unity Skills、`.cursor/skills/` 与 `.agents/skills/`

## 记忆分层
- L1 当前任务卡：`docs/current-task-card.md`
  - 只保存当前这一轮任务目标、边界、完成标准和明确不做项。
- L2 常驻项目记忆：`docs/ai-memory/`
  - 保存长期稳定规则、结构、文件导航、历史决策。
- L3 完整历史：
  - git 历史、PR 历史、长文档与旧阶段记录。
  - 仅支持搜索，不默认整段加载。

## 当前工作方式升级
- 当前项目已开始把智能体工作方式从“长会话自由推进”收口为：
  - 新需求先复述理解
  - 用户确认后再执行
  - 当前任务只做当前任务
  - 发现别的问题只提醒，不顺带处理
- 当前默认使用“探索 → 规划 → 行动”三段式：
  - 探索：只读检查，不改文件
  - 规划：复述理解、列边界、等待确认
  - 行动：确认后才改文件、改场景、执行 git
- 当前推荐按任务类型装配最小必要上下文，不默认把所有项目文档都当作当前任务上下文；具体见 `docs/workflow-context-packages.md`。
- 当前已启用最小闭环任务闸门：
  - `docs/current-task-card.md`
  - `docs/current-task-card.json`
  - `tools/check-task-gate.ps1`
- 渐进式上下文压缩已纳入设计，但当前默认不启用自动压缩；现阶段只保留人工分层方案。
- 做梦整理、L2 技能手册、L3 知识库已纳入第二部分建设，当前以人工整理、skill 沉淀和索引增强为主。
- 第一批 workflow skill 已开始落地：
  - `git-sync-upstream-main`
  - 用于安全同步本地 `main` 到 `ULookup:main`，并在同步后切回用户原来的工作分支
  - `unity-clear-generated-cache`
  - 用于只清理 `Library/ScriptAssemblies`、`Library/Bee`、`Temp`，让 Unity 在当前源码状态下重新完整编译
  - `apartment-scene-rollback-to-commit`
  - 用于只回退 `Apartment_Main.unity` 到指定 commit，避免把场景回退误扩大成整个项目回退
  - `pet-animation-reference-rebuild`
  - 用于只修复 `Pet_Angel` 的动画资源、clip、controller 与场景绑定引用链
  - `furniture-binding-check`
  - 用于只读盘点 Apartment 家具绑定状态，并明确区分脚本层、场景层与资源层问题

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
