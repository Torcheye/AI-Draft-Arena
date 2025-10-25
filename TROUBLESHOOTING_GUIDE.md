# Draft & Battle System - Troubleshooting Guide

## Common Issues & Solutions

---

## Draft Phase Issues

### Issue 1: Draft cards don't appear
**Symptoms**: DraftUI shows blank cards or nothing happens when draft starts

**Checklist**:
- [ ] DraftController is assigned in MatchController inspector
- [ ] DraftUI.Start() successfully finds DraftController via FindObjectOfType
- [ ] DraftController.OnPlayerOptionsGenerated event is fired (check Debug.Log)
- [ ] Base combinations loaded (check MatchState.BaseCombinations.Count > 0)
- [ ] DraftCard prefabs assigned in DraftUI inspector (3 cards)

**Debug Steps**:
```csharp
// In DraftController.GenerateOptions()
Debug.Log($"Pool size: {pool.Count}");
Debug.Log($"Generated options: {options.Count}");

// In DraftUI.DisplayOptions()
Debug.Log($"Displaying {options.Count} cards");
foreach (var card in cards)
{
    Debug.Log($"Card {card.name} assigned: {card != null}");
}
```

**Common Causes**:
- Base combinations not in `Resources/Data/BaseCombinations` folder
- DraftUI cards list is empty in inspector
- Event subscription failed (DraftController null)

**Solution**:
```csharp
// In DraftUI.Start()
draftController = FindObjectOfType<DraftController>();
if (draftController == null)
{
    Debug.LogError("DraftController not found in scene!");
    return;
}
Debug.Log("DraftController found, subscribing to events");
```

---

### Issue 2: Timer doesn't count down
**Symptoms**: Timer shows 15 but never changes

**Checklist**:
- [ ] DraftController.StartDraftAsync() is actually called
- [ ] UniTask is being awaited (not fire-and-forget)
- [ ] CancellationToken is not already cancelled
- [ ] OnTimerUpdate event has subscribers

**Debug Steps**:
```csharp
// In DraftController.RunTimerAsync()
Debug.Log("Timer starting at: " + timer);

while (timer > 0 && !hasSelected)
{
    await UniTask.Delay(100, cancellationToken: ct);
    timer -= 0.1f;
    Debug.Log($"Timer: {timer}"); // ← Add this temporarily

    OnTimerUpdate?.Invoke(timer);
}
```

**Common Causes**:
- MatchController not awaiting StartDraftAsync() (missing await keyword)
- CancellationToken cancelled before draft starts
- Exception thrown in timer loop (check console)

**Solution**:
```csharp
// In MatchController.RunDraftPhase()
var result = await draftController.StartDraftAsync(State, cancellationToken);
//           ↑
//           └─ MUST have await keyword!
```

---

### Issue 3: Can't select draft cards
**Symptoms**: Clicking cards does nothing

**Checklist**:
- [ ] DraftCard has Button component
- [ ] Button onClick assigned to DraftUI.OnCardClicked()
- [ ] Button interactable = true
- [ ] DraftController.SelectOption() is called
- [ ] hasSelected flag is false

**Debug Steps**:
```csharp
// In DraftUI.OnCardClicked()
public void OnCardClicked(int index)
{
    Debug.Log($"Card {index} clicked!");
    draftController.SelectOption(index);
}

// In DraftController.SelectOption()
public void SelectOption(int index)
{
    Debug.Log($"SelectOption called: index={index}, hasSelected={hasSelected}");

    if (hasSelected)
    {
        Debug.LogWarning("Already selected!");
        return;
    }
    // ... rest of method
}
```

**Common Causes**:
- Button onClick not assigned in inspector
- DraftCard passed wrong index to OnCardClicked()
- hasSelected flag stuck as true from previous draft

**Solution**:
```csharp
// Ensure buttons are set up correctly in DraftUI.Start()
for (int i = 0; i < cards.Count; i++)
{
    int index = i; // ← Capture index in closure!
    var button = cards[i].GetComponent<Button>();
    button.onClick.RemoveAllListeners(); // Clear old listeners
    button.onClick.AddListener(() => OnCardClicked(index));
}
```

---

### Issue 4: Auto-select doesn't work on timeout
**Symptoms**: Timer reaches 0, nothing happens

**Checklist**:
- [ ] hasSelected is false when timer expires
- [ ] AutoSelectRandom() is actually called
- [ ] currentOptions list has valid options
- [ ] OnPlayerSelected event is fired

