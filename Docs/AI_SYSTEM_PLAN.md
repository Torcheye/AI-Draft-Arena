# AI Generation System - Algorithmic Approach (Multi-Phase Plan)

**Status:** Planning Phase - Ready to Implement
**Approach:** Pure Algorithmic Counter-Generation (No API Required)
**Last Updated:** 2025-10-25

---

## Executive Summary

**What Changed:** No Claude API integration. Instead, we're building a **sophisticated rule-based counter-generation system** that analyzes player patterns and generates strategic counters using weighted scoring algorithms.

**Why This Approach:**
- ✅ **Demo-ready:** Works immediately for all players, no API keys
- ✅ **Fast:** Instant generation (add fake delay for UX)
- ✅ **Reliable:** Never fails, no network dependencies
- ✅ **Secure:** No sensitive data, safe for public release
- ✅ **Strategic:** Can be very smart with proper algorithms

**Core Concept:** Track player patterns → Score all possible counters → Pick best match → Feel like intelligent AI

---

## Multi-Phase Implementation Strategy

### Phase 1: MVP - Basic Counter System (3-4 hours)
**Goal:** Get AI generating valid counters that beat the player sometimes

### Phase 2: Core Strategy - Smart Counters (3-4 hours)
**Goal:** AI makes strategically sound decisions based on multiple factors

### Phase 3: Progressive Difficulty - Adaptive AI (2-3 hours)
**Goal:** AI gets smarter as match progresses, learns player patterns

### Phase 4: Polish - Intelligent Feel (2-3 hours)
**Goal:** AI feels like a thinking opponent (delays, variety, unpredictability)

**Total Estimated Time:** 10-14 hours

---

## System Architecture (Simplified)

```
┌─────────────────────────────────────────────────────────────┐
│                     MATCH CONTROLLER                        │
│              (Calls AI in RoundEndPhase)                    │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│            AI GENERATION ORCHESTRATOR                       │
│         Entry: GenerateCounterAsync()                       │
└──────────┬──────────────────┬───────────────────────────────┘
           │                  │
           ▼                  ▼
    ┌────────────┐    ┌──────────────────┐
    │  Player    │    │ Counter Strategy │
    │  Analyzer  │    │     Engine       │
    └────┬───────┘    └────────┬─────────┘
         │                     │
         │ PlayerProfile       │ Scored Combos
         └──────────┬──────────┘
                    ▼
         ┌─────────────────────┐
         │  TroopCombination   │
         │    (Generated)      │
         └──────────┬──────────┘
                    ▼
         ┌─────────────────────┐
         │    MatchState       │
         │ .AIGenerated        │
         │  Combinations       │
         └─────────────────────┘
```

---

## PHASE 1: MVP - Basic Counter System

**Priority:** CRITICAL
**Time:** 3-4 hours
**Goal:** AI generates valid combinations that sometimes counter the player

### Components to Build

#### 1.1 PlayerProfile.cs (Data Structure)
**Purpose:** Store analyzed player behavior

```csharp
public class PlayerProfile
{
    // Element tracking
    public Dictionary<string, int> elementUsage;     // "FIRE" → 3 times
    public string mostUsedElement;                   // "FIRE"

    // Tactical tracking
    public float avgAmount;                          // 2.5
    public float avgHP;                              // 12.0
    public float avgDamage;                          // 4.0

    // Recent picks (last 3)
    public List<TroopCombination> recentPicks;

    // Simple helpers
    public string GetCounterElement()                // FIRE → WATER
    public bool PrefersSwarm()                       // avgAmount >= 3
    public bool PrefersMelee()                       // avgRange < 2.5
}
```

**Implementation:**
- Simple POCO class, no logic yet
- Basic getters for counter suggestions
- Element counter map: `Fire→Water, Water→Nature, Nature→Fire`

---

#### 1.2 PlayerAnalyzer.cs (Pattern Detection - Simple)
**Purpose:** Analyze player picks and create profile

```csharp
public class PlayerAnalyzer
{
    public PlayerProfile AnalyzePlayer(MatchState state)
    {
        var profile = new PlayerProfile();

        // Simple: Count element usage
        foreach (var pick in state.PlayerPickHistory)
        {
            string element = pick.effect.moduleId;
            if (!profile.elementUsage.ContainsKey(element))
                profile.elementUsage[element] = 0;
            profile.elementUsage[element]++;
        }

        // Find most used element
        profile.mostUsedElement = profile.elementUsage
            .OrderByDescending(kvp => kvp.value)
            .First().Key;

        // Calculate averages
        profile.avgAmount = state.PlayerPickHistory.Average(p => p.amount);
        profile.avgHP = state.PlayerPickHistory.Average(p => p.CalculateTotalHP());
        profile.avgDamage = state.PlayerPickHistory.Average(p => p.CalculateTotalDamage());

        // Store last 3 picks
        profile.recentPicks = state.PlayerPickHistory
            .TakeLast(3)
            .ToList();

        return profile;
    }
}
```

