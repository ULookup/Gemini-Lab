# Git Sync Upstream Main

## 适用场景
当用户明确要求：
- “把本地仓库和 `ULookup:main` upstream 一下”
- “同步 upstream/main”
- “把本地 `main` 对齐到上游最新”

并且目标是：
- **只同步本地 `main`**
- **不顺手改当前功能分支**
- **同步完成后切回用户原来的工作分支**

这个 skill 就应该使用。

## 目标
安全完成以下结果：
1. 抓取 `upstream/main` 最新内容。
2. 将本地 `main` 对齐到 `upstream/main`。
3. 不在 `main` 上做功能开发。
4. 同步后切回用户原来的工作分支。

## 前置规则
1. 必须先遵守 `AGENTS.md`：
   - 先复述理解
   - 用户明确说“执行”后再操作
2. 必须先查看：
   - `git status --short --branch`
   - 当前所在分支
   - `git remote -v`
3. 如果当前工作树不干净，不要默认继续同步；必须先向用户说明风险。
4. 如果用户当前就在功能分支上，只同步本地 `main`，不要直接把功能分支 rebase 到 `upstream/main`，除非用户明确要求。

## 标准流程

### Step 1. 确认状态
执行只读检查：
- 当前分支
- 工作树状态
- `origin / upstream` 远程地址

目标：
- 确认当前仓库是否干净
- 确认 `upstream` 是否存在且指向 `ULookup/Gemini-Lab`

### Step 2. 抓取 upstream
优先执行：
```powershell
git fetch upstream
```

如果失败：
- 明确区分是 SSH 失败还是 HTTPS 失败
- 可根据当前机器状态切换 `upstream` URL（SSH / HTTPS）
- 重试一次 fetch

### Step 3. 记录当前工作分支
如果当前不在 `main`，先记住当前分支名，后面要切回去。

### Step 4. 切到本地 `main`
```powershell
git checkout main
```

### Step 5. 对齐本地 `main`
优先遵守项目文档里的推荐方式。

正常情况下可执行：
```powershell
git rebase upstream/main
```

如果当前机器环境或工作树导致 rebase 不稳定，但用户只是要求“本地 main 对齐 upstream/main”，可以执行：
```powershell
git reset --hard upstream/main
```

注意：
- 这里只允许在 **本地 `main`** 上这样做
- 不允许对功能分支直接做这种覆盖式同步

### Step 6. 切回用户原分支
例如：
```powershell
git checkout docs/git-fork-pr-workflow
```

### Step 7. 回报结果
至少明确告诉用户：
- `upstream/main` 当前 commit
- 本地 `main` 当前 commit
- 是否已经完全对齐
- 当前你已切回哪个工作分支

## 不要做的事
- 不自动 push `origin/main`
- 不自动 rebase 当前功能分支
- 不自动解决 PR 冲突
- 不顺手整理工作树里的其他改动
- 不把“同步 `main`”扩展成“处理整个分支策略”

## 成功判定
满足以下全部条件才算完成：
1. `git rev-parse main` 与 `git rev-parse upstream/main` 一致。
2. 当前分支已切回用户原来的工作分支。
3. 没有顺手改动用户当前功能分支内容。
