# Draft & Battle System - Architecture Diagrams

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          MatchController                            │
│                    (Orchestrates Full Match)                        │
│                                                                     │
│  RunRound() {                                                       │
│      1. RunDraftPhase() ────────────┐                              │
│      2. RunSpawnPhase()             │                              │
│      3. RunBattlePhase() ───────┐   │                              │
│      4. RunRoundEndPhase()      │   │                              │
│  }                              │   │                              │
└─────────────────────────────────┼───┼──────────────────────────────┘
                                  │   │
                                  │   │
        ┌─────────────────────────┘   └──────────────────────┐
        │                                                     │
        ▼                                                     ▼
┌──────────────────────┐                          ┌──────────────────────┐
│  BattleController    │                          │  DraftController     │
├──────────────────────┤                          ├──────────────────────┤
│ - StartBattleAsync() │                          │ - StartDraftAsync()  │
│ - SpawnTroops()      │                          │ - GenerateOptions()  │
│ - CheckVictory()     │                          │ - SelectOption()     │
│ - HP Comparison      │                          │ - Timer Loop         │
│                      │                          │ - Auto-Select        │
│ Events:              │                          │                      │
│  • OnBattleStarted   │                          │ Events:              │
│  • OnTimerUpdate     │                          │  • OnOptionsGenerated│
│  • OnBattleEnded     │                          │  • OnTimerUpdate     │
└──────┬───────────────┘                          │  • OnTimerWarning    │
       │                                          │  • OnPlayerSelected  │
       │ Uses                                     └──────┬───────────────┘
       ▼                                                 │
┌──────────────────────┐                                │
│   TroopSpawner       │                                │
│   (Existing)         │                                │
├──────────────────────┤                                │
│ - SpawnTroops()      │                                │
│ - Capacity Check     │                                │
│ - Position Logic     │                                │
└──────┬───────────────┘                                │
       │                                                 │
       │ Registers troops with                          │
       ▼                                                 │
┌──────────────────────┐                                │
│  TargetingSystem     │                                │
│  (Existing+Extended) │                                │
├──────────────────────┤                                │
│ - GetAliveTroops()   │◄───────────────────────────────┤
│ - GetTotalHP() [NEW] │                                │
│ - GetAliveCount()[NEW]                                │
└──────────────────────┘                                │
                                                        │
       ┌────────────────────────────────────────────────┘
       │
       │ Subscribes to events
       ▼
┌──────────────────────┐
│       UI Layer       │
├──────────────────────┤
│   DraftUI            │
│    - Display Cards   │
│    - Timer Display   │
│    - Click Handler   │
│                      │
│   BattleUI           │
│    - Timer Display   │
│    - HP Bars         │
│    - Victory Banner  │
└──────────────────────┘
```

---

## Data Flow: Complete Round Cycle

```
USER ACTION                 CONTROLLER                     STATE                    UI
─────────────────────────────────────────────────────────────────────────────────────

[Match Start]
                           MatchController
                           .StartMatchAsync()
                                │
                                ├─ Load base combos
                                │
                                ▼
                           RunRound(1)
                                │
                                │
┌───────────────────────────────┴───────────────────────────────────────────────────┐
│                              DRAFT PHASE (15 sec)                                 │
└───────────────────────────────┬───────────────────────────────────────────────────┘
                                │
                                ▼
                           DraftController
                           .StartDraftAsync()
                                │
                                ├─ GenerateOptions()
                                │     │
                                │     └─────────────▶ 3 combos         ─────────────┐
                                │                    from pool                      │
                                │                                                   │
                                ├─ OnOptionsGenerated ─────────────────────────────▶│
                                │                                            DraftUI│
                                │                                         Display 3 │
                                │                                           cards   │
                                │                                                   │
                                ├─ Start 15s timer                                  │
                                │                                                   │
                                │     Loop every 100ms:                             │
                                ├─ OnTimerUpdate(14.9) ────────────────────────────▶│
                                ├─ OnTimerUpdate(14.8) ────────────────────────────▶│
                                │     ...                                    Update │
[Player clicks card 2]◄─────────┤                                            timer  │
                                │                                            text   │
                           SelectOption(2)                                          │
                                │                                                   │
                                ├─ Store selection                                  │
                                │                                                   │
                                ├─ OnPlayerSelected(combo) ────────────────────────▶│
                                │                                           Show    │
                                │                                         selected  │
                                │                                           glow    │
                                │                                                   │
                                ├─ AI selects random                                │
                                │                                                   │
                                ├─ Return DraftResult ──▶ PlayerSelectedCombo      │
                                │                         AISelectedCombo           │
                                │                         PlayerPickHistory         │
                                ▼                                                   │
                                                                                    │