**Phase 1 Scope:**
- ✅ Track element usage (simple counter map)
- ✅ Calculate stat averages
- ✅ Store recent picks (avoid duplicates)
- ❌ Win/loss context (Phase 3)
- ❌ Ability tracking (Phase 2)
- ❌ Weapon tracking (Phase 2)

---

#### 1.3 CounterStrategyEngine.cs (Simple Scoring)
**Purpose:** Score combinations and pick counter

```csharp
public class CounterStrategyEngine
{
    private GameConfig config;

    public TroopCombination GenerateCounter(
        PlayerProfile profile,
        List<TroopCombination> availableCombos
    )
    {
        var scoredCombos = new List<(TroopCombination combo, int score)>();

        foreach (var combo in availableCombos)
        {
            int score = ScoreCombo(combo, profile);
            scoredCombos.Add((combo, score));
        }

        // Sort by score, pick highest
        var best = scoredCombos.OrderByDescending(x => x.score).First();
        return best.combo;
    }

    private int ScoreCombo(TroopCombination combo, PlayerProfile profile)
    {
        int score = 0;

        // PHASE 1: Simple element counter only
        string counterElement = profile.GetCounterElement();
        if (combo.effect.moduleId == counterElement)
        {
            score += 50; // Big bonus for element advantage
        }

        return score;
    }
}
```

**Phase 1 Scope:**
- ✅ Element-based scoring only (Fire → Water gets +50)
- ✅ Pick highest scoring combo
- ✅ Simple, predictable, but functional
- ❌ Multi-factor scoring (Phase 2)
- ❌ Randomness (Phase 4)

---

#### 1.4 AIGenerationOrchestrator.cs (Simple Flow)
**Purpose:** Coordinate analyzer + engine

```csharp
public class AIGenerationOrchestrator : MonoBehaviour
{
    [SerializeField] private GameConfig config;

    private PlayerAnalyzer analyzer;
    private CounterStrategyEngine engine;

    private void Awake()
    {
        analyzer = new PlayerAnalyzer();
        engine = new CounterStrategyEngine(config);
    }

    public async UniTask<TroopCombination> GenerateCounterAsync(
        MatchState state,
        CancellationToken ct
    )
    {
        // Step 1: Analyze player
        var profile = analyzer.AnalyzePlayer(state);

        // Step 2: Get available combos
        var availableCombos = state.BaseCombinations; // Phase 1: Use base pool only

        // Step 3: Generate counter
        var counter = engine.GenerateCounter(profile, availableCombos);

        // Phase 1: No delay, instant return
        return counter;
    }
}
```

**Phase 1 Scope:**
- ✅ Wire analyzer + engine together
- ✅ Return valid TroopCombination
- ✅ Use base combinations pool only
- ❌ AI-generated combos (Phase 2)
- ❌ "Thinking" delay (Phase 4)

---

### Integration with MatchController

**Modify:** `MatchController.RunRoundEndPhase()`

```csharp
[SerializeField] private AIGenerationOrchestrator aiOrchestrator;

private async UniTask RunRoundEndPhase(CancellationToken ct)
{
    State.CurrentPhase = MatchPhase.RoundEnd;
    OnPhaseChanged?.Invoke(MatchPhase.Battle, MatchPhase.RoundEnd);

    // Award wins based on round result
    if (currentRoundResult.Winner == Team.Player)
        State.PlayerWins++;
    else
        State.AIWins++;

    // Store result
    State.RoundHistory.Add(currentRoundResult);

    // TODO: Generate AI counter for next round (if not final round)
    if (State.CurrentRound < 7)
    {
        var counter = await aiOrchestrator.GenerateCounterAsync(State, ct);
        State.AIGeneratedCombinations.Add(counter);
        Debug.Log($"AI generated counter: {counter.DisplayName}");
    }

    await UniTask.Delay(2000, cancellationToken: ct);
}
```

---

### Phase 1 Testing Checklist

