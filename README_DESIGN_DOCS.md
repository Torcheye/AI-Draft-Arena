# Design Documentation - Navigation Guide

Welcome to the AI Draft Arena design documentation! This suite provides everything needed to implement the Draft and Battle systems from scratch.

---

## Document Overview

### 1. **DRAFT_BATTLE_DESIGN_DOC.md** (Main Document)
**Purpose**: Complete technical specification
**Length**: ~60 pages
**Use When**: You need detailed implementation guidance

**Contains**:
- Full component specifications
- Complete algorithms and code patterns
- Data flow diagrams
- Edge case handling
- Performance considerations
- Testing strategy
- Implementation checklist

**Best For**:
- Developers implementing the systems
- Technical reviewers
- Detailed reference during coding

---

### 2. **IMPLEMENTATION_SUMMARY.md** (Quick Reference)
**Purpose**: Fast-access implementation guide
**Length**: ~10 pages
**Use When**: You need to quickly remember how something works

**Contains**:
- Core algorithms (copy-paste ready)
- File structure
- Event patterns
- Common pitfalls
- Testing checklist
- Implementation order

**Best For**:
- Daily reference while coding
- Quick refreshers
- Copy-paste code snippets

---

### 3. **ARCHITECTURE_DIAGRAM.md** (Visual Reference)
**Purpose**: System architecture visualization
**Length**: ~15 pages
**Use When**: You need to understand system interactions

**Contains**:
- ASCII architecture diagrams
- Data flow visualizations
- Component interaction maps
- Event subscription patterns
- State flow diagrams
- Performance optimization visuals

**Best For**:
- Understanding system structure
- Planning integration points
- Explaining to team members
- Debugging interaction issues

---

### 4. **TROUBLESHOOTING_GUIDE.md** (Debug Reference)
**Purpose**: Common issues and solutions
**Length**: ~10 pages
**Use When**: Something doesn't work as expected

**Contains**:
- 15 common issues with solutions
- Debug steps for each problem
- Inspector checklist
- Performance debugging
- Emergency fallbacks

**Best For**:
- Debugging broken features
- Fixing integration issues
- Performance problems
- Quick fixes

---

### 5. **GAME_DESIGN_DOC.md** (Game Context)
**Purpose**: Overall game design
**Length**: ~100 pages (existing)
**Use When**: You need context on combat rules, modules, etc.

**Contains**:
- Module system details
- Combat mechanics
- Victory conditions
- Element interactions
- Ability specifications

**Best For**:
- Understanding game rules
- Module data structures
- Combat system integration

---

## Reading Order by Role

### If You're Implementing Draft System First:
1. **IMPLEMENTATION_SUMMARY.md** (Section: Draft System)
2. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 3.1: DraftController)
3. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 3.4: DraftUI)
4. **ARCHITECTURE_DIAGRAM.md** (Component Interaction: Draft Phase)
5. Keep **TROUBLESHOOTING_GUIDE.md** open while coding

### If You're Implementing Battle System First:
1. **IMPLEMENTATION_SUMMARY.md** (Section: Battle System)
2. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 3.2: BattleController)
3. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 3.3: TargetingSystem Extensions)
4. **ARCHITECTURE_DIAGRAM.md** (Component Interaction: Battle Phase)
5. Keep **TROUBLESHOOTING_GUIDE.md** open while coding

### If You're Reviewing Architecture:
1. **ARCHITECTURE_DIAGRAM.md** (System Overview)
2. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 2: Technical Architecture)
3. **IMPLEMENTATION_SUMMARY.md** (Event-Driven UI Pattern)

### If You're Testing:
1. **DRAFT_BATTLE_DESIGN_DOC.md** (Section 8: Testing Strategy)
2. **IMPLEMENTATION_SUMMARY.md** (Testing Checklist)
3. **TROUBLESHOOTING_GUIDE.md** (as issues arise)

---

## Quick Links by Topic

