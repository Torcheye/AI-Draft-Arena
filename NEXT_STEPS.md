# AI Draft Arena - Next Steps Plan

**Last Updated:** 2025-10-25
**Project Status:** Core Systems Complete, UI Integration In Progress

---

## Quick Summary

**What's Done:**
- ✅ Core Module System (Bodies, Weapons, Abilities, Effects)
- ✅ Combat System (Movement, Targeting, Health, Combat)
- ✅ Match System (Draft, Battle, Round management)
- ✅ UI Scripts (DraftCard, DraftUI, BattleUI, UIManager, RevealUI)
- ✅ UI Prefabs (Canvas, Draft Screen, Battle Screen, Reveal Screen)

**What's Next:**
1. Complete UI integration & testing
2. Integrate Reveal phase into match flow
3. Polish animations & transitions
4. Implement AI generation system
5. Add more content (bodies, weapons, abilities)
6. Audio system
7. Build & playtesting

---

## Phase 1: UI Integration & Testing (Current Phase)

**Priority:** HIGHEST
**Time Estimate:** 2-4 hours
**Status:** In Progress

### Tasks

#### 1.1 Complete Scene Setup
- [ ] Verify all UI prefabs are properly instantiated in BattleTest scene
- [ ] Verify UIManager has all references assigned in Inspector:
  - MatchController
  - DraftUI
  - BattleUI
  - RevealUI
- [ ] Verify DraftUI has 3 DraftCard references assigned
- [ ] Verify BattleUI has all HP bar and timer references assigned
- [ ] Verify RevealUI has player and AI card references

#### 1.2 Test Draft Phase
- [ ] Start match and verify draft screen appears
- [ ] Verify 3 draft cards display with correct data
- [ ] Verify timer counts down from 15 seconds
- [ ] Test card selection (click a card)
- [ ] Verify timer stops after selection
- [ ] Verify auto-select works if timer reaches 0
- [ ] Check for console errors during draft phase

#### 1.3 Test Reveal Phase Integration
- [ ] Verify reveal screen shows after draft selection
- [ ] Verify player card displays on left
- [ ] Verify AI card displays on right
- [ ] Verify cards slide in smoothly
- [ ] Verify 2-second hold duration
- [ ] Verify fade out before battle starts
- [ ] Check for console errors during reveal phase

#### 1.4 Test Battle Phase
- [ ] Verify battle screen appears after reveal
- [ ] Verify timer counts down from 30 seconds
- [ ] Verify HP bars update as troops take damage
- [ ] Verify HP text displays correctly
- [ ] Verify victory banner appears on battle end
- [ ] Verify "VICTORY" or "DEFEAT" text is correct
- [ ] Test elimination victory (kill all enemy troops)
- [ ] Test timer expiration victory (HP comparison)
- [ ] Check for console errors during battle phase

#### 1.5 Test Full Match Flow
- [ ] Play a complete best-of-7 match (4 wins to victory)
- [ ] Verify score updates correctly each round
- [ ] Verify UI transitions are smooth between phases
- [ ] Verify no UI elements persist incorrectly
- [ ] Verify no memory leaks (check Profiler)
- [ ] Verify consistent 60 FPS throughout
- [ ] Check for any console errors or warnings

**Completion Criteria:**
- All UI screens display correctly
- All timers work and display properly
- All transitions are smooth
- No console errors
- Match completes successfully

---

## Phase 2: Reveal Phase Integration

**Priority:** HIGH
**Time Estimate:** 1-2 hours
**Status:** Pending

### Tasks

#### 2.1 Add Reveal Phase to MatchPhase Enum
- [ ] Open `Assets/_Project/Scripts/Match/MatchPhase.cs`
- [ ] Add `Reveal` phase between `Draft` and `Spawn`:
  ```csharp
  public enum MatchPhase
  {
      MatchStart,
      Draft,
      Reveal,  // ← Add this
      Spawn,
      Battle,
      RoundEnd,
      MatchEnd
  }
  ```

