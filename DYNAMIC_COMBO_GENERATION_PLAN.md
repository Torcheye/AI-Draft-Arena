# Dynamic Combo Generation & 7-Bag Drafting - Implementation Plan

**Priority:** HIGH
**Estimated Time:** 4-5 hours (with UI polish)
**Status:** ✅ FINALIZED - Ready for Implementation
**Last Updated:** 2025-10-26

---

## Executive Summary

**What:** AI generates 4 random new troop combinations starting from Round 2, adds them to the draft pool. Both player and AI can select these new cards. Implement Tetris-style 7-bag algorithm to minimize duplicate drafting across all draft options.

**Why:**
- Increases variety and replayability
- Makes AI feel more creative and adaptive
- Reduces stale drafting where same combos appear repeatedly
- Player experiences unique combos each match
- Creates emergent gameplay through unexpected combinations

**Current State:**
- ✅ BuildDynamicCounter() infrastructure exists but is DISABLED (Phase 2)
- ❌ ScriptableObject.CreateInstance causes memory leak
- ❌ GetRandomCombinations() has no deduplication logic
- ❌ No bag-based randomization system

---

## Finalized Design Decisions

### ✅ Decision 1: Generation Strategy
**Chosen:** Truly random module mixing for variety

**Rationale:**
- AI already picks strategically from the pool (Phase 2/3 behavior)
- Random generation creates unexpected combos the player hasn't seen
- Adds discovery element ("Whoa, Water Archer with Berserk ability!")
- AI can still counter by selecting strategically from expanded pool

**Implementation:**
```csharp
// Truly random: Pick random module from each category
body = config.Bodies[Random.Range(0, config.Bodies.Count)];
weapon = config.Weapons[Random.Range(0, config.Weapons.Count)];
ability = config.Abilities[Random.Range(0, config.Abilities.Count)];
effect = config.Effects[Random.Range(0, config.Effects.Count)];
amount = {1, 2, 3, 5}[Random.Range(0, 4)];
```

**Future Enhancement:** Post-launch, can add "smart randomness" that avoids terrible synergies (e.g., slow melee + ranged weapon).

---

### ✅ Decision 2: Bag System Scope
**Chosen:** Both player and AI use bags, AI's bag honors guaranteed counter

**Rationale:**
- Player gets maximum variety (no repeats)
- AI also gets variety BUT strategic counter is inserted manually
- Fair gameplay: Both sides see diverse options
- Simpler architecture: One bag system, two instances

**Implementation:**
```csharp
// Player bag: Pure bag draws
CurrentPlayerOptions = playerBag.Draw(3);

// AI bag: Guaranteed counter + bag draws
if (latestCounter != null)
{
    aiOptions = [latestCounter] + aiBag.Draw(2);
}
else
{
    aiOptions = aiBag.Draw(3);
}
```

**Note:** AI's guaranteed counter is NOT drawn from bag, so it can repeat if needed for strategic purposes.

---

### ✅ Decision 3: Combo Count
**Chosen:** 4 combos per round starting Round 2

**Rationale:**
- Round 1: 10 base combos only (learning phase)
- Round 2+: +4 each round → 34 total by Round 7
- 4 is sweet spot: Enough variety, not overwhelming
- Pool growth: 10 → 14 → 18 → 22 → 26 → 30 → 34

**Math:**
```
Round 1: Pool = 10 (base only)
Round 2: Pool = 14 (10 base + 4 generated)
Round 3: Pool = 18 (10 base + 8 generated)
Round 4: Pool = 22 (10 base + 12 generated)
Round 5: Pool = 26 (10 base + 16 generated)
Round 6: Pool = 30 (10 base + 20 generated)
Round 7: Pool = 34 (10 base + 24 generated)
```

**Bag Behavior:** With 6-round memory, no repeats guaranteed if pool > 18 (happens Round 3+).

---

### ✅ Decision 4: Visual Feedback
**Chosen:** Subtle "NEW" badge on first appearance, then blends in

**Rationale:**
- First time seeing an AI combo: "NEW!" badge (yellow/gold)
- After drafted once by anyone: badge disappears
- Teaches player "this is AI-created" without being intrusive
- Creates excitement for discovering new combos

**Implementation:**
```csharp
// In DraftCard.cs
if (combo.isAIGenerated && !hasBeenSeen)
{
    ShowNewBadge(); // Small "NEW!" tag in corner
}
```

**Tracking:** Add `HashSet<ICombination> seenCombos` to MatchState or persist locally.

---

## Problem Analysis

### Issue 1: ScriptableObject Memory Leak
**File:** `CounterStrategyEngine.cs:146`
```csharp
var combo = ScriptableObject.CreateInstance<TroopCombination>();
```