- [ ] PlayerAnalyzer correctly identifies most-used element
- [ ] CounterStrategyEngine picks Water when player uses Fire
- [ ] CounterStrategyEngine picks Fire when player uses Nature
- [ ] CounterStrategyEngine picks Nature when player uses Water
- [ ] AI generates 1 combo per round (max 6 total)
- [ ] No errors in console
- [ ] AI sometimes wins rounds (not perfect, but competitive)

### Phase 1 Success Criteria

✅ AI generates valid counters every round
✅ AI uses element advantage consistently
✅ Player notices AI countering their element choice
✅ No crashes or null reference errors
✅ Takes <1 hour to implement and test

---

## PHASE 2: Core Strategy - Smart Counters

**Priority:** HIGH
**Time:** 3-4 hours
**Goal:** Multi-factor scoring that considers range, stats, abilities, tactics

### Enhancements to CounterStrategyEngine

#### 2.1 Multi-Layer Scoring System

```csharp
private int ScoreCombo(TroopCombination combo, PlayerProfile profile)
{
    int score = 0;

    // Layer 1: Element Advantage (30 points)
    if (CountersElement(combo, profile))
        score += 30;

    // Layer 2: Range Advantage (25 points)
    if (CountersRange(combo, profile))
        score += 25;

    // Layer 3: Stat Counter (20 points)
    if (CountersStats(combo, profile))
        score += 20;

    // Layer 4: Amount Counter (15 points)
    if (CountersAmount(combo, profile))
        score += 15;

    // Layer 5: Ability Synergy (10 points)
    if (HasAbilitySynergy(combo))
        score += 10;

    return score;
}
```

#### 2.2 Counter Logic Details

**Element Counter:**
```csharp
private bool CountersElement(TroopCombination combo, PlayerProfile profile)
{
    string playerElement = profile.mostUsedElement;
    string comboElement = combo.effect.moduleId;

    // Fire < Water < Nature < Fire
    if (playerElement == "FIRE" && comboElement == "WATER") return true;
    if (playerElement == "WATER" && comboElement == "NATURE") return true;
    if (playerElement == "NATURE" && comboElement == "FIRE") return true;

    return false;
}
```

**Range Counter:**
```csharp
private bool CountersRange(TroopCombination combo, PlayerProfile profile)
{
    float playerRange = profile.avgRange; // Need to track this
    float comboRange = combo.body.attackRange;

    // If player uses melee (range < 2), counter with ranged (range >= 3)
    if (playerRange < 2.0f && comboRange >= 3.0f)
        return true;

    // If player uses ranged (range >= 3), counter with fast melee (high speed)
    if (playerRange >= 3.0f && comboRange < 2.0f && combo.body.movementSpeed >= 2.0f)
        return true;

    return false;
}
```

**Stat Counter:**
```csharp
private bool CountersStats(TroopCombination combo, PlayerProfile profile)
{
    // If player builds tanky (high HP), counter with sustained DPS
    if (profile.avgHP > 12.0f)
    {
        float comboDPS = combo.weapon.baseDamage / combo.weapon.attackCooldown;
        if (comboDPS > 3.0f) return true; // High DPS counters tanks
    }

    // If player builds glass cannon (high DMG, low HP), counter with tank
    if (profile.avgDamage > 4.0f && profile.avgHP < 10.0f)
    {
        if (combo.CalculateTotalHP() > 15.0f) return true; // Tank outlasts glass cannon
    }

    return false;
}
```

**Amount Counter:**
```csharp
private bool CountersAmount(TroopCombination combo, PlayerProfile profile)
{
    bool playerUsesSwarm = profile.PrefersSwarm(); // avgAmount >= 3

    // Counter swarm with AOE or elite
    if (playerUsesSwarm)
    {
        // AOE ability counters swarm
        if (combo.ability != null && combo.ability.category == AbilityCategory.Offensive)
            return true;

        // Elite (amount 1-2) with high stats counters swarm
        if (combo.amount <= 2 && combo.CalculateTotalHP() > 12.0f)
            return true;
    }

    // Counter elite with swarm
    if (!playerUsesSwarm && combo.amount >= 3)
        return true;

    return false;
}
```

**Ability Synergy:**
```csharp
private bool HasAbilitySynergy(TroopCombination combo)
{
    // Check if ability matches body/weapon well
    if (combo.ability == null) return false;

    // Tank + Regeneration = good synergy
    if (combo.body.role == BodyRole.Tank &&
        combo.ability.moduleId == "REGENERATION")
        return true;

    // DPS + Berserk = good synergy
    if (combo.body.role == BodyRole.DPS &&
        combo.ability.moduleId == "BERSERK")
        return true;

    // Swarm + Shield Aura = good synergy
    if (combo.amount >= 3 &&
        combo.ability.moduleId == "SHIELD_AURA")
        return true;

    return false;
}
```

