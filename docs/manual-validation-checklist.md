# Gemini-Lab 人工验证清单

Updated: 2026-05-10

## 使用方式
- 人工验证后直接在“结果”列填写：`通过` / `不通过` / `未验证`
- 如有问题，把现象写进“备注”列
- 智能体后续修问题时，优先读取这份清单，而不是重新口头追问

## A. 当前文档与工程骨架
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `AGENTS.md` 存在且可作为项目总入口阅读 |  |  |
| `docs/` 目录存在且可正常打开 |  |  |
| `docs/ai-memory/` 中主记忆、架构、规则历史、开发手册、文件指南都存在 |  |  |
| `docs/skill-design-boundary.md` 存在且内容与当前项目 skill 组织一致 |  |  |
| 文档中文显示正常，无乱码 |  |  |
| `memory-index.paths.txt` 中列出的路径都存在或说明清楚 |  |  |
| `README.md`、`Assets/README.md`、`Assets/plan.md` 可正常阅读 |  |  |
| 当前项目版本确认为 `Unity 2022.3.62f3c1` |  |  |
| `.cursor/mcp.json` 与技能目录可正常访问 |  |  |

## B. Phase 1 基础工程与 FSM
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `Core` 目录已开始落真实 C# 代码 |  |  |
| asmdef 已按层级拆分完成 |  |  |
| `GameBootstrap` 已存在并能作为启动入口 |  |  |
| FSM 核心类已实现并可编译 |  |  |
| 宠物能在空场景中完成基本状态切换 |  |  |
| EditMode 测试可以运行 |  |  |

## B2. 框架 + 场景切换（P0 2026-05-10 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 从 `Boot.unity` 点 Play 后自动跳到 `MainMenu` |  |  |
| `MainMenu` 的"开始"按钮点击后进入 `Apartment_Main` |  |  |
| `MainMenu` 的"存档"按钮打开 SaveSlots 面板（当前骨架尚未注册可见 Panel 属正常） |  |  |
| `MainMenu` 的"设置"按钮打开 Settings 面板（同上） |  |  |
| `Apartment_Main` 的右上 `UI_WorldMapPortal` 按钮可切到 `WorldMap_Main` |  |  |
| `WorldMap_Main` 的左上 `Return` 按钮可切回 `Apartment_Main` |  |  |
| `WorldMap_Main` 中 `A` / `D` 键可左右平移摄像头；鼠标右键拖拽同理 |  |  |
| 在任意场景按 `F10` 可在公寓与 Desktop Overlay 之间切换（`DesktopOverlayManager`） |  |  |
| 跨场景 `F10` 不再丢失 manager（切回 Apartment 后 F10 仍响应） |  |  |
| EditorBuildSettings 顺序为 `Boot(0) / MainMenu / Apartment_Main / WorldMap_Main / Desktop_Overlay` |  |  |
| `DesktopOverlayManagerEditModeTests` 通过 |  |  |
| Console 无 `CS` 编译错误（CJK 字形 □ 警告在补 CJK 字体前可接受） |  |  |

## B3. 塔罗垂直切片（B1 2026-05-10 新增）
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 打开 Apartment → 侧边栏点"每日塔罗"，面板显示"抽今日塔罗"按钮 |  |  |
| 点按钮后卡面刷新、显示牌名 + 正/逆位、按钮变"今天已抽过，明天再来" |  |  |
| 右侧 AngelBubble 显示"（天使视角）… 愿光照亮你…" 或 Gateway 真实回复 |  |  |
| 右侧 DevilBubble 显示"（恶魔视角）… 别被光晃得太舒服…" 或 Gateway 真实回复 |  |  |
| 关闭 Panel 再打开，按钮保持"明天再来"状态（今日已抽过） |  |  |
| 次日重启游戏，按钮恢复成"抽今日塔罗" |  |  |
| Console 无 `TarotBootstrap` ERROR；看到 `[TarotBootstrap] TarotService registered.` |  |  |

## C. Phase 2 家具、建造与导航
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 公寓主场景已创建 |  |  |
| 家具 Prefab 已开始落地 |  |  |
| `Assets/_Project/Prefabs/Furniture/**` 中已存在当前全部家具 Sprite 资源对应的真实 prefab 资产 |  |  |
| `Assets/_Project/ScriptableObjects/FurnitureConfig/**` 中已存在当前全部家具 Sprite 资源对应的真实 `FurnitureDefinitionSO` 资产 |  |  |
| 长按 `V` 的建造模式可进入 / 退出 |  |  |
| 家具能正确吸附地板或墙体 |  |  |
| 家具放置合法性校验可用 |  |  |
| 宠物可对家具发起自主交互 |  |  |
| 家具摆放后导航系统能正确更新 |  |  |
| `Furniture/StaticFurnitureDecorOnly` 下的纯静态补景对象与现有可交互家具不会明显重叠或错位 |  |  |
| `Furniture` 根上的 `ApartmentSceneFurnitureBindings` 已把当前主要可交互对象正确接入家具系统，且不存在重复或空 `_target` 绑定 |  | 当前至少应覆盖首批关键家具，并扩展到镜子、地毯、园地毯、沙发、凳子、椅子、纸张、耳机、音响、柜体、窗台、玩偶、枕头、小圆镜、羽翼边柜、左下小家具、左下窄家具、恶魔盆栽、照片板等对象 |
| `SceneFurnitureDefinitionHint` 配置的类别和 Buff 会优先于名称推断生效 |  |  |
| 睡眠交互 / 装饰观察 / 休闲交互 三种交互类型会正确传递到运行时摘要与状态面板 |  |  |
| 镜子 / 地毯 / 沙发 / 凳子 / 椅子 / 画架 / 照片板 已进入对象级交互链路，且在场景中位置可达、不会明显错位 |  |  |
| 窗台 / 床上玩偶 / 沙发上枕头 已使用更贴近对象语义的交互类型，而不是继续借用植物或沙发语义 |  |  |