**Debug Steps**:
```csharp
// In DraftController.RunTimerAsync()
if (!hasSelected)
{
    Debug.Log("Timeout! Auto-selecting...");
    AutoSelectRandom();
}

// In DraftController.AutoSelectRandom()
private void AutoSelectRandom()
{
    Debug.Log($"Auto-select: {currentOptions.Count} options available");

    if (currentOptions == null || currentOptions.Count == 0)
    {
        Debug.LogError("No options to auto-select!");
        return;
    }

    int randomIndex = Random.Range(0, currentOptions.Count);
    Debug.Log($"Auto-selected index {randomIndex}");
    SelectOption(randomIndex);
}
```

**Solution**: Ensure AutoSelectRandom() calls SelectOption(), not duplicate logic.

---

## Battle Phase Issues

### Issue 5: Troops don't spawn
**Symptoms**: Battle phase starts but no troops appear

**Checklist**:
- [ ] BattleController.StartBattleAsync() is called
- [ ] TroopSpawner reference assigned in BattleController
- [ ] TroopPrefab assigned in TroopSpawner
- [ ] PlayerSelectedCombo and AISelectedCombo are not null
- [ ] SpawnTroopsWithLimit() is called for both teams

**Debug Steps**:
```csharp
// In BattleController.StartBattleAsync()
Debug.Log($"Starting battle with:");
Debug.Log($"  Player: {playerCombo?.DisplayName ?? "NULL"}");
Debug.Log($"  AI: {aiCombo?.DisplayName ?? "NULL"}");

// In BattleController.SpawnTroopsWithLimit()
Debug.Log($"Spawning {combo.DisplayName} for {team}");
Debug.Log($"  Current count: {currentCount}");
Debug.Log($"  Available slots: {availableSlots}");
Debug.Log($"  Amount to spawn: {amountToSpawn}");

var troops = troopSpawner.SpawnTroops(combo, team);
Debug.Log($"  Actually spawned: {troops.Count} troops");
```

**Common Causes**:
- TroopSpawner reference null (not assigned in inspector)
- TroopPrefab missing TroopController component
- Draft phase didn't store selected combos in MatchState
- Spawn zone Rect has invalid values (width/height = 0)

**Solution**:
```csharp
// In BattleController.Awake()
private void Awake()
{
    if (troopSpawner == null)
    {
        Debug.LogError("TroopSpawner not assigned!");
        troopSpawner = FindObjectOfType<TroopSpawner>();
        if (troopSpawner == null)
        {
            Debug.LogError("TroopSpawner not found in scene!");
        }
    }
}
```

---

### Issue 6: Battle never ends
**Symptoms**: Timer reaches 0, troops all dead, but battle continues

**Checklist**:
- [ ] RunBattleLoopAsync() while loop exits when timer <= 0
- [ ] Victory checks actually return RoundResult
- [ ] OnBattleEnded event is fired
- [ ] MatchController awaits StartBattleAsync()

**Debug Steps**:
```csharp
// In BattleController.RunBattleLoopAsync()
while (battleTimer > 0 && !battleEnded)
{
    await UniTask.Delay(100, cancellationToken: ct);
    battleTimer -= 0.1f;

    Debug.Log($"Battle timer: {battleTimer:F1}s");

    var playerAlive = TargetingSystem.GetAliveTroops(Team.Player).Count;
    var aiAlive = TargetingSystem.GetAliveTroops(Team.AI).Count;

    Debug.Log($"  Player: {playerAlive} alive, AI: {aiAlive} alive");

    // Check conditions
    if (playerAlive == 0 && aiAlive > 0)
    {
        Debug.Log("AI wins by elimination!");
        return CreateResult(Team.AI, VictoryReason.Elimination);
    }
    // ... other checks
}

Debug.Log("Timer expired, checking HP...");
return DetermineWinnerByHP();
```

**Common Causes**:
- While loop condition wrong (e.g., `while (battleTimer > 0)` without exit on victory)
- Victory checks don't return (missing `return` keyword)
- TargetingSystem not registering troops correctly

**Solution**: Ensure every victory path has `return CreateResult(...)`.

---

### Issue 7: HP comparison returns wrong winner
**Symptoms**: Timer expires, player clearly has more HP, but AI wins

