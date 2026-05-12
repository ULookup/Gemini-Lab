# Gemini-Lab 项目 Skill 清单

Updated: 2026-04-27

## 这份文档记录什么
这份文档记录当前 Gemini-Lab 仓库内可见的项目本地 skill 清单。

当前只统计项目本地两套目录：
- `d:/unity/project/Gemini-Lab/.agents/skills/`
- `d:/unity/project/Gemini-Lab/.cursor/skills/`

不统计系统内置 skill。

## 当前状态
- `.agents/skills/` 与 `.cursor/skills/` 当前仍是镜像关系。
- 两边目录数都为 `77`。
- 当前没有发现“只存在于一边”的 skill。
- 相比早期清单，当前已新增动画与 Animator 相关 skill。
- 当前工作流升级后，skill 的职责更明确地收口为：
  - 固定流程
  - 工具组合流程
  - 高风险操作的标准化 workflow
  - 不承载即时项目状态

## 存放位置

### 主工作区暴露目录
- `d:/unity/project/Gemini-Lab/.agents/skills/`

### Cursor 侧镜像目录
- `d:/unity/project/Gemini-Lab/.cursor/skills/`

### 单个 skill 的实际说明文件
每个 skill 都在自己的目录下放一个 `SKILL.md`，例如：
- `d:/unity/project/Gemini-Lab/.agents/skills/scene-open/SKILL.md`
- `d:/unity/project/Gemini-Lab/.cursor/skills/scene-open/SKILL.md`

## 使用建议
1. 优先把 `.agents/skills/` 当作当前工作区技能源。
2. 如果修改或新增项目本地 skill，检查 `.cursor/skills/` 是否需要同步。
3. 技能清单变化后，记得同步更新：
   - `docs/ai-memory/memory-index.paths.txt`
   - `docs/ai-memory/gemini-lab-project-file-guide.md`
   - `docs/skill-design-boundary.md`
   - 本文档
4. 后续优先考虑新增“单用途 workflow skill”，而不是继续把高风险操作拼成长 shell 命令链。

## 建议优先规划的 workflow skill
- `同步 upstream/main`
- `回退 Apartment 场景到指定 commit`
- `重建 Pet 动画引用`
- `检查家具绑定`
- `清理 Unity 生成缓存`

这些 workflow skill 的目标是：
- 降低高风险命令链长度
- 提高可审查性
- 降低任务边界漂移导致的误操作概率

## 分类总览

### 1. Animation 与 Animator
| Skill | 用途 |
| :--- | :--- |
| `animation-create` | 创建 Unity AnimationClip 资产。 |
| `animation-get-data` | 读取 AnimationClip 的曲线、事件与元数据。 |
| `animation-modify` | 修改 AnimationClip 曲线、属性与事件。 |
| `animator-create` | 创建 AnimatorController 资产。 |
| `animator-get-data` | 读取 AnimatorController 的层、参数、状态与过渡。 |
| `animator-modify` | 修改 AnimatorController 的参数、状态、层与过渡。 |

### 2. Asset 与 Prefab 资产操作
| Skill | 用途 |
| :--- | :--- |
| `assets-copy` | 复制项目资产到新路径。 |
| `assets-create-folder` | 在 `Assets/` 下创建新文件夹。 |
| `assets-delete` | 删除项目资产。 |
| `assets-find` | 按搜索串在 AssetDatabase 中查找资产。 |
| `assets-find-built-in` | 查找 Unity 内置资源。 |
| `assets-get-data` | 读取资产的可序列化数据。 |
| `assets-material-create` | 创建新材质资产。 |
| `assets-modify` | 直接修改资产文件中的序列化数据。 |
| `assets-move` | 移动或重命名资产。 |
| `assets-prefab-open` | 打开指定 Prefab 进入 Prefab 编辑模式。 |
| `assets-prefab-save` | 保存当前打开的 Prefab。 |
| `assets-prefab-close` | 关闭当前打开的 Prefab。 |
| `assets-prefab-create` | 从场景中的 GameObject 或已有 Prefab 创建 Prefab / Variant。 |
| `assets-prefab-instantiate` | 将 Prefab 实例化到当前场景。 |
| `assets-refresh` | 刷新 AssetDatabase，并可触发脚本重编译感知。 |
| `assets-shader-get-data` | 读取 Shader 的详细结构与编译信息。 |
| `assets-shader-list-all` | 列出项目与包中可用的 Shader 名称。 |

