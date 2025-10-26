# Adaptive Draft Arena - Game Design Document
## Real-Time Modular Arena Battler with AI-Generated Troops

---

## Table of Contents
1. [Game Overview](#game-overview)
2. [Core Game Loop](#core-game-loop)
3. [Modular Design System](#modular-design-system)
4. [Real-Time Combat System](#real-time-combat-system)
5. [Module Categories](#module-categories)
6. [AI Generation System](#ai-generation-system)
7. [Data Structures](#data-structures)
8. [Assets Needed](#assets-needed)
9. [Scope & Timeline](#scope--timeline)

---

## Game Overview

### High Concept
A real-time arena battler (like Clash Royale) where you draft modular troops that spawn and fight autonomously. An AI opponent learns your strategy and generates custom troop combinations by mixing and matching pre-made modules—like LEGO blocks—to counter your tactics.

### Core Innovation
**AI-Generated Modular Troops**: The AI doesn't create entirely new content—it combines pre-existing modules (bodies, weapons, abilities, effects) in novel ways to counter your strategy. Every troop is a unique combination of universal building blocks.

### Victory Condition
**Best of 7 rounds**. First player to win 4 rounds wins the match.

### Key Mechanics
- **Real-Time Combat**: Troops spawn, move freely, and fight in real-time (no turns)
- **30 Second Round Maximum**: Fast battles with HP-based victory if time expires
- **Amount Multiplier**: Draft options spawn ×1, ×2, ×3, or ×5 troops
- **15 Second Draft Timer**: Quick decision-making under pressure
- **Modular Troops**: Every troop = Body + Weapon + Ability + Effect
- **Max 4 Troops Per Side**: Strategic capacity limit

### Target Session Length
6-8 minutes per full match (7 rounds × ~45-50 seconds per round)

---

## Core Game Loop

```
MATCH START
  ↓
ROUND START (Round 1/7)
  ↓
DRAFT PHASE (15 seconds, simultaneous)
  ├─ Player sees 3 modular troop options
  ├─ AI sees 3 modular troop options
  ├─ Each option shows: Body + Weapon + Ability + Effect + Amount
  └─ Pick 1 option (auto-pick random if timeout)
  ↓
SPAWN PHASE (1 second)
  ├─ Reveal both picks
  ├─ Troops spawn on battlefield (amount determines count)
  └─ Show formations
  ↓
REAL-TIME BATTLE PHASE (max 30 seconds)
  ├─ Troops move freely toward enemies
  ├─ Attack when in range (continuous real-time combat)
  ├─ Abilities trigger dynamically
  ├─ Element interactions apply
  ├─ Battle ends when:
  │   • One side has 0 troops (instant victory), OR
  │   • 30 seconds elapsed (winner = highest total HP remaining)
  ↓
ROUND END (2 seconds)
  ├─ Show winner + HP comparison
  ├─ Update score (first to 4 wins)
  ├─ AI analyzes player's troop modules
  └─ AI generates 2 new modular combinations
  ↓
[Repeat until someone reaches 4 wins]
  ↓
MATCH END
  └─ Show winner + match stats
```

### Timing Breakdown
- **Draft**: 15 seconds
- **Spawn**: 1 second
- **Battle**: 0-30 seconds (variable)
- **Round End**: 2 seconds
- **Total Per Round**: ~18-48 seconds
- **Full Match**: ~3-8 minutes

---

## Modular Design System

### Core Concept: LEGO Blocks for Troops

Every troop is composed of **4 independent modules**:

```
TROOP = BODY + WEAPON + ABILITY + EFFECT + AMOUNT
```

**Why Modular?**
- **Infinite Combinations**: 4,800+ possible troops from pre-made parts
- **AI-Friendly**: AI just picks modules, no content generation needed
- **Balanced**: Each module is balanced independently
- **Visual Clarity**: Players see composition at a glance

**Example Combinations**:

| Body | Weapon | Ability | Effect | Amount | Result |
|------|--------|---------|--------|--------|--------|
| Knight | Sword | Shield Aura | Fire | ×1 | Tanky fire knight with defensive buff |
| Archer | Bow | First Strike | Water | ×3 | 3 water archers with bonus first hit |
| Scout | Daggers | Speed Boost | Nature | ×5 | 5 fast nature scouts with rapid attacks |
| Tank | Hammer | Splash Damage | Fire | ×1 | Ultimate frontline with AOE |

---

## Module Categories

### Complete Module Library

All modules are **pre-generated** before the hackathon. AI combines them at runtime.

---

### BODY MODULES (4 types)

Determines: HP, movement speed, attack range, size

#### 1. KNIGHT BODY
- **Base HP**: 8
- **Movement Speed**: 1.5 units/sec (Slow)
- **Attack Range**: 1.5 units (Melee)
- **Size**: Medium
- **Role**: Balanced frontline fighter

#### 2. ARCHER BODY
- **Base HP**: 3
- **Movement Speed**: 2.5 units/sec (Medium)
- **Attack Range**: 5.0 units (Ranged)
- **Size**: Small
- **Role**: Backline damage dealer

#### 3. SCOUT BODY
- **Base HP**: 5
- **Movement Speed**: 4.0 units/sec (Fast)
- **Attack Range**: 1.5 units (Melee)
- **Size**: Small
- **Role**: Mobile flanker

#### 4. TANK BODY
- **Base HP**: 12
- **Movement Speed**: 1.0 units/sec (Very Slow)
- **Attack Range**: 1.5 units (Melee)
- **Size**: Large
- **Role**: Ultimate tank

---

### WEAPON MODULES (5 types)

Determines: Damage, attack speed, attack pattern

#### 1. SWORD
- **Damage**: 3
- **Attack Speed**: 0.8 sec cooldown (Fast)
- **Attack Type**: Single melee strike
- **Special**: None

#### 2. BOW
- **Damage**: 2
- **Attack Speed**: 1.2 sec cooldown (Medium)
- **Attack Type**: Ranged projectile
- **Special**: Travels in straight line

#### 3. HAMMER
- **Damage**: 5
- **Attack Speed**: 2.0 sec cooldown (Slow)
- **Attack Type**: AOE melee slam
- **Special**: Hits all units in 2-unit radius

#### 4. DAGGERS
- **Damage**: 2
- **Attack Speed**: 0.5 sec cooldown (Very Fast)
- **Attack Type**: Dual single-target strikes
- **Special**: Attacks twice per cooldown

#### 5. STAFF
- **Damage**: 4
- **Attack Speed**: 1.5 sec cooldown (Medium-Slow)
- **Attack Type**: Magic projectile
- **Special**: Homing, can't miss

---

### ABILITY MODULES (20 types - Universal)

**CRITICAL**: All abilities are **universal** and work with ANY body/weapon/effect combination.

#### Offensive Abilities (5)

**1. BERSERK**
- **Effect**: +50% damage when below 50% HP
- **Works With**: Any troop type
- **Counter**: High burst damage to finish before berserk triggers

**2. FIRST STRIKE**
- **Effect**: Deal 2× damage on first attack only
- **Works With**: Any troop type
- **Counter**: Tanky units that survive first hit

**3. ARMOR PIERCE**
- **Effect**: Ignore 50% of target's effective HP (treat as ×1.3 damage)
- **Works With**: Any troop type
- **Counter**: High-damage glass cannons

**4. LIFESTEAL**
- **Effect**: Heal for 30% of damage dealt
- **Works With**: Fast attackers benefit most
- **Counter**: Burst damage before healing accumulates

**5. CRITICAL STRIKE**
- **Effect**: 25% chance to deal 2× damage
- **Works With**: Any troop type
- **Counter**: Consistent damage over RNG

#### Defensive Abilities (5)

**6. SHIELD AURA**
- **Effect**: +2 HP to this unit and allies within 3 units
- **Works With**: Best on frontline, benefits allies
- **Counter**: Focus fire, spread units out

**7. DODGE**
- **Effect**: 30% chance to completely avoid incoming attack
- **Works With**: Any troop type
- **Counter**: AOE damage (can't dodge), overwhelming attacks

**8. THORNS**
- **Effect**: Reflect 20% of damage taken back to attacker
- **Works With**: High HP units to survive and reflect more
- **Counter**: Ranged attacks, burst damage

**9. REGENERATION**
- **Effect**: Heal 1 HP per second
- **Works With**: Tanks and high HP units
- **Counter**: High burst damage to kill before regen matters

**10. LAST STAND**
- **Effect**: Survive 1 fatal hit with 1 HP (once per battle)
- **Works With**: Any troop, especially glass cannons
- **Counter**: Multi-hit attacks, DOT effects

#### Utility Abilities (5)

**11. SPEED BOOST**
- **Effect**: +50% movement speed
- **Works With**: Melee units to close distance, scouts
- **Counter**: Ranged kiting, slowing effects

**12. SPLASH DAMAGE**
- **Effect**: Attacks hit nearby enemies for 50% damage (2-unit radius)
- **Works With**: Any attack type
- **Counter**: Spread units out

**13. TAUNT**
- **Effect**: Enemies prioritize attacking this unit
- **Works With**: Tanks with high HP
- **Counter**: Ignore and focus backline if possible

**14. STEALTH**
- **Effect**: Invisible until first attack (enemies ignore)
- **Works With**: Assassins, flankers
- **Counter**: AOE damage, detection

**15. RALLY**
- **Effect**: Allies within 4 units attack 20% faster
- **Works With**: Support units in groups
- **Counter**: Kill rally unit first

#### Control Abilities (5)

**16. SLOW**
- **Effect**: Attacks reduce target movement speed by 30% for 2 sec
- **Works With**: Ranged units to kite melee
- **Counter**: Ranged attackers unaffected by slow

**17. STUN**
- **Effect**: 20% chance on hit to stun target for 1 sec
- **Works With**: Fast attackers for more chances
- **Counter**: High HP to survive stuns

**18. KNOCKBACK**
- **Effect**: Attacks push enemies backward 1 unit
- **Works With**: Frontline to create space
- **Counter**: Heavy units resist knockback

**19. ROOT**
- **Effect**: First attack roots target in place for 2 sec
- **Works With**: Ranged units to lock down melee
- **Counter**: Already-engaged units unaffected

**20. CHAIN LIGHTNING**
- **Effect**: Attacks jump to 1 nearby enemy (50% damage to second target)
- **Works With**: Against grouped enemies
- **Counter**: Spread out units

---

### EFFECT MODULES (3 types - Elements)

Determines: Element, visual effects, damage modifiers

#### 1. FIRE EFFECT
- **Element**: Fire
- **Color**: Red/Orange (#FF6B35)
- **Particles**: Embers floating upward
- **Strong Against**: Nature (×1.5 damage)
- **Weak Against**: Water (×0.75 damage)
- **Visual**: Orange glow aura around troop

#### 2. WATER EFFECT
- **Element**: Water
- **Color**: Blue/Cyan (#00B4D8)
- **Particles**: Water droplets
- **Strong Against**: Fire (×1.5 damage)
- **Weak Against**: Nature (×0.75 damage)
- **Visual**: Blue shimmer aura around troop

#### 3. NATURE EFFECT
- **Element**: Nature
- **Color**: Green (#70E000)
- **Particles**: Leaves spiraling
- **Strong Against**: Water (×1.5 damage)
- **Weak Against**: Fire (×0.75 damage)
- **Visual**: Green leafy aura around troop

**Element Triangle**:
```
    FIRE
     ↓ ×1.5
  NATURE ← WATER
     ×1.5 ↓
```

---

### AMOUNT MULTIPLIER (4 values)

**Balancing Rule**: Total power roughly equal across amounts

| Amount | Stat Multiplier | Ability | Total Power | Use Case |
|--------|----------------|---------|-------------|----------|
| **×1** | 100% | Full | 100% | Elite single unit |
| **×2** | 80% | Full | ~160% | Balanced pair |
| **×3** | 60% | 50% effectiveness | ~180% | Small swarm |
| **×5** | 40% | Disabled | ~200% | Massive swarm |

**Examples**:

**Fire Knight ×1**:
- 8 HP, 3 DMG, Shield Aura (+2 HP to allies)
- Total: 8 HP, 3 DMG, strong ability

**Fire Knight ×2**:
- 6.4 HP each, 2.4 DMG each, Shield Aura (+2 HP each)
- Total: 12.8 HP, 4.8 DMG, full abilities

**Fire Knight ×3**:
- 4.8 HP each, 1.8 DMG each, Shield Aura (+1 HP each)
- Total: 14.4 HP, 5.4 DMG, weakened abilities

**Fire Knight ×5**:
- 3.2 HP each, 1.2 DMG each, no ability
- Total: 16 HP, 6 DMG, overwhelming numbers

**Strategic Implications**:
- **×1**: Hard to kill, strong abilities, vulnerable to swarms
- **×2**: Balanced, good all-around
- **×3**: Swarmy, good against single targets, weak to AOE
- **×5**: Maximum swarm, surrounds enemies, very weak to AOE

---

### The 4 Base Combinations

These are always available in every draft pool:

#### 1. **Fire Knight** (Balanced Frontline)
```
Body: KNIGHT (8 HP, 1.5 speed, melee)
Weapon: SWORD (3 DMG, fast)
Ability: SHIELD_AURA (+2 HP to allies)
Effect: FIRE (vs Nature)
Amount Options: ×1 or ×2
```
**Role**: Tanky frontline with team support

#### 2. **Water Archer** (Backline DPS)
```
Body: ARCHER (3 HP, 2.5 speed, ranged)
Weapon: BOW (2 DMG, projectile)
Ability: FIRST_STRIKE (2× first hit)
Effect: WATER (vs Fire)
Amount Options: ×2 or ×3
```
**Role**: Safe backline damage with strong opener

#### 3. **Nature Scout** (Fast Flanker)
```
Body: SCOUT (5 HP, 4.0 speed, melee)
Weapon: DAGGERS (2 DMG, very fast)
Ability: SPEED_BOOST (+50% move speed)
Effect: NATURE (vs Water)
Amount Options: ×3 or ×5
```
**Role**: Overwhelm with speed and numbers

#### 4. **Fire Tank** (Ultimate Frontline)
```
Body: TANK (12 HP, 1.0 speed, melee)
Weapon: HAMMER (5 DMG, AOE)
Ability: LAST_STAND (survive 1 death)
Effect: FIRE (vs Nature)
Amount Options: ×1 only
```
**Role**: Nearly unkillable frontline with AOE damage

---

## Real-Time Combat System

### Battlefield Layout

```
╔═══════════════════════════════════════════╗
║                                           ║
║  PLAYER SPAWN          AI SPAWN          ║
║  (Left 1/4)            (Right 1/4)       ║
║                                           ║
║    [P][P]               [A][A]            ║
║    [P][P]               [A][A]            ║
║                                           ║
║  ←──── Troops move freely ────→          ║
║                                           ║
║  Battlefield: 20 × 12 units               ║
║  Max 4 troops per side at once            ║
╚═══════════════════════════════════════════╝
```

**Key Rules**:
- Open 2D battlefield (no lanes, no slots)
- Troops spawn randomly within team's spawn zone
- Movement is free in all directions
- Collision detection prevents overlap
- Max 4 troops per side (enforced during spawn)

---

### Movement Behavior

**AI Pathfinding** (all troops follow same logic):
1. **Find Closest Enemy**: Scan for nearest living enemy
2. **Move Toward Target**: Move at movement speed
3. **Stop at Range**: Stop when within attack range
4. **Attack**: Begin attack cycle
5. **Re-target on Death**: If target dies, immediately find new closest enemy

**Movement Speeds** (from Body Module):
- Very Slow: 1.0 units/sec (Tank)
- Slow: 1.5 units/sec (Knight)
- Medium: 2.5 units/sec (Archer)
- Fast: 4.0 units/sec (Scout)
- Modified by abilities (Speed Boost = +50%)

**Pathfinding**:
- Simple straight-line movement toward target
- Basic collision avoidance (steer around allies/enemies)
- No complex pathfinding needed for open battlefield

---

### Attack Behavior

**Attack Cycle**:
```
In Range? → Yes → Wait for cooldown → Play animation → Deal damage → Repeat
            ↓ No
         Move closer
```

**Attack Ranges** (from Body Module):
- Melee: 1.5 units (must be very close)
- Ranged: 5.0 units (can attack from distance)

**Attack Speeds** (from Weapon Module):
- Very Fast: 0.5 sec cooldown (Daggers)
- Fast: 0.8 sec cooldown (Sword)
- Medium: 1.2 sec cooldown (Bow)
- Medium-Slow: 1.5 sec cooldown (Staff)
- Slow: 2.0 sec cooldown (Hammer)

**Projectiles** (for ranged weapons):
- Bow: Arrow projectile (straight line, can miss if target moves)
- Staff: Magic bolt (homing, can't miss)
- Speed: 10 units/sec

---

### Damage Calculation

**Formula**:
```
finalDamage = weaponDamage × elementMultiplier × abilityModifiers
```

**Element Multipliers**:
- Strong advantage: ×1.5 (Fire vs Nature, Water vs Fire, Nature vs Water)
- Neutral: ×1.0 (same element)
- Weak disadvantage: ×0.75 (Fire vs Water, Water vs Nature, Nature vs Fire)

**Ability Modifiers** (examples):
- Berserk (if < 50% HP): ×1.5 damage
- First Strike (first hit only): ×2.0 damage
- Critical Strike (25% chance): ×2.0 damage
- Armor Pierce: treat target HP as 0.75× effective

**Example Calculation**:
```
Scout (Nature) with Daggers attacks Archer (Water) with Berserk active
- Base: 2 damage (Daggers)
- Element: 2 × 1.5 = 3 (Nature vs Water advantage)
- Berserk: 3 × 1.5 = 4.5 (if Scout below 50% HP)
- Final: 4.5 damage to Archer
```

---

### Battle Timer & Victory

**30-Second Maximum Battle**:
- Timer starts when troops spawn
- Counts down from 30.0 seconds
- Displayed prominently

**Victory Conditions** (checked continuously):

**Instant Victory** (battle ends immediately):
- One side reduced to 0 troops → Other side wins

**Timer Expiration** (at 30.0 seconds):
```
Calculate: playerTotalHP = sum of all player troop HP
Calculate: aiTotalHP = sum of all AI troop HP

If playerTotalHP > aiTotalHP:
    Player wins round
Else if aiTotalHP > playerTotalHP:
    AI wins round
Else (exactly tied):
    Player wins (tie-breaker rule)
```

**Example Scenarios**:

*Scenario 1 - Instant Win*:
- 12 sec: Player has 3 troops, AI has 2 troops
- 18 sec: AI's last troop dies
- **Result**: Player wins instantly at 18 seconds

*Scenario 2 - Timer Win*:
- 30 sec: Player has 1 Knight (6 HP) + 1 Archer (2 HP) = 8 total
- 30 sec: AI has 2 Scouts (3 HP each) = 6 total
- **Result**: Player wins by HP (8 > 6)

*Scenario 3 - Exact Tie*:
- 30 sec: Both sides have 10 HP total
- **Result**: Player wins (tie-breaker)

---

### Ability Interactions

#### Passive Abilities (Always Active)

**Shield Aura**:
- Continuously adds +2 HP to allies within 3 units
- HP updates dynamically as allies enter/leave range
- HP bonus lost if aura unit dies

**Regeneration**:
- Heals 1 HP per second continuously
- Stops at max HP
- Continues until unit dies

**Speed Boost**:
- Permanently increases movement speed by 50%
- Applied from spawn

**Thorns**:
- Triggers every time unit takes damage
- Reflects 20% back to attacker instantly

---

#### Triggered Abilities (Activate on Condition)

**First Strike**:
- Activates on first attack only
- Doubles damage on that attack
- Then disabled for rest of battle

**Last Stand**:
- Activates when HP would drop to 0
- Sets HP to 1 instead
- Only works once per battle

**Berserk**:
- Activates when HP drops below 50%
- Increases all damage by 50%
- Remains active until death

**Dodge**:
- 30% chance per incoming attack
- Completely negates damage if triggered

**Critical Strike**:
- 25% chance per attack
- Doubles damage if triggered

---

#### Duration Abilities (Apply Status Effects)

**Slow**:
- Applied on each hit
- Reduces target movement speed by 30%
- Lasts 2 seconds (refreshed on each hit)

**Stun**:
- 20% chance on each hit
- Target frozen for 1 second (can't move or attack)
- Can trigger multiple times

**Root**:
- Applied on first hit only
- Target can't move for 2 seconds (but can still attack)
- After 2 seconds or if source dies, root ends

---

#### Special Attack Abilities

**Chain Lightning**:
- Primary attack hits main target
- Damage jumps to 1 nearest enemy within 3 units
- Second target takes 50% damage

**Splash Damage**:
- Primary attack hits main target
- All enemies within 2 units of target take 50% damage

**Lifesteal**:
- After dealing damage, heal for 30% of damage dealt
- Instant healing, capped at max HP

---

### Amount Balancing Examples

**Knight (8 HP, 3 DMG, 1.5 speed) with Shield Aura**:

**×1**:
- 1 unit: 8 HP, 3 DMG, +2 HP to nearby allies
- Total power: 8 HP, 3 DMG, strong support

**×2**:
- 2 units: 6.4 HP each, 2.4 DMG each, +2 HP to allies
- Total power: 12.8 HP, 4.8 DMG, double support

**×3**:
- 3 units: 4.8 HP each, 1.8 DMG each, +1 HP to allies (50% ability)
- Total power: 14.4 HP, 5.4 DMG, weakened support

**×5**:
- 5 units: 3.2 HP each, 1.2 DMG each, no ability
- Total power: 16 HP, 6 DMG, pure numbers

**Power Comparison**:
- ×1: Best ability, hardest to kill individually, vulnerable to swarms
- ×5: Weakest individually, but 5 bodies overwhelm single targets

---

### Visual Feedback (Juice)

**Movement**:
- Dust trail particles behind moving troops
- Speed lines on fast units (Scout)
- Unit bobbing/breathing animation while idle

**Combat**:
- Attack animation (swing/shoot/cast)
- Hit spark effect on impact
- Screen shake on heavy hits (Hammer)
- Damage numbers float up from target
- Element-colored damage numbers (red/blue/green)

**Projectiles**:
- Arrow: Visible arrow sprite traveling
- Magic bolt: Glowing orb with trail
- Hammer AOE: Ground impact wave effect

**Health**:
- HP bar above each troop
- Bar color: Green (>50%) → Yellow (25-50%) → Red (<25%)
- Flash white when taking damage
- Shake when damaged

**Death**:
- Fade out over 0.5 seconds
- Particle burst (element-colored)
- Corpse remains briefly (faded sprite)

**Abilities**:
- Aura ring for Shield Aura (pulsing blue circle)
- Speed trails for Speed Boost
- Shield icon popup for Last Stand activation
- Lightning chain effect for Chain Lightning
- Slow icon above slowed units

**Timer**:
- Normal: White text (30-11 sec)
- Warning: Yellow text (10-6 sec)
- Urgent: Red text + ticking sound (5-0 sec)

---

## AI Generation System

### When AI Generates Combinations

**Trigger**: After each round ends (starting after Round 1)

**Frequency**: 2 new combinations per round

**Timeline**:
- Round 1 end: AI generates 2 combos (pool now has 6 total: 4 base + 2 AI)
- Round 2 end: AI generates 2 combos (pool now has 8 total)
- Round 3 end: AI generates 2 combos (pool now has 10 total)
- ... and so on
- Round 6 end: AI generates 2 combos (pool now has 16 total)
- Round 7: Draft from full pool (no more generation after match ends)

---

### Player Analysis

After each round, AI analyzes player's last pick:

**Composition Data**:
```json
{
  "lastPick": {
    "body": "SCOUT",
    "weapon": "DAGGERS",
    "ability": "SPEED_BOOST",
    "effect": "NATURE",
    "amount": 3
  },
  
  "matchHistory": {
    "bodies": {"KNIGHT": 2, "SCOUT": 1},
    "weapons": {"SWORD": 2, "DAGGERS": 1},
    "abilities": {"SHIELD_AURA": 2, "SPEED_BOOST": 1},
    "effects": {"FIRE": 2, "NATURE": 1},
    "amounts": {"1": 2, "3": 1}
  },
  
  "patterns": {
    "prefersM elee": true,  // 3/3 picks are melee range
    "prefersDefensive": true,  // 2/3 have defensive abilities
    "fireHeavy": true,  // 2/3 are fire element
    "mixedAmounts": true  // Uses both elite and swarm
  },
  
  "weaknesses": {
    "vulnerableToElement": "WATER",  // Has lots of Fire
    "vulnerableToRanged": true,  // All melee bodies
    "lacksAOE": true,  // No Hammer or Splash Damage
    "slowMovement": true  // Knights are slow
  }
}
```

---

### Counter Generation Prompt

**Sent to Claude API**:

```
You are designing counter troop combinations for a real-time arena battler.

PLAYER'S LAST TROOP:
- Body: SCOUT (5 HP, 4.0 speed, melee range)
- Weapon: DAGGERS (2 DMG, 0.5 sec cooldown)
- Ability: SPEED_BOOST (+50% movement speed)
- Effect: NATURE (strong vs Water, weak vs Fire)
- Amount: ×3 (60% stats, 50% ability effectiveness)

PLAYER PATTERNS ACROSS MATCH:
Bodies Used:
- KNIGHT: 2 times
- SCOUT: 1 time

Weapons Used:
- SWORD: 2 times
- DAGGERS: 1 time

Abilities Used:
- SHIELD_AURA: 2 times
- SPEED_BOOST: 1 time

Effects Used:
- FIRE: 2 times (vulnerable to WATER)
- NATURE: 1 time (vulnerable to FIRE)

Amounts Used:
- ×1: 2 times (prefers elite units)
- ×3: 1 time (occasionally uses swarms)

DETECTED WEAKNESSES:
1. Element: Player has 2/3 FIRE troops → Vulnerable to WATER
2. Range: Player has 3/3 MELEE troops → Vulnerable to RANGED kiting
3. AOE: Player has 0 AOE weapons/abilities → Vulnerable to SWARMS
4. Speed: Player has 2 KNIGHT bodies (slow 1.5 speed) → Vulnerable to fast flankers

AVAILABLE MODULES TO COMBINE:

Bodies:
- KNIGHT: 8 HP, 1.5 speed, 1.5 range (melee), balanced
- ARCHER: 3 HP, 2.5 speed, 5.0 range (ranged), glass cannon
- SCOUT: 5 HP, 4.0 speed, 1.5 range (melee), flanker
- TANK: 12 HP, 1.0 speed, 1.5 range (melee), ultimate tank

Weapons:
- SWORD: 3 DMG, 0.8 sec, single melee
- BOW: 2 DMG, 1.2 sec, ranged projectile
- HAMMER: 5 DMG, 2.0 sec, AOE melee (2 unit radius)
- DAGGERS: 2 DMG, 0.5 sec, dual strikes
- STAFF: 4 DMG, 1.5 sec, homing magic projectile

Abilities (20 total):
OFFENSIVE: Berserk, First Strike, Armor Pierce, Lifesteal, Critical Strike
DEFENSIVE: Shield Aura, Dodge, Thorns, Regeneration, Last Stand
UTILITY: Speed Boost, Splash Damage, Taunt, Stealth, Rally
CONTROL: Slow, Stun, Knockback, Root, Chain Lightning

Effects:
- FIRE: Strong vs NATURE, weak vs WATER
- WATER: Strong vs FIRE, weak vs NATURE
- NATURE: Strong vs WATER, weak vs FIRE

Amounts:
- ×1: Elite (100% stats, full ability)
- ×2: Pair (80% stats, full ability)
- ×3: Swarm (60% stats, 50% ability)
- ×5: Mass (40% stats, no ability)

DESIGN 2 COUNTER COMBINATIONS:

COMBINATION 1: Direct counter to their last troop
COMBINATION 2: Exploit their overall strategy weakness

RULES:
1. Use ELEMENT ADVANTAGE (player has Fire/Nature → use Water)
2. Exploit RANGE WEAKNESS (player is all melee → use ranged)
3. Counter AMOUNT appropriately (×3 swarm → ×1 elite with AOE OR ×5 overwhelming swarm)
4. Abilities should directly counter their patterns
5. Balance total power appropriately

RESPOND WITH JSON ONLY (no explanation):
[
  {
    "body": "ARCHER",
    "weapon": "BOW",
    "ability": "SLOW",
    "effect": "WATER",
    "amount": 2,
    "reasoning": "Ranged Water to kite and counter Fire, Slow to prevent closing distance"
  },
  {
    "body": "KNIGHT",
    "weapon": "HAMMER",
    "ability": "SPLASH_DAMAGE",
    "effect": "WATER",
    "amount": 1,
    "reasoning": "Elite with double AOE (Hammer + Splash) to devastate ×3 swarms"
  }
]
```

---

### AI Response Parsing

**Expected Response**:
```json
[
  {
    "body": "ARCHER",
    "weapon": "BOW",
    "ability": "SLOW",
    "effect": "WATER",
    "amount": 2,
    "reasoning": "..."
  },
  {
    "body": "TANK",
    "weapon": "STAFF",
    "ability": "REGENERATION",
    "effect": "WATER",
    "amount": 1,
    "reasoning": "..."
  }
]
```

**Validation**:
1. Check all fields exist
2. Validate module IDs exist in library
3. Validate amount is 1, 2, 3, or 5
4. If invalid, fall back to random combination

**Create TroopCombination Objects**:
```
combo1 = new TroopCombination(
    body: GetBodyModule("ARCHER"),
    weapon: GetWeaponModule("BOW"),
    ability: GetAbilityModule("SLOW"),
    effect: GetEffectModule("WATER"),
    amount: 2,
    isAIGenerated: true,
    generationRound: currentRound
)
```

**Add to Pool**:
- Add both combos to aiGeneratedCombinations list
- Next draft will randomly select from basePool + aiGeneratedCombinations

---

### Example AI Adaptation

**Round 1**:
- **Player Picks**: Fire Knight ×1 (Knight + Sword + Shield Aura + Fire)
- **AI Picks**: Water Archer ×2 (from base pool)
- **Battle**: Player's tank knight wins against fragile archers
- **Player Wins Round 1** (Score: 1-0)

**Round 1 Analysis**:
- Player used: Melee, Tank, Fire, Elite (×1)
- Weaknesses: Slow, vulnerable to swarms, weak vs Water

**Round 2 AI Generation**:
- **Combo 1**: Nature Scout ×5 (Scout + Daggers + Speed Boost + Nature) - Fast swarm to overwhelm
- **Combo 2**: Water Archer ×3 (Archer + Bow + Slow + Water) - Ranged kiting with control

**Round 2**:
- **Player Picks**: Nature Scout ×3 (adapting, trying swarm)
- **AI Picks**: Fire Tank ×1 (from base pool) - Counters swarm with AOE Hammer
- **Battle**: Fire Tank's Hammer AOE destroys Nature Scout swarm
- **AI Wins Round 2** (Score: 1-1)

**Round 2 Analysis**:
- Player adapted to swarm strategy
- Still lacks ranged
- Now using Nature (weak vs Fire)

**Round 3 AI Generation**:
- **Combo 1**: Fire Archer ×2 (Archer + Staff + Chain Lightning + Fire) - Ranged with multi-target
- **Combo 2**: Fire Tank ×1 (Tank + Hammer + Taunt + Fire) - Anti-swarm specialist

**Round 3**:
- Player sees AI's counters evolving
- Must adapt or keep losing
- Draft now has 10 options (4 base + 6 AI-generated)

... and so on for 7 rounds total.

---

## Data Structures

### TroopCombination (Draft Option)
```
TroopCombination:
  - body: BodyModule (reference)
  - weapon: WeaponModule (reference)
  - ability: AbilityModule (reference)
  - effect: EffectModule (reference)
  - amount: int (1, 2, 3, or 5)
  - isAIGenerated: bool
  - generationRound: int
  - counterReasoning: string
```

### BodyModule
```
BodyModule:
  - id: string ("KNIGHT", "ARCHER", "SCOUT", "TANK")
  - displayName: string
  - baseHP: int
  - movementSpeed: float
  - attackRange: float
  - size: float
  - spriteAsset: Sprite (reference to body sprite)
```

### WeaponModule
```
WeaponModule:
  - id: string ("SWORD", "BOW", "HAMMER", "DAGGERS", "STAFF")
  - displayName: string
  - baseDamage: int
  - attackCooldown: float
  - attackType: enum (SINGLE, AOE, PROJECTILE, HOMING)
  - aoeRadius: float (if AOE)
  - spriteAsset: Sprite
  - projectileSprite: Sprite (if projectile)
  - projectileSpeed: float (if projectile)
```

### AbilityModule
```
AbilityModule:
  - id: string ("BERSERK", "SHIELD_AURA", etc.)
  - displayName: string
  - category: enum (OFFENSIVE, DEFENSIVE, UTILITY, CONTROL)
  - description: string
  - parameters: Dictionary<string, float>
    Examples:
      Berserk: {damageBonus: 0.5, hpThreshold: 0.5}
      Shield Aura: {hpBonus: 2, radius: 3}
      Slow: {speedReduction: 0.3, duration: 2}
  - particleEffectPrefab: GameObject (reference)
  - iconSprite: Sprite
```

### EffectModule
```
EffectModule:
  - id: string ("FIRE", "WATER", "NATURE")
  - displayName: string
  - color: Color
  - strongVs: string (element this beats)
  - weakVs: string (element this loses to)
  - auraParticleSystem: GameObject (reference)
  - hitEffectPrefab: GameObject (reference)
```

### Troop (Runtime Instance)
```
Troop:
  // Module references (composition)
  - body: BodyModule
  - weapon: WeaponModule
  - ability: AbilityModule
  - effect: EffectModule
  - baseAmount: int (from draft)
  
  // Current stats (modified by amount multiplier)
  - currentHP: float
  - maxHP: float
  - movementSpeed: float
  - attackDamage: int
  - attackRange: float
  - attackCooldown: float
  
  // Combat state
  - position: Vector2
  - velocity: Vector2
  - currentTarget: Troop (reference)
  - attackTimer: float (countdown to next attack)
  - isAlive: bool
  - team: enum (PLAYER, AI)
  
  // Status effects
  - statusEffects: List<StatusEffect>
    StatusEffect: {type, duration, value}
  
  // Ability-specific state
  - abilityState: Dictionary<string, object>
    Examples:
      First Strike: {hasTriggered: false}
      Last Stand: {hasTriggered: false}
      Berserk: {isActive: false}
  
  // Visual references
  - spriteRenderer: SpriteRenderer
  - healthBar: HealthBar
  - auraEffect: ParticleSystem
```

### MatchState
```
MatchState:
  // Round tracking
  - currentRound: int (1-7)
  - playerWins: int
  - aiWins: int
  - phase: enum (DRAFT, SPAWN, BATTLE, ROUND_END, MATCH_END)
  
  // Draft pools
  - baseCombinations: List<TroopCombination> (4 fixed)
  - aiGeneratedCombinations: List<TroopCombination> (0-12)
  
  // Current round state
  - playerDraftOptions: List<TroopCombination> (3 options)
  - aiDraftOptions: List<TroopCombination> (3 options)
  - playerSelectedCombo: TroopCombination
  - aiSelectedCombo: TroopCombination
  
  // Active troops
  - playerTroops: List<Troop> (max 4)
  - aiTroops: List<Troop> (max 4)
  
  // Timers
  - draftTimer: float (15.0 counting down)
  - battleTimer: float (30.0 counting down)
  
  // Analysis
  - playerAnalysis: PlayerAnalysis
  
  // History
  - roundHistory: List<RoundResult>
```

### PlayerAnalysis
```
PlayerAnalysis:
  - lastPick: TroopCombination
  
  - matchHistory: {
      bodies: Dictionary<string, int>,
      weapons: Dictionary<string, int>,
      abilities: Dictionary<string, int>,
      effects: Dictionary<string, int>,
      amounts: Dictionary<int, int>
    }
  
  - patterns: {
      prefersElement: string,
      prefersRanged: bool,
      prefersMelee: bool,
      prefersSwarm: bool,
      prefersElite: bool,
      avgAmount: float
    }
  
  - weaknesses: {
      vulnerableToElement: string,
      vulnerableToRange: string,
      lacksAOE: bool,
      lacksControl: bool,
      slowMovement: bool
    }
```

---

## Assets Needed

### Sprite Modules (Pre-Generate All)

All sprites in **cartoon style**, simple and colorful, optimized for quick generation.

#### Body Sprites (4 bodies × 3 elements = 12 sprites)
- `knight_body_fire.png` (128×128px)
- `knight_body_water.png`
- `knight_body_nature.png`
- `archer_body_fire.png`
- `archer_body_water.png`
- `archer_body_nature.png`
- `scout_body_fire.png`
- `scout_body_water.png`
- `scout_body_nature.png`
- `tank_body_fire.png`
- `tank_body_water.png`
- `tank_body_nature.png`

**Design Notes**:
- Each body has distinct silhouette
- Element shows in color tint and small effect
- Front-facing pose for clarity

#### Weapon Overlays (5 weapons × 1 neutral = 5 sprites)
- `sword_overlay.png` (64×64px, held in hand)
- `bow_overlay.png`
- `hammer_overlay.png`
- `daggers_overlay.png`
- `staff_overlay.png`

**Design Notes**:
- Positioned to align with body sprites
- Transparent background
- Element color applied at runtime

#### Ability Icons (20 abilities)
- `icon_berserk.png` (32×32px)
- `icon_first_strike.png`
- `icon_armor_pierce.png`
- ... (all 20 abilities)

**Design Notes**:
- Simple, recognizable symbols
- Displayed on draft cards

#### Element Aura Effects (3 particle systems)
- `aura_fire.prefab` (particle system, orange embers)
- `aura_water.prefab` (particle system, blue droplets)
- `aura_nature.prefab` (particle system, green leaves)

---

### UI Sprites

#### Draft Card Frame
- `card_frame.png` (256×384px)
- `card_frame_selected.png` (glowing version)

#### Icons
- `icon_hp.png` (heart icon, 32×32px)
- `icon_damage.png` (sword icon)
- `icon_speed.png` (lightning icon)
- `icon_range_melee.png` (crossed swords)
- `icon_range_ranged.png` (bow and arrow)

#### Battlefield
- `battlefield_bg.png` (1920×1080px, simple grass/stone)
- `spawn_zone_indicator.png` (semi-transparent overlay)

---

### VFX Sprites/Prefabs

#### Combat Effects
- `hit_spark.prefab` (white flash particle burst)
- `slash_trail.png` (sword swing trail, 128×32px)
- `arrow_projectile.png` (32×8px)
- `magic_bolt.png` (32×32px glowing orb)
- `explosion.prefab` (hammer impact, 8-frame animation)

#### Damage Numbers
- Floating text prefab with color coding:
  - Normal: White
  - Element advantage: Bright element color
  - Critical: Yellow + bigger

#### Ability Effects
- `shield_aura_ring.prefab` (pulsing blue circle, 128×128px)
- `speed_lines.prefab` (motion blur trails)
- `stun_stars.prefab` (stars spinning above head)
- `slow_icon.png` (displayed above slowed units)

---

### Audio Assets

#### Music
- `menu_music.mp3` (calm, looping)
- `battle_music.mp3` (upbeat, energetic, looping)
- `victory_sting.mp3` (5 sec, triumphant)
- `defeat_sting.mp3` (5 sec, sad)

#### SFX - Draft Phase
- `card_appear.wav` (whoosh)
- `card_select.wav` (click)
- `card_hover.wav` (subtle bleep)
- `timer_tick.wav` (last 5 seconds)
- `timer_expire.wav` (buzzer)

#### SFX - Spawn
- `troop_spawn.wav` (materialize sound)

#### SFX - Combat
- `sword_swing.wav`
- `bow_shoot.wav`
- `arrow_hit.wav`
- `hammer_slam.wav`
- `dagger_stab.wav`
- `staff_cast.wav`
- `magic_impact.wav`
- `hit_impact_light.wav`
- `hit_impact_heavy.wav`
- `troop_hurt.wav`
- `troop_death.wav`

#### SFX - Abilities
- `shield_aura.wav` (gentle hum)
- `speed_boost.wav` (whoosh)
- `stun_proc.wav` (zap)
- `berserk_activate.wav` (roar)

#### SFX - UI
- `round_start.wav` (gong)
- `round_end.wav` (bell)
- `victory.wav` (fanfare)
- `ui_button.wav` (click)

---

### Fonts
- **Main UI**: Bold, readable sans-serif (e.g., "Bangers" from Google Fonts)
- **Damage Numbers**: Impact-style font
- **Card Text**: Clean legible (e.g., "Roboto")

---

### Total Asset Count Summary
- **Sprites**: ~40 sprites
- **Particle Systems**: ~15 prefabs
- **Audio**: ~30 files
- **Fonts**: 3 fonts

**Generation Strategy**:
1. Use AI image generation for body sprites (DALL-E, Midjourney)
2. Simple geometric shapes for weapons (can draw in Photoshop/Figma)
3. Unity particle systems for effects (built-in)
4. Free SFX from freesound.org or generate with AI
5. Free fonts from Google Fonts

---

## Scope & Timeline

### Must-Have Features (P0 - Core Loop)

Essential for functional demo:

- ✅ Match flow state machine (best of 7)
- ✅ Draft phase (15 sec, pick 1 of 3)
- ✅ Real-time movement and combat
- ✅ 30-second battle timer with HP-based victory
- ✅ Max 4 troops per side enforcement
- ✅ Modular troop system (4 module types)
- ✅ 4 base combinations always in pool
- ✅ Element triangle (Fire/Water/Nature with ×1.5/×0.75 multipliers)
- ✅ Amount multiplier (×1/×2/×3/×5 with stat scaling)
- ✅ AI opponent draft selection (basic logic)
- ✅ AI combination generation via Claude API (2 combos/round)
- ✅ Player analysis system
- ✅ Basic UI (draft screen with cards, battlefield with HP bars, score display)
- ✅ Victory/defeat conditions

**Time Estimate**: 24-28 hours

---

### Should-Have Features (P1 - Polish & Juice)

Makes it feel good:

- ✅ Movement animations (walking, idle)
- ✅ Attack animations (weapon swings, projectiles)
- ✅ Hit effects and damage numbers
- ✅ Particle effects (element auras, impacts, deaths)
- ✅ Sound effects (all major actions)
- ✅ Background music (2 tracks)
- ✅ Health bars above troops
- ✅ Timer countdown with color changes
- ✅ Draft card hover/select feedback
- ✅ Screen shake on heavy hits
- ✅ Smooth transitions between phases
- ✅ Ability visual indicators (auras, status icons)

**Time Estimate**: 10-14 hours

---

### Nice-to-Have Features (P2 - Stretch Goals)

If time permits:

- ⭐ More sophisticated AI draft logic (not just random from pool)
- ⭐ Ability trigger animations (unique VFX per ability)
- ⭐ Troop formation preview during draft
- ⭐ Combo display (show which abilities activated)
- ⭐ Match statistics screen (damage dealt, abilities used, etc.)
- ⭐ Tutorial/How to Play overlay
- ⭐ Settings menu (volume controls)
- ⭐ More complex pathfinding (avoid allies better)
- ⭐ Additional universal abilities (expand from 20 to 30)

**Time Estimate**: 4-8 hours

---

### Development Timeline (48 hours)

#### **Day 1 (24 hours)**

**Hours 0-3: Project Setup & Core Framework** (3h)
- Create Unity project (2D, desktop)
- Set up folder structure
- Create scenes (MainMenu, Game)
- GameManager state machine skeleton
- MatchState data structures

**Hours 3-8: Module System** (5h)
- Define all 4 module types (ScriptableObjects)
- Create 4 base combinations
- CardFactory to combine modules
- Visual composition system (layer body + weapon sprites)
- Generate/import body sprites (12 sprites)
- Create weapon overlay sprites (5 sprites)

**Hours 8-14: Draft System** (6h)
- Draft UI layout
- Card display prefab (shows modules + amount)
- Draft pool management (base + AI-generated)
- Selection logic (player + AI)
- 15-second timer
- Auto-pick on timeout

**Hours 14-20: Real-Time Combat Core** (6h)
- Battlefield setup (2D space)
- Troop spawning (respect max 4, apply amount multiplier)
- Movement AI (find closest enemy, path toward)
- Basic attack cycle (cooldown, range check, damage)
- Element damage calculation
- Death and respawn logic
- HP bars above troops

**Hours 20-24: Battle Timer & Victory** (4h)
- 30-second countdown timer
- Instant victory on elimination
- HP total comparison at timeout
- Round end logic
- Score tracking (first to 4 wins)

---

#### **Day 2 (24 hours)**

**Hours 24-30: AI Generation System** (6h)
- Player analysis after each round
- Claude API integration
- Prompt engineering (test and refine)
- JSON parsing and validation
- Add generated combos to pool
- Mock fallback for testing

**Hours 30-36: Abilities System** (6h)
- Implement all 20 universal abilities
- Passive abilities (Shield Aura, Regen, Speed Boost, etc.)
- Triggered abilities (First Strike, Last Stand, Berserk, etc.)
- Control abilities (Slow, Stun, Root, etc.)
- Special attacks (Chain Lightning, Splash, Lifesteal)
- Ability state tracking per troop
- Visual indicators for active abilities

**Hours 36-42: Polish & Juice** (6h)
- Attack animations (simple swing/shoot)
- Hit spark effects
- Damage numbers (floating text)
- Particle systems (element auras, impacts, deaths)
- Sound effects (attacks, hits, UI)
- Background music
- Screen shake on heavy hits
- Draft card hover/select feedback

**Hours 42-46: Testing & Balance** (4h)
- Playtest full matches
- Balance module stats
- Tune AI generation prompts
- Fix bugs
- Performance optimization
- Ensure all abilities work correctly

**Hours 46-48: Video & Submission** (2h)
- Record gameplay footage
- Capture AI generation moment (show prompt/response in dev console)
- Edit demo video highlighting:
  - Real-time combat
  - Modular combinations
  - AI generating counters
  - Element interactions
  - Amount multipliers
- Upload to YouTube
- Submit on Junction platform
- **Remove API keys before submission**
- Build final executable

---

### Risk Mitigation

**Risk 1: Claude API slow or fails**
- **Mitigation**: Generate async with "AI thinking..." animation
- **Fallback**: Mock generator (random counters based on simple rules)

**Risk 2: Real-time combat too complex**
- **Mitigation**: Start with simple nearest-target movement
- **Fallback**: Reduce to 3 troops per side, simplify abilities

**Risk 3: Too many abilities to implement**
- **Mitigation**: Implement 10 core abilities first, add more if time
- **Fallback**: Ship with 10 abilities instead of 20

**Risk 4: Sprite generation takes too long**
- **Mitigation**: Use simple shapes, focus on body sprites only
- **Fallback**: Use colored geometric shapes (circles, squares) with labels

**Risk 5: Running out of time**
- **Mitigation**: Strict priority system (P0 → P1 → P2)
- **Fallback**: Submit working P0 features, minimal polish

---

## Success Metrics

### Demo Must Show:

1. ✅ **Full Match Flow**: At least 3 complete rounds (draft → battle → AI generation)
2. ✅ **Real-Time Combat**: Troops moving and fighting freely on battlefield
3. ✅ **Modular Combinations**: Clear visual of modules being combined
4. ✅ **AI Adaptation**: Show AI analyzing player, then generating counters (dev console visible)
5. ✅ **Generated Counters Work**: AI's generated troops clearly counter player's strategy
6. ✅ **Element Triangle**: Demonstrate ×1.5 damage advantage
7. ✅ **Amount Multipliers**: Show ×1 elite vs ×5 swarm difference
8. ✅ **30-Second Timer**: Show both instant victory and timer expiration victory
9. ✅ **Abilities Working**: At least 5 different abilities triggering visibly

### Judging Criteria Focus:

**Innovation** (35%):
- "Couldn't exist without AI" → Emphasize modular generation
- Show AI learning and adapting across rounds
- Highlight 4,800 possible combinations

**Technical Excellence** (25%):
- Real-time combat system
- Modular architecture
- Claude API integration
- Clean code structure

**Polish & UX** (20%):
- Juice (particles, sounds, screen shake)
- Clear visual feedback
- Intuitive draft UI
- Smooth gameplay flow

**Completeness** (20%):
- Full game loop works
- All core features functional
- No game-breaking bugs
- Proper win/loss conditions

---

## Implementation Notes

### Module System Benefits

**For Development**:
- Parallel work (bodies, weapons, abilities can be done separately)
- Easy to balance (tweak one module affects all combos using it)
- Easy to extend (add new module = instant new combos)

**For Demo**:
- Easy to explain ("LEGO blocks for troops")
- Visually clear what AI is doing
- Impressive combinatorial explosion (4,800 combos!)

**For AI**:
- Simple JSON format
- No sprite generation needed at runtime
- Easy validation (module exists or doesn't)

---

### Critical Path

To ensure demo works, implement in this order:

1. **Module system** (can't progress without this)
2. **Draft system** (need to pick modules)
3. **Basic combat** (movement + attacking)
4. **AI generation** (core innovation)
5. **Timer & victory** (makes it a game)
6. **Abilities** (adds depth)
7. **Polish** (makes it impressive)

---

## Next Steps

1. ✅ Review this design doc
2. ✅ Get clarification on any unclear points
3. ✅ Hand off to Claude Code for implementation
4. → Start with module system (ScriptableObjects)
5. → Set up GameManager state machine
6. → Build draft UI
7. → Implement real-time combat
8. → Integrate Claude API
9. → Add abilities and polish
10. → Test, record, submit!

---

**Good luck with your hackathon! 🎮🚀**

*Remember: Start with P0 features, get them working, THEN add polish. A working simple game beats a broken complex game!*