#### 2.2 Implement RunRevealPhase in MatchController
- [ ] Open `Assets/_Project/Scripts/Match/MatchController.cs`
- [ ] Add new method:
  ```csharp
  private async UniTask RunRevealPhase(CancellationToken ct)
  {
      State.CurrentPhase = MatchPhase.Reveal;
      OnPhaseChanged?.Invoke(MatchPhase.Draft, MatchPhase.Reveal);

      // Wait for reveal animation (handled by UIManager/RevealUI)
      await UniTask.Delay(3000, cancellationToken: ct); // 3 second reveal

      Debug.Log("Reveal phase complete");
  }
  ```

#### 2.3 Update RunRound to Include Reveal
- [ ] Modify `RunRound()` method to call `RunRevealPhase()`:
  ```csharp
  private async UniTask RunRound(int roundNumber, CancellationToken ct)
  {
      await RunDraftPhase(ct);
      await RunRevealPhase(ct);  // ← Add this line
      await RunSpawnPhase(ct);
      await RunBattlePhase(ct);
      await RunRoundEndPhase(ct);
  }
  ```

#### 2.4 Update UIManager to Show Reveal Screen
- [ ] Open `Assets/_Project/Scripts/UI/UIManager.cs`
- [ ] Add case for Reveal phase in `HandlePhaseChanged()`:
  ```csharp
  case MatchPhase.Reveal:
      if (draftUI != null) draftUI.Hide();
      if (revealUI != null) revealUI.ShowReveal(playerCombo, aiCombo);
      break;
  ```
- [ ] Pass selected combos from MatchState to RevealUI

#### 2.5 Test Reveal Integration
- [ ] Play match and verify reveal screen appears after draft
- [ ] Verify smooth transition: Draft → Reveal → Battle
- [ ] Verify no UI overlap
- [ ] Check console for errors

**Completion Criteria:**
- Reveal phase shows between draft and battle
- Player and AI selections are displayed
- Transitions are smooth
- No errors in console

---

## Phase 3: Animation & Polish

**Priority:** MEDIUM
**Time Estimate:** 3-5 hours
**Status:** Pending

### Tasks

#### 3.1 Draft Screen Animations
- [ ] Add card hover scale animation (already in DraftCard.cs - verify working)
- [ ] Add timer color transitions (white → yellow → red)
- [ ] Add timer warning pulse at 5 seconds
- [ ] Add card selection glow effect
- [ ] Add draft screen fade in/out transitions
- [ ] Test all animations for smoothness

#### 3.2 Battle Screen Animations
- [ ] Add battle timer color transitions (white → yellow → red)
- [ ] Add HP bar smooth lerping (instead of instant updates)
- [ ] Add victory banner entrance animation (scale + fade)
- [ ] Add troop spawn effects (optional particle burst)
- [ ] Test all animations for smoothness

#### 3.3 Reveal Screen Animations
- [ ] Verify card slide-in animation works
- [ ] Verify fade-in/fade-out transitions
- [ ] Add "VS" text animation (scale bounce)
- [ ] Test timing (2 second hold is comfortable)

#### 3.4 General Polish
- [ ] Add screen transition effects between phases
- [ ] Ensure all DoTween animations use proper easing
- [ ] Add subtle idle animations (optional floating text)
- [ ] Optimize animation performance (no frame drops)
- [ ] Test on different screen resolutions

**Completion Criteria:**
- All UI elements have smooth animations
- Transitions feel polished and professional
- No animation stutters or frame drops
- Cute and whimsical aesthetic achieved

---

## Phase 4: AI Generation System

**Priority:** MEDIUM-HIGH
**Time Estimate:** 6-10 hours
**Status:** Not Started

### Overview
Implement the AI counter-generation system that analyzes player patterns and generates custom troop combinations using Claude API.

### Tasks

#### 4.1 Player Analysis System
- [ ] Create `PlayerAnalyzer.cs` in `Assets/_Project/Scripts/AI/`
- [ ] Implement pattern detection:
  - Track most used bodies, weapons, abilities, effects
  - Track amount preferences (swarm vs. elite)
  - Track element choices
  - Analyze win/loss patterns