---

#### 2.3 Enhanced PlayerAnalyzer

**Add tracking for:**
```csharp
public class PlayerProfile
{
    // PHASE 1 (existing)
    public Dictionary<string, int> elementUsage;
    public string mostUsedElement;
    public float avgAmount;
    public float avgHP;
    public float avgDamage;
    public List<TroopCombination> recentPicks;

    // PHASE 2 (new)
    public Dictionary<string, int> bodyUsage;        // "KNIGHT" → 5
    public Dictionary<string, int> weaponUsage;      // "SWORD" → 4
    public Dictionary<string, int> abilityUsage;     // "BERSERK" → 3
    public float avgRange;                           // 2.1
    public float avgSpeed;                           // 1.8

    // Helpers
    public string GetMostUsedBody()
    public string GetMostUsedWeapon()
    public bool PrefersMelee() => avgRange < 2.5f
    public bool PrefersRanged() => avgRange >= 3.0f
    public bool PrefersFastUnits() => avgSpeed >= 2.0f
}
```

---

#### 2.4 Dynamic Combo Generation

**Instead of picking from base pool only, BUILD new combos:**

```csharp
public TroopCombination GenerateCounter(PlayerProfile profile)
{
    // Step 1: Pick counter element
    var counterElement = PickCounterElement(profile);

    // Step 2: Pick counter body
    var counterBody = PickCounterBody(profile);

    // Step 3: Pick counter weapon
    var counterWeapon = PickCounterWeapon(profile);

    // Step 4: Pick synergistic ability
    var counterAbility = PickCounterAbility(profile, counterBody);

    // Step 5: Pick amount
    var amount = PickCounterAmount(profile);

    // Step 6: Create combination
    var combo = ScriptableObject.CreateInstance<TroopCombination>();
    combo.body = counterBody;
    combo.weapon = counterWeapon;
    combo.ability = counterAbility;
    combo.effect = counterElement;
    combo.amount = amount;

    return combo;
}

private EffectModule PickCounterElement(PlayerProfile profile)
{
    string counterElementId = profile.GetCounterElement();
    return config.Effects.First(e => e.moduleId == counterElementId);
}

private BodyModule PickCounterBody(PlayerProfile profile)
{
    // If player uses melee, pick ranged body (Archer)
    if (profile.PrefersMelee())
        return config.Bodies.First(b => b.attackRange >= 3.0f);

    // If player uses ranged, pick fast melee (Scout)
    if (profile.PrefersRanged())
        return config.Bodies.First(b => b.movementSpeed >= 2.0f);

    // Default: Tank
    return config.Bodies.First(b => b.role == BodyRole.Tank);
}

private WeaponModule PickCounterWeapon(PlayerProfile profile)
{
    // Match weapon to chosen body's range
    // If body is ranged, pick Bow; if melee, pick Sword
    // (simplified for now)
    return config.Weapons[Random.Range(0, config.Weapons.Count)];
}

private AbilityModule PickCounterAbility(PlayerProfile profile, BodyModule body)
{
    // Pick synergistic ability
    if (body.role == BodyRole.Tank)
        return config.Abilities.FirstOrDefault(a => a.moduleId == "REGENERATION");

    if (body.role == BodyRole.DPS)
        return config.Abilities.FirstOrDefault(a => a.moduleId == "BERSERK");

    // Default: random
    return config.Abilities[Random.Range(0, config.Abilities.Count)];
}

private int PickCounterAmount(PlayerProfile profile)
{
    // Counter swarm with elite, counter elite with swarm
    if (profile.PrefersSwarm())
        return Random.Range(0, 2) == 0 ? 1 : 2; // Elite
    else
        return Random.Range(0, 2) == 0 ? 3 : 5; // Swarm
}
```

---

### Phase 2 Testing Checklist

- [ ] AI picks ranged when player uses melee
- [ ] AI picks melee when player uses ranged
- [ ] AI picks tank when player uses glass cannon
- [ ] AI picks DPS when player uses tank
- [ ] AI picks swarm when player uses elite
- [ ] AI picks elite when player uses swarm
- [ ] AI combinations feel strategic
- [ ] AI sometimes generates unexpected but valid combos

### Phase 2 Success Criteria

