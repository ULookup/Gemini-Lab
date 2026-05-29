# UI/Panels

## 当前定位
这里是 Apartment / Hub UI 面板 prefab 的工程落点。

当前阶段只建立目录和收口规则，不在本轮绑定任何 `profile`、`spacesystem`、`tarot` 美术资源，也不把未定稿的场景 UI 强行转成 prefab。

## 后续迁移规则
- `Panel_PetStatus`、`Panel_Tarot`、`Panel_Inventory`、`Panel_Collection`、`Panel_Garden` 后续稳定后迁入本目录。
- 面板 prefab 根节点应挂对应 `IUIPanel` 实现，例如 `ProfilePanelStub`、`TarotPanelStub`、`InventoryPanelStub`。
- 面板 prefab 只保存结构、组件引用和可复用行为；最终贴图、立绘和视觉定稿由人工美术作者化后再绑定。
- 不允许 prefab 引用具体场景里的 GameObject。
