# Profile Panel 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 PetStatus 面板升级为 Profile 双宠展示页，替换侧边栏按钮图标，新增 SpaceSys PanelId。

**Architecture:** ProfilePanelStub 继承 StubPanelBase 复刻双宠数据绑定（IPetRoster + IPersonalityEvolutionService），MCP 在场景中直接创建 UI 层级并 wiring SerializeField。SpaceSysPanelStub 为空壳占位。

**Tech Stack:** Unity UI (uGUI), TMPro, MCP AI-GameDeveloper tools, C# StubPanelBase pattern

---

### Task 1: 创建 ProfilePanelStub.cs

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs`

- [ ] **Step 1: 使用 script-update-or-create 创建脚本**

MCP: `script-update-or-create`

```csharp
#nullable enable
using GeminiLab.Core;
using GeminiLab.Core.Events;
using GeminiLab.Core.UI;
using GeminiLab.Modules.Pet;
using GeminiLab.Modules.Pet.Personality;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class ProfilePanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.PetStatus;

        [Header("标题")]
        [SerializeField] private Image? _titleIcon;

        [Header("Angel 侧")]
        [SerializeField] private Image? _angelBg;
        [SerializeField] private PersonalityRadarGraphic? _angelRadar;
        [SerializeField] private Image? _angelMoodIcon;
        [SerializeField] private TMP_Text? _angelMoodText;
        [SerializeField] private Image? _angelEnergyIcon;
        [SerializeField] private TMP_Text? _angelEnergyText;
        [SerializeField] private Image? _angelRelationIcon;
        [SerializeField] private TMP_Text? _angelRelationText;

        [Header("Evil 侧")]
        [SerializeField] private Image? _evilBg;
        [SerializeField] private PersonalityRadarGraphic? _evilRadar;
        [SerializeField] private Image? _evilMoodIcon;
        [SerializeField] private TMP_Text? _evilMoodText;
        [SerializeField] private Image? _evilEnergyIcon;
        [SerializeField] private TMP_Text? _evilEnergyText;
        [SerializeField] private Image? _evilRelationIcon;
        [SerializeField] private TMP_Text? _evilRelationText;

        [Header("宠物立绘（中心）")]
        [SerializeField] private Image? _angelPetImage;
        [SerializeField] private Image? _evilPetImage;

        private IPetRoster? _roster;
        private IPersonalityEvolutionService? _evolution;
        private IDisposable? _snapshotSub;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            _snapshotSub?.Dispose();
            base.OnDestroy();
        }

        public override void OnOpen(object? payload)
        {
            base.OnOpen(payload);
            ResolveServicesIfNeeded();
            SubscribeSnapshotIfNeeded();
            RefreshAll();
        }

        public override void OnClose()
        {
            base.OnClose();
            _snapshotSub?.Dispose();
            _snapshotSub = null;
        }

        private void ResolveServicesIfNeeded()
        {
            if (_roster == null) ServiceLocator.TryResolve(out _roster);
            if (_evolution == null) ServiceLocator.TryResolve(out _evolution);
        }

        private void SubscribeSnapshotIfNeeded()
        {
            if (_snapshotSub != null) return;
            if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
            {
                _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
            }
        }

        private void OnSnapshotChanged(PetRuntimeSnapshotChangedEvent evt)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshPet(PetId.Angel, _angelMoodText, _angelEnergyText, _angelRelationText, _angelRadar);
            RefreshPet(PetId.Devil, _evilMoodText, _evilEnergyText, _evilRelationText, _evilRadar);
        }

        private void RefreshPet(PetId id, TMP_Text? moodText, TMP_Text? energyText,
            TMP_Text? relationText, PersonalityRadarGraphic? radar)
        {
            if (_roster != null)
            {
                var data = _roster.TryGet(id);
                if (data != null)
                {
                    if (moodText != null) moodText.text = Mathf.RoundToInt(data.Mood).ToString();
                    if (energyText != null) energyText.text = Mathf.RoundToInt(data.Energy).ToString();
                }
            }
            if (relationText != null) relationText.text = "--";

            if (radar != null && _evolution != null)
            {
                var matrix = _evolution.GetMatrix(id);
                var values = new System.Collections.Generic.List<float>(7)
                {
                    matrix.Kindness, matrix.Evilness, matrix.Calmness,
                    matrix.Bravery, matrix.Shyness, matrix.Integrity, matrix.Curiosity
                };
                radar.SetValues(values);
            }
        }
    }
}
```

- [ ] **Step 2: 验证脚本编译**

MCP: `console-get-logs` — 确认没有编译错误。

---

### Task 2: 更新 PanelId.cs 添加 SpaceSys

**Files:**
- Modify: `Assets/_Project/Scripts/Core/UI/PanelId.cs`

- [ ] **Step 1: 在枚举中添加 SpaceSys**

在 `Garden = 24` 之后添加:

```csharp
SpaceSys = 25,
```

使用 MCP `script-update-or-create` 读取并修改文件，或使用 Edit 工具直接编辑。

- [ ] **Step 2: 验证编译**

MCP: `console-get-logs`。

---

### Task 3: 创建 SpaceSysPanelStub.cs

**Files:**
- Create: `Assets/_Project/Scripts/Modules/HubUI/Panels/SpaceSysPanelStub.cs`

- [ ] **Step 1: 使用 script-update-or-create 创建空壳脚本**

```csharp
#nullable enable
using GeminiLab.Core.UI;
using UnityEngine;

