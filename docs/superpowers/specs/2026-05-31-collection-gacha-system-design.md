# Collection Gacha System Design

**Date**: 2026-05-31
**Branch**: merge/pr-14-devil-anim-space-sys

## Overview

在 Apartment 场景 Sidebar 的 Collection 页面上，实现完整的抽卡（gacha）系统：单抽/五连抽、金币管理、宠物掉落金币、收藏品展示。

---

## 1. Collectible Items

7 个收藏物，等概率抽取。每个归属于一个分类 tag：

| ID | 名称 | 分类 tag | 素材 |
|----|------|----------|------|
| acrylic_sign | Acrylic sign | partner_tag | Acrylic sign.png |
| photo | photo | partner_tag | photo.png |
| polaroid | Polaroid | partner_tag | Polaroid.png |
| postcard | postcard | partner_tag | postcard.png |
| sticker | sticker | partner_tag | sticker.png |
| angel_badge | angel_badge | angel_tag | angel_badge.png |
| evil_badge | evil_badge | evil_tag | evil_badge.png |

---

## 2. Gacha Mechanics

- **单抽**: 消耗 100 金币，抽取 1 个收藏物
- **五连抽**: 消耗 500 金币，一次性抽取 5 个
- **概率**: 等概率，每个 1/7
- **重复处理**: 抽到已解锁的收藏物，返还 30 金币
- **无保底**: 等概率下不需要保底机制

---

## 3. Coin System

### CoinService

- 实现 `ICoinService` 接口 / `IPersistentService` 持久化
- `Balance` 属性，`Add(amount)`, `TrySpend(amount) → bool`
- 余额变化发布 `CoinChangedEvent`
- 保存/加载到持久化数据

### 金币获取（CoinDropController）

- 挂在 Pet 父节点
- 每 10 秒判定一次，随机掉落 10~40 金币
- 金币以世界空间 GameObject 形式出现在宠物位置 + 随机偏移
- 玩家鼠标点击 → `CoinService.Add(amount)` → 销毁
- 5 秒内未点击 → 自动销毁
- 使用 `coin_button.png` 作为金币图标

---

## 4. Collection Board UI

### 分页展示

- 3 个 tag 按钮：partner_tag, angel_tag, evil_tag
- 点击 tag 切换显示对应分类的收藏物
- 已解锁 → 显示实际 sprite
- 未解锁 → 显示 `unlocked.png` 占位

### 抽卡结果

- 抽卡后直接弹出 `reward_window`
- 展示抽到的收藏物（含重复转化金币提示）
- 玩家关闭后刷新 board

---

## 5. Architecture

```
EventBus
  ├── GachaService ──→ ICollectionService.Add()
  ├── CoinService (IPersistentService)
  ├── CoinDropController (MonoBehaviour, on Pet)
  └── GachaPanelController (StubPanelBase)
```

### Events

| Event | Publisher | Subscribers |
|-------|-----------|-------------|
| `CoinChangedEvent(balance)` | CoinService | GachaPanelController (UI) |
| `CoinCollectedEvent(amount)` | CoinDropController | Bootstrap (log/audio) |
| `GachaPullEvent(items)` | GachaService | Bootstrap (log/audio) |

### Data Models

```
GachaItem { string Id; bool IsNew; }
GachaResult { List<GachaItem> Items; int CoinRefund; }
```

---

## 6. Files

### New Files

| File | Path |
|------|------|
| ICoinService.cs | `Modules/Collection/` |
| CoinService.cs | `Modules/Collection/` |
| IGachaService.cs | `Modules/Collection/` |
| GachaService.cs | `Modules/Collection/` |
| CoinChangedEvent.cs | `Modules/Collection/` |
| CoinCollectedEvent.cs | `Modules/Collection/` |
| GachaPullEvent.cs | `Modules/Collection/` |
| GachaItem.cs | `Modules/Collection/` |
| GachaResult.cs | `Modules/Collection/` |
| CoinDropController.cs | `Modules/Collection/` |
| GachaPanelController.cs | `Modules/HubUI/Panels/` |
| GachaRuntimeBootstrap.cs | `Modules/Collection/` |

### Modified Files

| File | Change |
|------|--------|
| `CollectionCategory.cs` | Add `GachaCollectible = 4` |
| `Apartment_Main.unity` | Replace CollectionPanelStub with GachaPanelController on Panel_Collection |

---

## 7. Edge Cases

- 金币不足时点击抽卡：按钮置灰 / 弹出提示
- 7 个已全部解锁：仍可抽卡但全为重复（纯返还金币）
- 宠物不存在时：CoinDropController 不报错，静默跳过
- 场景加载：CoinService 从持久化恢复余额
