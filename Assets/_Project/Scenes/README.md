# Scenes/ — 工程场景（2D）

## 文件夹职责
存放**所有 Unity 2D 场景文件**。以"功能/形态"划分，不以版本号划分（版本差异用 VCS 管理）。

## 场景清单

| 场景 | 状态 | 形态 | 说明 |
| :--- | :--- | :--- | :--- |
| `Boot.unity` | ✅ 已落地 | 启动场景 | 仅含 `GameBootstrap`，完成服务注册后由 `SceneFlowService` 自动跳转到 `MainMenu`。**Build Index 固定为 0。** |
| `MainMenu/MainMenu.unity` | ✅ 已落地（占位 UI） | 主菜单 | Canvas + 开始 / 存档 / 设置 三个按钮，挂 `MainMenuController`。占位按钮样式为纯色块 + TMP 文字；美术交付后改为 `UIArtCatalogSO` 引用的 Sprite。 |
| `Apartment/Apartment_Main.unity` | ✅ 已落地 | 公寓主场景（2D 正交） | 家具交互、宠物表现层、侧边栏（`SidebarController` + 4 个占位面板）、通往 WorldMap 的入口按钮。 |
| `WorldMap/WorldMap_Main.unity` | ✅ 已落地（骨架） | 2D 横板 | 横板摄像头（`WorldMapCameraController`，A/D 左右平移、右键拖拽）、Garden Zone 标记、返回公寓按钮。真实场景美术、花园交互、视差背景待补齐。 |
| `Desktop/Desktop_Overlay.unity` | ✅ 已落地（原型） | 桌面 Overlay | 透明摄像机 + 宠物 Overlay；当前原生透明窗口仍是占位层，由 `DesktopOverlayManager` 驱动切换。 |
| `Dev/FSM_Sandbox.unity` | ❌ 未落地 | FSM 沙盒 | 规划中。Release 构建不打包。 |
| `Dev/Furniture_Sandbox.unity` | ❌ 未落地 | 家具沙盒 | 规划中。 |

### Build Settings 顺序（实际）
```
0: Boot.unity
1: MainMenu/MainMenu.unity
2: Apartment/Apartment_Main.unity
3: WorldMap/WorldMap_Main.unity
4: Desktop/Desktop_Overlay.unity
```

### 场景间跳转拓扑
```
Boot → MainMenu → Apartment ⇄ WorldMap
                      ⇅
                DesktopOverlay（任何时候可切入/切出）
```

所有切换都走 `GeminiLab.Core.SceneFlow.ISceneFlowService`；业务代码不直接调用 `SceneManager`。

## 依赖关系
- 场景文件**只能引用** `_Project/Prefabs/**`、`_Project/ScriptableObjects/**`、`_Project/Art/**`（含 Tiles / Sprites）、`_Project/Audio/**`。
- **禁止**在场景中直接放置编辑器临时 Prefab、插件自带 Demo Prefab。

## 代码规范/注意事项
1. 每个场景必须有一个 `_SceneRoot` GameObject，所有对象作为其子节点，方便批量管理。
2. **摄像机必须为 Orthographic**；`Size` 与像素基准在 `Settings/Rendering/CameraPreset.asset` 统一配置，禁止逐场景随意调整。
3. **Sorting Layers** 顺序在 ProjectSettings 中统一定义：`Background → Floor → Wall → Furniture → Pet → FX → UI`；场景内禁止自建新 Sorting Layer。
4. 默认 `Boot.unity` 为 Build Settings 的 index 0；构建脚本会校验。
5. `Dev/` 目录内的沙盒场景在 `BuildPipeline` 中通过 scene 名前缀 `Dev_*` 排除。
6. 场景内不得残留仅本机可用的绝对路径引用；提交前运行 `Tools/Scene Validator` 自检。
7. **2D 场景无光照烘焙**；2D Lights（Global / Point / Freeform / Spot）默认为实时光。若使用 Shadow Caster 2D，需要在 `Renderer Features` 中显式启用 `Shadow Caster Pass`。
8. 场景合并冲突：启用 `Force Text` + `SmartMerge`，禁止二进制合并。
