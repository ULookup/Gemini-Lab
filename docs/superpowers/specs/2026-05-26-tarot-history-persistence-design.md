# 塔罗历史记录解读文本持久化 — 设计文档

**日期**: 2026-05-26
**状态**: 设计中

---

## 概述

目前塔罗 LLM 完整解读文本（`TarotReading.Text`）、运势总结（`TarotSummaryResult`）、幸运提示等数据仅存在于运行时的 `TarotSession` 中，不持久化。用户关闭面板 / 重启游戏后这些数据丢失，历史记录只能看到卡面缩略图和星级，无法查看详细解读内容。

**目标**：将完整 session 数据（三牌 + 六段双宠解读 + 运势总结 + 幸运提示 + 建议）持久化并入档，在历史记录中点击条目可查看完整详情。

---

## 1. 数据模型：`TarotSessionRecord`

**新文件**: `Assets/_Project/Scripts/Modules/Tarot/TarotSessionRecord.cs`

以 session 为单位的纯数据 `[Serializable]` 类，平铺字段兼容 `UnityEngine.JsonUtility`：

```csharp
[Serializable]
public sealed class TarotSessionRecord
{
    public string SessionId;           // "tarot_session_<dateIso>_<hash>"
    public string Question;            // 用户问题，空=今日运势
    public string SessionDateIso;      // "2026-05-26"

    // 过去
    public string PastCardId;
    public string PastOrientation;     // "upright" / "reversed"
    public string PastAngelReading;
    public string PastDevilReading;

    // 当下
    public string PresentCardId;
    public string PresentOrientation;
    public string PresentAngelReading;
    public string PresentDevilReading;

    // 未来
    public string FutureCardId;
    public string FutureOrientation;
    public string FutureAngelReading;
    public string FutureDevilReading;

    // 总结
    public int FortuneLevel;           // 1-5
    public string LuckyColor;
    public string LuckyNumber;
    public string LuckyTime;
    public string LuckyAction;
    public string Advice;
}
```

无 Dictionary / 嵌套对象，平铺保证 `JsonUtility` 序列化稳定。

---

## 2. 存储服务：`TarotSessionRecordStore`

**新文件**: `Assets/_Project/Scripts/Modules/Tarot/TarotSessionRecordStore.cs`

实现 `IPersistentService`，key = `"tarot_history"`：

```csharp
public interface ITarotSessionRecordStore
{
    void Add(TarotSessionRecord record);
    IReadOnlyList<TarotSessionRecord> GetAll();       // 日期倒序
    TarotSessionRecord? GetLatest();
    bool Remove(string sessionId);
}

public sealed class TarotSessionRecordStore : ITarotSessionRecordStore, IPersistentService
{
    // 内部 List<TarotSessionRecord> _records
    // Key => "tarot_history"
    // CaptureJson / RestoreJson: JsonUtility.ToJson / FromJson
}
```

- `Add()`: 按 `SessionId` 去重覆盖
- `CaptureJson()`: 将 `_records` 包装为 `{ version, records[] }` 序列化
- `RestoreJson()`: 反序列化并回填 `_records`

---

## 3. 注册与 DI 接入

**修改文件**: `Assets/_Project/Scripts/Modules/Tarot/TarotRuntimeBootstrap.cs`（如不存在则新建）

- 创建 `TarotSessionRecordStore` 实例
- 注册到 `ServiceLocator`（接口 `ITarotSessionRecordStore`）
- 注册到 `IPersistentServiceRegistry`

---

## 4. 写入时机

**修改文件**: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotPanelStub.cs`

在 `SaveToCollection()` 方法中追加写入逻辑：

```
SaveToCollection(session)
  ├── 既有逻辑: 3 条 CollectionEntry → CollectionService.Add()
  └── 新增逻辑:
      1. 从 session 构建 1 条 TarotSessionRecord
      2. 提取 6 段解读（session.Readings["past_angel"] 等）
      3. 提取总结（session.SummaryResult）
      4. 提取三张卡的 Id 和 Orientation
      5. _recordStore.Add(record)