✅ AI considers 5+ factors when generating counters
✅ AI generates NEW combinations (not just from base pool)
✅ AI counters feel intelligent and strategic
✅ Player notices AI adapting to their tactics
✅ AI win rate is 40-60% (balanced difficulty)

---

## PHASE 3: Progressive Difficulty - Adaptive AI

**Priority:** MEDIUM-HIGH
**Time:** 2-3 hours
**Goal:** AI evolves strategy across rounds, learns player patterns

### 3.1 Difficulty Levels by Round

```csharp
public enum DifficultyLevel
{
    Exploration,    // Rounds 1-2: Simple counters, test player
    Adaptation,     // Rounds 3-5: Strategic counters, exploit patterns
    Mastery         // Rounds 6-7: Sophisticated counters, complex synergies
}

public class CounterStrategyEngine
{
    private DifficultyLevel GetDifficultyLevel(int roundNumber)
    {
        if (roundNumber <= 2) return DifficultyLevel.Exploration;
        if (roundNumber <= 5) return DifficultyLevel.Adaptation;
        return DifficultyLevel.Mastery;
    }

    public TroopCombination GenerateCounter(
        PlayerProfile profile,
        int currentRound
    )
    {
        var difficulty = GetDifficultyLevel(currentRound);

        switch (difficulty)
        {
            case DifficultyLevel.Exploration:
                return GenerateExplorationCounter(profile);

            case DifficultyLevel.Adaptation:
                return GenerateAdaptiveCounter(profile);

            case DifficultyLevel.Mastery:
                return GenerateMasteryCounter(profile);
        }
    }
}
```

---

### 3.2 Exploration Phase (Rounds 1-2)

**Strategy:** Simple, readable counters to test player

```csharp
private TroopCombination GenerateExplorationCounter(PlayerProfile profile)
{
    // Use only 1-2 scoring layers
    // Focus on element advantage
    // Pick from base pool (don't generate complex combos yet)

    var candidates = config.BaseCombinations
        .Where(c => CountersElement(c, profile))
        .ToList();

    if (candidates.Count == 0)
        candidates = config.BaseCombinations.ToList();

    return candidates[Random.Range(0, candidates.Count)];
}
```

**Characteristics:**
- Element-focused counters
- Simple, predictable
- Uses base combinations
- Lets player establish patterns

---

### 3.3 Adaptation Phase (Rounds 3-5)

**Strategy:** Multi-factor counters based on confirmed patterns

```csharp
private TroopCombination GenerateAdaptiveCounter(PlayerProfile profile)
{
    // Use 3-4 scoring layers
    // Consider element + range + stats
    // Generate new combos with good synergies

    // Build counter based on multiple factors
    var counterElement = PickCounterElement(profile);
    var counterBody = PickCounterBody(profile);
    var counterWeapon = PickCounterWeapon(profile, counterBody);
    var counterAbility = PickCounterAbility(profile, counterBody);
    var amount = PickCounterAmount(profile);

    return CreateCombo(counterBody, counterWeapon, counterAbility, counterElement, amount);
}
```

**Characteristics:**
- Multi-factor counters
- Exploits confirmed player patterns
- Generates custom combinations
- Noticeable adaptation

---

### 3.4 Mastery Phase (Rounds 6-7)

**Strategy:** Complex synergies, hard counters, meta-game

```csharp
private TroopCombination GenerateMasteryCounter(PlayerProfile profile)
{
    // Use all 5 scoring layers
    // Add meta-game analysis (what beat player before)
    // Generate complex synergies
    // Prioritize hard counters

    // Analyze what worked against player previously
    var successfulCounters = profile.GetSuccessfulCounters(); // From round history

    if (successfulCounters.Count > 0)
    {
        // Build similar combo to what worked before
        var reference = successfulCounters.Last();
        return BuildSimilarCombo(reference, profile);
    }

    // Otherwise, generate sophisticated counter
    return GenerateAdaptiveCounter(profile); // Same as Phase 2, but with all layers
}
```

**Characteristics:**
- Maximum sophistication
- Uses win/loss history
- Hard counters to player's strategy
- Feels like AI "learned" player's style

---

### 3.5 Enhanced PlayerProfile for Win/Loss Tracking

```csharp
public class PlayerProfile
{
    // ... existing fields ...

    // PHASE 3: Win/Loss Context
    public List<TroopCombination> winningPicks;      // Player picks that won
    public List<TroopCombination> losingPicks;       // Player picks that lost
    public List<TroopCombination> successfulCounters; // AI picks that beat player

    public List<TroopCombination> GetSuccessfulCounters()
    {
        return successfulCounters;
    }
}
```

