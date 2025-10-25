# Draft & Battle System - Quick Implementation Guide

## What You're Building

Two complete systems that integrate with your existing `MatchController`:

1. **Draft System** - Player picks 1 of 3 troop combinations in 15 seconds
2. **Battle System** - Troops fight in real-time with 30-second timer and victory detection

---

## File Structure (What to Create)

```
Assets/_Project/Scripts/
├── Draft/
│   ├── DraftController.cs       ← Core draft logic, timer, selection
│   └── DraftResult.cs           ← Simple struct for results
│
├── Battle/
│   ├── BattleController.cs      ← Core battle logic, spawning, victory
│   └── VictoryReason.cs         ← Enum for victory types
│
└── UI/
    ├── DraftUI.cs               ← Draft screen display and input
    ├── DraftCard.cs             ← Individual card component
    └── BattleUI.cs              ← Battle timer and HP display
```

**Plus**:
- Extend `TargetingSystem.cs` with 2 new methods: `GetTotalHP()` and `GetAliveCount()`
- Modify `MatchController.cs` to call Draft/Battle controllers

---

## Core Algorithms (The Important Parts)

### Draft Controller - Option Generation
```csharp
private List<TroopCombination> GenerateOptions(MatchState state)
{
    var pool = new List<TroopCombination>();
    pool.AddRange(state.BaseCombinations);           // 4 base combos
    pool.AddRange(state.AIGeneratedCombinations);    // AI-generated ones

    return pool.OrderBy(x => Random.value).Take(3).ToList();  // Shuffle, pick 3
}
```

### Draft Controller - Timer Loop
```csharp
private async UniTask RunTimerAsync(CancellationToken ct)
{
    timer = 15f;
    bool warningFired = false;

    while (timer > 0 && !hasSelected)
    {
        await UniTask.Delay(100, cancellationToken: ct);  // Update every 100ms
        timer -= 0.1f;

        OnTimerUpdate?.Invoke(timer);  // UI subscribes to this

        if (timer <= 5f && !warningFired)
        {
            warningFired = true;
            OnTimerWarning?.Invoke();  // Trigger visual warning
        }
    }

    if (!hasSelected)
        AutoSelectRandom();  // Timeout: pick random
}
```

### Battle Controller - Spawn with Capacity Limit
```csharp
private void SpawnTroopsWithLimit(TroopCombination combo, Team team)
{
    int currentCount = TargetingSystem.GetAliveTroops(team).Count;
    int availableSlots = config.maxTroopsPerSide - currentCount;  // Max 4

    if (availableSlots <= 0) return;

    // Spawn partial if needed (e.g., want 5 troops but only 2 slots available)
    int amountToSpawn = Mathf.Min(combo.amount, availableSlots);

    var troops = troopSpawner.SpawnTroops(combo, team);  // Use existing spawner
    // Store references to spawned troops
}
```

### Battle Controller - Victory Check (Hybrid)
```csharp
private async UniTask<RoundResult> RunBattleLoopAsync(CancellationToken ct)
{
    battleTimer = 30f;

    while (battleTimer > 0)
    {
        await UniTask.Delay(100, cancellationToken: ct);
        battleTimer -= 0.1f;

        OnTimerUpdate?.Invoke(battleTimer);

        // Instant victory check (event-driven from troop deaths)
        var playerAlive = TargetingSystem.GetAliveTroops(Team.Player).Count;
        var aiAlive = TargetingSystem.GetAliveTroops(Team.AI).Count;

        if (playerAlive == 0 && aiAlive > 0)
            return CreateResult(Team.AI, VictoryReason.Elimination);
        else if (aiAlive == 0 && playerAlive > 0)
            return CreateResult(Team.Player, VictoryReason.Elimination);
        else if (playerAlive == 0 && aiAlive == 0)
            return CreateResult(Team.Player, VictoryReason.Elimination);  // Tie
    }

    // Timer expired: HP comparison
    return DetermineWinnerByHP();
}

private RoundResult DetermineWinnerByHP()
{
    float playerHP = TargetingSystem.GetTotalHP(Team.Player);
    float aiHP = TargetingSystem.GetTotalHP(Team.AI);

    Team winner = (playerHP > aiHP) ? Team.Player :
                  (aiHP > playerHP) ? Team.AI :
                  Team.Player;  // Tie goes to player

    return CreateResult(winner, VictoryReason.TimerExpiration);
}
```

---

## Integration with MatchController