**Problem:** ScriptableObjects created at runtime persist in memory forever (Unity doesn't garbage collect them). Each AI-generated combo allocates ~500 bytes that's never freed.

**Impact:** After 50 rounds (7-8 matches), memory leaks become noticeable. On mobile, this causes crashes.

**Solution Options:**

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **A: Runtime C# Class** | Zero memory leak, full control | Need parallel class structure | ⭐ **BEST** |
| **B: Object Pooling** | Reuses ScriptableObjects | Complex lifecycle, Unity serialization issues | ❌ Too complex |
| **C: Destroy After Use** | Simple | DestroyImmediate in builds is dangerous | ❌ Risky |

**Chosen:** **Option A - Runtime C# Class**

---

### Issue 2: Duplicate Drafting
**File:** `DraftController.cs:152-180`
```csharp
private List<TroopCombination> GetRandomCombinations(List<TroopCombination> pool, int count)
{
    // Uses simple Random.Range() with HashSet for uniqueness
    // BUT no cross-round deduplication
}
```

**Problem:** Player can see same combo in Round 2 and Round 3 draft options.

**Solution:** Implement **7-bag algorithm** (Tetris randomization)

---

## Solution Architecture

### 1. RuntimeTroopCombination Class

Create a plain C# class that mirrors TroopCombination but without ScriptableObject.

```csharp
// New file: Assets/_Project/Scripts/Modules/RuntimeTroopCombination.cs
namespace AdaptiveDraftArena.Modules
{
    /// <summary>
    /// Runtime-only troop combination created by AI.
    /// Unlike TroopCombination (ScriptableObject), this is GC-friendly.
    /// </summary>
    public class RuntimeTroopCombination
    {
        // Module references (point to existing ScriptableObject modules)
        public BodyModule body;
        public WeaponModule weapon;
        public AbilityModule ability;
        public EffectModule effect;

        // Amount
        public int amount;

        // Metadata
        public bool isAIGenerated = true;
        public int generationRound;
        public string counterReasoning;

        // Computed property
        public string DisplayName =>
            $"{(effect != null ? effect.displayName : "Unknown")} " +
            $"{(body != null ? body.displayName : "Unknown")} ×{amount}";

        // Validation
        public bool IsValid()
        {
            return body != null && weapon != null && ability != null && effect != null &&
                   (amount == 1 || amount == 2 || amount == 3 || amount == 5);
        }

        // Stat calculations (same as TroopCombination)
        public float GetFinalHP() => body.baseHP * TroopStats.GetStatMultiplier(amount);
        public float GetFinalDamage() => weapon.baseDamage * TroopStats.GetStatMultiplier(amount);
        public float GetFinalSpeed() => body.movementSpeed;
        public float GetAbilityEffectiveness() => TroopStats.GetAbilityMultiplier(amount);
    }
}
```

**Key Design Decisions:**
- ✅ Plain C# class (no UnityEngine.Object inheritance)
- ✅ References existing ScriptableObject modules (no duplication)
- ✅ Mirrors TroopCombination interface for compatibility
- ✅ Garbage collected automatically when no references exist

---

### 2. ICombination Interface (Polymorphism)

Create interface so both TroopCombination and RuntimeTroopCombination can be used interchangeably.

```csharp
// New file: Assets/_Project/Scripts/Modules/ICombination.cs
namespace AdaptiveDraftArena.Modules
{
    /// <summary>
    /// Common interface for both static (ScriptableObject) and runtime (AI-generated) combos.
    /// </summary>
    public interface ICombination
    {
        BodyModule body { get; }
        WeaponModule weapon { get; }
        AbilityModule ability { get; }
        EffectModule effect { get; }
        int amount { get; }
        bool isAIGenerated { get; }
        string DisplayName { get; }

        bool IsValid();
        float GetFinalHP();
        float GetFinalDamage();
        float GetFinalSpeed();
        float GetAbilityEffectiveness();
    }
}
```

**Then update:**
```csharp
// TroopCombination.cs
public class TroopCombination : ScriptableObject, ICombination { ... }

// RuntimeTroopCombination.cs
public class RuntimeTroopCombination : ICombination { ... }
```

**Impact:** All code that uses `TroopCombination` can now accept `ICombination` instead, enabling polymorphism.

---

### 3. 7-Bag Randomization System

Inspired by Tetris' bag randomization to ensure variety.

```csharp
// New file: Assets/_Project/Scripts/Draft/CombinationBag.cs
namespace AdaptiveDraftArena.Draft
{
    /// <summary>
    /// Implements 7-bag randomization algorithm for draft options.
    /// Ensures no combo appears too frequently across rounds.
    /// </summary>
    public class CombinationBag
    {
        private List<ICombination> pool;           // Full pool of available combos
        private List<ICombination> currentBag;     // Current bag being drawn from
        private HashSet<ICombination> recentPicks; // Last N picks to avoid immediate repeats

        public CombinationBag(List<ICombination> initialPool, int recentMemory = 6)
        {
            pool = new List<ICombination>(initialPool);
            currentBag = new List<ICombination>();
            recentPicks = new HashSet<ICombination>();
            RefillBag();
        }

        /// <summary>
        /// Updates the pool with new AI-generated combos.
        /// </summary>
        public void AddToPool(List<ICombination> newCombos)
        {
            foreach (var combo in newCombos)
            {
                if (!pool.Contains(combo))
                {
                    pool.Add(combo);
                    currentBag.Add(combo); // Also add to current bag
                }
            }
        }

        /// <summary>
        /// Draws N unique combinations from the bag.
        /// Refills bag when empty (shuffle pool and create new bag).
        /// </summary>
        public List<ICombination> Draw(int count)
        {
            var drawn = new List<ICombination>();

            for (int i = 0; i < count; i++)
            {
                // Refill if bag is empty
                if (currentBag.Count == 0)
                    RefillBag();

                // If still empty (pool too small), break
                if (currentBag.Count == 0)
                {
                    Debug.LogWarning($"[CombinationBag] Pool exhausted, only drew {drawn.Count}/{count}");
                    break;
                }

                // Draw random from bag
                int randomIndex = Random.Range(0, currentBag.Count);
                var combo = currentBag[randomIndex];
                currentBag.RemoveAt(randomIndex);

                drawn.Add(combo);
                recentPicks.Add(combo);

                // Trim recent picks to memory size
                if (recentPicks.Count > 6)
                {
                    // Remove oldest (simplified: just clear if too large)
                    // In production, use Queue<ICombination> for FIFO
                    recentPicks.Clear();
                }
            }

            return drawn;
        }

        /// <summary>
        /// Refills the bag by shuffling the pool (excluding recent picks).
        /// </summary>
        private void RefillBag()
        {
            currentBag.Clear();

            // Add all pool combos except recent picks
            foreach (var combo in pool)
            {
                if (!recentPicks.Contains(combo))
                    currentBag.Add(combo);
            }

            // Fisher-Yates shuffle
            for (int i = currentBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (currentBag[i], currentBag[j]) = (currentBag[j], currentBag[i]);
            }

            Debug.Log($"[CombinationBag] Refilled bag with {currentBag.Count} combos (pool size: {pool.Count}, recent: {recentPicks.Count})");
        }
    }
}
```

**Algorithm Explanation:**
1. **Pool:** All available combos (base + AI-generated)
2. **Bag:** Shuffled subset of pool (excludes recent picks)
3. **Draw:** Take from bag, remove from bag
4. **Refill:** When bag empty, create new shuffled bag from pool (minus recent picks)
5. **Recent Memory:** Last 6 drawn combos are excluded from next bag

**Result:** Combos rotate through systematically, minimum 7 rounds between repeats (if pool > 7).

---

### 4. AI Combo Generation Integration

**Where:** MatchController.RunRound() - BEFORE RunDraftPhase()

```csharp
// MatchController.cs
private async UniTask RunRound(int roundNumber, CancellationToken cancellationToken)
{
    Debug.Log($"=== Round {roundNumber} Start ===");

    // NEW: Generate AI combos starting from Round 2
    if (roundNumber >= 2)
    {
        await GenerateAICombinations(roundNumber, cancellationToken);
    }

    // Draft Phase
    await RunDraftPhase(cancellationToken);

    // ... rest of phases
}

private async UniTask GenerateAICombinations(int roundNumber, CancellationToken ct)
{
    Debug.Log($"[MatchController] Generating 4 random AI combos for Round {roundNumber}...");

    var newCombos = new List<ICombination>();

    for (int i = 0; i < 4; i++)
    {
        var combo = GenerateRandomCombo();
        if (combo != null && combo.IsValid())
        {
            combo.generationRound = roundNumber;
            newCombos.Add(combo);
            Debug.Log($"[MatchController] Generated: {combo.DisplayName}");
        }
    }

    // Add to pool
    State.AIGeneratedCombinations.AddRange(newCombos);

    // Update bag (if using bag system)
    if (combinationBag != null)
    {
        combinationBag.AddToPool(newCombos);
    }

    Debug.Log($"[MatchController] Added {newCombos.Count} combos to pool (total: {State.GetFullDraftPool().Count})");
}

private RuntimeTroopCombination GenerateRandomCombo()
{
    // Truly random generation (not strategic, just mix modules)
    if (config.Bodies.Count == 0 || config.Weapons.Count == 0 ||
        config.Abilities.Count == 0 || config.Effects.Count == 0)
    {
        Debug.LogError("[MatchController] Module pools are empty!");
        return null;
    }

    var combo = new RuntimeTroopCombination
    {
        body = config.Bodies[Random.Range(0, config.Bodies.Count)],
        weapon = config.Weapons[Random.Range(0, config.Weapons.Count)],
        ability = config.Abilities[Random.Range(0, config.Abilities.Count)],
        effect = config.Effects[Random.Range(0, config.Effects.Count)],
        amount = PickRandomAmount(),
        isAIGenerated = true
    };

    return combo;
}

private int PickRandomAmount()
{
    int[] validAmounts = { 1, 2, 3, 5 };
    return validAmounts[Random.Range(0, validAmounts.Length)];
}
```

**Key Points:**
- ✅ Generates 4 combos per round starting Round 2
- ✅ Truly random (not strategic) - adds variety
- ✅ Validates each combo before adding
- ✅ Updates both State.AIGeneratedCombinations and bag

---

### 5. DraftController Integration

**Update:** Use CombinationBag instead of GetRandomCombinations()

```csharp
// DraftController.cs
private CombinationBag playerBag;
private CombinationBag aiBag;

private void GenerateDraftOptions()
{
    var fullPool = matchState.GetFullDraftPool();

    if (fullPool.Count == 0)
    {
        Debug.LogError("Draft pool is empty! Cannot generate options.");
        return;
    }

    // Initialize bags if null (first round)
    if (playerBag == null)
        playerBag = new CombinationBag(fullPool);

    if (aiBag == null)
        aiBag = new CombinationBag(fullPool);

    // Draw player options from bag
    CurrentPlayerOptions = playerBag.Draw(config.draftOptionsCount);
    matchState.PlayerDraftOptions = new List<ICombination>(CurrentPlayerOptions);

    // Draw AI options from bag (with guaranteed counter if available)
    CurrentAIOptions = GenerateAIOptions(fullPool, config.draftOptionsCount);
    matchState.AIDraftOptions = new List<ICombination>(CurrentAIOptions);

    OnPlayerOptionsGenerated?.Invoke(CurrentPlayerOptions);

    Debug.Log($"Generated draft options - Player: {CurrentPlayerOptions.Count} | AI: {CurrentAIOptions.Count}");
}

private List<ICombination> GenerateAIOptions(List<ICombination> pool, int count)
{
    // Check if we have a recently generated counter
    ICombination latestCounter = null;
    if (matchState.AIGeneratedCombinations != null && matchState.AIGeneratedCombinations.Count > 0)
    {
        latestCounter = matchState.AIGeneratedCombinations[matchState.AIGeneratedCombinations.Count - 1];
        Debug.Log($"[DraftController] Including latest AI counter: {latestCounter.DisplayName}");
    }

    if (latestCounter == null)
    {
        // No counter, use bag
        return aiBag.Draw(count);
    }

    // Build AI options: 1 guaranteed counter + (count - 1) from bag
    var aiOptions = new List<ICombination> { latestCounter };
    var bagOptions = aiBag.Draw(count - 1);
    aiOptions.AddRange(bagOptions);

    return aiOptions;
}
```

**Changes:**
- ✅ Separate bags for player and AI (independent randomization)
- ✅ Player gets pure bag draws (max variety)
- ✅ AI still gets guaranteed counter when available
- ✅ Bags auto-update when new combos added to pool

---

## Implementation Checklist (FINALIZED)

### Phase A: Foundation (1.5 hours) ⭐ CRITICAL
**Goal:** Create polymorphic architecture for combos

- [ ] Create `Assets/_Project/Scripts/Modules/ICombination.cs` interface
  - [ ] Define all required properties (body, weapon, ability, effect, amount, etc.)
  - [ ] Define all required methods (IsValid, GetFinalHP, GetFinalDamage, etc.)
- [ ] Update `TroopCombination.cs` to implement `ICombination`
  - [ ] Add `: ICombination` to class declaration
  - [ ] Ensure all interface members are implemented (already are)
  - [ ] Add explicit interface implementation if needed
- [ ] Create `Assets/_Project/Scripts/Modules/RuntimeTroopCombination.cs` class
  - [ ] Implement `ICombination` interface
  - [ ] Mirror all TroopCombination functionality (stats, validation, display)
  - [ ] Add null-safe DisplayName property
  - [ ] Add generationRound and counterReasoning fields
- [ ] Update `MatchState.cs` to use `List<ICombination>`
  - [ ] Change `AIGeneratedCombinations` from `List<TroopCombination>` → `List<ICombination>`
  - [ ] Change `BaseCombinations` from `List<TroopCombination>` → `List<ICombination>`
  - [ ] Update `GetFullDraftPool()` to return `List<ICombination>`
- [ ] Update all other files that reference these types
  - [ ] `DraftController.cs` - CurrentPlayerOptions, CurrentAIOptions
  - [ ] `CounterStrategyEngine.cs` - GenerateCounter parameters/return types
  - [ ] `AIGenerationOrchestrator.cs` - return type
- [ ] **Test:** Compilation succeeds, no functionality change yet

**Acceptance Criteria:**
✅ Code compiles without errors
✅ Existing gameplay unchanged (still uses TroopCombination only)
✅ ICombination interface allows polymorphism

---

### Phase B: Bag System (1 hour) ⭐ CRITICAL
**Goal:** Implement 7-bag algorithm for deduplication

- [ ] Create `Assets/_Project/Scripts/Draft/CombinationBag.cs`
  - [ ] Implement pool/bag/recentPicks architecture
  - [ ] Implement `Draw(int count)` method
  - [ ] Implement `RefillBag()` with Fisher-Yates shuffle
  - [ ] Implement `AddToPool(List<ICombination>)` for dynamic combos
  - [ ] Add comprehensive debug logging
- [ ] Update `DraftController.cs` to use bags
  - [ ] Add `playerBag` and `aiBag` fields
  - [ ] Initialize bags in `GenerateDraftOptions()` (first round)
  - [ ] Replace `GetRandomCombinations()` with `playerBag.Draw()`
  - [ ] Update `GenerateAIOptions()` to use `aiBag.Draw()` with counter insertion
  - [ ] Update bags when pool changes (handled by MatchState update)
- [ ] **Test:** Play 3 rounds, verify no duplicate combos in player options

**Acceptance Criteria:**
✅ Player sees different combos each round
✅ Same combo doesn't appear within 6 rounds
✅ Bag refills correctly when exhausted
✅ AI still gets guaranteed counter when available

---

### Phase C: Random Generation (1 hour) ⭐ CRITICAL
**Goal:** Generate 4 random combos per round starting Round 2

- [ ] Add `GenerateAICombinations()` to `MatchController.cs`
  - [ ] Loop 4 times, call `GenerateRandomCombo()`
  - [ ] Validate each combo with `IsValid()`
  - [ ] Set `generationRound` metadata
  - [ ] Add to `State.AIGeneratedCombinations`
  - [ ] Update both bags with new combos
- [ ] Add `GenerateRandomCombo()` to `MatchController.cs`
  - [ ] Pick random Body from `config.Bodies`
  - [ ] Pick random Weapon from `config.Weapons`
  - [ ] Pick random Ability from `config.Abilities`
  - [ ] Pick random Effect from `config.Effects`
  - [ ] Pick random amount from {1, 2, 3, 5}
  - [ ] Create `RuntimeTroopCombination` instance
  - [ ] Return combo (not null if successful)
- [ ] Update `RunRound()` in `MatchController.cs`
  - [ ] Add `if (roundNumber >= 2)` check
  - [ ] Call `await GenerateAICombinations(roundNumber, ct)`
  - [ ] Place BEFORE `RunDraftPhase()`
- [ ] Update bags after generation
  - [ ] Call `playerBag.AddToPool(newCombos)` if bag exists
  - [ ] Call `aiBag.AddToPool(newCombos)` if bag exists
- [ ] **Test:** Play through Round 2, verify 4 new combos appear in pool

**Acceptance Criteria:**
✅ Round 1: 10 base combos only
✅ Round 2+: 4 new combos generated each round
✅ All generated combos are valid (IsValid() returns true)
✅ Generated combos appear in both player and AI draft options
✅ Pool grows correctly: 10→14→18→22→26→30→34

---

### Phase D: Strategic Counter Enhancement (0.5 hour) 🔵 OPTIONAL
**Goal:** Re-enable dynamic counter generation (now using RuntimeTroopCombination)

- [ ] Update `CounterStrategyEngine.CanBuildDynamicCombo()`
  - [ ] Change `return false` → `return true` (or conditional logic)
  - [ ] Keep existing validation (Bodies/Weapons/Abilities/Effects count > 0)
- [ ] Update `CounterStrategyEngine.BuildDynamicCounter()`
  - [ ] Change `ScriptableObject.CreateInstance<TroopCombination>()`
  - [ ] To `new RuntimeTroopCombination()`
  - [ ] Return type already supports ICombination
  - [ ] All module selection logic remains same
- [ ] Update `AIGenerationOrchestrator.GenerateCounterAsync()`
  - [ ] No changes needed (already uses ICombination)
- [ ] **Test:** Verify AI generates strategic counters when needed

**Acceptance Criteria:**
✅ AI can generate strategic counters dynamically
✅ No memory leaks (monitor in Profiler)
✅ Strategic counters score higher than random combos
✅ AI prefers strategic counter over random pool combos

**Note:** This phase is optional because AI already picks strategically from the expanded pool. Dynamic counter generation adds an extra strategic layer but isn't required for core functionality.

---

### Phase E: UI Polish (0.5-1 hour) 🎨 OPTIONAL
**Goal:** Visual feedback for AI-generated combos

- [ ] Add `seenCombos` tracking to `MatchState.cs` or `DraftController.cs`
  - [ ] `private HashSet<ICombination> seenCombos = new HashSet<ICombination>()`
  - [ ] Mark combo as seen when drafted by either player or AI
- [ ] Update `DraftCard.cs` to show "NEW" badge
  - [ ] Add `newBadgeImage` UI element (small yellow tag in corner)
  - [ ] Check `if (combo.isAIGenerated && !seenCombos.Contains(combo))`
  - [ ] Show badge if true, hide if false
  - [ ] Use DOTween for subtle pulse/glow effect
- [ ] Create badge sprite
  - [ ] Simple "NEW!" text in yellow/gold
  - [ ] Small size (32x32px)
  - [ ] Place in top-right corner of card
- [ ] **Test:** Verify badge appears on first appearance, disappears after drafting

**Acceptance Criteria:**
✅ "NEW" badge appears on AI-generated combos
✅ Badge disappears after combo is drafted once
✅ Badge is visually appealing and non-intrusive
✅ No performance impact

---

### Phase F: Testing & Polish (1 hour) ⭐ CRITICAL
**Goal:** Comprehensive testing and bug fixes

- [ ] **Memory Profiling**
  - [ ] Play 5 full matches (35 rounds total)
  - [ ] Monitor memory in Unity Profiler
  - [ ] Verify no memory leaks (flat memory usage)
  - [ ] Target: <1MB growth over 5 matches
- [ ] **Gameplay Testing**
  - [ ] Verify pool growth: 10→14→18→22→26→30→34
  - [ ] Verify no duplicate combos within 6 rounds
  - [ ] Verify AI still picks strategically
  - [ ] Verify all generated combos spawn correctly
  - [ ] Verify all generated combos can fight (no null references)
- [ ] **Edge Case Testing**
  - [ ] Module pools empty (should gracefully fail)
  - [ ] Only 1 body/weapon/ability/effect (should work)
  - [ ] Player/AI both pick same AI combo (should work)
  - [ ] Round 7 with 34 combos (bag should still work)
- [ ] **Performance Testing**
  - [ ] Measure generation time (target: <2ms per round)
  - [ ] Measure bag refill time (target: <1ms)
  - [ ] Verify no frame drops during generation
- [ ] **Bug Fixes**
  - [ ] Fix any null references found
  - [ ] Fix any compilation errors
  - [ ] Fix any gameplay issues

**Acceptance Criteria:**
✅ No memory leaks detected
✅ No runtime exceptions
✅ All combos are valid and functional
✅ Performance within targets
✅ Gameplay feels smooth and varied

---

## Testing Strategy

### Unit Tests
1. **RuntimeTroopCombination:** Validate IsValid(), GetFinalHP(), etc.
2. **CombinationBag:** Verify no duplicates within 7 draws, correct refill logic

### Integration Tests
1. **Round 1:** No new combos, pool = base combos only
2. **Round 2:** 4 new combos added, pool = base + 4
3. **Round 3:** 4 more combos added, pool = base + 8
4. **Bag System:** Same combo shouldn't appear in player options for 7+ rounds
5. **AI Counter:** AI still gets strategic counter in options (not random)
6. **Memory:** Monitor memory usage over 10 matches (should be stable)

### Manual Playtesting
1. Play full 7-round match
2. Verify variety in draft options
3. Verify no repeated combos within 3 rounds
4. Verify AI combos are valid (can spawn and fight)
5. Verify performance (no lag during generation)

---

## Performance Considerations

### Memory
- **Before:** ~500 bytes * 24 combos (4 per round * 6 rounds) = ~12KB leaked per match
- **After:** 0 bytes leaked (RuntimeTroopCombination is GC-collected)

### CPU
- Generating 4 combos: ~0.1ms (negligible)
- Bag refill (shuffle): ~1ms for pool of 50 combos
- Total impact: <2ms per round (imperceptible)

### Scalability
- Pool grows from ~10 base combos to ~34 combos by Round 7
- Bag system handles up to 100+ combos efficiently
- No performance concerns for PC platform

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| ICombination refactor breaks existing code | High | Medium | Incremental rollout, extensive testing |
| Bag system creates imbalanced drafts | Medium | Low | Tune recent memory size (default 6) |
| RuntimeTroopCombination missing Unity features | High | Low | Mirrors TroopCombination exactly, only removes ScriptableObject |
| AI combos are invalid (null modules) | High | Medium | Validate before adding to pool, graceful fallback |
| Too much variety (player overwhelmed) | Low | Medium | Limit to 4 combos per round, can tune later |

---

## Future Enhancements (Post-Implementation)

1. **Strategic Generation:** AI generates combos that counter player's recent picks (not just random)
2. **Rarity System:** Mark some combos as "Rare" or "Epic" with visual flair
3. **Player Feedback:** Show "This combo was created to counter you!" tooltip
4. **Combo Naming:** Auto-generate names like "Flame Berserker" or "Aqua Tank"
5. **Persistence:** Save generated combos to disk for cross-session variety
6. **Balancing:** Track win rates of AI combos, remove overperforming ones

---

## Acceptance Criteria

✅ **Functional:**
- [ ] Starting Round 2, 4 new combos appear in draft pool
- [ ] Player can select AI-generated combos
- [ ] AI can select AI-generated combos
- [ ] No combo repeats within 7 rounds (bag system working)
- [ ] AI still prioritizes strategic counters when available

✅ **Technical:**
- [ ] No memory leaks (profiler confirms)
- [ ] No compilation errors
- [ ] No runtime exceptions
- [ ] Code follows Unity C# conventions

✅ **Quality:**
- [ ] All new combos are valid (IsValid() returns true)
- [ ] Performance stable (<2ms generation time)
- [ ] Code is modular and reusable
- [ ] Comments explain 7-bag algorithm

---

## Estimated Timeline (FINALIZED)

| Phase | Duration | Type | Dependencies |
|-------|----------|------|--------------|
| **Phase A: Foundation** | 1.5h | ⭐ CRITICAL | None |
| **Phase B: Bag System** | 1h | ⭐ CRITICAL | Phase A |
| **Phase C: Random Generation** | 1h | ⭐ CRITICAL | Phase A |
| **Phase D: Strategic Counter** | 0.5h | 🔵 OPTIONAL | Phase A, C |
| **Phase E: UI Polish** | 0.5-1h | 🎨 OPTIONAL | Phase C |
| **Phase F: Testing & Polish** | 1h | ⭐ CRITICAL | All phases |
| **TOTAL (Core)** | **4.5 hours** | A+B+C+F |
| **TOTAL (Full)** | **5.5-6 hours** | All phases |

**Critical Path:** A → B → C → F (must complete in order)
**Parallel Work:** Phase D and E can be done independently after Phase C

---

## Implementation Order (FINALIZED)

### Session 1: Foundation (1.5h)
1. Create ICombination interface
2. Update TroopCombination to implement it
3. Create RuntimeTroopCombination class
4. Update MatchState and all references
5. Test compilation

### Session 2: Bag System (1h)
1. Create CombinationBag.cs
2. Update DraftController.cs
3. Test no duplicates across 3 rounds

### Session 3: Random Generation (1h)
1. Add GenerateAICombinations() and GenerateRandomCombo()
2. Integrate into RunRound()
3. Test 4 combos generated per round

### Session 4: Optional Enhancements (1-1.5h)
1. Re-enable strategic counter (Phase D)
2. Add "NEW" badge UI (Phase E)

### Session 5: Final Testing (1h)
1. Memory profiling
2. Gameplay testing
3. Edge case testing
4. Bug fixes

**Milestone Commits:**
- Commit 1: Phase A complete (foundation)
- Commit 2: Phase B+C complete (core functionality)
- Commit 3: Phase D+E+F complete (full feature)

---

## Success Metrics (FINALIZED)

### Quantitative
- ✅ **Memory:** <1MB growth over 5 matches (0 leaks)
- ✅ **Performance:** <2ms generation time per round
- ✅ **Variety:** No duplicate combos within 6 rounds
- ✅ **Pool Growth:** 10→14→18→22→26→30→34 (exact)
- ✅ **Generation:** Exactly 4 combos per round (Round 2+)

### Qualitative
- ✅ **Player Experience:** "Every match feels different!"
- ✅ **AI Behavior:** Still picks strategically, but from bigger pool
- ✅ **Surprise Factor:** Player discovers unexpected combos
- ✅ **Balance:** AI combos aren't overpowered or underpowered
- ✅ **Code Quality:** Modular, reusable, well-commented

---

## Risk Register (FINALIZED)

| # | Risk | Impact | Probability | Mitigation | Owner |
|---|------|--------|-------------|------------|-------|
| 1 | ICombination refactor breaks existing code | **HIGH** | Medium | Incremental rollout, extensive testing after Phase A | Dev |
| 2 | RuntimeTroopCombination missing Unity features | **HIGH** | Low | Mirror TroopCombination exactly, test all methods | Dev |
| 3 | Bag system creates imbalanced drafts | Medium | Low | Tune recentMemory=6, monitor player feedback | Design |
| 4 | AI combos are invalid (null modules) | **HIGH** | Medium | Validate before adding to pool, graceful fallback | Dev |
| 5 | Memory still leaks due to module references | **HIGH** | Low | Module references are to existing ScriptableObjects (safe) | Dev |
| 6 | Too much variety overwhelms player | Low | Medium | Limit to 4/round, can reduce post-launch | Design |
| 7 | AI picks terrible random combos | Medium | Low | AI picks strategically from pool, not randomly | Design |
| 8 | Performance degrades with large pool | Low | Low | Pool caps at 34, bag handles 100+ efficiently | Dev |

**Key Mitigations:**
- ⭐ **Phase A testing:** Critical to catch breaking changes early
- ⭐ **Validation:** All generated combos validated before adding to pool
- ⭐ **Monitoring:** Memory profiling mandatory before commit

---

## Code Review Checklist (FINALIZED)

**Before Committing:**
- [ ] All new files have proper namespace and header comments
- [ ] ICombination interface is well-documented
- [ ] RuntimeTroopCombination has comprehensive XML comments
- [ ] CombinationBag algorithm is explained (7-bag reference)
- [ ] No LINQ allocations in hot paths
- [ ] All null checks in place (body, weapon, ability, effect)
- [ ] Debug.Log statements use consistent prefixes
- [ ] No magic numbers (use const/config)
- [ ] Performance profiled (no leaks, <2ms generation)
- [ ] Unit tests pass (if any)
- [ ] Integration tests pass (gameplay flow)
- [ ] Code follows Unity C# conventions
- [ ] No compiler warnings
- [ ] unity-code-reviewer agent approves

---

## Post-Implementation Tasks (FINALIZED)

### Immediate (Before Commit)
1. Code review via unity-code-reviewer agent
2. Memory profiling (5 matches, check for leaks)
3. Gameplay testing (play through full 7 rounds)
4. Create commit via unity-commit-manager

### Short-Term (Next Sprint)
1. Gather player feedback on variety
2. Monitor combo balance (are any AI combos too strong?)
3. Track most/least popular AI-generated combos
4. Consider tuning generation rate (4 per round vs 3?)

### Long-Term (Future Phases)
1. Strategic generation (counter player's weaknesses)
2. Rarity system (Common/Rare/Epic AI combos)
3. Combo naming ("Flame Berserker", "Aqua Tank")
4. Persistence (save combos across sessions)
5. Analytics (which combos win most?)

---

## Acceptance Criteria (FINALIZED)

### Phase A: Foundation
- [ ] ICombination interface created and documented
- [ ] TroopCombination implements ICombination
- [ ] RuntimeTroopCombination created and tested
- [ ] All type references updated (MatchState, etc.)
- [ ] Code compiles without errors
- [ ] Existing gameplay unchanged

### Phase B: Bag System
- [ ] CombinationBag class created
- [ ] 7-bag algorithm implemented correctly
- [ ] DraftController uses bags for both player and AI
- [ ] No duplicate combos within 6 rounds
- [ ] Bag refills correctly when exhausted

### Phase C: Random Generation
- [ ] 4 combos generated per round (Round 2+)
- [ ] All generated combos are valid (IsValid = true)
- [ ] Pool grows correctly (10→34)
- [ ] Both player and AI can draft generated combos
- [ ] Generation time <2ms per round

### Phase D: Strategic Counter (Optional)
- [ ] CanBuildDynamicCombo returns true
- [ ] BuildDynamicCounter creates RuntimeTroopCombination
- [ ] AI generates strategic counters when needed
- [ ] No memory leaks

### Phase E: UI Polish (Optional)
- [ ] "NEW" badge appears on first appearance
- [ ] Badge disappears after drafting
- [ ] Visual design is appealing
- [ ] No performance impact

### Phase F: Testing & Polish
- [ ] No memory leaks (profiled over 5 matches)
- [ ] No runtime exceptions
- [ ] All combos spawn and fight correctly
- [ ] Performance within targets
- [ ] Code review passed
- [ ] Ready to commit

---

## Next Steps

**Status:** ✅ **PLAN FINALIZED - READY FOR IMPLEMENTATION**

**To Begin Implementation:**
1. User says "start implementation" or "proceed"
2. Begin with Phase A (Foundation)
3. Commit after each major phase (A, B+C, D+E+F)
4. Code review after each commit

**Estimated Total Time:** 4.5-6 hours
**Critical Path:** A → B → C → F (4.5 hours minimum)

---

**Signed Off By:** User (2025-10-26)
**Ready to Proceed:** YES ✅