**Update PlayerAnalyzer:**
```csharp
public PlayerProfile AnalyzePlayer(MatchState state)
{
    var profile = new PlayerProfile();

    // ... existing analysis ...

    // PHASE 3: Analyze round outcomes
    for (int i = 0; i < state.RoundHistory.Count; i++)
    {
        var round = state.RoundHistory[i];
        var playerPick = state.PlayerPickHistory[i];
        var aiPick = state.AIPickHistory[i]; // Need to track this

        if (round.Winner == Team.Player)
            profile.winningPicks.Add(playerPick);
        else
        {
            profile.losingPicks.Add(playerPick);
            profile.successfulCounters.Add(aiPick); // What beat player
        }
    }

    return profile;
}
```

---

### Phase 3 Testing Checklist

- [ ] Round 1-2: AI uses simple element counters
- [ ] Round 3-5: AI uses multi-factor strategic counters
- [ ] Round 6-7: AI uses sophisticated hard counters
- [ ] AI difficulty feels progressive (not sudden spikes)
- [ ] Player notices AI "learning" their patterns
- [ ] AI reuses strategies that worked before

### Phase 3 Success Criteria

✅ AI clearly gets harder as rounds progress
✅ Player feels challenged but not overwhelmed
✅ AI exploits player's weaknesses by round 6-7
✅ Progression feels smooth (not jarring)

---

## PHASE 4: Polish - Intelligent Feel

**Priority:** LOW-MEDIUM (Nice to have)
**Time:** 2-3 hours
**Goal:** Make AI feel like it's thinking, add variety and unpredictability

### 4.1 "Thinking" Delay

```csharp
public async UniTask<TroopCombination> GenerateCounterAsync(
    MatchState state,
    CancellationToken ct
)
{
    // Show "AI analyzing..." message
    OnAIThinking?.Invoke(true);

    // Add realistic delay (1-2 seconds)
    float thinkTime = Random.Range(1.0f, 2.0f);
    await UniTask.Delay((int)(thinkTime * 1000), cancellationToken: ct);

    // Generate counter
    var profile = analyzer.AnalyzePlayer(state);
    var counter = engine.GenerateCounter(profile, state.CurrentRound);

    // Hide "AI analyzing..." message
    OnAIThinking?.Invoke(false);

    return counter;
}
```

**UI Integration:**
```csharp
// In UIManager or RoundEndUI
aiOrchestrator.OnAIThinking += (isThinking) =>
{
    if (isThinking)
        ShowMessage("AI analyzing your strategy...");
    else
        HideMessage();
};
```

---

### 4.2 Weighted Randomness (Avoid Predictability)

**Problem:** AI always picks highest-scored combo (too predictable)
**Solution:** Pick from top 3 with weighted probability

```csharp
private TroopCombination GenerateCounter(PlayerProfile profile, int round)
{
    // Score all possible combos
    var scoredCombos = ScoreAllCombos(profile);

    // Sort by score
    var sorted = scoredCombos.OrderByDescending(x => x.score).ToList();

    // Pick from top 3 with weighted probability
    // 60% pick best, 30% pick 2nd best, 10% pick 3rd best
    float roll = Random.Range(0f, 1f);

    if (roll < 0.6f)
        return sorted[0].combo; // Best
    else if (roll < 0.9f && sorted.Count > 1)
        return sorted[1].combo; // Second best
    else if (sorted.Count > 2)
        return sorted[2].combo; // Third best
    else
        return sorted[0].combo; // Fallback to best
}
```

**Benefits:**
- AI still favors good counters (60% optimal)
- But has variety (40% suboptimal but valid)
- Less predictable, feels more human

---

### 4.3 Duplicate Prevention

**Problem:** AI might generate same combo multiple rounds
**Solution:** Track last 2 AI picks, avoid repeating

```csharp
private TroopCombination GenerateCounter(PlayerProfile profile, int round)
{
    // Get last 2 AI picks
    var recentAIPicks = state.AIPickHistory.TakeLast(2).ToList();

    // Score all combos
    var scoredCombos = ScoreAllCombos(profile);

    // Filter out recent duplicates
    var filtered = scoredCombos
        .Where(x => !recentAIPicks.Any(recent => IsSameCombo(x.combo, recent)))
        .ToList();

    if (filtered.Count == 0)
        filtered = scoredCombos; // No valid options, allow duplicates

    // Pick from filtered list
    return PickWeightedRandom(filtered);
}

private bool IsSameCombo(TroopCombination a, TroopCombination b)
{
    return a.body.moduleId == b.body.moduleId &&
           a.weapon.moduleId == b.weapon.moduleId &&
           a.ability?.moduleId == b.ability?.moduleId &&
           a.effect.moduleId == b.effect.moduleId &&
           a.amount == b.amount;
}
```