**Checklist**:
- [ ] TargetingSystem.GetTotalHP() implemented correctly
- [ ] IsAlive flag accurate on troops
- [ ] CurrentHP values are not negative
- [ ] Tie-breaker logic favors player

**Debug Steps**:
```csharp
// In BattleController.DetermineWinnerByHP()
private RoundResult DetermineWinnerByHP()
{
    float playerHP = TargetingSystem.GetTotalHP(Team.Player);
    float aiHP = TargetingSystem.GetTotalHP(Team.AI);

    Debug.Log($"HP Comparison:");
    Debug.Log($"  Player total: {playerHP}");
    Debug.Log($"  AI total: {aiHP}");

    var playerTroops = TargetingSystem.GetAliveTroops(Team.Player);
    Debug.Log($"  Player alive troops: {playerTroops.Count}");
    foreach (var t in playerTroops)
    {
        Debug.Log($"    - {t.name}: {t.Health.CurrentHP} HP");
    }

    Team winner;
    if (playerHP > aiHP)
    {
        winner = Team.Player;
        Debug.Log("  → Player wins!");
    }
    else if (aiHP > playerHP)
    {
        winner = Team.AI;
        Debug.Log("  → AI wins!");
    }
    else
    {
        winner = Team.Player; // Tie-breaker
        Debug.Log("  → Tie! Player wins by default.");
    }

    return CreateResult(winner, VictoryReason.TimerExpiration);
}
```

**Common Causes**:
- GetTotalHP() includes dead troops (missing `IsAlive` check)
- CurrentHP already negative (death logic bug)
- Comparison logic reversed (`if (playerHP < aiHP)` instead of `>`)

---

### Issue 8: More than 4 troops spawn
**Symptoms**: 5+ troops on one team

**Checklist**:
- [ ] SpawnTroopsWithLimit() enforces capacity
- [ ] GetAliveTroops() returns accurate count
- [ ] No troops spawning outside battle controller (e.g., from test code)

**Debug Steps**:
```csharp
// In BattleController.SpawnTroopsWithLimit()
int currentCount = TargetingSystem.GetAliveTroops(team).Count;
int availableSlots = config.maxTroopsPerSide - currentCount;

Debug.Log($"Pre-spawn check for {team}:");
Debug.Log($"  Current alive: {currentCount}");
Debug.Log($"  Max allowed: {config.maxTroopsPerSide}");
Debug.Log($"  Available slots: {availableSlots}");
Debug.Log($"  Combo wants to spawn: {combo.amount}");

if (availableSlots <= 0)
{
    Debug.LogWarning($"No slots available! Skipping spawn.");
    return;
}

int amountToSpawn = Mathf.Min(combo.amount, availableSlots);
Debug.Log($"  Will spawn: {amountToSpawn}");

// After spawning
int afterCount = TargetingSystem.GetAliveTroops(team).Count;
Debug.Log($"  After spawn: {afterCount} total troops");
```

**Common Causes**:
- TargetingSystem.RegisterTroop() called multiple times per troop
- Troops not cleaning up on death (UnregisterTroop() not called)
- Multiple BattleControllers spawning simultaneously

**Solution**: Verify TroopController.OnDestroy() calls `TargetingSystem.UnregisterTroop(this)`.

---

## UI Issues

### Issue 9: Timer text doesn't update
**Symptoms**: Timer stuck at 15 or 30

**Checklist**:
- [ ] BattleUI/DraftUI.Start() finds controller
- [ ] Event subscription successful
- [ ] OnTimerUpdate event is fired (check controller debug log)
- [ ] UpdateTimer() method is called
- [ ] timerText reference assigned in inspector

**Debug Steps**:
```csharp
// In DraftUI.UpdateTimer()
private void UpdateTimer(float remaining)
{
    Debug.Log($"UpdateTimer called: {remaining}");

    if (timerText == null)
    {
        Debug.LogError("timerText is null!");
        return;
    }

    timerText.text = Mathf.CeilToInt(remaining).ToString();
    Debug.Log($"  Set text to: {timerText.text}");
}
```

**Common Causes**:
- timerText reference not assigned in inspector
- Event subscription failed (controller null at Start())
- Canvas disabled/inactive

**Solution**:
```csharp
// In DraftUI.Start()
if (timerText == null)
{
    Debug.LogError("TimerText not assigned in inspector!");
}

draftController = FindObjectOfType<DraftController>();
if (draftController == null)
{
    Debug.LogError("DraftController not found!");
    return;
}

draftController.OnTimerUpdate += UpdateTimer;
Debug.Log("Subscribed to OnTimerUpdate");
```