- [ ] Store analysis data in MatchState

#### 4.2 Counter Generation Prompt Builder
- [ ] Create `CounterPromptBuilder.cs`
- [ ] Implement prompt construction:
  - Summarize player patterns
  - Describe available modules
  - Request counter combination
  - Format as structured JSON request
- [ ] Test prompt quality (manual review)

#### 4.3 Claude API Integration
- [ ] Create `ClaudeAPIClient.cs`
- [ ] Implement API communication:
  - HTTP POST to Claude API endpoint
  - Handle authentication (API key from config)
  - Parse JSON response
  - Error handling and retries
- [ ] Store API key securely (not in Git!)

#### 4.4 AI Generation Controller
- [ ] Create `AIGenerationController.cs`
- [ ] Implement generation flow:
  - Analyze player patterns
  - Build prompt
  - Call Claude API
  - Parse response into TroopCombination
  - Validate generated combination
  - Add to AIGeneratedCombinations pool
- [ ] Implement fallback (rule-based) if API fails

#### 4.5 Fallback Rule-Based Generator
- [ ] Create `FallbackGenerator.cs`
- [ ] Implement simple counter logic:
  - Element counter (Fire → Water, etc.)
  - Body counter (Tank → DPS, etc.)
  - Amount mirror (swarm vs. swarm, elite vs. elite)
- [ ] Use as backup when API unavailable

#### 4.6 Integration with MatchController
- [ ] Call AIGenerationController in `RunRoundEndPhase()`
- [ ] Generate 1 new combination per round
- [ ] Add to MatchState.AIGeneratedCombinations
- [ ] Cap pool size at 20 combinations max
- [ ] Log generation success/failure

#### 4.7 Testing
- [ ] Test pattern detection accuracy
- [ ] Test API integration (successful calls)
- [ ] Test API failure handling (fallback works)
- [ ] Test generated combinations are valid
- [ ] Test counter effectiveness (manual play)
- [ ] Verify no performance impact

**Completion Criteria:**
- AI generates custom counters each round
- API integration works reliably
- Fallback system handles failures
- Generated combinations are balanced
- System adds strategic depth

**Security Note:**
- Store API key in local config file (NOT in Git)
- Add config file to .gitignore
- Document setup process for collaborators

---

## Phase 5: Content Expansion

**Priority:** MEDIUM
**Time Estimate:** 8-12 hours
**Status:** Not Started

### Overview
Expand the module library to increase variety and strategic depth.

### Tasks

#### 5.1 Additional Bodies
- [ ] **Archer** - Ranged body with low HP, high range
- [ ] **Scout** - Fast movement, low HP
- [ ] **Tank** - High HP, slow movement
- [ ] Create body ScriptableObject assets
- [ ] Create placeholder sprites (or use art guide)
- [ ] Test each body in combat

**Current:** 1/4 bodies (Knight only)
**Target:** 4/4 bodies

#### 5.2 Additional Weapons
- [ ] **Bow** - Ranged projectile weapon
- [ ] **Hammer** - AOE melee weapon
- [ ] **Daggers** - Fast attack speed, low damage
- [ ] **Staff** - Magic homing projectiles
- [ ] Implement projectile spawning for Bow and Staff
- [ ] Create weapon ScriptableObject assets
- [ ] Test each weapon in combat

**Current:** 1/5 weapons (Sword only)
**Target:** 5/5 weapons

#### 5.3 Additional Abilities
- [ ] **Passive Abilities (5 more):**
  - Armor (reduce incoming damage)
  - Lifesteal (heal on hit)
  - Thorns (reflect damage)
  - Speed Boost (permanent move speed)
  - Critical Strike (chance for 2× damage)