### Event-Driven UI
- **Summary**: IMPLEMENTATION_SUMMARY.md → "Event-Driven UI Pattern"
- **Detailed**: DRAFT_BATTLE_DESIGN_DOC.md → Section 2.2
- **Visual**: ARCHITECTURE_DIAGRAM.md → "Event Subscription Pattern"

### Timer Implementation
- **Algorithm**: IMPLEMENTATION_SUMMARY.md → "Draft Controller - Timer Loop"
- **Detailed**: DRAFT_BATTLE_DESIGN_DOC.md → Section 3.1 (DraftController)
- **Debug**: TROUBLESHOOTING_GUIDE.md → Issue 2

### Victory Conditions
- **Algorithm**: IMPLEMENTATION_SUMMARY.md → "Battle Controller - Victory Check"
- **Detailed**: DRAFT_BATTLE_DESIGN_DOC.md → Section 3.2 (BattleController)
- **Visual**: ARCHITECTURE_DIAGRAM.md → "Battle Phase"

### Spawn Capacity Enforcement
- **Algorithm**: IMPLEMENTATION_SUMMARY.md → "Spawn with Capacity Limit"
- **Detailed**: DRAFT_BATTLE_DESIGN_DOC.md → Section 3.2 (SpawnTroopsWithLimit)
- **Debug**: TROUBLESHOOTING_GUIDE.md → Issue 8

### Edge Cases
- **Summary**: IMPLEMENTATION_SUMMARY.md → "Critical Edge Cases"
- **Detailed**: DRAFT_BATTLE_DESIGN_DOC.md → Section 6
- **Debug**: TROUBLESHOOTING_GUIDE.md (specific issues)

---

## Document Conventions

### Code Snippets
All code follows Unity C# conventions from CLAUDE.md:
- Explicit public/private modifiers
- `var` instead of explicit types
- No redundant `this.`
- Async methods end with `Async`

### Diagrams
- `┌─┐` boxes = Components/Systems
- `│` vertical lines = Data flow
- `→` arrows = Direction of communication
- `▼` down arrows = Sequential steps

### Annotations
- `← Comment` = Inline explanation
- `// ...` = Code continuation
- `[NEW]` = File to create
- `[EXTEND]` = Existing file to modify
- `[MODIFY]` = Existing file to update

---

## File Locations (Absolute Paths)

All design documents are in the project root:

```
D:\TORCHEYE GAMES\workspace\AI Draft Arena\
├── DRAFT_BATTLE_DESIGN_DOC.md          ← Main spec
├── IMPLEMENTATION_SUMMARY.md           ← Quick reference
├── ARCHITECTURE_DIAGRAM.md             ← Visual guide
├── TROUBLESHOOTING_GUIDE.md            ← Debug help
├── GAME_DESIGN_DOC.md                  ← Game context (existing)
├── CLAUDE.md                           ← Coding conventions (existing)
└── README_DESIGN_DOCS.md               ← This file
```

---

## Implementation Workflow

### Phase 1: Draft System (Day 1)
1. Read: IMPLEMENTATION_SUMMARY.md (Draft section)
2. Implement: DraftController.cs
3. Implement: DraftUI.cs + DraftCard.cs
4. Reference: DRAFT_BATTLE_DESIGN_DOC.md (Section 3.1, 3.4)
5. Debug: TROUBLESHOOTING_GUIDE.md (Issues 1-4)
6. Test: DRAFT_BATTLE_DESIGN_DOC.md (Section 8.3, Draft tests)

### Phase 2: Battle System (Day 2)
1. Read: IMPLEMENTATION_SUMMARY.md (Battle section)
2. Implement: TargetingSystem extensions
3. Implement: BattleController.cs
4. Implement: BattleUI.cs
5. Reference: DRAFT_BATTLE_DESIGN_DOC.md (Section 3.2, 3.3, 3.5)
6. Debug: TROUBLESHOOTING_GUIDE.md (Issues 5-8)
7. Test: DRAFT_BATTLE_DESIGN_DOC.md (Section 8.3, Battle tests)