### Current Flow (What Exists)
```csharp
private async UniTask RunRound(int roundNumber, CancellationToken ct)
{
    await RunDraftPhase(ct);    // TODO: Empty placeholder
    await RunSpawnPhase(ct);    // TODO: Empty placeholder
    await RunBattlePhase(ct);   // TODO: Empty placeholder
    await RunRoundEndPhase(ct); // TODO: Random winner
}
```

### New Flow (What You'll Build)
```csharp
[SerializeField] private DraftController draftController;
[SerializeField] private BattleController battleController;

private async UniTask RunDraftPhase(CancellationToken ct)
{
    var result = await draftController.StartDraftAsync(State, ct);

    State.PlayerSelectedCombo = result.PlayerPick;
    State.AISelectedCombo = result.AIPick;
    State.PlayerPickHistory.Add(result.PlayerPick);
}

private async UniTask RunBattlePhase(CancellationToken ct)
{
    var result = await battleController.StartBattleAsync(
        State.PlayerSelectedCombo,
        State.AISelectedCombo,
        ct
    );

    State.AwardRoundWin(result.Winner);
    State.RoundHistory.Add(result);
}
```

---

## Event-Driven UI Pattern

### Controller Side (Fire Events)
```csharp
public class DraftController : MonoBehaviour
{
    public event Action<List<TroopCombination>> OnPlayerOptionsGenerated;
    public event Action<float> OnTimerUpdate;
    public event Action OnTimerWarning;
    public event Action<TroopCombination> OnPlayerSelected;

    private void SomeMethod()
    {
        OnPlayerOptionsGenerated?.Invoke(options);  // Emit event
    }
}
```

### UI Side (Subscribe to Events)
```csharp
public class DraftUI : MonoBehaviour
{
    private DraftController draftController;

    private void Start()
    {
        draftController = FindObjectOfType<DraftController>();
        draftController.OnPlayerOptionsGenerated += DisplayOptions;
        draftController.OnTimerUpdate += UpdateTimer;
        draftController.OnTimerWarning += ShowWarning;
    }

    private void OnDestroy()
    {
        // Always unsubscribe!
        draftController.OnPlayerOptionsGenerated -= DisplayOptions;
        draftController.OnTimerUpdate -= UpdateTimer;
        draftController.OnTimerWarning -= ShowWarning;
    }

    private void DisplayOptions(List<TroopCombination> options)
    {
        // Update UI here
    }

    private void UpdateTimer(float remaining)
    {
        timerText.text = Mathf.CeilToInt(remaining).ToString();
    }
}
```

---

## Critical Edge Cases (Must Handle)

### Draft Phase
| Scenario | Solution |
|----------|----------|
| Player doesn't click (timeout) | Auto-select random option |
| Player clicks during last second | Accept selection, stop timer |
| Player double-clicks | Ignore 2nd click (use `hasSelected` flag) |
| No options available | Use base combinations (always 4 available) |

### Battle Phase
| Scenario | Solution |
|----------|----------|
| Both teams die simultaneously | Player wins (tie-breaker) |
| Timer expires with equal HP | Player wins (tie-breaker) |
| Spawn exceeds 4-troop limit | Spawn partial amount (e.g., 2 out of 5) |
| Player selects invalid combo | Fallback: spawn 1 Fire Knight |

---

## TargetingSystem Extensions (Simple Addition)

Add these two helper methods to the existing `TargetingSystem.cs`:

```csharp
public static float GetTotalHP(Team team)
{
    if (!troopsByTeam.ContainsKey(team))
        return 0f;

    float total = 0f;
    foreach (var troop in troopsByTeam[team])
    {
        if (troop != null && troop.IsAlive)
            total += troop.Health.CurrentHP;
    }
    return total;
}

public static int GetAliveCount(Team team)
{
    return GetAliveTroops(team).Count;  // Uses existing method
}
```

---

## Scene Setup (What to Create in Unity)

### Hierarchy
```
MainGame Scene
├── GameManager (already exists)
├── MatchController (already exists)
│   ├── DraftController      ← Add as child
│   └── BattleController     ← Add as child
│       └── TroopSpawner (reference existing)
└── Canvas
    ├── DraftScreen          ← NEW panel
    │   ├── CardContainer
    │   │   ├── DraftCard_0  ← Button with DraftCard component
    │   │   ├── DraftCard_1
    │   │   └── DraftCard_2
    │   └── TimerText (TMP_Text)
    └── BattleScreen         ← NEW panel
        ├── TimerText (TMP_Text)
        ├── PlayerHPBar (Slider)
        ├── AIHPBar (Slider)
        └── VictoryBanner (hidden initially)
```