---

### 4.4 Strategic Reasoning (Debug/Display)

**Generate human-readable reasoning for AI picks:**

```csharp
public class GeneratedCounter
{
    public TroopCombination combo;
    public string reasoning;
}

private GeneratedCounter GenerateCounterWithReasoning(PlayerProfile profile)
{
    var counter = GenerateCounter(profile);
    var reasoning = BuildReasoning(counter, profile);

    return new GeneratedCounter
    {
        combo = counter,
        reasoning = reasoning
    };
}

private string BuildReasoning(TroopCombination counter, PlayerProfile profile)
{
    var reasons = new List<string>();

    if (CountersElement(counter, profile))
        reasons.Add($"{counter.effect.displayName} counters {profile.mostUsedElement}");

    if (CountersRange(counter, profile))
        reasons.Add(profile.PrefersMelee()
            ? "Ranged troops avoid melee combat"
            : "Fast melee units close the distance");

    if (CountersAmount(counter, profile))
        reasons.Add(profile.PrefersSwarm()
            ? "Elite units outlast swarms"
            : "Swarm overwhelms elite troops");

    return string.Join(". ", reasons);
}
```

**Display Options:**
- Show in debug logs: `Debug.Log($"AI: {reasoning}")`
- Show in UI (optional): During reveal phase, display reasoning
- Show in post-match stats (optional): Explain AI's strategy

---

### 4.5 Occasional "Experimental" Picks

**Add 10% chance to ignore scoring and pick randomly:**

```csharp
private TroopCombination GenerateCounter(PlayerProfile profile, int round)
{
    // 10% chance: Pick random combo (keeps player guessing)
    if (Random.Range(0f, 1f) < 0.1f)
    {
        Debug.Log("AI: Trying experimental strategy");
        return PickRandomCombo();
    }

    // 90% chance: Use strategic scoring
    return GenerateStrategicCounter(profile, round);
}
```

**Benefits:**
- Prevents player from 100% predicting AI
- Creates memorable moments ("AI tried something weird and it worked!")
- Feels more dynamic

---

### Phase 4 Testing Checklist

- [ ] "Thinking" delay shows for 1-2 seconds
- [ ] AI picks varied counters (not always same one)
- [ ] AI doesn't repeat same combo 2 rounds in a row
- [ ] AI reasoning logs make sense
- [ ] Occasional experimental picks feel natural (not broken)

### Phase 4 Success Criteria

✅ AI feels like it's "thinking" (not instant)
✅ AI has variety in picks (not repetitive)
✅ AI occasionally surprises player with unexpected picks
✅ Player can't easily predict AI's next pick
✅ Overall experience feels polished and intelligent

---

## Data Structures Reference

### PlayerProfile (Complete)
```csharp
public class PlayerProfile
{
    // Element tracking
    public Dictionary<string, int> elementUsage;
    public string mostUsedElement;

    // Module tracking
    public Dictionary<string, int> bodyUsage;
    public Dictionary<string, int> weaponUsage;
    public Dictionary<string, int> abilityUsage;

    // Stat tracking
    public float avgAmount;
    public float avgHP;
    public float avgDamage;
    public float avgRange;
    public float avgSpeed;

    // Recent history
    public List<TroopCombination> recentPicks;

    // Win/Loss context
    public List<TroopCombination> winningPicks;
    public List<TroopCombination> losingPicks;
    public List<TroopCombination> successfulCounters;

    // Helpers
    public string GetCounterElement();
    public bool PrefersSwarm();
    public bool PrefersMelee();
    public bool PrefersRanged();
    public List<TroopCombination> GetSuccessfulCounters();
}
```

### GeneratedCounter
```csharp
public class GeneratedCounter
{
    public TroopCombination combo;
    public string reasoning;
    public int confidenceScore; // 0-100, how good is this counter
}
```

---

## Implementation Checklist (By Phase)