namespace GeminiLab.Modules.HubUI.Panels
{
    public sealed class SpaceSysPanelStub : StubPanelBase
    {
        public override PanelId Id => PanelId.SpaceSys;
    }
}
```

- [ ] **Step 2: 验证编译**

MCP: `console-get-logs`。

---

### Task 4: MCP 构建 Profile 面板 UI 层级

**前提:** Unity Editor 已打开 Apartment_Main 场景。

- [ ] **Step 1: 查找 Panel_PetStatus 并清理 Content 子对象**

MCP calls:

```
gameobject-find: "Panel_PetStatus" → 获取 instanceId
```

对 Content 下每个子对象:
```
gameobject-find: "Panel_PetStatus/Content/..." → gameobject-destroy
```

（Content 下可能无子对象，如果有则逐个清理）

- [ ] **Step 2: 创建 TitleIcon**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content"
  name: "TitleIcon"
  components: ["RectTransform", "Image"]
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/TitleIcon"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0, y: 1}
    anchorMax: {x: 0, y: 1}
    pivot: {x: 0, y: 1}
    anchoredPosition: {x: 40, y: -20}
    sizeDelta: {x: 120, y: 40}
```

- [ ] **Step 3: 创建 AngelSide 容器**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content"
  name: "AngelSide"
  components: ["RectTransform"]
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/AngelSide"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0, y: 0}
    anchorMax: {x: 0.5, y: 1}
    offsetMin: {x: 0, y: 0}
    offsetMax: {x: 0, y: 0}
```

- [ ] **Step 4: 创建 AngelBg（ProfileBackground 圆形背景）**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide"
  name: "AngelBg"
  components: ["RectTransform", "Image"]
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/AngelSide/AngelBg"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0.5, y: 0.5}
    anchorMax: {x: 0.5, y: 0.5}
    pivot: {x: 0.5, y: 0.5}
    anchoredPosition: {x: 20, y: 40}
    sizeDelta: {x: 300, y: 300}
```

- [ ] **Step 5: 创建 AngelRadar（雷达图）**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide"
  name: "AngelRadar"
  components: ["RectTransform"]