### Phase 3: Integration (Day 3)
1. Read: ARCHITECTURE_DIAGRAM.md (Complete Round Flow)
2. Modify: MatchController.cs
3. Test: Full round cycle
4. Debug: TROUBLESHOOTING_GUIDE.md (Issues 11-12)
5. Polish: DoTween animations, UI transitions
6. Final test: IMPLEMENTATION_SUMMARY.md (Testing Checklist)

---

## Success Criteria

You're done when:

**Draft System**:
- [ ] 3 cards display on screen
- [ ] Timer counts down from 15
- [ ] Click works and timer stops
- [ ] Auto-select works on timeout
- [ ] Selected combo stored in MatchState

**Battle System**:
- [ ] Troops spawn (max 4 enforced)
- [ ] Timer counts down from 30
- [ ] Instant victory works (team elimination)
- [ ] Timer victory works (HP comparison)
- [ ] Player wins ties

**Integration**:
- [ ] Full round completes (draft → battle → end)
- [ ] Score updates correctly
- [ ] Multiple rounds work (best of 7)
- [ ] No console errors
- [ ] 60 FPS maintained

---

## Key Design Decisions (Locked In)

These were decided through Q&A and are final:

1. **UI Architecture**: Event-driven separation (Controller emits, UI subscribes)
2. **Draft Input**: Click-based selection
3. **Battle Victory**: Hybrid (instant check + timer expiration)
4. **Spawning**: Direct BattleController → TroopSpawner reference
5. **Battle State**: TargetingSystem helper methods (GetTotalHP, GetAliveCount)
6. **AI Draft**: Pure random for MVP
7. **Draft Timeout**: 5-second warning, auto-select random at 0
8. **Spawn Overflow**: Partial spawn (e.g., 2 out of 5)
9. **Simultaneous Death**: Player wins
10. **No Selection**: Auto-pick random

---

## Getting Help

### If something doesn't work:
1. Check **TROUBLESHOOTING_GUIDE.md** first
2. Add debug logs (examples in troubleshooting guide)
3. Verify inspector assignments (checklist in troubleshooting guide)
4. Check console for errors

### If you need to understand architecture:
1. Look at **ARCHITECTURE_DIAGRAM.md** visuals
2. Trace data flow in diagrams
3. Check event subscription pattern

### If you need code examples:
1. Check **IMPLEMENTATION_SUMMARY.md** for snippets
2. Reference **DRAFT_BATTLE_DESIGN_DOC.md** for full implementations
3. Look at existing code in project (MatchController, TroopSpawner, etc.)

---

## Document Maintenance

These documents are frozen for this implementation phase. After implementation, update:
- GAME_DESIGN_DOC.md if combat rules change
- CLAUDE.md if coding conventions change
- TROUBLESHOOTING_GUIDE.md if new issues discovered

Do NOT modify these design docs during implementation - they are your specification source of truth.

---

## Estimated Reading Time

- **DRAFT_BATTLE_DESIGN_DOC.md**: 2-3 hours (read once, reference often)
- **IMPLEMENTATION_SUMMARY.md**: 30-45 minutes (skim, use as reference)
- **ARCHITECTURE_DIAGRAM.md**: 45 minutes (understand, refer back)
- **TROUBLESHOOTING_GUIDE.md**: 15 minutes (read common issues)

**Total**: ~4-5 hours to read all docs thoroughly
**Practical**: 1 hour to skim summaries, refer to details as needed

---

## Final Notes

This design was created through:
1. Deep analysis of existing codebase
2. Structured Q&A on architecture decisions
3. Edge case exploration and mitigation planning
4. Integration with existing systems (MatchController, TroopSpawner, etc.)

Everything is designed to:
- Integrate cleanly with existing code
- Follow project conventions (CLAUDE.md)
- Handle edge cases gracefully
- Optimize for performance
- Enable easy testing

**You have everything you need to implement. Good luck!**

---

**Questions?** Refer to the appropriate document above. 95% of questions are answered in this suite.
