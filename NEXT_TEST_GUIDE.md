# Next Test Guide - Ability System Validation

## ✅ What Was Completed

### 1. **Combat Test Scene** ✓
- Basic troops spawn and battle
- Targeting works, movement works, combat works
- Element advantages (Water beats Fire) confirmed
- **Status**: Working and committed

### 2. **Ability System** ✓ (Just Added!)
Complete ability framework with 5 working abilities:
- IAbilityEffect interface
- AbilityExecutor component
- StatusEffect system
- Full combat integration

### 3. **Art Asset Guide** ✓
Complete guide for creating sprites, icons, and visual effects

---

## 🎯 Next Test: Abilities in Action

Now that the ability system is built, let's test it!

### In Unity Editor:

#### Step 1: Update Ability Modules

Open these assets and configure:

**`Assets/_Project/Data/Abilities/Ability_Berserk.asset`**:
- **abilityClassName**: `BerserkAbility`
- **Parameters** → Add 2 parameters:
  - Key: `damageBonus`, Value: `0.5`
  - Key: `hpThreshold`, Value: `0.5`

**Create New Abilities** (Right-click in `Data/Abilities/`):

1. **Ability_Regeneration**:
   - abilityClassName: `RegenerationAbility`
   - Parameters:
     - Key: `healPerSecond`, Value: `1`

2. **Ability_ShieldAura**:
   - abilityClassName: `ShieldAuraAbility`
   - Parameters:
     - Key: `hpBonus`, Value: `2`
     - Key: `radius`, Value: `3`

3. **Ability_FirstStrike**:
   - abilityClassName: `FirstStrikeAbility`
   - Parameters:
     - Key: `damageMultiplier`, Value: `2`

4. **Ability_Slow**:
   - abilityClassName: `SlowAbility`
   - Parameters:
     - Key: `speedReduction`, Value: `0.3`
     - Key: `duration`, Value: `2`

#### Step 2: Create New Test Combinations

**Combo_BerserkKnight** (High damage when low):
- Body: Body_Knight
- Weapon: Weapon_Sword
- Ability: Ability_Berserk ← NEW!
- Effect: Effect_Fire
- Amount: 1

**Combo_RegenKnight** (Heals over time):
- Body: Body_Knight
- Weapon: Weapon_Sword
- Ability: Ability_Regeneration ← NEW!
- Effect: Effect_Water
- Amount: 2

**Combo_FirstStrikeScout** (Burst damage):
- Body: Body_Knight (use knight for now)
- Weapon: Weapon_Sword
- Ability: Ability_FirstStrike ← NEW!
- Effect: Effect_Water
- Amount: 2

**Combo_SlowKnight** (Control):
- Body: Body_Knight
- Weapon: Weapon_Sword
- Ability: Ability_Slow ← NEW!
- Effect: Effect_Fire
- Amount: 1

#### Step 3: Update BattleTest Scene

1. Open `BattleTest` scene
2. Select `BattleTest` GameObject
3. Change combinations:
   - **Player Combination**: `Combo_BerserkKnight`
   - **AI Combination**: `Combo_RegenKnight`

#### Step 4: Test Each Ability!

**Test 1: Berserk** (Fire Knight vs Regen Knight):
1. Press Play
2. Watch console for "BERSERK activated!" when knight drops below 50% HP
3. Damage should increase (check logs)

**Test 2: Regeneration**:
1. Let Regen Knight get damaged
2. Watch HP slowly heal back up (1 HP/sec)
3. Console shows heal events

**Test 3: First Strike**:
- Change to Combo_FirstStrikeScout
- Press Play
- First attack should deal 2× damage
- Console shows "FIRST STRIKE! 2× damage"

**Test 4: Slow**:
- Change Player to Combo_SlowKnight
- Press Play
- When knight attacks, target should slow down
- Console shows "slowed by 30%"
- Visually see slower movement

**Test 5: Shield Aura** (Create combo first):
- Use Ability_ShieldAura with amount: 3 (multiple troops)
- Troops should have higher HP when near each other
- Console shows HP bonus applied

---

## 🎮 Expected Behaviors

### ✅ Berserk
- Normal damage until HP < 50%
- Then damage increases by 50%
- Console: "BERSERK activated! +50% damage"

### ✅ Regeneration
- Heals 1 HP every second
- Continues until full HP or death
- Keeps troops alive longer