┌───────────────────────────────────────────────────────────────────────────────────┘
│                              SPAWN PHASE (1 sec)
└───────────────────────────────┬───────────────────────────────────────────────────┐
                                │                                                   │
                                ├─ Show reveal UI ──────────────────────────────────▶│
                                │  (both picks visible)                      Reveal │
                                │                                             UI    │
                                ├─ DoTween card flip                                │
                                │                                                   │
                                ├─ Wait 1 second                                    │
                                │                                                   │
                                ▼                                                   │
                                                                                    │
┌───────────────────────────────────────────────────────────────────────────────────┘
│                             BATTLE PHASE (max 30 sec)
└───────────────────────────────┬───────────────────────────────────────────────────┐
                                │                                                   │
                                ▼                                                   │
                           BattleController                                         │
                           .StartBattleAsync()                                      │
                                │                                                   │
                                ├─ TargetingSystem                                  │
                                │  .ClearAll()                                      │
                                │                                                   │
                                ├─ SpawnTroopsWithLimit() ──▶ TroopSpawner         │
                                │   (Player combo)                  │               │
                                │                                   ├─ Check slots │
                                │                                   ├─ Spawn 3     │
                                │                                   └─ Register    │
                                │                                                   │
                                ├─ SpawnTroopsWithLimit()                           │
                                │   (AI combo)                                      │
                                │                                                   │
                                ├─ OnBattleStarted ────────────────────────────────▶│
                                │                                           BattleUI│
                                │                                            Show   │
                                │                                           battle  │
                                │                                           screen  │
                                │                                                   │
                                ├─ Start 30s timer                                  │
                                │                                                   │
                                │     Loop every 100ms:                             │
                                ├─ OnTimerUpdate(29.9) ────────────────────────────▶│
                                │                                            Update │
                                │     Check victory:                         timer  │
                                ├─ GetAliveTroops(Player) ──▶ TargetingSystem      │
                                ├─ GetAliveTroops(AI)                              │
                                │                                                   │
                                │     If player = 0 && ai > 0:                      │
                                │         → AI wins (elimination)                   │
                                │     If ai = 0 && player > 0:                      │
                                │         → Player wins (elimination)               │
                                │     If both = 0:                                  │
                                │         → Player wins (tie)                       │
                                │                                                   │
[Troop dies]                    │     ...timer continues...                         │
  ▼                             │                                                   │
[ai troop count = 0]            │                                                   │
                                │                                                   │
                                ├─ Player wins!                                     │
                                │                                                   │
                                ├─ OnBattleEnded(Player, Elimination) ─────────────▶│
                                │                                            Show   │
                                │                                          VICTORY! │
                                │                                           banner  │
                                │                                                   │
                                ├─ Return RoundResult ───▶ Winner = Player         │
                                │                          Reason = Elimination     │
                                │                          Duration = 12.3s         │
                                ▼                                                   │
                                                                                    │
┌───────────────────────────────────────────────────────────────────────────────────┘
│                            ROUND END PHASE (2 sec)
└───────────────────────────────┬───────────────────────────────────────────────────┐
                                │                                                   │
                                ├─ AwardRoundWin(Player) ──▶ PlayerWins++          │
                                │                                                   │
                                ├─ Add to RoundHistory                              │
                                │                                                   │
                                ├─ Show round summary ──────────────────────────────▶│
                                │                                            Round  │
                                │                                           summary │
                                │                                                   │
                                ├─ [Future] AI generation                           │
                                │                                                   │
                                ├─ Wait 2 seconds                                   │
                                │                                                   │
                                ▼                                                   │
                                                                                    │
                           Check if match over:                                    │
                           PlayerWins >= 4? → Match End                             │
                           AIWins >= 4? → Match End                                 │
                           Else → RunRound(2)                                       │
                                                                                    │
                                [Loop continues...]                                 │