```

`TarotPanelStub` 新增 `ITarotSessionRecordStore? _recordStore` 字段并在 `EnsureServices()` 中注入。

---

## 5. UI：历史详情弹窗

### 5a. `TarotHistoryEntry` 点击事件

**修改文件**: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotHistoryEntry.cs`

- 新增 `Button? _button` 序列化字段
- 新增 `event Action<TarotHistoryEntry>? OnClicked`
- `Awake()` 中绑定按钮
- `SetData()` 中存储当前关联的 `TarotSessionRecord`（或 sessionId）

### 5b. 新建 `TarotHistoryDetailPopup`

**新文件**: `Assets/_Project/Scripts/Modules/HubUI/Panels/TarotHistoryDetailPopup.cs`

全屏弹窗，结构：

| 区域 | 内容 |
|------|------|
| 顶部信息栏 | 日期 + 问题文本（或"今日整体运势"）+ 星级 |
| 三卡概览 | 卡面缩略图 + 正/逆位标注（过去/当下/未来） |
| Angel 解读 | 三个槽位各一段 LLM 解读正文（ScrollView） |
| Devil 解读 | 同上 |
| 幸运提示 | Color / Number / Time / Action 四格 |
| 建议 | Advice 文本 |
| 关闭按钮 | 关闭弹窗 + 遮罩层点击关闭（淡入淡出动画） |

Inspector 绑定字段：
```csharp
// 顶部
[SerializeField] TMP_Text _dateText;
[SerializeField] TMP_Text _questionText;
[SerializeField] TMP_Text _starsText;

// 三卡
[SerializeField] Image _cardImg1/_cardImg2/_cardImg3;
[SerializeField] TMP_Text _cardLabel1/_cardLabel2/_cardLabel3;

// Angel 解读（ScrollView）
[SerializeField] TMP_Text _angelPast/_angelPresent/_angelFuture;

// Devil 解读（ScrollView）
[SerializeField] TMP_Text _devilPast/_devilPresent/_devilFuture;

// 幸运提示
[SerializeField] TMP_Text _luckyColor/_luckyNumber/_luckyTime/_luckyAction;

// 建议
[SerializeField] TMP_Text _adviceText;

// 操作
[SerializeField] Button _closeButton;
[SerializeField] Button _overlayButton;
[SerializeField] CanvasGroup _canvasGroup;
```

`Show(TarotSessionRecord, TarotDeckSO deck)` — 从 deck 查卡面 Sprite，填充所有字段，淡入。
`Hide()` — 淡出后 `SetActive(false)`。

### 5c. `TarotPanelStub.PopulateHistory()` 修改

- 构造 `TarotHistoryEntry` 时连带传入 `TarotSessionRecord`（从 `TarotSessionRecordStore` 匹配）
- 绑定 `item.OnClicked` → `_historyDetailPopup.Show(record, deck)`
- 新增 `[SerializeField] TarotHistoryDetailPopup? _historyDetailPopup`

---

## 6. 变更总览

| 操作 | 文件 |
|------|------|
| 新建 | `Modules/Tarot/TarotSessionRecord.cs` |
| 新建 | `Modules/Tarot/TarotSessionRecordStore.cs` |
| 修改 | `Modules/Tarot/TarotRuntimeBootstrap.cs` — 注册 TarotSessionRecordStore |
| 新建 | `Modules/HubUI/Panels/TarotHistoryDetailPopup.cs` |
| 修改 | `Modules/HubUI/Panels/TarotPanelStub.cs` |
| 修改 | `Modules/HubUI/Panels/TarotHistoryEntry.cs` |
| 新建 | Prefab `TarotHistoryDetailPopup.prefab`（Unity Editor 搭建） |

---

## 7. 自检清单

- [x] 无 TBD / TODO / 占位符
- [x] 模型字段覆盖 session 中所有需持久化的数据
- [x] 序列化方案与现有一致（JsonUtility）
- [x] 持久化注册表注册不遗漏
- [x] UI 弹窗内容与需求一致（三牌 + 六解读 + 总结）
- [x] 范围聚焦，无无关变更