```

```
gameobject-component-add:
  target: "Panel_PetStatus/Content/AngelSide/AngelRadar"
  component: "PersonalityRadarGraphic"
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/AngelSide/AngelRadar"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0.5, y: 0.5}
    anchorMax: {x: 0.5, y: 0.5}
    pivot: {x: 0.5, y: 0.5}
    anchoredPosition: {x: 20, y: 40}
    sizeDelta: {x: 260, y: 260}
```

- [ ] **Step 6: 创建 AngelStatPanel + 6 个子元素**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide"
  name: "AngelStatPanel"
  components: ["RectTransform", "HorizontalLayoutGroup"]
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0, y: 0}
    anchorMax: {x: 1, y: 0}
    pivot: {x: 0.5, y: 0}
    sizeDelta: {x: 0, y: 80}
    anchoredPosition: {x: 0, y: 20}
```

HorizontalLayoutGroup 子元素:
```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "MoodIcon"
  components: ["RectTransform", "Image", "LayoutElement"]
```
LayoutElement preferredWidth: 48, preferredHeight: 48

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "MoodText"
  components: ["RectTransform", "TextMeshProUGUI"]
```
TMP text: "60", fontSize: 22, alignment: Center, color: white

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "EnergyIcon"
  components: ["RectTransform", "Image", "LayoutElement"]
```

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "EnergyText"
  components: ["RectTransform", "TextMeshProUGUI"]
```

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "RelationIcon"
  components: ["RectTransform", "Image", "LayoutElement"]
```

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/AngelSide/AngelStatPanel"
  name: "RelationText"
  components: ["RectTransform", "TextMeshProUGUI"]
```
TMP text: "--"

- [ ] **Step 7: 创建 EvilSide（镜像左侧）**

同 Step 3-6，name 替换为 EvilSide/EvilBg/EvilRadar/EvilStatPanel，anchorMin.x 改为 0.5。

- [ ] **Step 8: 创建 CenterPets 容器 + 两个立绘 Image**

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content"
  name: "CenterPets"
  components: ["RectTransform"]
```

```
gameobject-component-modify:
  target: "Panel_PetStatus/Content/CenterPets"
  component: "RectTransform"
  properties:
    anchorMin: {x: 0.5, y: 0.5}
    anchorMax: {x: 0.5, y: 0.5}
    pivot: {x: 0.5, y: 0.5}
    anchoredPosition: {x: 0, y: 40}
    sizeDelta: {x: 200, y: 280}
```

CenterPets 子元素:
```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/CenterPets"
  name: "AngelPetImage"
  components: ["RectTransform", "Image"]
```
位于左半边

```
gameobject-create:
  parentPath: "Panel_PetStatus/Content/CenterPets"
  name: "EvilPetImage"
  components: ["RectTransform", "Image"]
```
位于右半边

两个 Image 初始 color alpha = 0（透明占位）。

- [ ] **Step 9: 保存场景**

MCP: `scene-save`

---

### Task 5: 替换 Panel_PetStatus 上的组件 + 绑定 SerializeField

- [ ] **Step 1: 移除旧的 PetStatusPanelStub 组件，挂载 ProfilePanelStub**

```
gameobject-component-destroy:
  target: "Panel_PetStatus"
  component: "PetStatusPanelStub"
```

```
gameobject-component-add:
  target: "Panel_PetStatus"
  component: "ProfilePanelStub"
```

- [ ] **Step 2: 查找所有子对象引用并绑定**

使用 `gameobject-find` 获取各子对象的 instanceId，然后通过 `object-modify` 或 `reflection-method-call` 设置 SerializeField。

MCP calls (示例):
```
gameobject-find: "Panel_PetStatus/Content/TitleIcon" → titleIconId
gameobject-find: "Panel_PetStatus/Content/AngelSide/AngelBg" → angelBgId → 取 Image component
...
```

使用 `object-modify` 将这些引用赋值给 ProfilePanelStub 的 SerializeField。

- [ ] **Step 3: 保存场景**

MCP: `scene-save`

---