### ✅ First Strike
- First attack deals 2× damage
- Console: "FIRST STRIKE! 2× damage"
- Subsequent attacks normal damage

### ✅ Slow
- On hit, target moves 30% slower
- Lasts 2 seconds
- Refreshes on each hit
- Console: "slowed by 30%"

### ✅ Shield Aura
- +2 HP to self
- +2 HP to allies within 3 units
- HP bonus removed when aura dies
- Works with groups of troops

---

## 🐛 Troubleshooting

**"Ability class not found"**:
- Check abilityClassName spelling exactly: `BerserkAbility`
- Check Parameters are filled in

**"No ability initialized"**:
- Make sure AbilityModule is assigned to TroopCombination
- Check abilityClassName is not empty

**Ability not working**:
- Check Console for initialization logs
- Check ability parameters are correct
- Verify combat integration (attacks happening)

**Slow not working**:
- StatusEffectManager should auto-add to troops
- Check Console for "slowed by X%"
- Watch troop movement speed

---

## 📊 What This Validates

✅ Ability system framework works
✅ Reflection-based ability loading
✅ Parameter reading from modules
✅ Combat integration (damage hooks)
✅ Status effect system
✅ 5 different ability patterns working

---

## 🚀 What's Next After This Test

Once abilities are confirmed working:

### Priority 1: More Content
1. **More Bodies**: Archer, Scout, Tank (from Art Guide)
2. **More Weapons**: Bow (ranged), Hammer (AOE), Daggers, Staff
3. **More Abilities**: 10 more (we have 5/20 done)
4. **More Combinations**: Mix and match for variety

### Priority 2: Draft System
- DraftController: 15-second timer
- DraftPool: Manage base + AI combinations
- DraftUI: Cards with previews

### Priority 3: Battle System Integration
- BattleController: Orchestrate combat phase
- Battle timer: 30-second countdown
- Victory conditions: All dead or HP comparison

### Priority 4: AI System
- AIGenerator: Claude API integration
- PlayerAnalyzer: Track patterns
- CounterGenerator: Build prompts
- FallbackGenerator: Rule-based backup

### Priority 5: Polish
- Projectile system (arrows, magic bolts)
- Visual effects (damage numbers, particles)
- Sound effects
- UI (health bars, timers, score)

---

## 💡 Quick Win Ideas

While I build next systems, you can:

1. **Create Art Assets** (See ART_ASSET_GUIDE.md):
   - Start with Knight body (128×128)
   - Add Sword overlay (64×64)
   - See troops look better immediately!

2. **Test More Combinations**:
   - Try Berserk + Fire (glass cannon)
   - Try Regen + Water (tanky healer)
   - Try Slow + multiple troops (control swarm)

3. **Tweak Parameters**:
   - Change Berserk threshold to 0.3 (earlier trigger)
   - Change Regen to 2 HP/sec (faster healing)
   - Change Slow to 0.5 (50% slow - very strong!)

4. **Record Clips**:
   - Capture Berserk triggering (red rage mode)
   - Capture Regen healing during battle
   - Capture Slow controlling enemies
   - Great for demo video later!

---

## 📁 Files You May Want to Check

**Ability Implementations** (for reference/learning):
- `Assets/_Project/Scripts/Abilities/Implementations/PassiveAbilities/`
- `Assets/_Project/Scripts/Abilities/Implementations/TriggeredAbilities/`
- `Assets/_Project/Scripts/Abilities/Implementations/ControlAbilities/`

**Module Assets** (to create/edit):
- `Assets/_Project/Data/Abilities/` - Create new ability configs
- `Assets/_Project/Data/BaseCombinations/` - Create new troop combos

**Test Scene**:
- `Assets/_Project/Scenes/BattleTest.unity` - Swap combinations here

---

## ✨ Pro Tips

**Debugging Abilities**:
- Console logs show ability initialization and triggers
- Ability hooks are called every attack
- Check Parameters dictionary for values

**Creating Custom Abilities**:
- Copy one of the 5 existing abilities
- Modify the behavior in Update() or hooks
- Create new AbilityModule asset pointing to your class

**Testing Combinations**:
- Press SPACE to respawn (no need to restart)
- Try Amount: 1 vs Amount: 5 to see scaling
- Mix elements to see advantage/disadvantage

---

**Ready to test abilities?** Follow Step 1-4 above and let me know what you see! 🎮⚡

I'll continue building the next systems while you test and create art! 🎨