```

---

## Component Interaction: Draft Phase

```
┌──────────────┐
│ DraftController│
└───────┬────────┘
        │
        │ 1. StartDraftAsync(MatchState, CancellationToken)
        │
        ├─────────────────────────────────────────────────┐
        │                                                 │
        │ 2. GenerateOptions()                            │
        │    ┌─────────────────────────────┐             │
        │    │ Pool = BaseCombinations +   │             │
        │    │        AIGeneratedCombos    │             │
        │    │                             │             │
        │    │ Shuffle()                   │             │
        │    │ Take(3)                     │             │
        │    └─────────────────────────────┘             │
        │                                                 │
        │ 3. OnPlayerOptionsGenerated?.Invoke(options)    │
        │    ────────────────────────────────────┐        │
        │                                        │        │
        │                                        ▼        │
        │                              ┌──────────────┐   │
        │                              │   DraftUI    │   │
        │                              ├──────────────┤   │
        │                              │ DisplayCards │   │
        │                              │   (×3)       │   │
        │                              └──────────────┘   │
        │                                                 │
        │ 4. RunTimerAsync()                              │
        │    ┌─────────────────────────┐                 │
        │    │ while (timer > 0 &&     │                 │
        │    │        !hasSelected) {  │                 │
        │    │                         │                 │
        │    │   await Delay(100ms)    │                 │
        │    │   timer -= 0.1s         │                 │
        │    │                         │                 │
        │    │   OnTimerUpdate(timer) ────────┐          │
        │    │                         │      │          │
        │    │   if (timer <= 5s)      │      ▼          │
        │    │     OnTimerWarning() ──────▶ DraftUI     │
        │    │ }                       │   (red timer)   │
        │    │                         │                 │
        │    │ if (!hasSelected)       │                 │
        │    │   AutoSelectRandom()    │                 │
        │    └─────────────────────────┘                 │
        │                                                 │
        │ [Meanwhile...]                                  │
        │                                                 │
        │ 5. Player clicks card ◄────────┐               │
        │                                │               │
        │    SelectOption(index) ◄───────┤               │
        │    ┌─────────────────────┐     │               │
        │    │ if (hasSelected)    │     │               │
        │    │   return; // Ignore │     │               │
        │    │                     │     │               │
        │    │ playerSelection =   │     │               │
        │    │   options[index]    │   DraftCard        │
        │    │                     │   onClick()        │
        │    │ hasSelected = true  │                    │
        │    │                     │                    │
        │    │ OnPlayerSelected ──────────────▶ DraftUI │
        │    │                     │           (glow)   │
        │    └─────────────────────┘                    │
        │                                                │
        │ 6. AI instant selection                        │
        │    aiSelection = Random(options)               │
        │                                                │
        │ 7. Return DraftResult                          │
        │    { PlayerPick, AIPick, TimedOut }            │
        │                                                │
        └────────────────────────────────────────────────┘
