# PetStatus: Energy Status Bar + Personality Radar Binding

## Overview

Add an Energy fill bar (0-100%) to the PetStatus panel's stat sections, and wire the personality radar to react to real-time personality evolution events.

## 1. Energy Status Bar

### UI Structure

Each pet side (AngelStatPanel / EvilStatPanel) gets a fill-bar child:

```
AngelStatPanel / EvilStatPanel
  └── EnergyBar_Background   (existing base Image, unmodified)
        └── EnergyBar_Fill    (new Image, Type=Filled, Horizontal, fillAmount driven by code)
```

### Code Changes (`ProfilePanelStub.cs`)

- Add two `[SerializeField] private Image?` fields: `_angelEnergyFill`, `_evilEnergyFill`
- In `RefreshPet()`, after setting energy text, set `fill.fillAmount = data.Energy / 100f`
- No new subscriptions needed — `RefreshPet()` is already called on every `PetRuntimeSnapshotChangedEvent`

### Data Flow

```
PetController → PetRuntimeSnapshotChangedEvent.Energy
  → ProfilePanelStub.RefreshPet()
    → EnergyBar_Fill.fillAmount = Energy / 100f
```

## 2. Personality Radar Real-time Binding

### Problem

- No `PersonalityMatrixSO` assets exist → `GetMatrix()` returns default (all zeros) → radar stuck at 50%
- `ProfilePanelStub` does not subscribe to `IPersonalityEvolutionService.MatrixChanged` → radar never updates after personality evolves

### Solution

**a) Create PersonalityMatrixSO assets**

Two assets with initial 7-dimension values (range -1..1), one per pet. Assign to each `PetController._personality` in the scene.

**b) Subscribe to MatrixChanged in ProfilePanelStub**

In `SubscribeSnapshotIfNeeded()`, after resolving `IPersonalityEvolutionService`, subscribe to `MatrixChanged`:

```csharp
_matrixSub = evolution.MatrixChanged += (_, _) => RefreshAll();
```

Dispose in `OnDestroy` / `OnClose`.

## 3. Files Changed

| File | Change |
|---|---|
| `ProfilePanelStub.cs` | +2 serialized Image fields for fill bars; fillAmount assignment in RefreshPet; MatrixChanged subscription |
| Unity: PetStatus panel prefab/scene | Add EnergyBar_Fill child Images under each StatPanel |
| Unity: PersonalityMatrixSO assets ×2 (new) | Angel/Devil initial personality config |
| Unity: Scene PetControllers | Assign PersonalityMatrixSO to _personality field |

## 4. Testing

- Unit: `ProfilePanelStub` gets EnergyBar fill wired (verify fillAmount = Energy/100f)
- Unity Editor: drag bar from 0-100% visually
- Unity Editor: create PersonalityMatrixSO, assign to PetController, verify radar shows non-zero shape
- Unity Editor: trigger tarot draw or furniture interaction, verify radar updates in real time
