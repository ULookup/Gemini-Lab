# Pet Animation Reference Rebuild

## 适用场景
当用户明确要求：
- “重建 Pet 动画引用”
- “把 Pet 的动画资源重新接回 controller”
- “重新整理 Move / Idle / Interact / Sleep 的动画引用”

并且目标是：
- 优先修复 `Pet_Angel` 的动画资源引用链
- 不顺手扩大到其他模块
- 先核对资源与 clip，再修改 controller 或场景绑定

这个 skill 就应该使用。

## 目标
安全完成以下结果：
1. 确认当前 Pet 动画资源、`.anim`、`.controller` 的真实状态。
2. 明确是资源命名问题、clip 引用问题，还是 controller 状态引用问题。
3. 只修复 Pet 动画引用链本身。
4. 不把“动画引用修复”扩大成“顺手改交互逻辑、顺手改场景布局”。

## 前置规则
1. 必须先遵守 `AGENTS.md`：
   - 先复述理解
   - 用户明确说“执行”后再操作
2. 必须先确认：
   - 当前工作树状态
   - 目标资源是否真实存在
   - 当前 `.anim` 和 `.controller` 是否已存在
3. 如果动画资源命名、目录结构或 `.meta` 仍在变化中，必须先告诉用户当前修复会受影响。
4. 这是中高风险操作：
   - 可以改 `.anim`
   - 可以改 `.controller`
   - 可以改场景里 `PetController` 的 controller 绑定
   - 但不要顺手改无关场景对象

## 标准流程

### Step 1. 只读盘点
优先检查：
- `Assets/_Project/Art/Sprites/Pet/Frames/**`
- `Assets/_Project/Animations/Pet/**`
- `Assets/_Project/Scenes/Apartment/Apartment_Main.unity`
- `Assets/_Project/Scripts/Editor/Pet/PetMoveAnimationSetupEditor.cs`

目标：
- 先分清是资源层、clip 层、controller 层还是场景绑定层的问题

### Step 2. 明确范围
默认只处理：
- `Pet_Angel` 的 `Move / Idle / Interact / Sleep`

不默认处理：
- `Emotion`
- 其他宠物
- 家具交互逻辑本身

### Step 3. 修复顺序
推荐顺序：
1. 资源命名 / 目录核对
2. `.anim` clip 引用修复
3. `.controller` 状态与过渡修复
4. 场景中 `PetController` 的 controller 绑定修复

### Step 4. 验证
至少确认：
- 资源 GUID 是否稳定
- `Pet_Angel.controller` 是否引用正确 clip
- 场景中的 `Pet_Angel` 是否绑定了正确 controller

### Step 5. 回报结果
至少告诉用户：
- 修了哪一层引用链
- 还有哪一层没动
- 当前是否仍缺美术资源

## 不要做的事
- 不顺手改 `Apartment_Main` 的家具布局
- 不顺手改 `FurnitureService`
- 不顺手改 Git 分支
- 不把引用修复扩大成“重做整套动画系统”

## 成功判定
满足以下全部条件才算完成：
1. `Pet_Angel` 目标动画链的引用关系已恢复一致。
2. 修改范围没有超出 Pet 动画引用链。
3. 结果已清楚区分“已修复的引用问题”和“仍缺失的美术资源问题”。