```

---

## Component Interaction: Battle Phase

```
┌──────────────────┐
│ BattleController │
└────────┬─────────┘
         │
         │ 1. StartBattleAsync(playerCombo, aiCombo, CancellationToken)
         │
         ├───────────────────────────────────────────────────────┐
         │                                                       │
         │ 2. Clear previous battle                              │
         │    TargetingSystem.ClearAll()                         │
         │                                                       │
         │ 3. Spawn player troops                                │
         │    ┌────────────────────────────────┐                │
         │    │ SpawnTroopsWithLimit()         │                │
         │    │                                │                │
         │    │ currentCount = GetAliveTroops  │                │
         │    │ availableSlots = 4 - current   │                │
         │    │                                │                │
         │    │ if (availableSlots > 0) {      │                │
         │    │   amountToSpawn = Min(         │                │
         │    │     combo.amount,              │                │
         │    │     availableSlots             │                │
         │    │   )                            │                │
         │    │                                │                │
         │    │   TroopSpawner                 │                │
         │    │   .SpawnTroops() ──────────────┼───────┐        │
         │    │ }                              │       │        │
         │    └────────────────────────────────┘       │        │
         │                                             │        │
         │                                             ▼        │
         │                                   ┌──────────────┐   │
         │                                   │ TroopSpawner │   │
         │                                   ├──────────────┤   │
         │                                   │ Instantiate  │   │
         │                                   │   prefabs    │   │
         │                                   │              │   │
         │                                   │ Initialize   │   │
         │                                   │   TroopData  │   │
         │                                   │              │   │
         │                                   │ Register ────┼──┐│
         │                                   │   with       │  ││
         │                                   │ Targeting    │  ││
         │                                   └──────────────┘  ││
         │                                                     ││
         │ 4. Spawn AI troops (same process)                  ││
         │                                                     ││
         │                                                     ▼▼
         │                                          ┌────────────────┐
         │                                          │ TargetingSystem│
         │                                          ├────────────────┤
         │                                          │ RegisterTroop()│
         │                                          │                │
         │                                          │ troopsByTeam = │
         │                                          │   Player: [3]  │
         │                                          │   AI: [3]      │
         │                                          └────────────────┘
         │                                                     │
         │ 5. OnBattleStarted?.Invoke() ────────────────┐     │
         │                                              │     │
         │                                              ▼     │
         │                                     ┌──────────────┐
         │                                     │   BattleUI   │
         │                                     ├──────────────┤
         │                                     │ Show screen  │
         │                                     │ Start timer  │
         │                                     └──────────────┘
         │                                                     │
         │ 6. RunBattleLoopAsync()                            │
         │    ┌─────────────────────────────┐                 │
         │    │ while (timer > 0) {         │                 │
         │    │                             │                 │
         │    │   await Delay(100ms)        │                 │
         │    │   timer -= 0.1s             │                 │
         │    │                             │                 │
         │    │   OnTimerUpdate(timer) ─────┼─────────▶ BattleUI
         │    │                             │          (update text)
         │    │   // Instant victory check  │                 │
         │    │   playerAlive =             │                 │
         │    │     GetAliveTroops(Player) ◄┼─────────────────┘
         │    │   aiAlive =                 │
         │    │     GetAliveTroops(AI) ◄────┤
         │    │                             │
         │    │   if (playerAlive == 0 &&   │
         │    │       aiAlive > 0)          │
         │    │     return AI_WINS          │
         │    │                             │
         │    │   else if (aiAlive == 0 &&  │
         │    │            playerAlive > 0) │
         │    │     return PLAYER_WINS      │
         │    │                             │
         │    │   else if (both == 0)       │
         │    │     return PLAYER_WINS      │ (tie-breaker)
         │    │                             │
         │    │ } // while                  │
         │    │                             │
         │    │ // Timer expired            │
         │    │ DetermineWinnerByHP()       │
         │    │   ┌─────────────────────┐   │
         │    │   │ playerHP =          │   │
         │    │   │   GetTotalHP(Player)│◄──┼─────────────────┐
         │    │   │ aiHP =              │   │                 │
         │    │   │   GetTotalHP(AI)    │◄──┼─────────────────┤
         │    │   │                     │   │                 │
         │    │   │ if (playerHP > ai)  │   │                 │
         │    │   │   return PLAYER_WINS│   │  TargetingSystem│
         │    │   │ else if (ai > player)   │   .GetTotalHP() │
         │    │   │   return AI_WINS    │   │                 │
         │    │   │ else                │   │                 │
         │    │   │   return PLAYER_WINS│   │  (iterates alive│
         │    │   └─────────────────────┘   │   troops, sums  │
         │    │                             │    CurrentHP)   │
         │    └─────────────────────────────┘                 │
         │                                                    │
         │ 7. OnBattleEnded?.Invoke(winner, reason) ─────────┤
         │                                                    │
         │                                                    ▼
         │                                           ┌──────────────┐
         │                                           │   BattleUI   │
         │                                           ├──────────────┤
         │                                           │ Show victory │
         │                                           │   banner     │
         │                                           │              │
         │                                           │ DoTween fade │
         │                                           └──────────────┘
         │                                                    │
         │ 8. Return RoundResult                             │
         │    { Winner, Reason, Duration, etc. }             │
         │                                                    │
         └────────────────────────────────────────────────────┘
```

---

## Event Subscription Pattern

```
┌─────────────────────────────────────────────────────────────────┐
│                        Event Lifecycle                          │
└─────────────────────────────────────────────────────────────────┘

Controller Side (Publisher)                 UI Side (Subscriber)
────────────────────────                     ───────────────────

public class DraftController                 public class DraftUI
{                                            {
    // 1. Declare events                         private DraftController controller;
    public event Action<float>
        OnTimerUpdate;                           private void Start()
                                                 {
                                                     // 2. Find controller
    private void SomeMethod()                        controller =
    {                                                    FindObjectOfType<DraftController>();
        float time = 14.5f;
                                                     // 3. Subscribe to events
        // 3. Invoke event                           controller.OnTimerUpdate += UpdateTimer;
        OnTimerUpdate?.Invoke(time);             }
        //           ▲
        //           │                             // 4. Handle event
        //           └─────────────────────┐        private void UpdateTimer(float remaining)
    }                                     │        {
}                                         │            timerText.text =
                                          │                Mathf.CeilToInt(remaining).ToString();
                                          │        }
                                          │
                                          │        private void OnDestroy()
                                          │        {
                                          │            // 5. CRITICAL: Unsubscribe!
                                          └───────▶    controller.OnTimerUpdate -= UpdateTimer;
                                                   }
                                               }


