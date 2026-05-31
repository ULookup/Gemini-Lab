# PetStatus Energy Bar + Personality Radar Binding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Energy fill bars (0-100%) to PetStatus stat panels and wire personality radar to react to `MatrixChanged` events.

**Architecture:** Single-file C# change (`ProfilePanelStub.cs`) adds fill-bar fields and subscribes to `MatrixChanged`. Unity MCP creates `PersonalityMatrixSO` assets, adds fill-bar child Images to the panels, and assigns SOs to PetControllers. No new classes.

**Tech Stack:** Unity UI (Image.Filled), C# events, Unity MCP tools

---

### Task 1: Add Energy fill bar fields and logic to ProfilePanelStub

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs`

- [ ] **Step 1: Add serialized Image fields for fill bars**

Add two fields under the existing Energy text fields:

```csharp
// Add after _angelEnergyText field (line 28):
[SerializeField] private Image? _angelEnergyFill;

// Add after _evilEnergyText field (line 37):
[SerializeField] private Image? _evilEnergyFill;
```

- [ ] **Step 2: Wire fillAmount in RefreshPet**

Update the `RefreshPet` method signature to accept an `Image? energyFill` parameter, and inside the method set `fillAmount`:

```csharp
private void RefreshPet(PetId id, TMP_Text? moodText, TMP_Text? energyText,
    TMP_Text? relationText, PersonalityRadarGraphic? radar, Image? energyFill)
{
    if (_roster != null)
    {
        var data = _roster.TryGet(id);
        if (data != null)
        {
            if (moodText != null) moodText.text = Mathf.RoundToInt(data.Mood).ToString();
            if (energyText != null) energyText.text = Mathf.RoundToInt(data.Energy).ToString();

            if (energyFill != null) energyFill.fillAmount = data.Energy / 100f;
        }
    }
    // ... rest unchanged
}
```

- [ ] **Step 3: Update RefreshAll to pass new parameter**

```csharp
private void RefreshAll()
{
    RefreshPet(PetId.Angel, _angelMoodText, _angelEnergyText, _angelRelationText, _angelRadar, _angelEnergyFill);
    RefreshPet(PetId.Devil, _evilMoodText, _evilEnergyText, _evilRelationText, _evilRadar, _evilEnergyFill);
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs
git commit -m "feat(petstatus): add Energy fill bar wiring to ProfilePanelStub"
```

---

### Task 2: Subscribe to MatrixChanged for real-time radar updates

**Files:**
- Modify: `Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs`

- [ ] **Step 1: Add subscription field**

Add a field for the MatrixChanged subscription (below `_snapshotSub`):

```csharp
private IDisposable? _snapshotSub;
private IDisposable? _matrixSub;     // <-- add this line
```

- [ ] **Step 2: Subscribe to MatrixChanged in SubscribeSnapshotIfNeeded**

After the `_snapshotSub` subscription block, add `MatrixChanged` subscription:

```csharp
private void SubscribeSnapshotIfNeeded()
{
    if (_snapshotSub != null) return;
    if (ServiceLocator.TryResolve(out EventBus? bus) && bus is not null)
    {
        _snapshotSub = bus.Subscribe<PetRuntimeSnapshotChangedEvent>(OnSnapshotChanged);
    }

    // Subscribe to personality evolution changes
    if (_matrixSub == null &&
        ServiceLocator.TryResolve(out IPersonalityEvolutionService? evolution) &&
        evolution != null)
    {
        evolution.MatrixChanged += OnMatrixChanged;
        _matrixSub = new ActionDisposable(() => evolution.MatrixChanged -= OnMatrixChanged);
    }
}

private void OnMatrixChanged(PetId _, PersonalityVector __)
{
    RefreshAll();
}
```

- [ ] **Step 3: Add ActionDisposable helper if not already in project**

Check if `GeminiLab.Core.ActionDisposable` exists. If not, add a simple one:

```bash
grep -r "class ActionDisposable" Assets/_Project/ || echo "NOT_FOUND"
```

If NOT_FOUND, create `Assets/_Project/Scripts/Core/ActionDisposable.cs`:

```csharp
#nullable enable
using System;

namespace GeminiLab.Core
{
    public sealed class ActionDisposable : IDisposable
    {
        private Action? _dispose;
        public ActionDisposable(Action dispose) => _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}
```

- [ ] **Step 4: Dispose _matrixSub in OnDestroy and OnClose**

Update `OnDestroy`:

```csharp
protected override void OnDestroy()
{
    _snapshotSub?.Dispose();
    _matrixSub?.Dispose();
    base.OnDestroy();
}
```

Update `OnClose`:

```csharp
public override void OnClose()
{
    base.OnClose();
    _snapshotSub?.Dispose();
    _snapshotSub = null;
    _matrixSub?.Dispose();
    _matrixSub = null;
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scripts/Modules/HubUI/Panels/ProfilePanelStub.cs
# If ActionDisposable was created:
git add Assets/_Project/Scripts/Core/ActionDisposable.cs Assets/_Project/Scripts/Core/ActionDisposable.cs.meta
git commit -m "feat(petstatus): subscribe to MatrixChanged for real-time radar updates"
```

---

### Task 3: Create PersonalityMatrixSO assets and assign to PetControllers

**Files:**
- Create: `Assets/_Project/ScriptableObjects/PersonalityMatrix_Angel.asset`
- Create: `Assets/_Project/ScriptableObjects/PersonalityMatrix_Devil.asset`
- Modify: Scene PetControllers (assign `_personality` field)

- [ ] **Step 1: Check existing ScriptableObjects directory**

```
Assets/_Project/ScriptableObjects/PersonalityConfig/ already exists — use it.
```

- [ ] **Step 2: Create Angel PersonalityMatrixSO via MCP**

Use `mcp__ai-game-developer__script-execute` to run a one-shot C# script that creates the SO:

```csharp
var so = ScriptableObject.CreateInstance<PersonalityMatrixSO>();
so.Kindness = 0.7f; so.Evilness = -0.3f; so.Calmness = 0.3f;
so.Bravery = 0.2f; so.Shyness = -0.1f; so.Integrity = 0.5f; so.Curiosity = 0.4f;
UnityEditor.AssetDatabase.CreateAsset(so, "Assets/_Project/ScriptableObjects/PersonalityConfig/PersonalityMatrix_Angel.asset");
UnityEditor.AssetDatabase.SaveAssets();
```

- [ ] **Step 3: Create Devil PersonalityMatrixSO via MCP**

Same approach, different values:

```csharp
var so = ScriptableObject.CreateInstance<PersonalityMatrixSO>();
so.Kindness = -0.2f; so.Evilness = 0.6f; so.Calmness = -0.1f;
so.Bravery = 0.4f; so.Shyness = 0.1f; so.Integrity = -0.3f; so.Curiosity = 0.5f;
UnityEditor.AssetDatabase.CreateAsset(so, "Assets/_Project/ScriptableObjects/PersonalityConfig/PersonalityMatrix_Devil.asset");
UnityEditor.AssetDatabase.SaveAssets();
```

- [ ] **Step 4: Assign SOs to PetControllers in the scene**

Use `mcp__ai-game-developer__gameobject-component-modify` to set the `_personality` field. First find the PetController GameObjects with `gameobject-find`, then modify the component:

```
mcp: gameobject-component-modify --gameObjectName "<AngelPetGO>" --componentName "PetController" --field "_personality" --value "PersonalityMatrix_Angel.asset"
mcp: gameobject-component-modify --gameObjectName "<DevilPetGO>" --componentName "PetController" --field "_personality" --value "PersonalityMatrix_Devil.asset"
```

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/ScriptableObjects/PersonalityConfig/PersonalityMatrix_Angel.asset*
git add Assets/_Project/ScriptableObjects/PersonalityConfig/PersonalityMatrix_Devil.asset*
git add Assets/_Project/Scenes/Apartment/Apartment_Main.unity
git commit -m "feat(petstatus): add PersonalityMatrixSO assets and assign to PetControllers"
```

---

### Task 4: Add EnergyBar_Fill child Images to PetStatus panel

**Files:**
- Modify: PetStatus panel in `Apartment_Main.unity` (or the PetStatus prefab)

- [ ] **Step 1: Find AngelStatPanel and EvilStatPanel GameObjects**

Use `mcp__ai-game-developer__gameobject-find` with searchTerm "AngelStatPanel" and "EvilStatPanel". Search under the Panel_PetStatus hierarchy.

- [ ] **Step 2: Create EnergyBar_Fill under AngelStatPanel**

Use `mcp__ai-game-developer__gameobject-create` to add a child GameObject with Image component, then `mcp__ai-game-developer__gameobject-component-modify` to configure:

```
Create: "EnergyBar_Fill" child under AngelStatPanel
Add: UnityEngine.UI.Image component
Set: Image.type = Image.Type.Filled (value: 3)
Set: Image.fillMethod = Image.FillMethod.Horizontal (value: 0)
Set: Image.fillOrigin = 0 (Left)
```

Position the fill Image to match the EnergyBar_Background dimensions (same rect, anchored top-left).

- [ ] **Step 3: Create EnergyBar_Fill under EvilStatPanel**

Repeat Step 2 under the EvilStatPanel GameObject.

- [ ] **Step 4: Wire _angelEnergyFill / _evilEnergyFill references**

Use `mcp__ai-game-developer__gameobject-component-modify` on the `ProfilePanelStub` component to assign the new `EnergyBar_Fill` Image references to the `_angelEnergyFill` and `_evilEnergyFill` fields.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Scenes/Apartment/Apartment_Main.unity
git commit -m "feat(petstatus): add EnergyBar_Fill images to stat panels, wire references"
```