---

### Issue 10: Victory banner doesn't show
**Symptoms**: Battle ends, no victory UI appears

**Checklist**:
- [ ] VictoryBanner GameObject exists in scene
- [ ] VictoryBanner assigned in BattleUI inspector
- [ ] OnBattleEnded event is fired
- [ ] ShowVictory() method is called
- [ ] VictoryBanner has CanvasGroup component

**Debug Steps**:
```csharp
// In BattleUI.ShowVictory()
private void ShowVictory(Team winner, VictoryReason reason)
{
    Debug.Log($"ShowVictory called: {winner} by {reason}");

    if (victoryBanner == null)
    {
        Debug.LogError("VictoryBanner not assigned!");
        return;
    }

    victoryBanner.SetActive(true);
    winnerText.text = winner == Team.Player ? "VICTORY!" : "DEFEAT";

    Debug.Log($"VictoryBanner active: {victoryBanner.activeSelf}");
    Debug.Log($"WinnerText: {winnerText.text}");

    var canvasGroup = victoryBanner.GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
        Debug.LogError("VictoryBanner missing CanvasGroup!");
        return;
    }

    canvasGroup.alpha = 0f;
    canvasGroup.DOFade(1f, 0.5f);
}
```

**Common Causes**:
- VictoryBanner not assigned in inspector
- VictoryBanner already active (doesn't re-trigger animation)
- Missing CanvasGroup component

---

## Integration Issues

### Issue 11: Draft completes but battle doesn't start
**Symptoms**: Draft finishes, screen goes blank, nothing happens

**Checklist**:
- [ ] MatchController.RunBattlePhase() is called after draft
- [ ] MatchState.PlayerSelectedCombo is set
- [ ] MatchState.AISelectedCombo is set
- [ ] BattleController reference assigned in MatchController

**Debug Steps**:
```csharp
// In MatchController.RunRound()
private async UniTask RunRound(int roundNumber, CancellationToken ct)
{
    Debug.Log($"=== Round {roundNumber} Start ===");

    // Draft Phase
    Debug.Log("Starting draft phase...");
    await RunDraftPhase(ct);
    Debug.Log($"Draft complete: Player={State.PlayerSelectedCombo?.DisplayName}, AI={State.AISelectedCombo?.DisplayName}");

    // Spawn Phase
    Debug.Log("Starting spawn phase...");
    await RunSpawnPhase(ct);
    Debug.Log("Spawn complete");

    // Battle Phase
    Debug.Log("Starting battle phase...");
    await RunBattlePhase(ct);
    Debug.Log("Battle complete");

    // Round End
    Debug.Log("Starting round end phase...");
    await RunRoundEndPhase(ct);
    Debug.Log("Round end complete");
}
```

**Common Causes**:
- Exception thrown in RunDraftPhase() (check console)
- MatchController not awaiting properly
- BattleController null (not assigned)

---

### Issue 12: Memory leak / performance degradation
**Symptoms**: Game slows down after multiple rounds

**Checklist**:
- [ ] Event subscriptions cleaned up in OnDestroy()
- [ ] Troops destroyed properly
- [ ] TargetingSystem.UnregisterTroop() called on death
- [ ] No infinite loops

**Debug Steps**:
```csharp
// Check troop count
Debug.Log($"Active troops in scene: {FindObjectsOfType<TroopController>().Length}");
Debug.Log($"Registered in TargetingSystem: Player={TargetingSystem.GetAliveTroops(Team.Player).Count}, AI={TargetingSystem.GetAliveTroops(Team.AI).Count}");

// Check event subscriptions
// Use Profiler → Memory → Take Sample → Check event handlers
```

**Common Causes**:
- UI not unsubscribing from events in OnDestroy()
- Troops not destroyed after battle
- TargetingSystem accumulating dead troop references

**Solution**:
```csharp
// In BattleController cleanup
private void CleanupBattle()
{
    foreach (var troop in playerTroops)
    {
        if (troop != null)
            Destroy(troop.gameObject);
    }

    foreach (var troop in aiTroops)
    {
        if (troop != null)
            Destroy(troop.gameObject);
    }

    playerTroops.Clear();
    aiTroops.Clear();

    TargetingSystem.ClearAll();
}
```

---

## Testing Issues

### Issue 13: "OperationCanceledException" in console
**Symptoms**: Red error in console, but game works fine

**This is NORMAL!** When you stop play mode or transition scenes, UniTask throws this to cancel ongoing operations.

**Not a bug if**:
- Game works correctly
- Only happens when stopping play mode
- Caught in try-catch block

**Fix if annoying**:
```csharp
try
{
    await RunTimerAsync(cancellationToken);
}
catch (OperationCanceledException)
{
    // Normal cancellation, not an error
    // Don't log, just exit gracefully
}
```

---

### Issue 14: "NullReferenceException" on scene load
**Symptoms**: Error when entering play mode, before match starts

**Checklist**:
- [ ] All [SerializeField] references assigned in inspector
- [ ] FindObjectOfType() in Start(), not Awake()
- [ ] GameManager.Instance exists before MatchController

**Debug Steps**:
```csharp
// In MatchController.Awake()
private void Awake()
{
    if (GameManager.Instance == null)
    {
        Debug.LogError("GameManager missing! Add to scene.");
        enabled = false;
        return;
    }

    if (draftController == null)
    {
        Debug.LogError("DraftController not assigned!");
    }

    if (battleController == null)
    {
        Debug.LogError("BattleController not assigned!");
    }
}
```

---

## Performance Issues

### Issue 15: Low FPS during battle
**Symptoms**: Game stutters, FPS drops below 30

**Profiler Checkpoints**:
1. Open Window → Analysis → Profiler
2. Start play mode and run a battle
3. Check CPU Usage:
   - Update loops
   - GetAliveTroops() calls
   - UI text updates

**Common Causes**:
- UI updating every frame unnecessarily
- Too many Debug.Log() calls (remove in release)
- Expensive operations in Update()

**Solutions**:
```csharp
// Cache expensive lookups
private List<TroopController> cachedAliveTroops;
private float cacheRefreshTimer = 0f;

private void Update()
{
    cacheRefreshTimer += Time.deltaTime;

    if (cacheRefreshTimer >= 0.1f) // Refresh every 100ms
    {
        cachedAliveTroops = TargetingSystem.GetAliveTroops(Team.Player);
        cacheRefreshTimer = 0f;
    }

    // Use cachedAliveTroops instead of calling GetAliveTroops() every frame
}
```

---

## Quick Fixes Checklist

Before asking for help, verify:

- [ ] All inspector references assigned (check for "None" or "Missing")
- [ ] Console has no red errors (fix those first)
- [ ] GameManager exists in scene
- [ ] Config asset assigned in GameManager
- [ ] Base combinations exist in Resources/Data/BaseCombinations
- [ ] Troop prefab has TroopController component
- [ ] Canvas has EventSystem
- [ ] All UI text components are TMP_Text (not legacy Text)
- [ ] All event subscriptions have corresponding unsubscribe in OnDestroy()
- [ ] You're awaiting async methods (not fire-and-forget)

---

## Still Stuck?

**Add comprehensive logging**:
```csharp
// At the start of every public method:
Debug.Log($"[{GetType().Name}] {System.Reflection.MethodBase.GetCurrentMethod().Name} called");

// Example:
public void SelectOption(int index)
{
    Debug.Log($"[DraftController] SelectOption called with index={index}");
    // ... rest of method
}
```

**Check execution order**:
1. MatchController.StartMatchAsync()
2. RunRound(1)
3. RunDraftPhase() → DraftController.StartDraftAsync()
4. RunBattlePhase() → BattleController.StartBattleAsync()
5. Victory check
6. Round end

If any step doesn't log, that's where the problem is.

---

## Emergency Fallbacks

If something is completely broken and you need to continue:

**Skip Draft Phase**:
```csharp
private async UniTask RunDraftPhase(CancellationToken ct)
{
    // Emergency: Just pick first base combo
    State.PlayerSelectedCombo = State.BaseCombinations[0];
    State.AISelectedCombo = State.BaseCombinations[1];
    await UniTask.Delay(100, cancellationToken: ct);
}
```

**Skip Battle Phase**:
```csharp
private async UniTask RunBattlePhase(CancellationToken ct)
{
    // Emergency: Random winner
    State.AwardRoundWin(Random.value > 0.5f ? Team.Player : Team.AI);
    await UniTask.Delay(100, cancellationToken: ct);
}
```

These get you running so you can debug the actual systems.

---

**Good luck! 90% of issues are missing inspector references or forgotten event unsubscriptions.**