### Task 6: 替换侧边栏按钮 Sprite

- [ ] **Step 1: 查找侧边栏按钮 GameObject**

MCP: `gameobject-find` 查找侧边栏中的按钮对象名称。预期路径类似:
- `SidebarOverlay/.../Tab_PetStatus`
- `SidebarOverlay/.../Tab_Tarot`
- `SidebarOverlay/.../Tab_Collection`

需要先探索场景中实际的侧边栏按钮命名。

- [ ] **Step 2: 加载 Sprite 素材**

```
assets-find: "UI_Button_onProfile" → sprite guid
assets-find: "UI_Button_Tarot" → sprite guid
assets-find: "UI_Button_Collection" → sprite guid
assets-find: "UI_Button_SpaceSys" → sprite guid
```

- [ ] **Step 3: 替换各按钮的 Image.sprite**

```
gameobject-component-modify:
  target: "<sidebar_tab_petstatus_path>"
  component: "Image"
  properties:
    sprite: {guid: "<UI_Button_onProfile_guid>"}
```

同样替换 Tarot / Collection tabs。

SpaceSys tab 按钮是新增的，需要在侧边栏中创建。

- [ ] **Step 4: 添加 SpaceSys 按钮到侧边栏**

如果侧边栏使用 VerticalLayoutGroup，用 `gameobject-create` 在侧边栏容器中添加新按钮 `Tab_SpaceSys`，结构参考现有 tab 按钮（Image + Button 组件）。

- [ ] **Step 5: 保存场景**

MCP: `scene-save`

---

### Task 7: 创建 Panel_SpaceSys GameObject

- [ ] **Step 1: 在场景中创建 Panel_SpaceSys**

```
gameobject-create:
  name: "Panel_SpaceSys"
  components: ["RectTransform"]
```

设置 parent 为 Canvas 同级（和 Panel_PetStatus / Panel_Tarot 等同级）。

```
gameobject-component-add:
  target: "Panel_SpaceSys"
  component: "SpaceSysPanelStub"
```

- [ ] **Step 2: 创建 Content 子节点**

```
gameobject-create:
  parentPath: "Panel_SpaceSys"
  name: "Content"
  components: ["RectTransform", "Image"]
```

Content RectTransform 全屏 stretch（anchorMin: 0,0 / anchorMax: 1,1）。

```
gameobject-component-modify:
  target: "Panel_SpaceSys/Content"
  component: "Image"
  properties:
    color: {r: 0.12, g: 0.14, b: 0.2, a: 0.97}
```

- [ ] **Step 3: 创建关闭按钮**

```
gameobject-create:
  parentPath: "Panel_SpaceSys"
  name: "CloseButton"
  components: ["RectTransform", "Image", "Button"]
```

右上角，anchorMin: 1,1 / anchorMax: 1,1，sizeDelta: 40x40。

绑定到 StubPanelBase 的 `_closeButton` SerializeField。

- [ ] **Step 4: 创建标题 TMP**

Content 下创建 Title TMP: 文字 "空间系统"，fontSize 28。

- [ ] **Step 5: 绑定 Content 到 SpaceSysPanelStub._content**

使用 `object-modify` 将 Content GameObject 赋值给 `_content` SerializeField。

- [ ] **Step 6: 保存场景**

MCP: `scene-save`

---

### Task 8: 登记 UIArtCatalog entries

- [ ] **Step 1: 使用 script-update-or-create 创建临时注册脚本**

创建 `Assets/_Editor/RegisterProfileCatalogEntries.cs`:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GeminiLab.Modules.UI.Catalogs;

public static class RegisterProfileCatalogEntries
{
    [MenuItem("Tools/Gemini-Lab/Register Profile Catalog Entries")]
    public static void Register()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<UIArtCatalogSO>(
            "Assets/_Project/ScriptableObjects/UIArt/UIArtCatalog.asset");
        if (catalog == null)
        {
            Debug.LogError("UIArtCatalog.asset not found");
            return;
        }

