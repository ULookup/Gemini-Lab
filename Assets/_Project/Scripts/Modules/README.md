# Scripts/Modules/ — 业务模块总览

## 文件夹职责
承载 Gemini-Lab 的**全部业务领域逻辑**。每个子目录 = 一个垂直业务模块，拥有独立 asmdef，模块间仅通过 `Core` 提供的 `EventBus / ServiceLocator / 接口` 交互。

## 子模块地图

| 子目录 | 领域 | 对应 README 章节 |
| :--- | :--- | :--- |
| `Pet/` | 宠物状态值、性格矩阵、养成、FSM 绑定（双宠 Angel/Devil 后续接入） | 养成系统 |
| `Furniture/` | V-Decor 建造、家具数据、环境 Buff、羁绊 | V-Decor |
| `Navigation/` | NavMesh 动态烘焙、路径请求、障碍物管理 | 寻路 |
| `Gateway/` | OpenClaw 通信、Prompt 组装、事件路由 | 大模型链路 |
| `Travel/` | 旅行指令、异步小记/照片拉取、回归奖励 | 旅行系统 |
| `DesktopOverlay/` | 桌面透明 Overlay、前台应用感知、跨屏陪伴（切场景走 `ISceneFlowService`，自身 DontDestroyOnLoad） | 桌面场景 |
| `Persistence/` | 存档/读档、JSON 序列化、云同步占位 | 持久化 |
| `UI/` | 聊天、状态、性格雷达、家具库存等视图层；`Catalogs/` 提供 `UIArtCatalogSO` / `UIFontCatalogSO` 解耦美术资源 | UI |
| `MainMenu/` | 主菜单控制器（开始 / 存档 / 设置），点击"开始"走 `SceneFlowService.LoadAsync(SceneId.Apartment)` | 主菜单 |
| `HubUI/` | 可在 Apartment 与 WorldMap 共用的侧边栏、4 个占位 Panel、通往 WorldMap 的 Portal 与 `StubPanelBase` | Hub UI |
| `WorldMap/` | 2D 横板摄像头控制 + 返回公寓出口；花园 / 种地等玩法后续在 `GardenService` 中补齐 | 大地图 |
| `Tarot/` | 每日塔罗：SO（卡牌/牌堆）、Service（抽卡+每日限制）、Backend（Gateway 人格 prompt / LocalFallback）、Bootstrap；双宠人格切换在这里 | 塔罗 |

## 依赖关系（允许的引用方向）

```
Pet  ◄──────── Furniture   (Pet 消费 Buff；Furniture 不反向依赖 Pet 实例)
 ▲                ▲
 │                │
 └── Gateway ─────┘
 ▲
 │
Travel、Desktop、Persistence → Pet / Gateway 对外接口
```

- 所有模块 → `Core`：允许。
- 模块 → 模块：仅允许依赖对方暴露的**接口或事件**，禁止引用具体类。
- 模块 → `UI`：**禁止**。UI 反向订阅模块事件。

## 代码规范/注意事项
1. 每个子模块必须含 `*Service.cs`（对外门面）与 `*Installer.cs`（在 `GameBootstrap` 注册服务）。
2. 模块对外仅暴露接口 (`IXxxService`) + 事件 (`XxxEvent`)，具体实现 `internal`。
3. 任何跨模块调用都要经过 `EventBus` / 接口；**发现 `using GeminiLab.Modules.Xxx.Internal`** 一律拒 PR。
4. 新增模块必须配套 asmdef、单元测试目录 `Tests/` 与本级 README。