┌─────────────────────────────────────────────────────────────────┐
│                   Why This Pattern Works                        │
└─────────────────────────────────────────────────────────────────┘

✓ Loose coupling       Controller doesn't know UI exists
✓ Multiple subscribers Multiple UI elements can subscribe
✓ Easy testing         Mock events without UI
✓ Clean separation     Logic in controller, display in UI
✓ Unity-friendly       Works with scene loading/unloading

⚠ Common Mistake: Forgetting to unsubscribe
   → Causes memory leaks when UI is destroyed
   → Solution: Always unsubscribe in OnDestroy()
```

---

## State Flow: MatchState Updates

```
MatchState (Central Data Store)
├─ CurrentRound: int
├─ PlayerWins: int
├─ AIWins: int
├─ CurrentPhase: MatchPhase
├─ BaseCombinations: List<TroopCombination>
├─ AIGeneratedCombinations: List<TroopCombination>
├─ PlayerSelectedCombo: TroopCombination
├─ AISelectedCombo: TroopCombination
├─ PlayerPickHistory: List<TroopCombination>
└─ RoundHistory: List<RoundResult>


Update Timeline:
─────────────────

Match Start:
  MatchState.CurrentRound = 0
  MatchState.PlayerWins = 0
  MatchState.AIWins = 0
  MatchState.BaseCombinations = [4 combos loaded from Resources]

Round 1 Start:
  MatchState.CurrentRound = 1
  MatchState.CurrentPhase = Draft

Draft Complete:
  MatchState.PlayerSelectedCombo = Fire Knight ×1
  MatchState.AISelectedCombo = Water Archer ×2
  MatchState.PlayerPickHistory.Add(Fire Knight ×1)

Battle Complete:
  RoundResult result = new RoundResult {
    RoundNumber = 1,
    Winner = Player,
    Reason = Elimination,
    Duration = 18.3f,
    PlayerHP = 5.0f,
    AIHP = 0.0f
  }

  MatchState.RoundHistory.Add(result)
  MatchState.AwardRoundWin(Player) → PlayerWins = 1

Round 2 Start:
  MatchState.CurrentRound = 2
  [AI generates 2 new combos based on Round 1 pick]
  MatchState.AIGeneratedCombinations.Add(combo1)
  MatchState.AIGeneratedCombinations.Add(combo2)
  [Now 6 total combos in pool: 4 base + 2 AI]

...continues until PlayerWins >= 4 or AIWins >= 4

Match End:
  MatchState.CurrentPhase = MatchEnd
  Winner = Player (if PlayerWins >= 4)
```

---

## Error Handling Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Defensive Programming                        │
└─────────────────────────────────────────────────────────────────┘

Layer 1: Input Validation
─────────────────────────
public void SelectOption(int index)
{
    if (hasSelected) {
        Debug.LogWarning("Already selected!");
        return; // ← Early exit
    }

    if (index < 0 || index >= options.Count) {
        Debug.LogError($"Invalid index: {index}");
        AutoSelectRandom(); // ← Fallback
        return;
    }

    if (options[index] == null) {
        Debug.LogError("Null option!");
        AutoSelectRandom(); // ← Fallback
        return;
    }

    // Valid input → proceed
    playerSelection = options[index];
    hasSelected = true;
}


Layer 2: Capacity Enforcement
──────────────────────────────
private void SpawnTroopsWithLimit(TroopCombination combo, Team team)
{
    if (combo == null) {
        Debug.LogError("Null combo!");
        combo = GetDefaultCombo(); // ← Fallback: Fire Knight ×1
    }

    int currentCount = TargetingSystem.GetAliveTroops(team).Count;
    int availableSlots = config.maxTroopsPerSide - currentCount;

    if (availableSlots <= 0) {
        Debug.LogWarning($"No slots for {team}");
        return; // ← Graceful skip
    }

    // Partial spawn if needed
    int amountToSpawn = Mathf.Min(combo.amount, availableSlots);

    if (amountToSpawn < combo.amount) {
        Debug.Log($"Partial spawn: {amountToSpawn}/{combo.amount}");
    }

    // Proceed with safe amount
    troopSpawner.SpawnTroops(combo, team);
}


Layer 3: Null Safety in Victory Check
──────────────────────────────────────
public static float GetTotalHP(Team team)
{
    if (!troopsByTeam.ContainsKey(team))
        return 0f; // ← Safe default

    float total = 0f;
    foreach (var troop in troopsByTeam[team])
    {
        if (troop == null) continue; // ← Skip nulls
        if (!troop.IsAlive) continue; // ← Skip dead

        total += troop.Health.CurrentHP;
    }
    return total;
}


Layer 4: CancellationToken Handling
────────────────────────────────────
private async UniTask RunTimerAsync(CancellationToken ct)
{
    try
    {
        while (timer > 0 && !hasSelected)
        {
            await UniTask.Delay(100, cancellationToken: ct);
            //                        ↑
            //                        └─ Throws OperationCanceledException
            //                           if scene unloads
            timer -= 0.1f;
        }
    }
    catch (OperationCanceledException)
    {
        Debug.Log("Timer cancelled");
        // Clean exit, no crash
    }
}


Layer 5: UI Null Checks
───────────────────────
private void UpdateTimer(float remaining)
{
    if (timerText == null) {
        Debug.LogWarning("Timer text not assigned!");
        return; // ← Prevent NullReferenceException
    }

    timerText.text = Mathf.CeilToInt(remaining).ToString();
}
```

