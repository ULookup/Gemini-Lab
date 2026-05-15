# Modules/Pet/ — 宠物养成与 FSM 绑定

## 文件夹职责
负责**宠物实体**的运行时表现：状态值/性格值数据、HFSM 状态节点宿主、需求权重评估与对外事件广播。

## 核心文件/类（预估）

| 文件 | 说明 |
| :--- | :--- |
| `PetService.cs` | 对外门面（`IPetService`），提供查询状态/下发指令 API。 |
| `PetController.cs` | `MonoBehaviour` 运行时宿主，驱动 FSM Tick 与 2D 动画，并确保存在 `SpriteRenderer` / `Animator` / `Rigidbody2D` / `CapsuleCollider2D`。 |
| `PetContext.cs` | FSM 上下文数据包（引用 StateValues、Personality、2D NavAgent 桥接）。 |
| `States/IdleState.cs` | 空闲态，扫描家具需求权重。 |
| `States/MovingState.cs` | 调用 Navigation 模块寻路；位置推进统一通过 `Rigidbody2D.MovePosition`，并根据速度方向翻转 Sprite (`SpriteRenderer.flipX`)。 |
| `States/InteractingState.cs` | 播放 2D Animator 指定家具交互动画 + 状态值增减。 |
| `States/WorkingState.cs` | 被 Gateway 锁定，等待"任务完成"事件。 |
| `States/SleepingState.cs` | 精力耗尽或指令触发，强制唤醒触发惩罚。 |
| `Data/PetStateValues.cs` | 心情/饱食/精力/经验值结构。 |
| `Data/PersonalityMatrix.cs` | 正直/冷静/邪恶/善良/勇敢/懦弱/害羞 7 维。 |
| `Services/StatTickService.cs` | 每秒衰减/恢复数值。 |
| `Events/PetEvents.cs` | `PetStateChangedEvent`、`PetMoodChangedEvent` 等。 |

## 依赖关系
- **依赖**：`Core`（FSM、EventBus、ServiceLocator）、`Navigation`（仅接口 `INavigationService`）。
- **被依赖**：`Gateway`（读取状态值组装 Prompt）、`Furniture`（交互回调写入 StateValues）、`UI`（订阅状态变更事件）、`Travel`、`Persistence`。
- asmdef：`GeminiLab.Modules.Pet`，仅引用 `GeminiLab.Core`、`GeminiLab.Modules.Navigation.Abstractions`。

## 代码规范/注意事项
1. **所有状态值变更必须走 `StatTickService`**，不允许在其他地方直接赋值 `StateValues.Mood = xxx`，否则事件无法广播。
2. 性格值演化遵循"事件 → 累积 → 阶跃"三段式：记录事件、累加权重、达到阈值再改变性格等级。
3. `PetController` **禁止** `using UnityEngine.UI`；UI 只能订阅事件。
4. FSM 状态转移条件集中配置于 `PetStateMachineBuilder.cs`，便于可视化审查。
5. 强制唤醒惩罚、打断权重等"魔法数字"必须以 ScriptableObject 暴露到 `_Project/ScriptableObjects/PetConfig/`。
6. 2D 渲染顺序：宠物根节点挂 `SortingGroup`，`sortingLayer = "Pet"`；`sortingOrder` 按 Y 轴动态刷新以实现与家具的正确遮挡。
7. 所有物理位移走 `Rigidbody2D` (`bodyType = Dynamic`, `gravityScale = 0`, `freezeRotation = Z`)，严禁直接修改 `transform.position` 作为常规移动链，否则 NavAgent 与 Physics2D 都会错位。
8. 当前玩家控制链与运行时位置回写都应通过 `PetController` 内部统一的物理位置入口推进，不允许在别的脚本里绕过刚体直接推角色。
9. 桌宠活动范围当前采用场景内独立的 `PetMovementBounds`（`BoxCollider2D` + `isTrigger = true`）表示矩形可行动区域；`PetController` 在移动推进前会按这个矩形范围做位置钳制，后续只需在 Inspector 里调整该 `BoxCollider2D` 的位置和尺寸即可。