        var so = new SerializedObject(catalog);
        var entries = so.FindProperty("_entries");

        AddEntry(entries, "profile_title",       "UI_Icon_Title",                    "t:Texture2D");
        AddEntry(entries, "profile_angel_bg",    "UI_Icon_Angel_ProfileBackground",   "t:Texture2D");
        AddEntry(entries, "profile_evil_bg",     "UI_Icon_Evil_ProfileBackground",    "t:Texture2D");
        AddEntry(entries, "profile_bg_default",  "UI_Icon_ProfileBackground",         "t:Texture2D");
        AddEntry(entries, "icon_mood",           "UI_Icon_Mood",                      "t:Texture2D");
        AddEntry(entries, "icon_energy",         "UI_Icon_Energy",                    "t:Texture2D");
        AddEntry(entries, "icon_relationship",   "UI_Icon_Relationship",              "t:Texture2D");
        AddEntry(entries, "btn_profile_on",      "UI_Button_onProfile",               "t:Texture2D");
        AddEntry(entries, "btn_tarot",           "UI_Button_Tarot",                   "t:Texture2D");
        AddEntry(entries, "btn_collection",      "UI_Button_Collection",              "t:Texture2D");
        AddEntry(entries, "btn_spacesys",        "UI_Button_SpaceSys",                "t:Texture2D");

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[RegisterProfileCatalogEntries] 已注册 11 个 UI Art Catalog entries");
    }

    private static void AddEntry(SerializedProperty entries, string key,
        string spriteName, string filter)
    {
        var guids = AssetDatabase.FindAssets($"{spriteName} {filter}");
        Sprite sprite = null;
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        entries.arraySize++;
        var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        entry.FindPropertyRelative("key").stringValue = key;
        entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
    }
}
#endif
```

- [ ] **Step 2: 执行注册**

MCP: `reflection-method-call` — 调用 `RegisterProfileCatalogEntries.Register` 方法。

或使用 `script-execute` 执行脚本。

- [ ] **Step 3: 验证 Catalog**

MCP: `assets-get-data` 读取 `UIArtCatalog.asset`，确认 `_entries` 数组有 11 个元素。

---

### Task 9: 验证 + 截图

- [ ] **Step 1: 运行场景**

确认 Unity Editor 处于 Play Mode 或场景已保存。

MCP: `screenshot-game-view` — 截图验证 Profile 面板布局。

- [ ] **Step 2: 检查 Console**

MCP: `console-get-logs` — 确认无错误（特别关注 UIRouter 注册、ServiceLocator 解析）。

- [ ] **Step 3: 手动微调 RectTransform 位置**

如需要，用 `gameobject-component-modify` 调整关键元素的 anchoredPosition / sizeDelta。

---

### Task 10: 提交

- [ ] **Step 1: Git commit**

```bash
git -C "E:/UnityProject/Gemini_Lab" add \
  "Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs" \
  "Assets/_Project/Scripts/Modules/HubUI/Panels/SpaceSysPanelStub.cs" \
  "Assets/_Project/Scripts/Core/UI/PanelId.cs" \
  "Assets/_Project/ScriptableObjects/UIArt/UIArtCatalog.asset" \
  "Assets/_Project/Scenes/Apartment/Apartment_Main.unity" \
  "Assets/_Editor/RegisterProfileCatalogEntries.cs"

git -C "E:/UnityProject/Gemini_Lab" commit -m "feat(profile): upgrade PetStatus to dual-pet Profile panel with MCP-built UI

- Add ProfilePanelStub with Angel/Evil dual radar + stat panels
- Add SpaceSysPanelStub and PanelId.SpaceSys = 25
- Register 11 UI art catalog entries for Profile sprites
- Replace sidebar tab button images
- Create Panel_SpaceSys in Apartment_Main scene"
```