---

## Performance Optimization Points

```
┌─────────────────────────────────────────────────────────────────┐
│                    Performance Hotspots                         │
└─────────────────────────────────────────────────────────────────┘

1. Timer Loops
──────────────
❌ BAD: Update every frame (60 FPS = 1800 calls in 30 seconds)

private void Update()
{
    if (isBattleActive)
    {
        timer -= Time.deltaTime;
        OnTimerUpdate?.Invoke(timer); // ← 60 times per second!
    }
}

✓ GOOD: Update every 100ms (10 FPS = 300 calls in 30 seconds)

private async UniTask RunTimerAsync(CancellationToken ct)
{
    while (timer > 0)
    {
        await UniTask.Delay(100, cancellationToken: ct); // ← 10 times/sec
        timer -= 0.1f;
        OnTimerUpdate?.Invoke(timer);
    }
}

Performance Gain: 83% fewer calls
───────────────────────────────────────────────────────────────────

2. Victory Checks
─────────────────
✓ EFFICIENT: Leverage existing TargetingSystem

var playerAlive = TargetingSystem.GetAliveTroops(Team.Player).Count;
//                ↑
//                └─ Already maintains cached list per team
//                   O(1) lookup + O(n) filter (already optimized)

❌ AVOID: Manual iteration every frame

foreach (var obj in FindObjectsOfType<TroopController>()) // ← SLOW!
{
    if (obj.Team == Team.Player && obj.IsAlive)
        playerAlive++;
}

───────────────────────────────────────────────────────────────────

3. UI Updates
─────────────
✓ GOOD: Update only when value changes

private int lastTimerValue = -1;

private void UpdateTimer(float remaining)
{
    int displayValue = Mathf.CeilToInt(remaining);

    if (displayValue != lastTimerValue) {
        timerText.text = displayValue.ToString();
        lastTimerValue = displayValue;
    }
    // Skips 9/10 text updates
}

───────────────────────────────────────────────────────────────────

4. HP Bar Updates
─────────────────
✓ ACCEPTABLE: Poll in Update() (only 2 queries)

private void Update()
{
    // Smooth HP bar updates for good UX
    float playerHP = TargetingSystem.GetTotalHP(Team.Player); // Fast query
    float aiHP = TargetingSystem.GetTotalHP(Team.AI);

    playerHPBar.value = playerHP;
    aiHPBar.value = aiHP;
}

Performance: <1ms per frame
───────────────────────────────────────────────────────────────────

5. Memory Allocations
─────────────────────
✓ AVOID: Creating lists in loops

❌ BAD:
while (timer > 0)
{
    var troops = new List<TroopController>(); // ← Allocates every loop!
    foreach (var t in GetTroops())
        troops.Add(t);
}

✓ GOOD: Reuse existing lists
var troops = TargetingSystem.GetAliveTroops(team); // ← Returns cached list
```

---

This architecture ensures:
- Clean separation of concerns
- Event-driven UI updates
- Robust error handling
- Optimal performance
- Easy testing and maintenance