### 3. Console、Editor 状态与 Selection
| Skill | 用途 |
| :--- | :--- |
| `console-clear-logs` | 清空 MCP 缓存日志和 Unity Console。 |
| `console-get-logs` | 读取 Unity Editor 日志。 |
| `editor-application-get-state` | 读取 Editor 当前状态，如 playmode、暂停、编译状态。 |
| `editor-application-set-state` | 控制 Editor 进入 / 退出 / 暂停 playmode。 |
| `editor-selection-get` | 读取当前编辑器选择对象。 |
| `editor-selection-set` | 设置当前编辑器选择对象。 |

### 4. GameObject、Component、Object、粒子与反射
| Skill | 用途 |
| :--- | :--- |
| `gameobject-find` | 在当前打开的 Prefab 或当前场景中查找 GameObject。 |
| `gameobject-create` | 创建 GameObject。 |
| `gameobject-destroy` | 删除 GameObject 及其子层级。 |
| `gameobject-duplicate` | 复制 GameObject。 |
| `gameobject-modify` | 修改 GameObject 字段与属性。 |
| `gameobject-set-parent` | 调整 GameObject 父子关系。 |
| `gameobject-component-list-all` | 列出可添加的 Component 类型。 |
| `gameobject-component-add` | 给 GameObject 添加组件。 |
| `gameobject-component-destroy` | 删除 GameObject 上的组件。 |
| `gameobject-component-get` | 读取某个组件的详细数据。 |
| `gameobject-component-modify` | 修改某个组件的字段与属性。 |
| `object-get-data` | 读取任意 Unity Object 的序列化数据。 |
| `object-modify` | 修改任意 Unity Object 的数据。 |
| `particle-system-get` | 读取粒子系统模块数据。 |
| `particle-system-modify` | 修改粒子系统模块数据。 |
| `reflection-method-find` | 通过反射查找项目中的 C# 方法。 |
| `reflection-method-call` | 直接调用查找到的 C# 方法。 |
| `type-get-json-schema` | 为指定 C# 类型生成 JSON Schema。 |

### 5. Scene 与截图采样
| Skill | 用途 |
| :--- | :--- |
| `scene-create` | 创建新场景。 |
| `scene-get-data` | 读取场景根对象信息。 |
| `scene-list-opened` | 列出当前已打开的场景。 |
| `scene-open` | 打开场景资产。 |
| `scene-save` | 保存已打开场景。 |
| `scene-set-active` | 设置活动场景。 |
| `scene-unload` | 从 Editor 中卸载已打开场景。 |
| `screenshot-camera` | 从指定 Camera 抓图。 |
| `screenshot-game-view` | 从 Game View 抓图。 |
| `screenshot-scene-view` | 从 Scene View 抓图。 |

### 6. 脚本与代码执行
| Skill | 用途 |
| :--- | :--- |
| `script-read` | 读取脚本文件内容。 |
| `script-update-or-create` | 更新或创建 C# 脚本文件。 |
| `script-delete` | 删除脚本文件并等待编译完成。 |
| `script-execute` | 通过 Roslyn 动态编译并执行一段 C# 代码。 |

### 7. Package Manager
| Skill | 用途 |
| :--- | :--- |
| `package-search` | 搜索 UPM 注册表和本地已安装包。 |
| `package-list` | 列出当前项目已安装包。 |
| `package-add` | 安装包。 |
| `package-remove` | 卸载包。 |

### 8. 测试、工具与项目初始化
| Skill | 用途 |
| :--- | :--- |
| `tests-run` | 运行 Unity 测试，支持按模式或目标过滤。 |
| `tool-list` | 列出当前可用 MCP 工具。 |
| `tool-set-enabled-state` | 启用或禁用指定 MCP 工具。 |
| `ping` | 轻量级可用性探测。 |
| `unity-initial-setup` | 为项目做 Unity MCP / AI Skills 等初始接入配置。 |
| `unity-skill-create` | 通过 C# 代码创建新的 Unity skill。 |
| `unity-skill-generate` | 从项目现有 Tools 生成 skills。 |

