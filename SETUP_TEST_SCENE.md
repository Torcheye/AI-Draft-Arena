# Setup Test Scene - Step by Step Instructions

Follow these steps in Unity Editor to create a minimal combat test scene.

## Step 1: Create GameConfig Asset

1. In Unity, right-click in `Assets/_Project/Data/`
2. Select `Create → AdaptiveDraftArena → Config → GameConfig`
3. Name it `GameConfig`
4. **Leave all default values as-is** (they're already configured)

---

## Step 2: Create Module Assets (Minimal Set)

### Create Body Module - Knight
1. Right-click in `Assets/_Project/Data/Bodies/`
2. `Create → AdaptiveDraftArena → Modules → Body`
3. Name it `Body_Knight`
4. Configure in Inspector:
   - **moduleId**: `KNIGHT`
   - **displayName**: `Knight Body`
   - **baseHP**: `8`
   - **movementSpeed**: `1.5`
   - **attackRange**: `1.5`
   - **size**: `1`
   - **role**: `Tank`
   - ⚠️ **bodySprite**: Leave empty for now (we'll add later)
   - **weaponAnchorPoint**: `(0, 0)`

### Create Weapon Module - Sword
1. Right-click in `Assets/_Project/Data/Weapons/`
2. `Create → AdaptiveDraftArena → Modules → Weapon`
3. Name it `Weapon_Sword`
4. Configure:
   - **moduleId**: `SWORD`
   - **displayName**: `Sword Weapon`
   - **baseDamage**: `3`
   - **attackCooldown**: `0.8`
   - **attackType**: `Melee`
   - ⚠️ **weaponSprite**: Leave empty for now

### Create Effect Module - Fire
1. Right-click in `Assets/_Project/Data/Effects/`
2. `Create → AdaptiveDraftArena → Modules → Effect`
3. Name it `Effect_Fire`
4. Configure:
   - **moduleId**: `FIRE`
   - **displayName**: `Fire`
   - **elementType**: `Fire`
   - **tintColor**: Click color picker → Choose red/orange (e.g., `#FF6B35`)
   - **strongVsElement**: `NATURE`
   - **weakVsElement**: `WATER`
   - **advantageMultiplier**: `1.5`
   - **disadvantageMultiplier**: `0.75`

### Create Effect Module - Water (for enemy)
1. Repeat above but name it `Effect_Water`
2. Configure:
   - **moduleId**: `WATER`
   - **displayName**: `Water`
   - **elementType**: `Water`
   - **tintColor**: Blue (e.g., `#00B4D8`)
   - **strongVsElement**: `FIRE`
   - **weakVsElement**: `NATURE`

### Create Ability Module - Berserk
1. Right-click in `Assets/_Project/Data/Abilities/`
2. `Create → AdaptiveDraftArena → Modules → Ability`
3. Name it `Ability_Berserk`
4. Configure:
   - **moduleId**: `BERSERK`
   - **displayName**: `Berserk`
   - **category**: `Offensive`
   - **trigger**: `Conditional`
   - **abilityClassName**: `BerserkAbility` (we'll implement later)
   - **Parameters**: (leave empty for now - not critical for basic test)

### Create Simple Ability - None (placeholder)
1. Create another ability: `Ability_None`
2. Configure:
   - **moduleId**: `NONE`
   - **displayName**: `No Ability`
   - **category**: `Utility`
   - **trigger**: `Passive`
   - **abilityClassName**: Leave empty

---

## Step 3: Create TroopCombination Assets

### Create Player Combination - Fire Knight
1. Right-click in `Assets/_Project/Data/BaseCombinations/`
2. `Create → AdaptiveDraftArena → Modules → TroopCombination`
3. Name it `Combo_FireKnight`
4. Configure:
   - **body**: Drag `Body_Knight` here
   - **weapon**: Drag `Weapon_Sword` here
   - **ability**: Drag `Ability_None` here
   - **effect**: Drag `Effect_Fire` here
   - **amount**: `2` (will spawn 2 troops)

### Create AI Combination - Water Knight
1. Create another: `Combo_WaterKnight`
2. Configure:
   - **body**: Drag `Body_Knight` here
   - **weapon**: Drag `Weapon_Sword` here
   - **ability**: Drag `Ability_None` here
   - **effect**: Drag `Effect_Water` here
   - **amount**: `2` (will spawn 2 troops)

---

## Step 4: Create Placeholder Sprites

Since we don't have sprites yet, we'll use Unity's built-in sprites:

1. Right-click in `Assets/_Project/Sprites/Bodies/`
2. `Create → Sprites → Square` (if available)
3. If not available, we'll assign sprites to renderers directly in the prefab

**OR** Use this quick method:
1. Create a simple white square in Paint/Photoshop (32x32 pixels)
2. Save as `knight_placeholder.png`
3. Drag into `Assets/_Project/Sprites/Bodies/`
4. Set **Texture Type** to `Sprite (2D and UI)`

---

## Step 5: Create TroopBase Prefab

1. In Hierarchy, right-click → `Create Empty`
2. Name it `TroopBase`
3. Add Components (click `Add Component` in Inspector):
   - `TroopController` (your script)
   - `Rigidbody2D`
   - `Circle Collider 2D`
   - `Sprite Renderer` (for the body)

4. Configure **Rigidbody2D**:
   - **Body Type**: `Dynamic`
   - **Gravity Scale**: `0`
   - **Constraints**: Check `Freeze Rotation Z`

5. Configure **Circle Collider 2D**:
   - **Radius**: `0.5`

6. Configure **Sprite Renderer**:
   - **Sprite**: Use the white square or leave empty (TroopVisuals will set it)
   - **Color**: White
   - **Sorting Layer**: Default

7. **Drag TroopBase from Hierarchy to** `Assets/_Project/Prefabs/Troops/`
8. Delete TroopBase from Hierarchy (we just needed to create the prefab)

---

## Step 6: Setup Test Scene

### Create Battle Test Scene
1. `File → New Scene`
2. Delete default camera and light if you want
3. `File → Save As...`
4. Save to `Assets/_Project/Scenes/BattleTest.unity`

### Add Main Camera (if deleted)
1. `GameObject → Camera`
2. Set **Position**: `(0, 0, -10)`
3. Set **Size**: `10` (for orthographic)
4. Set **Background**: Dark color for visibility

### Create GameManager GameObject
1. In Hierarchy: Right-click → `Create Empty`
2. Name it `GameManager`
3. Add Component → `GameManager` (your script)
4. In Inspector:
   - **Config**: Drag the `GameConfig` asset you created

### Create BattleTest GameObject
1. In Hierarchy: Right-click → `Create Empty`
2. Name it `BattleTest`
3. Add Component → `BattleTestController`
4. Add Component → `TroopSpawner`

5. Configure **BattleTestController**:
   - **Player Combination**: Drag `Combo_FireKnight`
   - **AI Combination**: Drag `Combo_WaterKnight`
   - **Troop Spawner**: Drag the TroopSpawner component (same GameObject)
   - **Spawn On Start**: ✓ Checked
   - **Respawn Key**: `Space`

6. Configure **TroopSpawner**:
   - **Troop Prefab**: Drag `TroopBase` prefab from `Assets/_Project/Prefabs/Troops/`

---

## Step 7: Set up Layers (Important!)

1. `Edit → Project Settings → Tags and Layers`
2. Add these layers:
   - Layer 8: `PlayerTroops`
   - Layer 9: `AITroops`
   - Layer 10: `Projectiles`

3. `Edit → Project Settings → Physics 2D`
4. Scroll to **Layer Collision Matrix**
5. Configure:
   - `PlayerTroops` should collide with: `AITroops`
   - `AITroops` should collide with: `PlayerTroops`
   - Uncheck collisions within same team

---

## Step 8: Quick Visual Setup (Optional but Recommended)

To see troops better:

1. Open `TroopBase` prefab
2. Select the **Sprite Renderer** component
3. Set **Color**:
   - For testing, use bright colors
   - Or create two prefab variants (one red, one blue) for player/AI

---

## Step 9: TEST IT!

### Play the Scene:
1. Make sure `BattleTest` scene is open
2. Press **Play** ▶️

### What you should see:
- Console logs: "Spawned 2 troops: Fire Knight ×2 for Player"
- Console logs: "Spawned 2 troops: Water Knight ×2 for AI"
- 4 sprites on screen (2 on left, 2 on right)
- Troops moving toward each other
- Console logs of attacks: "TroopName melee attacks TroopName for X damage"
- Troops dying when HP reaches 0
- Top-left UI showing troop counts

### Press SPACE:
- Cleans up dead troops
- Respawns fresh troops for another battle

### Debug Tips:
- If you don't see anything, check Camera position
- If troops don't spawn, check Console for errors
- If troops don't move, check that TroopMovement component is attached
- If no attacks happen, check that weapon attackType is set to `Melee`

---

## Expected Behavior (Minimal Viable Test):

✅ 2 Fire Knights spawn on left (red-tinted)
✅ 2 Water Knights spawn on right (blue-tinted)
✅ Troops find closest enemy (TargetingSystem)
✅ Troops move toward enemies (TroopMovement)
✅ Troops stop when in range (1.5 units)
✅ Troops attack every 0.8 seconds (Sword cooldown)
✅ Damage is applied (3 base × 0.8 multiplier = 2.4 per hit)
✅ Water has advantage over Fire (×1.5 damage)
✅ Troops die when HP reaches 0
✅ Winner is determined (whoever survives)

---

## Troubleshooting:

**"NullReferenceException"**:
- Check GameManager has GameConfig assigned
- Check BattleTestController has all fields assigned

**"Troops not moving"**:
- Check Rigidbody2D has Gravity Scale = 0
- Check TroopMovement component exists

**"No attacks happening"**:
- Check attackRange (1.5) vs distance between troops
- Check attackCooldown is reasonable (0.8)
- Check Console for "melee attacks" logs

**"Can't see troops"**:
- Check Camera position (-10 on Z)
- Check Sprite Renderer has a sprite or color
- Check spawn zones in GameConfig are visible in camera view

---

## Once It Works:

You should see:
1. Troops spawn
2. Troops rush toward each other
3. Combat logs in Console
4. Health decreasing
5. Troops dying
6. A winner emerges

**This confirms the core combat system is working!** 🎉

Then we can build on this foundation:
- Add abilities
- Add projectiles
- Add visual effects
- Add proper UI
- Integrate with draft system

---

**Need Help?** Check Console for error messages and refer to the component configurations above.
