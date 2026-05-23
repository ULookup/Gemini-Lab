# Apartment 全屏面板设计

**日期**: 2026-05-21  
**状态**: 已确认

## 目标

将 Apartment 场景中侧边栏面板从"居中叠加层（900×620）"改为"全屏面板"，侧边栏保持最上层可见。

## 当前架构

```
UI_Sidebar Canvas (sortingOrder: 100, ScreenSpaceOverlay, 1920×1080)
├── Sidebar (240px 宽, 左侧, 展开/收起 + 5 个 Tab)
├── Panel_PetStatus   (居中 900×620, Content 显隐)
├── Panel_Tarot
├── Panel_Collection
├── Panel_Inventory
├── Panel_Garden
└── UI_WorldMapPortal
```

- **SidebarController**: 监听 Tab 点击 → 调用 `IUIRouter.Open(PanelId)`
- **UIRouter**: 内存级面板栈，管理 Register/Unregister/Open/Close
- **StubPanelBase**: 面板基类，`OnOpen` 显示 Content，`OnClose` 隐藏 Content

## 目标架构

### Canvas 分层

```
UI_Sidebar Canvas (sortingOrder: 100)
├── Panel_PetStatus   ← stretch 全屏 (anchor 0,0→1,1)
├── Panel_Tarot
├── Panel_Collection
├── Panel_Inventory
├── Panel_Garden
└── SidebarOverlay Canvas (sortingOrder: 101, 子 Canvas)
    ├── Sidebar (64px 宽, 左侧, 展开/收起 + 5 个 Tab)
    └── UI_WorldMapPortal
```

侧边栏与传送门按钮移入一个**子 Canvas**（`UI_Sidebar` 下的子 GameObject `SidebarOverlay`，挂独立 Canvas 组件，`overrideSorting = true`, `sortingOrder = 101`），确保始终渲染在面板之上。

### 面板布局

- RectTransform: `anchorMin = (0,0)`, `anchorMax = (1,1)`, `pivot = (0,0)`, offset/sizeDelta 归零 → 撑满屏幕
- 内容区左侧 padding ~72px（避开 64px 侧边栏 + 8px 间隙）
- 每个面板右上角统一加关闭按钮 ✕

### 交互逻辑

| 操作 | 行为 |
|---|---|
| 点击 Tab | 打开对应面板，全屏显示 |
| 再次点击已激活的 Tab | 关闭面板（toggle） |
| 点击不同 Tab | 关闭当前面板，打开新面板（同时只有一个面板打开） |
| 点击面板 ✕ | 关闭面板 |
| 侧边栏折叠（Toggle） | 不影响面板，面板仍全屏 |

### SidebarController 改动

- 新增 `_activePanelId`（`PanelId?`）跟踪当前打开的面板
- 新增 `EventBus? _eventBus` 字段，在 `Awake` 中通过 `ServiceLocator.TryResolve` 获取
- `Awake` 中订阅 `_eventBus.Subscribe<UIPanelClosedEvent>(OnPanelClosed)`, `OnDestroy` 中 Dispose
- `OnPanelClosed`: 若 closed id == `_activePanelId`，清零 `_activePanelId` 并取消 Tab 高亮
- `OpenPanel`: 已激活则关闭（toggle），否则关旧开新（**行为变更**: 同时只允许一个面板打开，弃用 Router 栈能力）
- 激活 Tab 视觉高亮：通过修改对应 Button 的 Image.color / 子节点颜色实现

### StubPanelBase 改动

- 新增 `[SerializeField] private Button? _closeButton`（可选，子类或 Authoring 设置引用）
- `Awake` 中若 `_closeButton != null` 则 `_closeButton.onClick.AddListener(CloseSelf)`
- 新增 `protected void CloseSelf()` → `_router?.Close(Id)`

### Authoring 脚本改动

- `ApartmentSidebarAuthoring`: 面板 RectTransform anchor 改为 stretch；侧边栏移入子 Canvas
- 各 Panel Authoring（Garden / InventoryCollection / PetStatus / Tarot）: 内容区左侧 padding 从 0 改为 ~72px

## 涉及文件

| 文件 | 改动 |
|---|---|
| `SidebarController.cs` | toggle 逻辑 + activePanelId + 事件订阅 |
| `StubPanelBase.cs` | 新增 closeButton 字段 + CloseSelf() |
| `ApartmentSidebarAuthoring.cs` | 面板 anchor + 子 Canvas 分层 |
| `ApartmentGardenSidebarPatch.cs` | 面板 anchor 对齐新规范 |
| `GardenPanelAuthoring.cs` | 内容区左侧 padding 72px |
| `InventoryCollectionPanelAuthoring.cs` | 内容区左侧 padding 72px |
| `PetStatusPanelAuthoring.cs` | 内容区左侧 padding 72px |
| `TarotPanelAuthoring.cs` | 内容区左侧 padding 72px |

## 不变项

- `UIRouter` / `IUIRouter` / `PanelId` 接口不变
- 各 Panel 的领域逻辑（GardenPanelStub、InventoryPanelStub 等）不变
- `ApartmentToWorldMapPortal` 跟随侧边栏移入子 Canvas 即可，逻辑不变
- 场景文件中的面板绑定关系不变（仅 RectTransform 数值变化）
