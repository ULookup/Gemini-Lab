# Apartment Scene Rollback To Commit

## 适用场景
当用户明确要求：
- “把公寓场景回退到某个 commit”
- “恢复 `Apartment_Main.unity` 到指定版本”
- “只回退 Apartment 场景，不要动别的文件”

并且目标是：
- 精准回退 `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
- 必要时只补充极少量用户明确指定的直接相关文件
- 不顺手回退整个项目
- 不顺手改 Pet、Furniture、NuGet 或其他场景

这个 skill 就应该使用。

## 目标
安全完成以下结果：
1. 明确回退目标 commit。
2. 精准回退 `Apartment_Main.unity` 到该 commit 版本。
3. 只有在用户明确要求时，才额外回退与 Apartment 场景直接相关的少量文件。
4. 保证回退范围可审查、可解释。

## 前置规则
1. 必须先遵守 `AGENTS.md`：
   - 先复述理解
   - 用户明确说“执行”后再操作
2. 必须先检查：
   - `git status --short --branch`
   - 当前分支
   - 目标 commit 是否存在
3. 这是高风险操作：
   - 默认只回退 `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
   - 不能默认扩大到整个 `Assets/_Project/Scenes/`
   - 不能默认扩大到整个项目
4. 如果发现“只回退场景文件不够”，必须先向用户说明，再等进一步确认。

## 标准流程

### Step 1. 确认目标
至少确认：
- 用户给出的 commit hash
- 用户是要回退“场景文件本身”，还是“场景相关整条链”

默认解释：
- 先只回退 `Apartment_Main.unity`

### Step 2. 只读检查
执行：
- 当前工作树状态
- 目标 commit 下 `Apartment_Main.unity` 是否存在
- 当前工作树相对目标 commit 的差异范围

目标：
- 让回退范围先可见
- 避免误把无关改动一起带进去

### Step 3. 精准回退
默认执行：
```powershell
git checkout <commit> -- Assets/_Project/Scenes/Apartment/Apartment_Main.unity
```

如果用户明确指定还要一起回退：
- `PetController.cs`
- `PetPlayerInputController.cs`
- `PetPlayerFurnitureInteractionController.cs`
- 指定资源 `.meta`

则只把这些**明确点名文件**额外加入命令。

### Step 4. 回退后核对
至少检查：
- `git diff --name-only <commit> -- Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
- 当前工作树里是否只留下预期范围的变化

### Step 5. 回报结果
至少告诉用户：
- 实际回退了哪些文件
- 哪些文件没有动
- 当前是否已经与目标 commit 对齐

## 不要做的事
- 不自动 `reset --hard`
- 不自动回退整个 `Assets/_Project/Scenes/`
- 不自动回退所有 Pet / Furniture / Animation 文件
- 不因为“感觉相关”就顺手扩大范围

## 成功判定
满足以下全部条件才算完成：
1. `Apartment_Main.unity` 已对齐到用户指定 commit。
2. 回退范围没有超出用户确认的边界。
3. 最终结果已向用户明确说明哪些文件被回退、哪些没有。