- [ ] **Triggered Abilities (5 more):**
  - Execute (bonus damage below 30% HP)
  - Last Stand (invincible for 3s at 1 HP)
  - Enrage (damage boost when hit)
  - Revenge (damage boost when ally dies)
  - Second Wind (heal when below 25% HP)

- [ ] **Control Abilities (5 more):**
  - Stun (disable enemy for 1.5s)
  - Root (prevent movement)
  - Silence (disable abilities)
  - Blind (miss attacks)
  - Charm (reverse enemy team temporarily)

- [ ] Implement ability classes
- [ ] Create ability ScriptableObject assets
- [ ] Test each ability in combat

**Current:** 5/20 abilities
**Target:** 20/20 abilities

#### 5.4 Base Combinations
- [ ] Create 4 base combinations using new modules
- [ ] Test for balance
- [ ] Ensure variety (melee, ranged, tank, support)
- [ ] Add to BaseCombinations pool

**Current:** 4 base combos
**Target:** 8-12 base combos

**Completion Criteria:**
- All 4 bodies implemented and tested
- All 5 weapons implemented and tested
- At least 15 abilities implemented
- 8+ base combinations available
- All content balanced and functional

---

## Phase 6: Audio System

**Priority:** LOW-MEDIUM
**Time Estimate:** 4-6 hours
**Status:** Not Started

### Tasks

#### 6.1 Audio Manager Setup
- [ ] Create `AudioManager.cs` singleton
- [ ] Implement music playback system
- [ ] Implement SFX playback system
- [ ] Add volume controls
- [ ] Pool AudioSources for SFX

#### 6.2 Music Tracks
- [ ] Main menu music (optional)
- [ ] Draft phase music (calm, thoughtful)
- [ ] Battle phase music (energetic, intense)
- [ ] Victory music (triumphant fanfare)
- [ ] Defeat music (somber tone)

#### 6.3 Sound Effects
- [ ] Draft timer tick (last 5 seconds)
- [ ] Card hover sound
- [ ] Card select sound
- [ ] Draft warning sound (5 seconds)
- [ ] Reveal transition sound
- [ ] Battle start sound
- [ ] Troop spawn sound
- [ ] Attack sounds (per weapon type)
- [ ] Damage hit sounds
- [ ] Troop death sounds
- [ ] Ability activation sounds
- [ ] Victory/defeat sounds

#### 6.4 Integration
- [ ] Hook audio events to UI interactions
- [ ] Hook audio events to combat events
- [ ] Hook audio events to phase transitions
- [ ] Test audio volume balance
- [ ] Add audio settings (optional)

**Completion Criteria:**
- Music plays during all phases
- SFX provide clear feedback
- Audio volume is balanced
- No audio stutters or pops

**Note:** Can use placeholder sounds initially, polish later.

---

## Phase 7: Build & Playtesting

**Priority:** LOW (do last)
**Time Estimate:** 2-4 hours
**Status:** Not Started

### Tasks

#### 7.1 Build Preparation
- [ ] Configure build settings (target PC)
- [ ] Set proper screen resolution options
- [ ] Configure quality settings
- [ ] Create app icon
- [ ] Test build process

#### 7.2 Performance Optimization
- [ ] Profile with Unity Profiler
- [ ] Optimize any bottlenecks
- [ ] Ensure 60 FPS on target hardware
- [ ] Check memory usage
- [ ] Fix any memory leaks

#### 7.3 Playtesting
- [ ] Play 10+ complete matches
- [ ] Test all abilities in action
- [ ] Verify AI generation quality
- [ ] Check for bugs or edge cases
- [ ] Balance pass (adjust module stats)

#### 7.4 Bug Fixes & Polish
- [ ] Fix all critical bugs found in playtesting
- [ ] Fix all high-priority bugs
- [ ] Polish any rough edges
- [ ] Final code review

#### 7.5 Build & Deploy
- [ ] Create final build
- [ ] Test build on clean machine
- [ ] Package with README/instructions
- [ ] Upload to distribution platform (itch.io, etc.)

**Completion Criteria:**
- Build runs without errors
- Game is playable and fun
- No critical bugs
- Performance is smooth
- Ready for distribution

