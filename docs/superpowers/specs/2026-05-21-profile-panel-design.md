# Profile Panel 设计文档

**日期:** 2026-05-21
**版本:** 1.0
**范围:** PetStatus 面板升级为 Profile 双宠展示页 + 侧边栏按钮素材替换 + 新增 SpaceSys PanelId

---

## 概述

将现有 PetStatus 面板升级为 Profile 页面：双宠（Angel / Evil）并排展示，各带性格雷达图、状态面板，中心双立绘区域。同时替换侧边栏 4 个 tab 按钮图标，新增 SpaceSys 面板 ID。

---

## 1. Profile 面板布局

### 1.1 GameObject 层级

```
Panel_PetStatus (已存在, 挂 ProfilePanelStub + 旧 PetStatusPanelStub 替换)
└── Content (全屏 Stretch)
    ├── TitleIcon (Image)
    │   └── sprite: UI_Icon_Title
    │   └── 位置: 左上角 (anchoredPosition 约 40, -40)
    │
    ├── AngelSide (占左半区, anchorMin=0,0 / anchorMax=0.5,1)
    │   ├── AngelBg (Image — UI_Icon_Angel_ProfileBackground, 居中偏上)
    │   ├── AngelRadar (PersonalityRadarGraphic — 叠在 Bg 上方)
    │   └── AngelStatPanel (底部横排, 与 Evil 对称)
    │       ├── MoodIcon (Image — UI_Icon_Mood)
    │       ├── MoodText (TMP — 数值)
    │       ├── EnergyIcon (Image — UI_Icon_Energy)
    │       ├── EnergyText (TMP — 数值)
    │       ├── RelationIcon (Image — UI_Icon_Relationship)
    │       └── RelationText (TMP — "--" 占位)
    │
    ├── EvilSide (占右半区, anchorMin=0.5,0 / anchorMax=1,1)
    │   ├── EvilBg (Image — UI_Icon_Evil_ProfileBackground, 居中偏上)
    │   ├── EvilRadar (PersonalityRadarGraphic)
    │   └── EvilStatPanel (底部横排, 结构同上)
    │       └── ... (对称结构)
    │
    └── CenterPets (绝对居中, 叠在分界线上, 最高层级)
        ├── AngelPetImage (Image — 立绘占位, 后续替换)
        └── EvilPetImage (Image — 立绘占位, 后续替换)
```

### 1.2 素材映射

| 素材文件 | 目标 GameObject | 说明 |
|---|---|---|
| `UI_Icon_Title.png` | TitleIcon | 左上角标题 |
| `UI_Icon_Angel_ProfileBackground.png` | AngelBg | Angel 雷达底层背景 |
| `UI_Icon_Evil_ProfileBackground.png` | EvilBg | Evil 雷达底层背景 |
| `UI_Icon_ProfileBackground.png` | 不直接使用 | 备选通用背景, 登记到 Catalog |
| `UI_Icon_Mood.png` | AngelMoodIcon / EvilMoodIcon | 心情图标 |
| `UI_Icon_Energy.png` | AngelEnergyIcon / EvilEnergyIcon | 精力图标 |
| `UI_Icon_Relationship.png` | AngelRelationIcon / EvilRelationIcon | 关系值图标（占位） |

### 1.3 数据绑定

- **Mood / Energy**: 从 `IPetRoster` + `PetRuntimeSnapshotChangedEvent` 读取，和现有 PetStatusPanel 一致
- **Relation**: 暂无数据源，TMP 显示 `"--"` 占位
- **雷达图**: Angel / Evil 各独立 `PersonalityRadarGraphic`，从 `IPersonalityEvolutionService` 获取矩阵
- Title 在打开时就固定，不切换

---

## 2. ProfilePanelStub 脚本

新建 `Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs`。

- 继承 `StubPanelBase`
- `PanelId` = `PetStatus`（复用现有 ID，旧 PetStatusPanelStub 移除）
- SerializeField 绑定 21 个引用点（详见 GameObject 层级）

**关键行为:**
- `OnOpen`: 同时渲染 Angel 和 Evil 数据（无需 tab 切换）
- 雷达中心保留立绘 Image 引用，当前置为透明/无 sprite，等待后续立绘素材到位

---

## 3. 侧边栏按钮替换

### 3.1 映射

| 素材 | 侧边栏目标 | 状态 |
|---|---|---|
| `UI_Button_onProfile.png` | PetStatus tab | 激活态（点击/当前页） |
| `UI_Button_Tarot.png` | Tarot tab | 普通态 |
| `UI_Button_Collection.png` | Collection tab | 普通态 |
| `UI_Button_SpaceSys.png` | SpaceSys tab（新增） | 普通态 |

### 3.2 实施方式

- MCP 查找侧边栏对应的 Button GameObject，替换 `Image.sprite`
- 每个 tab 需要两态（普通 / 激活），当前只提供了一态素材。按钮高亮逻辑沿用现有的 sidebar toggle 机制处理颜色/缩放变化即可

---

## 4. 新增 PanelId: SpaceSys

### 4.1 PanelId 枚举

在 `PanelId.cs` 中新增:

```csharp
SpaceSys = 25,  // 公寓空间系统面板
```

### 4.2 面板 Stub

新建 `Assets/_Project/Scripts/Modules/HubUI/Panels/SpaceSysPanelStub.cs`:

- 继承 `StubPanelBase`
- `PanelId` = `SpaceSys`
- 初期为空壳（Content 内只有背景色块 + 标题 TMP "空间系统"）
- 后续由独立的 SpaceSys 功能迭代填充

### 4.3 场景注册

- 在 `Apartment_Main.unity` 中创建 `Panel_SpaceSys` GameObject，挂载 `SpaceSysPanelStub`
- 结构: Content（全屏 Stretch）+ 关闭按钮，和现有 5 个面板一致

---

## 5. UIArtCatalog 登记

在 `UIArtCatalog.asset` 中添加以下 key:

```
profile_title        → UI_Icon_Title
profile_angel_bg     → UI_Icon_Angel_ProfileBackground
profile_evil_bg      → UI_Icon_Evil_ProfileBackground
profile_bg_default   → UI_Icon_ProfileBackground
icon_mood            → UI_Icon_Mood
icon_energy          → UI_Icon_Energy
icon_relationship    → UI_Icon_Relationship
btn_profile_on       → UI_Button_onProfile
btn_tarot            → UI_Button_Tarot
btn_collection       → UI_Button_Collection
btn_spacesys         → UI_Button_SpaceSys
```

---

## 6. MCP 构建顺序

1. 创建 `ProfilePanelStub.cs` + `SpaceSysPanelStub.cs`
2. 更新 `PanelId.cs` 添加 `SpaceSys`
3. 用 MCP 在 `Panel_PetStatus/Content` 下创建 UI 层级
4. SerializeField 绑定 ProfilePanelStub 引用
5. 用 MCP 替换侧边栏 4 个 tab 的 Image.sprite
6. 创建 `Panel_SpaceSys` GameObject
7. 登记 UIArtCatalog entries

---

## 7. 不变项 / 约束

- `UIRouter` 不需要修改
- `IUIPanel` 接口不变
- `StubPanelBase` 不变
- 侧边栏结构不变（只换图）
- `PersonalityRadarGraphic` 组件复用，不改动
- 旧 `PetStatusPanelStub.cs` 保留在代码库中但不再使用（不在场景中注册）