## D. Phase 3 Gateway 与 AI 交互
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| Gateway 配置资产已落地 |  |  |
| 聊天模式请求链路可用 |  |  |
| 工作模式请求链路可用 |  |  |
| 响应中的 `traceId` 可追踪 |  |  |
| 断网 / 超时 / 重试策略可验证 |  |  |
| 敏感字段未出现在日志中 |  |  |

## E. Phase 4 UI、桌面 Overlay 与旅行
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 聊天面板、状态面板、库存 UI 可正常显示 |  |  |
| 桌面 Overlay 模式可以进入 |  |  |
| 点击穿透可切换且行为正确 |  |  |
| 前台应用感知功能行为符合预期 |  |  |
| 旅行指令可以触发离场与回归 |  |  |
| 相册与旅行小记可展示 |  |  |
| 公寓模式与 Overlay 模式切换正常 |  |  |

## F. 美术与资源替换
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 新 Sprite 放置在 `_Project/Art/` 对应目录 |  |  |
| 第三方资源没有误放进 `_Project/Art/` |  |  |
| 新资源命名符合规范 |  |  |
| Sprite 导入设置符合项目约定 |  |  |
| SpriteAtlas / RuleTile / Animator / Prefab / SO 引用已同步更新 |  |  |
| 替换后场景中无丢图、错位或排序错误 |  |  |

## G. 文档同步
| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 文件结构变化后已刷新主记忆与文件指南 |  |  |
| 场景结构变化后已更新结构总览 |  |  |
| 玩法变化后已更新玩法规范 |  |  |
| 手工验证结果已回写本清单 |  |  |