### 9. Workflow Skill
| Skill | 用途 |
| :--- | :--- |
| `git-sync-upstream-main` | 安全同步本地 `main` 到 `ULookup:main`，并在完成后切回用户原来的工作分支。 |
| `unity-clear-generated-cache` | 只清理 Unity 生成缓存（`Library/ScriptAssemblies`、`Library/Bee`、`Temp`），不改源码。 |
| `apartment-scene-rollback-to-commit` | 精准把 `Apartment_Main.unity` 回退到指定 commit，不顺手扩大到整个项目。 |
| `pet-animation-reference-rebuild` | 只修复 `Pet_Angel` 的动画资源、clip、controller 与场景绑定引用链。 |
| `furniture-binding-check` | 只读盘点 Apartment 家具绑定状态，区分脚本层、场景层与资源层问题。 |

## 常见工作流组合

### 动画资产链
`animation-get-data` -> `animation-modify`  
`animator-get-data` -> `animator-modify`

适合：
- 先读取现有动画 / 状态机结构
- 再做精确修改

### 资产修改链
`assets-find` -> `assets-get-data` -> `assets-modify`

适合：
- 先找到资产
- 再读取结构
- 最后只改目标字段

### Prefab 编辑链
`assets-prefab-open` -> `gameobject-find` / `gameobject-component-get` / `gameobject-component-modify` -> `assets-prefab-save` -> `assets-prefab-close`

适合：
- 打开 Prefab
- 精确定位对象与组件
- 保存并退出

### 场景编辑链
`scene-open` -> `gameobject-find` -> `gameobject-modify` / `gameobject-create` / `gameobject-component-add` -> `scene-save`

适合：
- 打开场景
- 修改层级或对象属性
- 保存场景

### 脚本编辑链
`script-read` -> `script-update-or-create`

适合：
- 先阅读现有脚本
- 再更新实现

### 包管理链
`package-search` -> `package-list` -> `package-add` / `package-remove`

适合：
- 先查可用包
- 再确认当前安装状态
- 最后执行增删

### 日志排查链
`console-clear-logs` -> 执行动作 -> `console-get-logs`

适合：
- 想隔离某次操作引发的问题日志

### Git upstream 同步链
`git-sync-upstream-main`

适合：
- 用户明确要求“把本地仓库和 `ULookup:main` upstream 一下”
- 只同步本地 `main`
- 不顺手改当前功能分支

### Unity 生成缓存清理链
`unity-clear-generated-cache`

适合：
- 用户明确要求“只清理 Unity 生成缓存”
- 想让 Unity 在当前源码状态下重新完整编译
- 不希望顺手修改源码或场景

### Apartment 场景精准回退链
`apartment-scene-rollback-to-commit`

适合：
- 用户明确要求“把公寓场景回退到某个 commit”
- 默认只回退 `Apartment_Main.unity`
- 不希望顺手回退整个项目

### Pet 动画引用修复链
`pet-animation-reference-rebuild`

适合：
- 用户明确要求“重建 Pet 动画引用”
- 需要核对 `Move / Idle / Interact / Sleep`
- 不希望顺手把动画修复扩大成场景或交互逻辑重构

### 家具绑定巡检链
`furniture-binding-check`

适合：
- 用户明确要求“检查家具绑定”
- 需要核对 Apartment 家具对象是否真的接进了绑定链
- 需要区分脚本问题、场景问题和资源问题

## 当前维护建议
1. `.agents/skills/` 继续视为主工作区技能源，`.cursor/skills/` 继续作为镜像目录维护。
2. 如果新增项目 skill，最好在本文档里同步加条目与用途说明。
3. 当前 skill 设计边界已经单独写在 `docs/skill-design-boundary.md`，新增 skill 时应先遵守它，再补清单。
4. 如果技能结构发生大改，除了更新清单，还要同步更新 `docs/skill-design-boundary.md`、文件指南与主记忆。
