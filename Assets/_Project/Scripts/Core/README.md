# Scripts/Core/ — 底层基础设施

## 文件夹职责
提供**与具体业务无关、可被所有模块复用**的基础设施代码（HFSM、事件总线、命令模式、ServiceLocator、日志、工具类）。

## 核心文件/类（预估）

| 子目录 / 文件 | 说明 |
| :--- | :--- |
| `FSM/IState.cs` | 状态接口：`Enter / Tick / FixedTick / Exit`。 |
| `FSM/StateMachine.cs` | 通用状态机泛型容器 `StateMachine<TContext>`。 |
| `FSM/SubStateMachine.cs` | 分层 HFSM 支持。 |
| `FSM/StateTransition.cs` | 带条件的状态转移规则。 |
| `Events/EventBus.cs` | 全局事件总线（基于类型 Key 的发布/订阅）。 |
| `Events/ICommand.cs`、`CommandDispatcher.cs` | 事件驱动的命令模式实现。 |
| `ServiceLocator.cs` | 轻量 IoC 容器，注册运行期单例服务。 |
| `GameBootstrap.cs` | 启动引导入口，负责按序初始化核心服务，并在 Boot 场景完成后自动跳转到 `MainMenu`。 |
| `SceneFlow/SceneId.cs` | 全部互斥场景的逻辑标识枚举（Boot / MainMenu / Apartment / WorldMap / DesktopOverlay）。 |
| `SceneFlow/ISceneCatalog.cs`、`DefaultSceneCatalog.cs` | `SceneId → Unity scene name` 映射；scene 名变更只改 catalog。 |
| `SceneFlow/ISceneFlowService.cs`、`SceneFlowService.cs` | 业务代码切换场景的唯一入口；通过 EventBus 广播 `SceneLoadStartedEvent` / `SceneLoadCompletedEvent`。 |
| `SceneFlow/SceneTransitionPayload.cs` | 跨场景一次性数据包（例：进入 WorldMap 时附带 spawn id）。 |
| `UI/PanelId.cs` | UI 面板的逻辑标识（SaveSlots / Settings / PetStatus / Tarot / Collection / Inventory …）。 |
| `UI/IUIPanel.cs`、`IUIRouter.cs`、`UIRouter.cs` | 面板栈路由；面板 Prefab 在 Awake 时 Register，业务代码只通过 `Open(PanelId)` 发意图。 |
| `Utils/` | `CoroutineRunner`、`MainThreadDispatcher`、`ObjectPool<T>`、`Logger` 等工具。 |
| `Interfaces/` | 跨模块共享的抽象接口（如 `ISaveable`、`ITickable`）。 |

### 场景切换硬规则
- 业务代码 **禁止** 直接调用 `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync` 等 API。
- 切换场景只能通过 `ISceneFlowService.LoadAsync(SceneId)`；如需在切换时传递少量数据，使用 `SceneTransitionPayload`。
- 全局服务（EventBus / SceneFlowService / UIRouter / Gateway 运行态等）挂在 Boot.unity 的 BootstrapRoot 下，`DontDestroyOnLoad` 跨场景存活；场景内的 *Bootstrap 只注册本场景生命周期内的服务。

### UI Router 硬规则
- 侧边栏 / 主菜单等 UI 入口只发意图：`uiRouter.Open(PanelId.X)`，禁止直接 `SetActive(true)`。
- 面板 Prefab 统一实现 `IUIPanel`，在 Awake 时 `Register`、OnDestroy 时 `Unregister`。
- 面板之间不得互相直引；跨面板数据通过 `payload` 或 EventBus 传递。

## 依赖关系
- **对外**：不依赖任何其他业务模块或 UI。可依赖 Unity 核心 API (`UnityEngine`、`UnityEngine.Events`) 与少量稳定第三方库。
- **对内**：被 `Scripts/Modules/**` 与 `Scripts/UI/**` 大量引用。
- asmdef 命名：`GeminiLab.Core`。

## 代码规范/注意事项
1. **零业务耦合**：不得出现"宠物""家具""网关"等业务词汇；只提供泛型/抽象能力。
2. **零 GC 敏感**：事件总线、对象池需提供 struct event 版本，避免 `Update` 热路径分配。
3. **单元测试覆盖率 ≥ 80%**，所有公开 API 必须有 EditMode 测试。
4. **禁止 `MonoBehaviour` 承载纯逻辑**；除非必要（如 `CoroutineRunner`），否则使用 POCO + 接口。
5. 接口命名以 `I` 开头；抽象基类以 `Abstract` 或 `Base` 前缀可选，保持团队一致。