## H. Apartment 物件交互与状态显示联调
适用范围：
- 目标场景：`Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
- 只验证当前已有美术资源支撑的内容
- 当前应至少覆盖：`Bed_Angel`、`Nightstand_Angel`、`Harp_Angel`、`WorkDesk_Angel`、`WorkDesk_Devil`
- 暂不要求验证缺失美术资源对应的更完整交互动画或正式人格雷达美术

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| 打开 `Apartment_Main.unity` 后场景能正常进入 PlayMode |  |  |
| 场景中的现成家具对象会被 `FurnitureService` 注册，而不是只有运行时新摆放家具才能交互 |  |  |
| 宠物在空闲态会优先选择现有 `Leisure` / `Bed` 类家具发起自主交互 |  |  |
| 未下发工作请求时，宠物不会把 `WorkDesk` 当作默认自主交互目标 |  |  |
| 白天中等能量时，宠物会优先选择 `Leisure`，而不会过早跑向 `Bed` |  |  |
| 夜间中等能量时，宠物比白天更容易把 `Bed` 当作自主目标 |  |  |
| 宠物移动到家具后会进入 `Interacting`，并能在交互后回到 `Idle` |  |  |
| 与床交互后，状态面板中的 `Energy` 有提升或符合预期变化 |  |  |
| 与竖琴/休闲家具交互后，状态面板中的 `Mood` 有提升或符合预期变化 |  |  |
| 最近一次交互结果会显示在左上角状态面板的 `Last Interaction` 文本中 |  |  |
| 左上角状态面板会实时显示 `State / Mood / Energy / Satiety / Target / Work` |  |  |
| 右侧库存面板会显示当前 build palette 条目，且包含已有资源对应家具定义 |  |  |
| 按 `V` 可切换建造模式，`Tab` 可轮换 palette，左键摆放、右键移除可用 |  |  |
| 摆放或移除家具后，右侧库存面板中的 `Placed` 数量会同步变化 |  |  |
| 摆放墙面类家具时，其排序表现高于普通地面家具，未出现明显遮挡错误 |  |  |
| 右下角概览面板会显示 `Mood / Energy / Satiety / Work Focus` 百分比文本 |  |  |
| `WorkDesk` 仍可作为工作目标使用，不影响已有工作链路 |  |  |
| 当前文本面板可以工作，即使还没有正式雷达图 / 交互动画美术，也不会阻断本轮联调 |  |  |

## I. Pet 基础动画资源补齐范围确认
适用范围：
- 目标资源目录：`Assets/_Project/Art/Sprites/Pet/Frames/`
- 目标动画目录：`Assets/_Project/Animations/Pet/`
- 目标场景对象：`Assets/_Project/Scenes/Apartment/Apartment_Main.unity` 中的 `Pet_Angel`
- 本节先确认“现有资源可以直接支持哪些补齐项”，不要求本节内直接产出缺失美术

当前确认事实：
- `Move/` 目录当前已切换为三组真实序列帧子目录：`正面 / 背面 / 侧面`
- `Idle/`、`Emotion/` 目录当前为空；`Interact/` 已新增 `read/` 与 `beside door/` 两组状态帧
- 当前已有 `Pet_Angel_Move_Front.anim`、`Pet_Angel_Move_Back.anim`、`Pet_Angel_Move_Side.anim`
- 当前已有 `Pet_Angel.controller`，但其中只有 `Move_Front / Move_Back / Move_Side`
- `Apartment_Main.unity` 里的 `Pet_Angel` 目前还没有实际 `Animator` 组件挂载记录
- 当前编辑器工具 `PetMoveAnimationSetupEditor` 已可优先读取 `正面 / 背面 / 侧面` 子目录，并保留旧命名规则兜底

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `Frames/Move/正面`、`背面`、`侧面` 序列帧目录存在且编号连续、无缺帧 |  |  |
| `Pet_Angel_Move_Front.anim` 可对应 `正面` 6 帧移动序列 |  |  |
| `Pet_Angel_Move_Back.anim` 可对应 `背面` 6 帧移动序列 |  |  |
| `Pet_Angel_Move_Side.anim` 可对应 `侧面` 6 帧移动序列 |  |  |
| `Pet_Angel.controller` 当前只包含 `Move_Front / Move_Back / Move_Side` 三个状态 |  |  |
| 当前 controller 参数与 `PetController` 驱动保持一致：`IsMoving / MoveX / MoveY / MoveDir` |  |  |
| `PetController` 当前会用 `Move_Front / Move_Back / Move_Side` 实现四方向移动表现：前后分离、左右共用侧面并通过 `flipX` 翻转 |  |  |
| `Apartment_Main.unity` 中的 `Pet_Angel` 已绑定 `Pet_Angel.controller` 引用 |  |  |
| `Pet_Angel` 在进入 PlayMode 后会自动补 `Animator` 并使用现有 Move controller |  |  |
| 使用现有资源可以直接进入补齐范围的内容：移动动画 clip 校验、controller 整理、场景 `Animator` 挂载与移动表现验证 |  |  |
| 使用现有资源暂时不能直接完成的内容：独立 `Idle` 序列、独立 `Interact` 序列、独立 `Emotion` 序列 |  |  |
| 若不新增美术，本轮不应把 `Idle / Interact / Emotion` 扩展成“正式完整资源交付” |  |  |
| 后续若进入正式制作，优先顺序应为：先挂 `Animator` 并验证 Move，再补 `Idle`，最后补 `Interact / Emotion` |  |  |

## J. Pet 新增交互动画接线
适用范围：
- 新增状态帧目录：`Assets/_Project/Art/Sprites/Pet/Frames/Interact/read/`
- 新增状态帧目录：`Assets/_Project/Art/Sprites/Pet/Frames/Interact/beside door/`
- 新增动画资产：`Assets/_Project/Animations/Pet/Pet_Angel_Interact_Read.anim`
- 新增动画资产：`Assets/_Project/Animations/Pet/Pet_Angel_Interact_BesideDoor.anim`
- 规则：两个状态的首尾帧都应保持 10 帧时长

| 检查项 | 结果 | 备注 |
| :--- | :--- | :--- |
| `read` 状态序列帧已按当前资源目录完整纳入 clip |  |  |
| `beside door` 状态序列帧已按当前资源目录完整纳入 clip |  |  |
| `read` 状态帧当前命名符合 `Pet_Angel_Interact_Read_0001.png` 规则 |  |  |
| `beside door` 状态帧当前命名符合 `Pet_Angel_Interact_BesideDoor_0001.png` 规则 |  |  |
| `Pet_Angel_Interact_Read.anim` 的首帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_Read.anim` 的尾帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_BesideDoor.anim` 的首帧保持时长为 10 帧 |  |  |
| `Pet_Angel_Interact_BesideDoor.anim` 的尾帧保持时长为 10 帧 |  |  |
| `Pet_Angel.controller` 已包含 `Interact_Read` 和 `Interact_BesideDoor` 状态 |  |  |
| `PetController` 在 `Interacting` 时会根据目标家具类别切换到新增交互动画状态 |  |  |
| `WorkDesk` 与 `Leisure` 目标当前使用 `Interact_Read` 作为已有资源下的临时交互表现 |  |  |
| `Decoration` 目标当前使用 `Interact_BesideDoor` 作为已有资源下的临时交互表现 |  |  |
| `Apartment_Main.unity` 继续使用现有环境贴图，未擅自把 `公寓场景.psd` 替换进场景 |  |  |
