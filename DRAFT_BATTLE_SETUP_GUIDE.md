# Draft & Battle Systems - Unity Editor Setup Guide

## Overview

The Draft and Battle systems have been implemented and integrated with MatchController. This guide will walk you through setting up these systems in your Unity scenes.

## What's Been Implemented

### Core Controllers
1. **BattleController** - Manages 30-second battle timer and victory conditions
2. **DraftController** - Manages 15-second draft timer and option generation
3. **MatchController** - Updated to integrate with both controllers

### Key Features
- ✅ Draft phase with 3 random options per team
- ✅ Auto-selection on timeout (with 5-second warning)
- ✅ Battle timer with instant victory or HP comparison
- ✅ Proper round result tracking
- ✅ Event-driven architecture for UI integration
- ✅ Performance optimized (LINQ allocations fixed)

---

## Unity Editor Setup

### Step 1: Update BattleTest Scene

1. **Open Scene**: `Assets/_Project/Scenes/BattleTest.unity`

2. **Locate MatchController GameObject** (or create one if it doesn't exist):
   - If creating new: Right-click in Hierarchy → Create Empty → Name it "MatchController"

3. **Add Required Components** to MatchController GameObject:
   ```
   - MatchController (script)
   - DraftController (script)
   - BattleController (script)
   - TroopSpawner (script) - should already exist
   ```

4. **Inspector Setup** for MatchController:
   - The controller references should auto-populate in Awake()
   - If they don't, manually drag components into:
     - **Draft Controller** → DraftController component
     - **Battle Controller** → BattleController component
     - **Troop Spawner** → TroopSpawner component

5. **Verify GameManager** exists in scene:
   - Should have GameManager script
   - GameConfig assigned to Config field

---

### Step 2: Configure Test Flow

#### Option A: Automated Test (Recommended for Now)

Since UI is not yet implemented, the systems will work headlessly:

1. **Keep BattleTestController** for manual testing:
   - Select MatchController GameObject
   - Add a simple test script to start the match:

```csharp
// Add this to a test script or modify BattleTestController
void Start()
{
    var matchController = GetComponent<MatchController>();
    matchController.StartMatchAsync().Forget();
}
```

2. **Expected Flow**:
   - Draft phase: 15 seconds (both teams auto-select random)
   - Spawn phase: 1 second (troops spawn)
   - Battle phase: Up to 30 seconds (until elimination or timeout)
   - Round end: 2 seconds
   - Repeat for 7 rounds or until 4 wins

#### Option B: Manual Draft Input (When UI is Ready)

1. Subscribe to DraftController events in a test script:

```csharp
void Start()
{
    var draftController = FindObjectOfType<DraftController>();
    draftController.OnPlayerOptionsGenerated += options =>
    {
        Debug.Log("Draft Options:");
        for (int i = 0; i < options.Count; i++)
        {
            Debug.Log($"  [{i}] {options[i].DisplayName}");
        }
    };

    // To manually select option 0:
    draftController.OnPlayerOptionsGenerated += options =>
    {
        draftController.SelectCombination(options[0]);
    };
}
```

---

### Step 3: Console Monitoring

When you press Play, watch the Console for these log patterns:

**Match Start**:
```
Match starting...
Loaded X base combinations
MatchController initialized successfully
```

**Round Flow**:
```
=== Round 1 Start ===
Draft phase started
Generated draft options - Player: 3 | AI: 3
AI selected: [combination name]
Player auto-selected: [combination name] (timeout)
Draft phase ended - Player: [name] | AI: [name]

Spawn phase started
Initialized [troop] on team Player with [HP] HP
Initialized [troop] on team AI with [HP] HP
Spawn phase ended

Battle phase started!
Battle ended! Winner: [Player/AI] ([method])
Battle phase ended - Winner: [Player/AI] | Method: [Elimination/HP Comparison]

Round end phase started
Round 1 winner: [Player/AI]
Score - Player: X, AI: Y
Round end phase ended
```

**Battle Victory Methods**:
- `"Elimination"` - One team reduced to 0 troops
- `"HP Comparison"` - Timer expired, winner determined by total HP

---

### Step 4: Event Integration (For Future UI)

All controllers expose events for UI integration:

#### DraftController Events
```csharp
// Subscribe to these in your UI scripts
draftController.OnPlayerOptionsGenerated += (options) => { /* Update card UI */ };
draftController.OnTimerUpdated += (timeRemaining) => { /* Update timer display */ };
draftController.OnTimerWarning += () => { /* Play warning sound/animation */ };
draftController.OnPlayerSelected += (combo) => { /* Show selection feedback */ };
draftController.OnAISelected += (combo) => { /* Show AI choice */ };
draftController.OnDraftCompleted += (playerCombo, aiCombo) => { /* Transition to spawn */ };
```

#### BattleController Events
```csharp
// Subscribe to these in your UI scripts
battleController.OnBattleStarted += () => { /* Show battle UI */ };
battleController.OnTimerUpdated += (timeRemaining) => { /* Update timer, change color at 10s/5s */ };
battleController.OnBattleEnded += (winner, timerExpired, playerHP, aiHP) => { /* Show results */ };
```

#### MatchController Events
```csharp
// High-level match events
matchController.OnPhaseChanged += (oldPhase, newPhase) => { /* Update UI state */ };
matchController.OnRoundStarted += (roundNumber) => { /* Show "Round X" banner */ };
matchController.OnRoundEnded += (result) => { /* Show round result screen */ };
matchController.OnMatchEnded += (winner) => { /* Show match victory screen */ };
```

---

## Testing Checklist

### Basic Functionality
- [ ] Match starts without errors
- [ ] Draft phase generates 3 options per team
- [ ] Draft timer counts down from 15 seconds
- [ ] Both teams auto-select on timeout
- [ ] Troops spawn correctly (check spawn zones)
- [ ] Battle timer counts down from 30 seconds
- [ ] Victory conditions work (test both elimination and timeout)
- [ ] Round scores update correctly
- [ ] Match ends after 4 wins (best of 7)

### Performance
- [ ] No GC allocations spam in Profiler during battle (fixed LINQ issues)
- [ ] Smooth 60 FPS during 30-second battles
- [ ] No frame drops during troop spawning

### Edge Cases
- [ ] Both teams eliminated simultaneously → Player wins
- [ ] Draft timeout with no selection → Random auto-select
- [ ] Timer expiration with exact HP tie → Player wins
- [ ] Empty BaseCombinations → Error logged gracefully

---

## Current Limitations (To Be Added)

### UI Components (Not Yet Implemented)
- Draft card display
- Draft timer visual
- Battle timer visual
- Round score display
- Battle HP bars
- Victory/defeat screens

### Manual Player Input (Not Yet Implemented)
Currently, player selection is auto-random on timeout. To add manual selection:
1. Create UI buttons for draft cards
2. Call `draftController.SelectCombination(combination)` on button click
3. Verify selection is in `CurrentPlayerOptions` before calling

---

## Debug Commands (Optional)

Add these to a debug script for testing:

```csharp
void Update()
{
    var draftController = FindObjectOfType<DraftController>();
    var battleController = FindObjectOfType<BattleController>();

    // Force select first option during draft
    if (Input.GetKeyDown(KeyCode.Alpha1) && draftController.IsDraftActive)
    {
        draftController.SelectCombination(draftController.CurrentPlayerOptions[0]);
    }

    // Force end battle early
    if (Input.GetKeyDown(KeyCode.E) && battleController.IsBattleActive)
    {
        battleController.StopBattle();
    }

    // Show current state
    if (Input.GetKeyDown(KeyCode.I))
    {
        var matchController = FindObjectOfType<MatchController>();
        Debug.Log($"Phase: {matchController.CurrentPhase} | Round: {matchController.State.CurrentRound} | Score: P{matchController.State.PlayerWins}-{matchController.State.AIWins}A");
    }
}
```

---

## What's Next

1. **UI Implementation** (Draft cards, timers, battle UI)
2. **Visual Polish** (Transitions, animations, particles)
3. **Audio** (Draft timer ticks, battle music, victory sounds)
4. **AI Generation System** (Currently placeholder in RoundEnd phase)

---

## Troubleshooting

**"DraftController not found!"**
- Add DraftController component to MatchController GameObject
- Ensure script is in `Assets/_Project/Scripts/Draft/` folder

**"BattleController not found!"**
- Add BattleController component to MatchController GameObject
- Ensure script is in `Assets/_Project/Scripts/Battle/` folder

**"Draft pool is empty!"**
- Check GameConfig has BaseCombinations assigned
- Verify TroopCombination assets exist in `Assets/_Project/Data/BaseCombinations/`

**"No troops spawning"**
- Check TroopSpawner component is attached
- Verify TroopBase prefab is assigned in TroopSpawner
- Check spawn zones in GameConfig

**"Battle never ends"**
- Check troops have HealthComponent and TroopController
- Verify troops are registering with TargetingSystem
- Check Console for "Battle ended!" log

---

## File Locations

**New Scripts**:
- `Assets/_Project/Scripts/Draft/DraftController.cs`
- `Assets/_Project/Scripts/Battle/BattleController.cs`

**Modified Scripts**:
- `Assets/_Project/Scripts/Match/MatchController.cs`
- `Assets/_Project/Scripts/Match/MatchState.cs`
- `Assets/_Project/Scripts/Combat/TargetingSystem.cs`

**Scenes**:
- `Assets/_Project/Scenes/BattleTest.unity` (update this)

---

## Summary

The Draft and Battle systems are now fully integrated with MatchController. The core game loop is complete and functional:

**Draft (15s) → Spawn (1s) → Battle (30s max) → RoundEnd (2s)** × 7 rounds

All systems use async/await with proper cancellation, emit events for UI integration, and are performance-optimized with zero LINQ allocations in hot paths.

**You can now test the full match flow in Unity Editor without UI - just press Play and watch the Console logs!**
