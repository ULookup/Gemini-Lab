# UI/Widgets

## 当前定位
这里是可复用 UI 小控件 prefab 的工程落点，例如列表项、页签按钮、Tooltip、塔罗历史条目、塔罗图鉴卡片等。

当前阶段只建立目录和收口规则，不在本轮制作或绑定任何新的美术资源。

## 后续迁移规则
- `TarotHistoryEntry`、`TarotGuideCard` 后续可从 `UI/Tarot` 逐步归并到本目录或保留为 Tarot 子域 prefab。
- Inventory slot、Collection entry、Garden plot / seed button 等运行时生成控件，稳定后可以沉淀为 prefab。
- Widget prefab 必须通过序列化字段接收数据，不直接依赖场景对象名。
- Widget 的视觉贴图、颜色与最终样式由人工美术作者化后再绑定。
