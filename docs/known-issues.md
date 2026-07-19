# 已知隐患清单

> 记录当前不触发、但未来功能扩展时会爆发的潜伏问题。新增条目请附：机制、位置、触发条件、修复时机、修复方法。

---

## 1. `GetComponent<T>() ?? AddComponent<T>()` 假 null 陷阱（Furniture 模块 + 部分 Editor 工具）

**状态**：未修复（2026-07-18 记录，决定延后）

### 机制

Unity Editor 下（含 Play in Editor），`GetComponent<T>()` 找不到组件时返回的不是真 null，而是一个引用非空的"假 null"存根对象（用于抛 `MissingComponentException` 友好报错）。C# 的 `??` / `?.` / `??=` 无法被重载，只做裸引用比较，不走 Unity 重载的 `== null`——因此 `??` 会误判"有值"，跳过右侧的 `AddComponent`，返回存根，首次访问成员即抛 `MissingComponentException`。

打包后的运行时 `GetComponent` 返回真 null，`??` 行为正常。**此陷阱仅在 Editor 下触发。**

### 实际案例

2026-07-18，`WorldMapEmotionGardenUIPatch` 对全新创建的 `DebugBloomBtn`（空 GameObject，必然缺 RectTransform）使用此写法，AutoSetup v10 升级当场崩溃：

```
[AutoSetup] 失败: There is no 'RectTransform' attached to the "DebugBloomBtn" game object, but a script is trying to access it.
```

该文件已全部替换为 `GetOrAdd<T>` 显式判空写法（见"修复方法"）。

### 遗留位置（8 处）

| 文件 | 行号* | 组件 |
|---|---|---|
| `Assets/_Project/Scripts/Editor/Tools/AddPetStatusBarsToPanels.cs` | 219 | Image |
| `Assets/_Project/Scripts/Editor/Tools/AddPetStatusBarsToPanels.cs` | 255 | TMP_Text |
| `Assets/_Project/Scripts/Modules/Furniture/ApartmentSceneFurnitureBindings.cs` | 53 | InteractionAnchor |
| `Assets/_Project/Scripts/Modules/Furniture/ApartmentSceneFurnitureBindings.cs` | 54 | SceneFurnitureDefinitionHint |
| `Assets/_Project/Scripts/Modules/Furniture/ApartmentSceneFurnitureBindings.cs` | 68 | Furniture |
| `Assets/_Project/Scripts/Modules/Furniture/Furniture.cs` | 59 | InteractionAnchor |
| `Assets/_Project/Scripts/Modules/Furniture/Furniture.cs` | 85 | InteractionAnchor |
| `Assets/_Project/Scripts/Modules/Furniture/FurnitureService.cs` | 519 | SortingGroup |

*行号会随代码演进漂移，可用正则重新定位：`GetComponent<\w+>\(\) \?\?`

### 当前不触发的原因

公寓家具全部是场景预搭的固定对象，组件齐全，`GetComponent` 每次都命中真组件，`?? AddComponent` 兜底分支从未被执行过（= 未经验证的死代码）。

### 触发条件

Editor 下代码运行到**缺少对应组件**的对象上。典型场景：

- 接入家具修改/替换/动态生成功能后，新家具对象缺 `InteractionAnchor` 等组件
- prefab 被改动删掉了组件
- Editor 工具作用于全新创建的空 GameObject

### 修复时机

接入家具修改/替换功能时**必须**一并修复 Furniture 模块的 6 处；Editor 工具的 2 处在下次改动该工具时顺手修。

### 修复方法

参照 `Assets/_Project/Scripts/Editor/SceneBootstrap/WorldMapEmotionGardenUIPatch.cs` 中的 `GetOrAdd<T>`：

```csharp
private static T GetOrAdd<T>(GameObject go) where T : Component
{
    var c = go.GetComponent<T>();
    return c != null ? c : go.AddComponent<T>();   // Unity 重载的 != 能识别假 null
}
```

原则：对 `UnityEngine.Object` 及其子类，永远不用 `??` / `?.` / `??=`，只用显式 `== null` / `!= null`。
