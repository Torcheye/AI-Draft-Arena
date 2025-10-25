# Draft & Battle System Implementation Design Document
**AI Draft Arena - Comprehensive Technical Specification**

---

## Document Overview

This design document specifies the complete implementation of the Draft and Battle systems for AI Draft Arena, integrating with the existing MatchController, TroopSpawner, and TargetingSystem. This document serves as the single source of truth for implementation.

**Status**: Implementation Ready
**Target Platform**: PC (2D Unity)
**Dependencies**: UniTask, DoTween, Existing Combat System
**Estimated Implementation Time**: 16-20 hours

---

## Table of Contents

1. [Feature Overview](#1-feature-overview)
2. [Technical Architecture](#2-technical-architecture)
3. [Component Specifications](#3-component-specifications)
4. [Data Flow & Integration](#4-data-flow--integration)
5. [Implementation Details](#5-implementation-details)
6. [Edge Cases & Error Handling](#6-edge-cases--error-handling)
7. [Performance Considerations](#7-performance-considerations)
8. [Testing Strategy](#8-testing-strategy)
9. [Future Extensibility](#9-future-extensibility)
10. [Implementation Checklist](#10-implementation-checklist)

---

## 1. Feature Overview

### 1.1 Core Features

**Draft System**:
- Present 3 random troop combinations to player
- 15-second timer with visual countdown
- Click-based selection with hover feedback
- Auto-random selection on timeout
- Simultaneous AI selection (instant for MVP)

**Battle System**:
- Spawn troops with 4-troop capacity enforcement
- 30-second battle timer
- Instant victory detection (team elimination)
- Timer expiration victory (HP comparison)
- Player wins ties

**Round Flow Integration**:
- Seamless phase transitions
- Score tracking
- AI generation trigger after rounds

### 1.2 Success Criteria

- Player can complete full draft-to-battle cycle
- All edge cases handled gracefully
- No game-breaking bugs
- Integration with existing combat system verified
- Event-driven UI communication functional

### 1.3 Dependencies

**Existing Systems** (Already Implemented):
- `MatchController` - Phase management, round loop
- `MatchState` - State storage
- `TroopSpawner` - Troop instantiation
- `TargetingSystem` - Team tracking, alive troops query
- `TroopController` - Individual troop behavior
- `HealthComponent` - HP tracking
- `GameConfig` - Configuration data

**New Systems** (To Be Implemented):
- `DraftController` - Draft logic
- `BattleController` - Battle timing and victory
- `DraftUI` - Draft screen UI
- `BattleUI` - Battle timer, HP display

---

## 2. Technical Architecture

### 2.1 System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      MatchController                        │
│  (Orchestrator - Calls DraftController & BattleController) │
└────────┬────────────────────────────────┬──────────────────┘
         │                                │
         ▼                                ▼
┌────────────────────┐          ┌────────────────────┐
│  DraftController   │          │ BattleController   │
│  - Timer Logic     │          │ - Battle Timer     │
│  - Option Gen      │          │ - Victory Logic    │
│  - Selection       │          │ - Spawn Trigger    │
└────────┬───────────┘          └──────┬─────────────┘
         │                             │
         │ Events                      │ Events
         ▼                             ▼
┌────────────────────┐          ┌────────────────────┐
│    DraftUI         │          │     BattleUI       │
│  - Card Display    │          │  - Timer Display   │
│  - Click Handler   │          │  - HP Display      │
│  - Visual Feedback │          │  - Victory Banner  │
└────────────────────┘          └────────────────────┘
         │                             │
         │                             │
         └──────────┬──────────────────┘
                    ▼
         ┌─────────────────────┐
         │   TroopSpawner      │
         │   (Existing)        │
         └──────────┬──────────┘
                    ▼
         ┌─────────────────────┐
         │  TargetingSystem    │
         │  (Existing)         │
         └─────────────────────┘
```

### 2.2 Communication Pattern

**Event-Driven UI Updates**:
- Controllers emit C# events
- UI components subscribe to events
- No direct controller → UI coupling
- Follows Observer pattern

**Direct References for Core Logic**:
- MatchController → DraftController (direct reference)
- MatchController → BattleController (direct reference)
- BattleController → TroopSpawner (direct reference)
- BattleController → TargetingSystem (static helper methods)

### 2.3 Component Lifecycle

```
MatchController.RunRound()
    ↓
1. RunDraftPhase()
    ├─ DraftController.StartDraft()
    ├─ [15 sec timer + player input]
    ├─ DraftController.OnDraftComplete event
    └─ Returns selected combinations

2. RunSpawnPhase()
    ├─ Reveal both picks
    └─ [1 sec reveal animation]

3. RunBattlePhase()
    ├─ BattleController.StartBattle(combos)
    ├─ TroopSpawner.SpawnTroops() × 2
    ├─ [Battle timer + victory checks]
    ├─ BattleController.OnBattleComplete event
    └─ Returns RoundResult

4. RunRoundEndPhase()
    ├─ Update scores
    ├─ AI generation (if not final round)
    └─ [2 sec pause]
```

---

## 3. Component Specifications

### 3.1 DraftController

**File Path**: `Assets/_Project/Scripts/Draft/DraftController.cs`

**Responsibilities**:
- Generate 3 random draft options per team
- Manage 15-second countdown timer
- Handle player selection input
- Handle AI selection (random for MVP)
- Auto-select on timeout
- Emit events for UI updates

**Public Interface**:
```csharp
public class DraftController : MonoBehaviour
{
    // Events
    public event Action<List<TroopCombination>> OnPlayerOptionsGenerated;
    public event Action<float> OnTimerUpdate; // Remaining seconds
    public event Action OnTimerWarning; // At 5 seconds
    public event Action<TroopCombination> OnPlayerSelected;
    public event Action<TroopCombination> OnAISelected;
    public event Action<DraftResult> OnDraftComplete;

    // Methods
    public async UniTask<DraftResult> StartDraftAsync(MatchState state, CancellationToken ct);
    public void SelectOption(int index); // Called by UI

    // Properties
    public float RemainingTime { get; private set; }
    public bool HasPlayerSelected { get; private set; }
    public List<TroopCombination> CurrentOptions { get; private set; }
}

public struct DraftResult
{
    public TroopCombination PlayerPick;
    public TroopCombination AIPick;
    public bool TimedOut;
}
```

**Key Data Structures**:
```csharp
private List<TroopCombination> currentOptions;
private TroopCombination playerSelection;
private bool hasSelected;
private float timer;
private GameConfig config;
```

**Algorithm - Option Generation**:
```csharp
private List<TroopCombination> GenerateOptions(MatchState state)
{
    var pool = new List<TroopCombination>();
    pool.AddRange(state.BaseCombinations);
    pool.AddRange(state.AIGeneratedCombinations);

    // Shuffle and take 3
    return pool.OrderBy(x => Random.value).Take(3).ToList();
}
```

**Algorithm - Timer Loop**:
```csharp
private async UniTask RunTimerAsync(CancellationToken ct)
{
    timer = config.draftDuration;
    bool warningFired = false;

    while (timer > 0 && !hasSelected)
    {
        await UniTask.Delay(100, cancellationToken: ct);
        timer -= 0.1f;

        OnTimerUpdate?.Invoke(timer);

        // Warning at 5 seconds
        if (timer <= 5f && !warningFired)
        {
            warningFired = true;
            OnTimerWarning?.Invoke();
        }
    }

    // Auto-select if no selection made
    if (!hasSelected)
    {
        AutoSelectRandom();
    }
}
```

**Edge Cases**:
- **No options available**: Fallback to base combinations only
- **Selection during timeout**: Timer stops immediately
- **Double selection**: Ignore subsequent clicks
- **Scene transition during draft**: CancellationToken cancels task

---

### 3.2 BattleController

**File Path**: `Assets/_Project/Scripts/Battle/BattleController.cs`

**Responsibilities**:
- Trigger troop spawning via TroopSpawner
- Enforce 4-troop capacity limit
- Manage 30-second battle timer
- Continuously check victory conditions
- Calculate round winner
- Emit battle events

**Public Interface**:
```csharp
public class BattleController : MonoBehaviour
{
    // Events
    public event Action OnBattleStarted;
    public event Action<float> OnTimerUpdate;
    public event Action<Team, VictoryReason> OnBattleEnded;

    // Methods
    public async UniTask<RoundResult> StartBattleAsync(
        TroopCombination playerCombo,
        TroopCombination aiCombo,
        CancellationToken ct
    );

    // Properties
    public float RemainingTime { get; private set; }
    public bool IsBattleActive { get; private set; }

    // Dependencies (assigned via Inspector or Awake)
    [SerializeField] private TroopSpawner troopSpawner;
}

public enum VictoryReason
{
    Elimination,
    TimerExpiration
}
```

**Key Data Structures**:
```csharp
private List<TroopController> playerTroops;
private List<TroopController> aiTroops;
private float battleTimer;
private bool battleEnded;
private GameConfig config;
private TroopSpawner troopSpawner;
```

**Algorithm - Spawn with Capacity Enforcement**:
```csharp
private void SpawnTroopsWithLimit(TroopCombination combo, Team team)
{
    int currentCount = TargetingSystem.GetAliveTroops(team).Count;
    int availableSlots = config.maxTroopsPerSide - currentCount;

    if (availableSlots <= 0)
    {
        Debug.LogWarning($"No slots available for {team}");
        return;
    }

    // Partial spawn if exceeds capacity
    int amountToSpawn = Mathf.Min(combo.amount, availableSlots);

    if (amountToSpawn < combo.amount)
    {
        Debug.Log($"Spawning partial amount: {amountToSpawn}/{combo.amount}");
    }

    // Create temporary combo with adjusted amount
    var adjustedCombo = ScriptableObject.CreateInstance<TroopCombination>();
    adjustedCombo.CopyFrom(combo);
    adjustedCombo.amount = amountToSpawn;

    var troops = troopSpawner.SpawnTroops(adjustedCombo, team);

    if (team == Team.Player)
        playerTroops.AddRange(troops);
    else
        aiTroops.AddRange(troops);
}
```

**Algorithm - Victory Check (Hybrid Approach)**:
```csharp
private async UniTask<RoundResult> RunBattleLoopAsync(CancellationToken ct)
{
    battleTimer = config.battleDuration;
    battleEnded = false;

    OnBattleStarted?.Invoke();

    while (battleTimer > 0 && !battleEnded)
    {
        await UniTask.Delay(100, cancellationToken: ct);
        battleTimer -= 0.1f;

        OnTimerUpdate?.Invoke(battleTimer);

        // Check instant victory (event-driven from troop deaths)
        var playerAlive = TargetingSystem.GetAliveTroops(Team.Player).Count;
        var aiAlive = TargetingSystem.GetAliveTroops(Team.AI).Count;

        if (playerAlive == 0 && aiAlive > 0)
        {
            return CreateResult(Team.AI, VictoryReason.Elimination);
        }
        else if (aiAlive == 0 && playerAlive > 0)
        {
            return CreateResult(Team.Player, VictoryReason.Elimination);
        }
        else if (playerAlive == 0 && aiAlive == 0)
        {
            // Simultaneous death - player wins tie
            return CreateResult(Team.Player, VictoryReason.Elimination);
        }
    }

    // Timer expired - HP comparison
    return DetermineWinnerByHP();
}

private RoundResult DetermineWinnerByHP()
{
    float playerHP = TargetingSystem.GetTotalHP(Team.Player);
    float aiHP = TargetingSystem.GetTotalHP(Team.AI);

    Team winner;
    if (playerHP > aiHP)
        winner = Team.Player;
    else if (aiHP > playerHP)
        winner = Team.AI;
    else
        winner = Team.Player; // Tie goes to player

    OnBattleEnded?.Invoke(winner, VictoryReason.TimerExpiration);

    return CreateResult(winner, VictoryReason.TimerExpiration);
}
```

**Edge Cases**:
- **Both teams 0 HP**: Player wins
- **Spawn overflow**: Spawn partial amount (e.g., 2/5 troops)
- **Battle cancelled mid-fight**: CancellationToken cleanup
- **Zero troops selected**: Fallback spawn 1 Fire Knight

---

### 3.3 TargetingSystem Extensions

**File Path**: Extend existing `Assets/_Project/Scripts/Combat/TargetingSystem.cs`

**New Methods to Add**:
```csharp
public static float GetTotalHP(Team team)
{
    if (!troopsByTeam.ContainsKey(team))
        return 0f;

    float total = 0f;
    foreach (var troop in troopsByTeam[team])
    {
        if (troop != null && troop.IsAlive)
        {
            total += troop.Health.CurrentHP;
        }
    }
    return total;
}

public static int GetAliveCount(Team team)
{
    return GetAliveTroops(team).Count;
}
```

---

### 3.4 DraftUI

**File Path**: `Assets/_Project/Scripts/UI/DraftUI.cs`

**Responsibilities**:
- Display 3 draft cards
- Show troop combination details
- Handle click input
- Visual feedback (hover, selection)
- Timer countdown display
- Warning animations at 5 seconds

**UI Structure**:
```
Canvas
└── DraftScreen
    ├── CardContainer
    │   ├── DraftCard_0 (TMP_Button)
    │   ├── DraftCard_1 (TMP_Button)
    │   └── DraftCard_2 (TMP_Button)
    ├── TimerText (TMP_Text)
    ├── PromptText (TMP_Text)
    └── Background (Image)
```

**DraftCard Prefab Structure**:
```
DraftCard (200×300px)
├── Frame (Image)
├── BodyIcon (Image)
├── WeaponIcon (Image)
├── AbilityIcon (Image)
├── EffectIcon (Image)
├── AmountText (TMP_Text) "×3"
├── NameText (TMP_Text) "Fire Knight"
├── StatsText (TMP_Text) "8 HP | 3 DMG"
└── SelectionGlow (Image, disabled by default)
```

**Public Interface**:
```csharp
public class DraftUI : MonoBehaviour
{
    [SerializeField] private List<DraftCard> cards;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text promptText;

    private DraftController draftController;

    private void Start()
    {
        draftController = FindObjectOfType<DraftController>();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        draftController.OnPlayerOptionsGenerated += DisplayOptions;
        draftController.OnTimerUpdate += UpdateTimer;
        draftController.OnTimerWarning += ShowWarning;
        draftController.OnPlayerSelected += OnSelectionMade;
    }

    private void DisplayOptions(List<TroopCombination> options)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetCombination(options[i]);
            cards[i].SetInteractable(true);
        }
    }

    private void UpdateTimer(float remaining)
    {
        timerText.text = Mathf.CeilToInt(remaining).ToString();

        // Color coding
        if (remaining <= 5f)
            timerText.color = Color.red;
        else if (remaining <= 10f)
            timerText.color = Color.yellow;
        else
            timerText.color = Color.white;
    }

    private void ShowWarning()
    {
        // DoTween scale pulse
        timerText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);

        // Play warning sound
        // AudioManager.PlaySound("timer_warning");
    }

    public void OnCardClicked(int index)
    {
        draftController.SelectOption(index);
    }
}
```

**DraftCard Component**:
```csharp
public class DraftCard : MonoBehaviour
{
    [SerializeField] private Image bodyIcon;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image effectIcon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image selectionGlow;

    private TroopCombination combination;
    private Button button;

    public void SetCombination(TroopCombination combo)
    {
        combination = combo;

        bodyIcon.sprite = combo.body.icon;
        weaponIcon.sprite = combo.weapon.icon;
        abilityIcon.sprite = combo.ability.icon;
        effectIcon.sprite = combo.effect.icon;
        effectIcon.color = combo.effect.color;

        amountText.text = $"×{combo.amount}";
        nameText.text = combo.DisplayName;

        var stats = combo.GetModifiedStats();
        statsText.text = $"{stats.maxHP:F1} HP | {stats.damage:F1} DMG";
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    public void OnHoverEnter()
    {
        transform.DOScale(1.05f, 0.2f);
    }

    public void OnHoverExit()
    {
        transform.DOScale(1f, 0.2f);
    }

    public void ShowSelected()
    {
        selectionGlow.enabled = true;
        selectionGlow.DOFade(1f, 0.2f);
    }
}
```

---

### 3.5 BattleUI

**File Path**: `Assets/_Project/Scripts/UI/BattleUI.cs`

**Responsibilities**:
- Display 30-second countdown
- Show team HP totals
- Victory/defeat banner
- Screen transitions

**UI Structure**:
```
Canvas
└── BattleScreen
    ├── TimerText (TMP_Text)
    ├── PlayerHPBar (Slider)
    ├── AIHPBar (Slider)
    ├── PlayerHPText (TMP_Text)
    ├── AIHPText (TMP_Text)
    └── VictoryBanner (Panel, hidden initially)
        ├── WinnerText (TMP_Text)
        └── ReasonText (TMP_Text)
```

**Public Interface**:
```csharp
public class BattleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private Slider aiHPBar;
    [SerializeField] private TMP_Text playerHPText;
    [SerializeField] private TMP_Text aiHPText;
    [SerializeField] private GameObject victoryBanner;
    [SerializeField] private TMP_Text winnerText;

    private BattleController battleController;

    private void Start()
    {
        battleController = FindObjectOfType<BattleController>();
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        battleController.OnTimerUpdate += UpdateTimer;
        battleController.OnBattleEnded += ShowVictory;
    }

    private void Update()
    {
        // Poll HP every frame for smooth UI updates
        UpdateHPBars();
    }

    private void UpdateTimer(float remaining)
    {
        timerText.text = Mathf.CeilToInt(remaining).ToString();

        // Urgent color at 5 seconds
        if (remaining <= 5f)
            timerText.color = Color.red;
        else
            timerText.color = Color.white;
    }

    private void UpdateHPBars()
    {
        float playerHP = TargetingSystem.GetTotalHP(Team.Player);
        float aiHP = TargetingSystem.GetTotalHP(Team.AI);

        playerHPBar.value = playerHP;
        aiHPBar.value = aiHP;

        playerHPText.text = $"{playerHP:F0} HP";
        aiHPText.text = $"{aiHP:F0} HP";
    }

    private void ShowVictory(Team winner, VictoryReason reason)
    {
        victoryBanner.SetActive(true);
        winnerText.text = winner == Team.Player ? "VICTORY!" : "DEFEAT";

        // DoTween fade in
        var canvasGroup = victoryBanner.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
    }
}
```

---

## 4. Data Flow & Integration

### 4.1 Complete Round Flow

```
MatchController.RunRound(roundNum)
    │
    ├─ RunDraftPhase()
    │   ├─ DraftController.StartDraftAsync(state, ct)
    │   │   ├─ Generate 3 options for player
    │   │   ├─ Generate 3 options for AI
    │   │   ├─ Emit OnPlayerOptionsGenerated → DraftUI displays
    │   │   ├─ Start 15-second timer → DraftUI updates countdown
    │   │   ├─ [Player clicks card] → DraftUI calls SelectOption()
    │   │   ├─ Emit OnPlayerSelected → DraftUI shows selection
    │   │   ├─ [AI instantly selects random]
    │   │   ├─ Emit OnAISelected
    │   │   └─ Return DraftResult(playerPick, aiPick)
    │   └─ Store selections in MatchState
    │
    ├─ RunSpawnPhase()
    │   ├─ Show reveal UI (both picks visible)
    │   ├─ DoTween card flip animations
    │   └─ Wait 1 second
    │
    ├─ RunBattlePhase()
    │   ├─ BattleController.StartBattleAsync(playerCombo, aiCombo, ct)
    │   │   ├─ Clear previous troops (TargetingSystem.ClearAll())
    │   │   ├─ Spawn player troops (TroopSpawner)
    │   │   ├─ Spawn AI troops (TroopSpawner)
    │   │   ├─ Emit OnBattleStarted → BattleUI shows timer
    │   │   ├─ Start 30-second timer loop
    │   │   │   ├─ Update BattleUI every 100ms
    │   │   │   ├─ Check instant victory (troop count = 0)
    │   │   │   └─ If timer expires → HP comparison
    │   │   ├─ Determine winner
    │   │   ├─ Emit OnBattleEnded → BattleUI shows victory
    │   │   └─ Return RoundResult(winner, reason, duration)
    │   └─ Store result in MatchState
    │
    └─ RunRoundEndPhase()
        ├─ Update scores (PlayerWins/AIWins)
        ├─ Show round summary UI
        ├─ [If not final round] Trigger AI generation
        └─ Wait 2 seconds
```

### 4.2 Event Flow Diagram

```
DraftController                 DraftUI
     │                             │
     ├─ OnPlayerOptionsGenerated ──▶ DisplayOptions()
     ├─ OnTimerUpdate ──────────────▶ UpdateTimer()
     ├─ OnTimerWarning ─────────────▶ ShowWarning()
     ├─ OnPlayerSelected ───────────▶ OnSelectionMade()
     └─ OnDraftComplete ────────────▶ HideDraftScreen()

BattleController                BattleUI
     │                             │
     ├─ OnBattleStarted ────────────▶ ShowBattleScreen()
     ├─ OnTimerUpdate ──────────────▶ UpdateTimer()
     └─ OnBattleEnded ──────────────▶ ShowVictory()
```

### 4.3 State Persistence

**MatchState Updates**:
```csharp
// After Draft
state.PlayerSelectedCombo = draftResult.PlayerPick;
state.AISelectedCombo = draftResult.AIPick;
state.PlayerPickHistory.Add(draftResult.PlayerPick);

// After Battle
state.RoundHistory.Add(roundResult);
state.AwardRoundWin(roundResult.Winner);
```

---

## 5. Implementation Details

### 5.1 File Structure

```
Assets/
└── _Project/
    └── Scripts/
        ├── Draft/
        │   ├── DraftController.cs          [NEW]
        │   └── DraftResult.cs              [NEW]
        │
        ├── Battle/
        │   ├── BattleController.cs         [NEW]
        │   └── VictoryReason.cs            [NEW]
        │
        ├── UI/
        │   ├── DraftUI.cs                  [NEW]
        │   ├── DraftCard.cs                [NEW]
        │   └── BattleUI.cs                 [NEW]
        │
        ├── Combat/
        │   └── TargetingSystem.cs          [EXTEND - Add GetTotalHP, GetAliveCount]
        │
        └── Match/
            └── MatchController.cs          [MODIFY - Integrate Draft/Battle calls]
```

### 5.2 Scene Setup

**Hierarchy**:
```
Scene: MainGame
├── GameManager (persistent)
├── MatchController
│   ├── DraftController
│   └── BattleController
│       └── TroopSpawner
├── Canvas
│   ├── DraftScreen
│   │   ├── CardContainer
│   │   │   ├── DraftCard_0
│   │   │   ├── DraftCard_1
│   │   │   └── DraftCard_2
│   │   └── TimerPanel
│   ├── BattleScreen
│   │   ├── TimerPanel
│   │   ├── HPBars
│   │   └── VictoryBanner
│   └── ScorePanel (persistent)
└── Battlefield
    ├── Background
    ├── PlayerSpawnZone (visual debug)
    └── AISpawnZone (visual debug)
```

### 5.3 Inspector Configuration

**MatchController**:
```csharp
[Header("Controllers")]
[SerializeField] private DraftController draftController;
[SerializeField] private BattleController battleController;
```

**BattleController**:
```csharp
[Header("Dependencies")]
[SerializeField] private TroopSpawner troopSpawner;
```

**DraftUI**:
```csharp
[Header("References")]
[SerializeField] private List<DraftCard> draftCards;
[SerializeField] private TMP_Text timerText;
[SerializeField] private TMP_Text promptText;
```

### 5.4 Configuration Values (GameConfig)

**Already Defined**:
- `draftDuration = 15f`
- `battleDuration = 30f`
- `maxTroopsPerSide = 4`
- `draftOptionsCount = 3`

**No New Config Needed** - Use existing values.

---

## 6. Edge Cases & Error Handling

### 6.1 Draft Phase Edge Cases

| Edge Case | Solution | Code Location |
|-----------|----------|---------------|
| **No draft options available** | Use base combinations only (always 4 available) | `DraftController.GenerateOptions()` |
| **Player clicks during timeout** | Timer stops immediately, selection accepted | `DraftController.SelectOption()` |
| **Double-click on card** | Ignore subsequent clicks (`hasSelected` flag) | `DraftController.SelectOption()` |
| **AI generation fails** | Pool still has base combos, continue normally | N/A (future feature) |
| **Scene transition during draft** | CancellationToken cancels UniTask cleanly | `DraftController.StartDraftAsync()` |
| **Timer reaches exactly 0 with selection** | Selection takes priority | `DraftController.RunTimerAsync()` |

### 6.2 Battle Phase Edge Cases

| Edge Case | Solution | Code Location |
|-----------|----------|---------------|
| **Both teams 0 troops simultaneously** | Player wins (tie-breaker rule) | `BattleController.RunBattleLoopAsync()` |
| **Spawn exceeds 4-troop limit** | Spawn partial amount, log warning | `BattleController.SpawnTroopsWithLimit()` |
| **Timer expires at exact 0.0f** | HP comparison determines winner | `BattleController.DetermineWinnerByHP()` |
| **HP totals exactly equal** | Player wins (tie-breaker rule) | `BattleController.DetermineWinnerByHP()` |
| **Player selects 0 troops (invalid combo)** | Fallback: spawn 1 Fire Knight | `BattleController.StartBattleAsync()` |
| **TroopSpawner fails to instantiate** | Log error, continue with spawned troops | `BattleController.SpawnTroopsWithLimit()` |
| **Battle cancelled mid-fight** | CancellationToken cleanup, troops remain | `BattleController.StartBattleAsync()` |

### 6.3 Error Handling Strategy

**Defensive Validation**:
```csharp
// Example: DraftController.SelectOption()
public void SelectOption(int index)
{
    if (hasSelected)
    {
        Debug.LogWarning("Draft: Selection already made!");
        return;
    }

    if (index < 0 || index >= currentOptions.Count)
    {
        Debug.LogError($"Draft: Invalid option index {index}!");
        return;
    }

    if (currentOptions[index] == null)
    {
        Debug.LogError("Draft: Selected null option!");
        AutoSelectRandom();
        return;
    }

    // Valid selection
    playerSelection = currentOptions[index];
    hasSelected = true;
    OnPlayerSelected?.Invoke(playerSelection);
}
```

**Fallback Behaviors**:
- Invalid draft → Auto-select random valid option
- Invalid battle combo → Spawn default Fire Knight ×1
- Missing UI reference → Log error but continue match flow
- TargetingSystem empty → Return 0 for HP/count queries

---

## 7. Performance Considerations

### 7.1 Optimization Strategies

**Timer Updates**:
- Update UI every 100ms (not every frame)
- Use `UniTask.Delay(100)` instead of `Update()`
- Avoids 600 Update calls over 10 seconds

**Victory Checks**:
- Leverage existing `TargetingSystem.GetAliveTroops()` (already optimized)
- Cache troop lists per team
- No per-frame raycasts or expensive queries

**UI Updates**:
- DoTween animations (hardware accelerated)
- Text updates: `Mathf.CeilToInt(timer)` only when value changes
- HP bars: Update only when HP changes (event-driven preferred, polling acceptable)

**Memory Management**:
- Reuse DraftCard GameObjects (no instantiation per draft)
- Temporary combo copies: Create only when needed, destroy after spawn
- Event subscription cleanup in `OnDestroy()`

### 7.2 Profiling Checkpoints

**Critical Path**:
1. Draft option generation (<1ms)
2. Timer loop (100ms intervals)
3. Troop spawning (4-5ms per troop)
4. Victory checks (<1ms per check)
5. UI updates (<2ms per frame)

**Performance Budget**:
- Draft phase: <10ms per frame
- Battle phase: <16ms per frame (60 FPS)
- UI animations: Use DoTween (GPU accelerated)

### 7.3 Garbage Collection Concerns

**Allocations to Avoid**:
- `new List<>()` in Update loops
- String concatenation in UI updates (use `StringBuilder` if needed)
- Boxing from enum comparisons (already minimal)

**Allocations Acceptable**:
- Draft option generation (once per draft)
- RoundResult struct creation (once per round)
- Event invocations (minimal overhead)

---

## 8. Testing Strategy

### 8.1 Unit Tests

**DraftController Tests**:
```csharp
[Test]
public void GenerateOptions_Returns3UniqueOptions()
{
    var state = new MatchState();
    state.BaseCombinations.AddRange(LoadBaseComboData());

    var options = draftController.GenerateOptionsForTesting(state);

    Assert.AreEqual(3, options.Count);
    Assert.IsTrue(options.All(o => o != null));
    Assert.AreEqual(3, options.Distinct().Count()); // No duplicates
}

[Test]
public void SelectOption_IgnoresDoubleSelection()
{
    draftController.StartDraft(mockState, CancellationToken.None);
    draftController.SelectOption(0);
    draftController.SelectOption(1); // Should be ignored

    var result = draftController.GetResultForTesting();

    Assert.AreEqual(mockState.CurrentOptions[0], result.PlayerPick);
}
```

**BattleController Tests**:
```csharp
[Test]
public void DetermineWinner_PlayerWinsWithMoreHP()
{
    SetupMockTroops(playerHP: 15f, aiHP: 10f);

    var result = battleController.DetermineWinnerByHPForTesting();

    Assert.AreEqual(Team.Player, result.Winner);
    Assert.AreEqual(VictoryReason.TimerExpiration, result.Reason);
}

[Test]
public void SpawnWithLimit_EnforcesMaxTroops()
{
    var combo = CreateCombo(amount: 5);
    SetupMockTroops(playerHP: 0f, aiHP: 0f, playerCount: 2);

    battleController.SpawnTroopsWithLimitForTesting(combo, Team.Player);

    var spawned = TargetingSystem.GetAliveTroops(Team.Player);
    Assert.AreEqual(4, spawned.Count); // Max 4 total (2 existing + 2 spawned)
}
```

### 8.2 Integration Tests

**Full Round Flow**:
```csharp
[UnityTest]
public IEnumerator FullRound_CompletesSuccessfully()
{
    var match = SetupMatchController();
    var task = match.RunRoundForTesting(1, CancellationToken.None);

    yield return task.ToCoroutine();

    Assert.IsTrue(task.IsCompleted);
    Assert.AreEqual(1, match.State.RoundHistory.Count);
    Assert.IsNotNull(match.State.RoundHistory[0].Winner);
}
```

### 8.3 Play Mode Tests

**Manual Test Cases**:

1. **Draft Timeout**
   - Start match
   - Wait 15 seconds without selecting
   - Verify: Random option auto-selected

2. **Draft Selection**
   - Start match
   - Click option #2 at 10 seconds
   - Verify: Selection accepted, timer stops

3. **Battle Instant Victory**
   - Draft 5× Nature Scouts (player)
   - AI drafts 1× Fire Tank
   - Verify: Fire Tank wins via elimination

4. **Battle Timer Victory**
   - Draft 1× Fire Knight (player)
   - AI drafts 1× Water Archer
   - Let timer run to 0
   - Verify: Winner determined by HP comparison

5. **Capacity Enforcement**
   - Modify test to spawn 3 troops (player)
   - Draft 5× scouts next round
   - Verify: Only 1 additional scout spawns (4 max total)

### 8.4 Edge Case Tests

**Simultaneous Death**:
```csharp
[Test]
public void BattleEnds_BothTeamsDie_PlayerWins()
{
    SetupMockTroops(playerHP: 1f, aiHP: 1f);
    SimultaneousKillAll();

    var result = battleController.GetLastResultForTesting();

    Assert.AreEqual(Team.Player, result.Winner);
}
```

---

## 9. Future Extensibility

### 9.1 Planned Extension Points

**DraftController Extensibility**:
```csharp
// Future: Weighted option generation
protected virtual List<TroopCombination> GenerateOptions(MatchState state)
{
    // Override in DraftControllerAdvanced for AI-weighted options
}

// Future: Player hints/recommendations
public event Action<int> OnRecommendedOptionHighlighted;
```

**BattleController Extensibility**:
```csharp
// Future: Replays/spectator mode
public void EnableRecording()
{
    // Record all troop actions for replay
}

// Future: Battle speed control
public void SetBattleSpeed(float multiplier)
{
    Time.timeScale = multiplier;
}
```

### 9.2 Potential Future Enhancements

**Draft System**:
- Ban/pick phase (each player bans 1 option)
- Re-roll mechanic (spend resource to refresh options)
- Draft history viewer (see past picks)
- Combo preview (spawn formation visualization)

**Battle System**:
- Pause/resume battle (for tutorials)
- Battle replay viewer
- Slow-motion on victory
- Camera controls (zoom in/out)

**UI Enhancements**:
- Draft card tooltips (hover for detailed stats)
- Battle minimap (top-down view)
- Damage dealt statistics
- MVP troop indicator

### 9.3 Scalability Considerations

**More Troops**:
- Current: Max 4 troops per side
- Future: Configurable via `GameConfig.maxTroopsPerSide`
- No code changes needed (already uses config value)

**More Draft Options**:
- Current: 3 options
- Future: Configurable via `GameConfig.draftOptionsCount`
- UI scaling: Grid layout for 4-5 options

**Longer Matches**:
- Current: Best of 7
- Future: Configurable via `GameConfig.maxRounds`
- No code changes needed (already uses config value)

---

## 10. Implementation Checklist

### Phase 1: Core Draft System (4-6 hours)

- [ ] **Create DraftController.cs**
  - [ ] Option generation method
  - [ ] Timer loop with UniTask
  - [ ] Selection handling
  - [ ] Auto-select on timeout
  - [ ] Event emissions

- [ ] **Create DraftResult.cs**
  - [ ] Struct definition
  - [ ] Constructor

- [ ] **Create DraftUI.cs**
  - [ ] Event subscriptions
  - [ ] Card display logic
  - [ ] Timer text updates
  - [ ] Warning animations

- [ ] **Create DraftCard.cs**
  - [ ] SetCombination() method
  - [ ] Hover animations
  - [ ] Selection visual feedback

- [ ] **Build Draft UI Prefabs**
  - [ ] DraftScreen canvas
  - [ ] DraftCard prefab (×3)
  - [ ] Timer panel
  - [ ] Button click bindings

- [ ] **Integrate with MatchController**
  - [ ] Add DraftController reference
  - [ ] Modify `RunDraftPhase()` to call DraftController
  - [ ] Store DraftResult in MatchState

- [ ] **Test Draft Standalone**
  - [ ] Manual play test
  - [ ] Verify timeout works
  - [ ] Verify selection works
  - [ ] Check UI updates

**Milestone 1**: Player can draft troops with timer and selection.

---

### Phase 2: Battle System (6-8 hours)

- [ ] **Create BattleController.cs**
  - [ ] Spawn integration with TroopSpawner
  - [ ] Capacity enforcement logic
  - [ ] Battle timer loop
  - [ ] Victory check logic (instant + timer)
  - [ ] HP comparison method
  - [ ] Event emissions

- [ ] **Create VictoryReason.cs**
  - [ ] Enum definition

- [ ] **Extend TargetingSystem.cs**
  - [ ] Add `GetTotalHP(Team)` method
  - [ ] Add `GetAliveCount(Team)` method
  - [ ] Test helper methods

- [ ] **Create BattleUI.cs**
  - [ ] Event subscriptions
  - [ ] Timer display
  - [ ] HP bar updates (polling)
  - [ ] Victory banner logic
  - [ ] DoTween animations

- [ ] **Build Battle UI Prefabs**
  - [ ] BattleScreen canvas
  - [ ] Timer panel
  - [ ] HP bars (×2)
  - [ ] Victory banner

- [ ] **Integrate with MatchController**
  - [ ] Add BattleController reference
  - [ ] Modify `RunBattlePhase()` to call BattleController
  - [ ] Pass draft results to battle
  - [ ] Store RoundResult in MatchState

- [ ] **Test Battle Standalone**
  - [ ] Manual play test
  - [ ] Verify troops spawn correctly
  - [ ] Test instant victory (kill all enemies)
  - [ ] Test timer victory (HP comparison)
  - [ ] Test capacity enforcement

**Milestone 2**: Full battle with victory conditions works.

---

### Phase 3: Integration & Polish (4-6 hours)

- [ ] **Full Round Integration**
  - [ ] Test draft → spawn → battle flow
  - [ ] Verify state persistence across phases
  - [ ] Test multiple rounds (best of 7)
  - [ ] Verify score tracking

- [ ] **Spawn Phase Implementation**
  - [ ] Reveal UI (show both picks)
  - [ ] DoTween card flip animations
  - [ ] 1-second delay

- [ ] **Round End Phase**
  - [ ] Round summary UI
  - [ ] Score update display
  - [ ] 2-second pause

- [ ] **UI Transitions**
  - [ ] Fade in/out between screens
  - [ ] DoTween scale/position animations
  - [ ] Screen activation/deactivation

- [ ] **Edge Case Handling**
  - [ ] Test all edge cases from Section 6
  - [ ] Add defensive validation
  - [ ] Implement fallback behaviors
  - [ ] Log warnings appropriately

- [ ] **Audio Integration** (if time permits)
  - [ ] Draft timer tick sound
  - [ ] Card selection sound
  - [ ] Battle start sound
  - [ ] Victory/defeat sounds

**Milestone 3**: Complete draft-to-battle cycle with polish.

---

### Phase 4: Testing & Bug Fixes (2-3 hours)

- [ ] **Play Mode Testing**
  - [ ] Play 3 full matches (7 rounds each)
  - [ ] Test timeout scenarios
  - [ ] Test capacity overflow
  - [ ] Test both victory types
  - [ ] Test tie scenarios

- [ ] **Bug Fixes**
  - [ ] Fix any crashes
  - [ ] Fix UI glitches
  - [ ] Fix timing issues
  - [ ] Fix event subscription leaks

- [ ] **Performance Profiling**
  - [ ] Check frame rate during battle
  - [ ] Profile timer loops
  - [ ] Check GC allocations

- [ ] **Code Review**
  - [ ] Follow CLAUDE.md conventions
  - [ ] Add necessary comments
  - [ ] Remove debug logs
  - [ ] Ensure SOLID principles

**Milestone 4**: Stable, bug-free draft and battle system.

---

## Implementation Order Summary

**Priority Order** (Critical Path):

1. **DraftController** → Core draft logic
2. **DraftUI** → Visual feedback for draft
3. **MatchController Integration** → Connect draft to match flow
4. **BattleController** → Core battle logic
5. **TargetingSystem Extensions** → Helper methods for victory
6. **BattleUI** → Visual feedback for battle
7. **MatchController Integration** → Connect battle to match flow
8. **Edge Case Handling** → Robust error handling
9. **Polish & Transitions** → Smooth UI experience
10. **Testing** → Verify all scenarios

**Dependencies**:
- DraftUI depends on DraftController
- BattleController depends on TroopSpawner (existing)
- BattleController depends on TargetingSystem extensions
- BattleUI depends on BattleController
- MatchController integration depends on both controllers

**Parallelization Opportunities**:
- Draft UI can be built while DraftController is in progress
- Battle UI can be built while BattleController is in progress
- TargetingSystem extensions can be done independently

---

## Verification Criteria

Before marking implementation complete, verify:

- [ ] Player can draft from 3 options
- [ ] 15-second timer counts down correctly
- [ ] Auto-select triggers on timeout
- [ ] Selected combination is stored in MatchState
- [ ] Troops spawn correctly in battle
- [ ] Max 4 troops enforced (partial spawning works)
- [ ] 30-second battle timer counts down correctly
- [ ] Instant victory triggers when team eliminated
- [ ] Timer victory triggers with HP comparison
- [ ] Player wins ties (simultaneous death, equal HP)
- [ ] UI updates reflect all state changes
- [ ] Events fire correctly and subscriptions clean up
- [ ] No console errors or warnings
- [ ] Frame rate stays above 60 FPS
- [ ] Full best-of-7 match completes successfully

---

## Conclusion

This design document provides a complete specification for implementing the Draft and Battle systems. All architectural decisions have been finalized based on user feedback, edge cases are documented, and the implementation path is clear.

**Key Architectural Patterns**:
- Event-driven UI (Observer pattern)
- Async/await with UniTask for timers
- Separation of concerns (controller/UI split)
- Direct references for core logic
- Configuration-driven behavior

**Integration Points**:
- Extends existing MatchController
- Uses existing TroopSpawner
- Extends TargetingSystem
- Follows existing code conventions

**Success Metrics**:
- Full draft → battle cycle functional
- All edge cases handled gracefully
- Clean event-driven architecture
- Performance within budget (60 FPS)
- Ready for AI generation integration

Proceed with implementation following the checklist in Section 10.

---

**Document Version**: 1.0
**Last Updated**: 2025-10-24
**Implementation Status**: Ready to Begin