### PHASE 1: MVP (Do First)
- [ ] Create `PlayerProfile.cs`
- [ ] Create `PlayerAnalyzer.cs` (simple version)
- [ ] Create `CounterStrategyEngine.cs` (element counter only)
- [ ] Create `AIGenerationOrchestrator.cs` (simple flow)
- [ ] Integrate with `MatchController.RunRoundEndPhase()`
- [ ] Test: AI counters player's element

### PHASE 2: Core Strategy (Do Second)
- [ ] Add multi-layer scoring to `CounterStrategyEngine`
- [ ] Implement range counter logic
- [ ] Implement stat counter logic
- [ ] Implement amount counter logic
- [ ] Implement ability synergy logic
- [ ] Add dynamic combo generation (build new combos)
- [ ] Enhance `PlayerAnalyzer` (track more patterns)
- [ ] Test: AI generates strategic multi-factor counters

### PHASE 3: Progressive Difficulty (Do Third)
- [ ] Add difficulty level enum
- [ ] Implement `GenerateExplorationCounter()`
- [ ] Implement `GenerateAdaptiveCounter()`
- [ ] Implement `GenerateMasteryCounter()`
- [ ] Add win/loss tracking to `PlayerProfile`
- [ ] Update `PlayerAnalyzer` to track round outcomes
- [ ] Test: AI gets harder each phase

### PHASE 4: Polish (Do Last)
- [ ] Add "thinking" delay with events
- [ ] Implement weighted randomness (top 3 picks)
- [ ] Add duplicate prevention
- [ ] Generate strategic reasoning strings
- [ ] Add experimental pick chance (10%)
- [ ] Test: AI feels intelligent and varied

---

## Testing Strategy

### Unit Tests (Optional but Recommended)
- `PlayerAnalyzer.AnalyzePlayer()` returns correct averages
- `CounterStrategyEngine.ScoreCombo()` scores correctly
- Element counters work (Fire→Water, Water→Nature, Nature→Fire)
- Range counters work (Melee→Ranged, Ranged→Fast Melee)

### Integration Tests
- Full generation flow completes without errors
- Generated combos are always valid
- AI generates 1 combo per round (max 6)
- No duplicate generations within 2 rounds

### Manual Playtesting
- Play 10 complete matches
- Track AI win rate (should be 40-60%)
- Note if AI counters feel strategic
- Check for repetitive patterns
- Verify progressive difficulty

---

## Performance Considerations

### Phase 1-2: Negligible Impact
- All operations are fast (<1ms)
- Simple LINQ queries on small lists
- No API calls, no network delays

### Phase 3-4: Still Fast
- Scoring all combos: ~10-20ms (4 bodies × 5 weapons × 20 abilities × 3 effects = ~1200 combos max)
- Can optimize with early exit scoring if needed
- Add delay is FAKE (for UX only)

### Memory
- PlayerProfile: ~1KB
- Generated combos: ScriptableObjects (already in memory)
- Total overhead: <10KB

---

## Edge Cases to Handle

### 1. Round 1 (No History)
**Problem:** Can't analyze patterns (no picks yet)
**Solution:** Use default balanced profile or pick random from base pool

### 2. All Base Combos Have Same Element
**Problem:** Can't find element counter
**Solution:** Fall back to range/stat counters

### 3. Player Uses Same Combo Every Round
**Problem:** Pattern is obvious, AI should hard counter
**Solution:** Phase 3 detects this, generates perfect counter by round 3

### 4. AI Generates Invalid Combo
**Problem:** Missing module reference, amount = 0, etc.
**Solution:** Validate before returning, fall back to base combo if invalid

### 5. Player Switches Strategy Mid-Match
**Problem:** AI counters old pattern, not new one
**Solution:** Weight recent picks more heavily (last 3 rounds)

---

## Success Metrics

**Phase 1:**
- ✅ AI generates valid counters
- ✅ Element advantage works
- ✅ No crashes

**Phase 2:**
- ✅ AI win rate: 40-60%
- ✅ Counters feel strategic
- ✅ Player notices adaptation

**Phase 3:**
- ✅ Clear difficulty progression
- ✅ AI learns player patterns
- ✅ Rounds 6-7 feel challenging

**Phase 4:**
- ✅ AI feels intelligent (not robotic)
- ✅ Varied picks (not repetitive)
- ✅ Polished experience

---

## Estimated Timeline

**Phase 1:** 3-4 hours
**Phase 2:** 3-4 hours
**Phase 3:** 2-3 hours
**Phase 4:** 2-3 hours

**Total:** 10-14 hours over 2-3 days

---

**Ready to start Phase 1!** Let me know if you want any clarifications or changes to the plan. 🚀