---

## Recommended Work Order

### Week 1
**Day 1-2:** Phase 1 (UI Integration & Testing)
**Day 3:** Phase 2 (Reveal Phase Integration)
**Day 4-5:** Phase 4.1-4.3 (AI System Foundation)

### Week 2
**Day 1-2:** Phase 4.4-4.7 (AI System Completion)
**Day 3-4:** Phase 3 (Animation & Polish)
**Day 5:** Phase 5.1-5.2 (Bodies & Weapons)

### Week 3
**Day 1-2:** Phase 5.3 (Abilities)
**Day 3:** Phase 5.4 (Base Combinations)
**Day 4:** Phase 6 (Audio System)
**Day 5:** Phase 7 (Build & Playtesting)

**Total Estimated Time:** 3 weeks (assuming 5-6 hours/day)

---

## Current Blockers

**None identified** - Ready to proceed with Phase 1

---

## Critical Path Items

1. **Phase 1** (UI Integration) - MUST complete before anything else
2. **Phase 2** (Reveal Phase) - Needed for complete match flow
3. **Phase 4** (AI System) - Core feature, highest value
4. **Phase 5** (Content) - Needed for variety and replayability

**Optional/Polish:**
- Phase 3 (Animations) - Nice to have, but not critical
- Phase 6 (Audio) - Enhances experience but not required
- Phase 7 (Build) - Do last before release

---

## Success Metrics

**Minimum Viable Product (MVP):**
- ✅ Full match flow works (draft → reveal → battle → result)
- ✅ UI is functional and clear
- ✅ Combat system works reliably
- ⏳ AI generates custom counters each round
- ⏳ At least 8 base combinations available
- ⏳ Basic animations and polish

**Polished Product:**
- All UI animations smooth and cute
- All 20 abilities implemented
- AI generates effective counters
- Audio system complete
- 60 FPS on target hardware
- Extensive playtesting complete

**Stretch Goals:**
- Multiple game modes
- Player progression/unlocks
- Leaderboard/stats tracking
- Tutorial system
- Mobile build

---

## Questions to Answer

Before starting each phase, consider:

**Phase 1:**
- Are all UI references properly assigned in Inspector?
- Should we add debug tools for testing UI?

**Phase 2:**
- How long should the reveal phase last? (Currently 3 seconds)
- Should reveal be skippable?

**Phase 4:**
- What should happen if API is down? (Fallback ready)
- How do we store API keys securely?
- What's the rate limit for Claude API?

**Phase 5:**
- Which abilities provide the most strategic variety?
- How do we balance new modules?

**Phase 6:**
- Royalty-free music sources?
- SFX library recommendations?

**Phase 7:**
- Target platform (Windows only? Mac/Linux too)?
- Itch.io or other distribution?

---

## Documentation Reference

**Core Design:**
- `GAME_DESIGN_DOC.md` - Game mechanics and rules
- `DRAFT_BATTLE_DESIGN_DOC.md` - Technical specifications
- `ARCHITECTURE_DIAGRAM.md` - System architecture
- `TROUBLESHOOTING_GUIDE.md` - Common issues and fixes

**Implementation:**
- `UI_IMPLEMENTATION_GUIDE.md` - UI setup instructions
- `ART_ASSET_GUIDE.md` - Art creation guidelines
- `CLAUDE.md` - Project coding standards

**Navigation:**
- `README_DESIGN_DOCS.md` - Documentation index

---

## Getting Started

**To begin Phase 1:**
1. Open Unity project
2. Open BattleTest scene
3. Verify all UI prefabs are in scene
4. Check all Inspector references
5. Press Play and test draft phase
6. Follow checklist in Phase 1 above

**If you encounter issues:**
1. Check TROUBLESHOOTING_GUIDE.md
2. Verify all references in Inspector
3. Check Console for errors
4. Add debug logs to track execution

---

**Ready to continue! Start with Phase 1 UI testing.** 🚀
