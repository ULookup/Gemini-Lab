# Unity Clear Generated Cache

## 适用场景
当用户明确要求：
- “清理 Unity 生成缓存”
- “删掉 Library/ScriptAssemblies、Bee、Temp 后重编译”
- “不改源码，只清缓存”

并且目标是：
- 不碰源码、场景、Prefab、资源
- 只清理 Unity 生成缓存
- 让 Unity 在当前源码状态下重新完整编译

这个 skill 就应该使用。

## 目标
安全完成以下结果：
1. 关闭当前 Unity 进程。
2. 清理生成缓存目录。
3. 不修改源码文件。
4. 告知用户重新打开 Unity 触发完整重编译。

## 前置规则
1. 必须先遵守 `AGENTS.md`：
   - 先复述理解
   - 用户明确说“执行”后再操作
2. 必须先确认当前任务边界是“只清缓存，不改源码”。
3. 必须先检查：
   - Unity 进程是否仍在运行
   - 工作树状态
4. 这是高风险操作：
   - 只能删除生成缓存目录
   - 不得顺手删除 `Assets/`、`Packages/`、`ProjectSettings/`

## 标准流程

### Step 1. 记录现状
执行只读检查：
- `git status --short --branch`
- Unity / UnityHub 进程状态
- 目标缓存目录是否存在

### Step 2. 关闭 Unity
如果 Unity 正在运行，先关闭 Unity 进程。

目标：
- 避免删除缓存时文件被占用
- 避免触发半编译状态

### Step 3. 清理缓存目录
只删除以下目录：
- `Library/ScriptAssemblies`
- `Library/Bee`
- `Temp`

注意：
- 不默认删整个 `Library`
- 除非用户明确要求，否则不要扩大范围

### Step 4. 再次核对
确认：
- 目标缓存目录已不存在
- 工作树里没有新增源码改动

### Step 5. 回报用户
至少明确告诉用户：
- 已删除哪些缓存目录
- 没有动哪些源码目录
- 现在需要重新打开 Unity 触发完整重编译

## 不要做的事
- 不顺手删整个 `Library`
- 不顺手删 `Assets/`
- 不顺手改 `.meta`
- 不顺手回退场景
- 不顺手重建动画引用

## 成功判定
满足以下全部条件才算完成：
1. `Library/ScriptAssemblies`、`Library/Bee`、`Temp` 已清掉。
2. 源码文件未被修改。
3. 用户得到明确提示：重新打开 Unity 以重新完整编译。