### Inspector Assignments
- `MatchController`: Assign DraftController and BattleController references
- `BattleController`: Assign TroopSpawner reference
- `DraftUI`: Assign 3 DraftCard references, TimerText
- `BattleUI`: Assign TimerText, HP bars, Victory banner

---

## Testing Checklist (How to Verify)

### Draft System
- [ ] Start match → See 3 draft cards appear
- [ ] Click a card → Selection accepted immediately
- [ ] Don't click anything → Random card selected at 0 seconds
- [ ] Timer shows warning at 5 seconds (color change)

### Battle System
- [ ] Draft completes → Troops spawn on battlefield
- [ ] Kill all enemy troops → Instant victory message
- [ ] Let timer run to 0 → Winner determined by HP
- [ ] Try to spawn 5 troops when 3 already exist → Only 1 spawns (max 4 total)

### Full Match Flow
- [ ] Play 3 rounds → Score updates correctly
- [ ] Win 4 rounds → Match ends with victory screen
- [ ] All UI transitions smooth (no flicker/glitches)

---

## Implementation Order (Do This in Sequence)

### Day 1 (8 hours)
1. **DraftController** (3h) - Core logic + timer
2. **DraftUI** (2h) - Card display + click handling
3. **MatchController Integration** (1h) - Connect draft phase
4. **Test Draft** (2h) - Verify everything works

### Day 2 (8 hours)
5. **TargetingSystem Extensions** (0.5h) - Add 2 helper methods
6. **BattleController** (3h) - Spawn + timer + victory logic
7. **BattleUI** (2h) - Timer display + HP bars + victory banner
8. **MatchController Integration** (1h) - Connect battle phase
9. **Test Battle** (1.5h) - Verify everything works

### Day 3 (4 hours)
10. **Edge Cases** (2h) - Handle all edge cases from doc
11. **Polish** (1h) - DoTween animations, screen transitions
12. **Full Testing** (1h) - Play 3 complete matches

**Total**: 16-20 hours

---

## Quick Reference: Key Events

### DraftController Events
```csharp
OnPlayerOptionsGenerated(List<TroopCombination>)  // Display 3 cards
OnTimerUpdate(float)                              // Update countdown
OnTimerWarning()                                  // Show warning at 5 sec
OnPlayerSelected(TroopCombination)                // Show selected card
OnDraftComplete(DraftResult)                      // Draft phase done
```

### BattleController Events
```csharp
OnBattleStarted()                        // Battle begins
OnTimerUpdate(float)                     // Update countdown
OnBattleEnded(Team, VictoryReason)       // Show victory/defeat
```

---

## Common Pitfalls to Avoid

1. **Forgetting to unsubscribe from events** → Memory leaks
   - Always unsubscribe in `OnDestroy()`

2. **Not handling CancellationToken** → Tasks run after scene unload
   - Always pass `ct` to `UniTask.Delay()` and check `ct.IsCancellationRequested`

3. **Spawning troops without capacity check** → More than 4 troops
   - Always use `SpawnTroopsWithLimit()` method

4. **Not handling null options** → NullReferenceException
   - Validate options before using them

5. **Updating UI every frame** → Performance issues
   - Use events or 100ms intervals, not `Update()`

6. **Hardcoded values** → Difficult to balance
   - Always use `GameConfig` for durations, limits, etc.

---

## Success Criteria (When Are You Done?)

- [x] Player can draft from 3 options with working timer
- [x] Auto-select triggers correctly on timeout
- [x] Troops spawn in battle (max 4 enforced)
- [x] Instant victory works (team elimination)
- [x] Timer victory works (HP comparison)
- [x] All edge cases handled gracefully
- [x] No console errors or warnings
- [x] Full best-of-7 match completes successfully
- [x] 60 FPS maintained throughout

**Then you're done!** Ready for AI generation integration.

---

## Where to Find Detailed Info

- **Full Design Doc**: `DRAFT_BATTLE_DESIGN_DOC.md` (60+ pages)
- **Game Design**: `GAME_DESIGN_DOC.md` (combat rules, modules, etc.)
- **Project Conventions**: `CLAUDE.md` (coding style, workflow)

**This summary**: Quick reference for implementation.

---

**Good luck! Focus on getting DraftController working first, then BattleController. Everything else builds on those two.**
