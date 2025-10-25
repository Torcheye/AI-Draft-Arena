# Art Asset Guide - Adaptive Draft Arena

Complete guide for creating all art assets for the game.

---

## 📐 Technical Specifications

### General Settings
- **Canvas Size**: 128×128px (for bodies), 64×64px (for weapons)
- **Resolution**: 72 DPI minimum, 144 DPI recommended
- **Color Mode**: RGB
- **Background**: Transparent (PNG with alpha channel)
- **Style**: Cartoon/stylized 2D (simple, readable, colorful)
- **View**: Front-facing, slight 3/4 view acceptable
- **Pivot Point**: Center-bottom for bodies (feet position)

### Unity Import Settings (After Creating)
1. **Texture Type**: Sprite (2D and UI)
2. **Sprite Mode**: Single
3. **Pixels Per Unit**: 32 (so 128px sprite = 4 units in-game)
4. **Filter Mode**: Point (for pixel art) or Bilinear (for smooth art)
5. **Compression**: None (for quality) or Normal Quality

---

## 🎨 Art Assets Needed

### Priority 1 - Minimal Viable Set (For Next Test)

#### **4 Body Types** (128×128px each)

**1. Knight Body**
- **Silhouette**: Medium humanoid, armored
- **Height**: ~100px (leaving top/bottom margins)
- **Details**:
  - Plate armor chest, helmet
  - Shield on one arm (or back)
  - Sturdy stance
- **Variants Needed**:
  - `knight_body.png` - Neutral gray/white (we'll tint in Unity)
- **Weapon Anchor**: Right hand area (mark in notes)
- **Reference**: Classic medieval knight, stocky build

**2. Archer Body**
- **Silhouette**: Slim humanoid, light armor
- **Height**: ~90px
- **Details**:
  - Leather/cloth armor
  - Quiver on back
  - Athletic stance
- **Variants Needed**:
  - `archer_body.png` - Neutral colors
- **Weapon Anchor**: Both hands forward (for bow)
- **Reference**: Robin Hood style, agile

**3. Scout Body**
- **Silhouette**: Small/nimble humanoid, minimal armor
- **Height**: ~80px (smallest)
- **Details**:
  - Light cloth/leather
  - Hood or bandana
  - Quick/sneaky pose
- **Variants Needed**:
  - `scout_body.png` - Neutral colors
- **Weapon Anchor**: Both hands (dual weapons)
- **Reference**: Rogue/assassin, quick stance

**4. Tank Body**
- **Silhouette**: Large/bulky humanoid, heavy armor
- **Height**: ~110px (tallest)
- **Details**:
  - Massive armor plates
  - Large shield
  - Imposing stance
- **Variants Needed**:
  - `tank_body.png` - Neutral colors
- **Weapon Anchor**: Right hand, lower (heavy weapon)
- **Reference**: Juggernaut, heavily armored

**🎨 Color Tinting Strategy**:
- Bodies are drawn in NEUTRAL colors (gray/white/beige)
- Unity will apply element colors (Fire=red, Water=blue, Nature=green)
- This saves you from drawing 12 variants (4 bodies × 3 elements)

---

#### **5 Weapon Overlays** (64×64px each)

These are transparent overlays placed on top of bodies.

**1. Sword** (`sword_overlay.png`)
- **Type**: One-handed medieval sword
- **Position**: Held in right hand, angled upward
- **Details**: Simple blade, crossguard, handle
- **Size**: ~50px tall

**2. Bow** (`bow_overlay.png`)
- **Type**: Recurve bow
- **Position**: Held in both hands, string visible
- **Details**: Curved wood, string, arrow nocked
- **Size**: ~55px tall

**3. Hammer** (`hammer_overlay.png`)
- **Type**: Two-handed war hammer
- **Position**: Held over shoulder or in both hands
- **Details**: Large head, long handle
- **Size**: ~60px tall (biggest weapon)

**4. Daggers** (`daggers_overlay.png`)
- **Type**: Dual daggers/knives
- **Position**: One in each hand, ready stance
- **Details**: Short blades, simple hilts
- **Size**: ~40px each

**5. Staff** (`staff_overlay.png`)
- **Type**: Magic staff with crystal/orb
- **Position**: Held vertically or at angle
- **Details**: Wooden shaft, glowing top
- **Size**: ~60px tall

**📍 Positioning Notes**:
- Weapons should align with body anchor points
- Keep consistent hand positions across all weapon sprites
- Slight angle (10-15°) adds visual interest

---

#### **20 Ability Icons** (32×32px each)

Small icons displayed on draft cards and above troops.

**Format**: Simple, bold, easily recognizable at small size

**Offensive Icons** (5):
1. `icon_berserk.png` - Angry face or red aura
2. `icon_first_strike.png` - Lightning bolt
3. `icon_armor_pierce.png` - Broken shield
4. `icon_lifesteal.png` - Heart with fangs
5. `icon_critical_strike.png` - Star burst

**Defensive Icons** (5):
6. `icon_shield_aura.png` - Blue shield bubble
7. `icon_dodge.png` - Motion blur silhouette
8. `icon_thorns.png` - Spikes
9. `icon_regeneration.png` - Green heart with +
10. `icon_last_stand.png` - Cracked shield

**Utility Icons** (5):
11. `icon_speed_boost.png` - Wing or wind lines
12. `icon_splash_damage.png` - Explosion waves
13. `icon_taunt.png` - Megaphone or shout
14. `icon_stealth.png` - Eye with slash through it
15. `icon_rally.png` - Flag or banner

**Control Icons** (5):
16. `icon_slow.png` - Snowflake or ice
17. `icon_stun.png` - Yellow stars
18. `icon_knockback.png` - Hand pushing
19. `icon_root.png` - Vine/roots
20. `icon_chain_lightning.png` - Forked lightning

**🎨 Style Tips**:
- High contrast (works on any background)
- Thick lines (readable when small)
- Single dominant color + black outline

---

### Priority 2 - Visual Effects (Can Use Placeholders)

#### **Particle Effects** (Not Critical for Next Test)

Unity can generate these using built-in particle systems:

**Element Auras**:
- Fire Aura: Orange/red particles rising upward
- Water Aura: Blue droplets floating around
- Nature Aura: Green leaves spiraling

**Combat Effects**:
- Hit Spark: White flash on impact
- Death Burst: Element-colored explosion
- Projectile Trails: Arrow trail, magic glow

**If you want to create custom textures**:
- Size: 64×64px
- Format: PNG with alpha
- Style: Soft gradient, glowing edges

---

### Priority 3 - UI Elements (Later)

#### **Draft Cards** (256×384px)
- Card frame with ornate border
- Space for troop preview (center)
- Stats display area (bottom)
- Element indicator (corner)

#### **UI Icons** (32×32px)
- HP heart icon
- Damage sword icon
- Speed boot icon
- Timer clock icon

#### **Backgrounds** (1920×1080px)
- Battlefield background (simple grass/stone)
- Menu background
- Victory/defeat screens

---

## 🎨 Asset Creation Workflow

### Option 1: Digital Art (Procreate/Photoshop)
1. Create new canvas at specified size
2. Use layers:
   - Layer 1: Outline (black)
   - Layer 2: Base colors
   - Layer 3: Shadows/highlights
   - Layer 4: Details
3. Flatten and export as PNG
4. Import to Unity

### Option 2: Pixel Art (Aseprite/Photoshop)
1. Create canvas at half-size (64×64 for bodies)
2. Draw pixel by pixel
3. Export at 2× scale (nearest neighbor)
4. Import to Unity with Point filter

### Option 3: AI Generation (DALL-E/Midjourney)
**Prompts for Bodies**:
```
"Top-down view of a [knight/archer/scout/tank] character for a 2D game,
simple cartoon style, neutral colors, transparent background,
front-facing view, single character, game sprite"
```

**Prompts for Weapons**:
```
"2D game sprite of a [sword/bow/hammer/daggers/staff],
transparent background, simple style, weapon overlay, game asset"
```

Then clean up in image editor and resize to spec.

---

## 📁 File Organization

Place created assets in these folders:

```
Assets/_Project/Sprites/
├── Bodies/
│   ├── knight_body.png
│   ├── archer_body.png
│   ├── scout_body.png
│   └── tank_body.png
├── Weapons/
│   ├── sword_overlay.png
│   ├── bow_overlay.png
│   ├── hammer_overlay.png
│   ├── daggers_overlay.png
│   └── staff_overlay.png
├── Icons/
│   ├── icon_berserk.png
│   ├── icon_first_strike.png
│   └── ... (all 20 ability icons)
└── UI/
    ├── card_frame.png
    └── ... (UI elements)
```

---

## 🔧 Unity Setup After Import

### For Body Sprites:
1. Select sprite in Project window
2. Inspector → Texture Type: `Sprite (2D and UI)`
3. Sprite Mode: `Single`
4. Pixels Per Unit: `32`
5. Filter Mode: `Bilinear` (or Point for pixel art)
6. Click `Apply`
7. Drag to `Body_Knight` asset → **bodySprite** field

### For Weapon Sprites:
1. Same import settings as bodies
2. Drag to `Weapon_Sword` asset → **weaponSprite** field

### For Ability Icons:
1. Import settings:
   - Pixels Per Unit: `32`
   - Max Size: `64` (small icons)
2. Drag to respective `Ability_X` assets → **icon** field

---

## 🎯 Quick Start - Minimum Viable Art

**If you're short on time, create ONLY these 4 sprites first**:

1. **knight_body.png** (128×128) - Simple gray knight
2. **sword_overlay.png** (64×64) - Simple sword
3. **icon_none.png** (32×32) - Blank/circle
4. **icon_berserk.png** (32×32) - Red aura

This is enough to make troops look better than placeholder squares!

Then add more bodies/weapons/icons incrementally.

---

## 🎨 Color Palette Recommendations

### Element Colors (For Tinting):
- **Fire**: `#FF6B35` (Bright orange-red)
- **Water**: `#00B4D8` (Cyan blue)
- **Nature**: `#70E000` (Lime green)

### Neutral Body Colors:
- Base: `#E0E0E0` (Light gray)
- Shadows: `#B0B0B0` (Medium gray)
- Outlines: `#404040` (Dark gray)
- Highlights: `#FFFFFF` (White)

### UI Colors:
- Player Side: Blue tones
- AI Side: Red tones
- Background: Dark gray/brown
- Highlights: Gold/yellow

---

## ✅ Quality Checklist

Before importing to Unity:

- [ ] Correct size (128×128 for bodies, 64×64 for weapons, 32×32 for icons)
- [ ] Transparent background (alpha channel)
- [ ] Centered and properly framed (margins on all sides)
- [ ] Consistent art style across all assets
- [ ] Clear silhouette (readable at small scale)
- [ ] Saved as PNG (not JPG)
- [ ] Descriptive filename (lowercase, underscores)
- [ ] Neutral colors for bodies (if using tinting)

---

## 🚀 Next Steps After Creating Art

1. **Import sprites** to respective folders in Unity
2. **Configure import settings** (Sprite mode, PPU, etc.)
3. **Assign to modules**:
   - Bodies → BodyModule.bodySprite
   - Weapons → WeaponModule.weaponSprite
   - Icons → AbilityModule.icon
4. **Test in BattleTest scene** - troops should now show sprites!
5. **Adjust colors** - tweak element tint colors in EffectModule if needed
6. **Fine-tune positioning** - adjust weaponAnchorPoint in BodyModule

---

## 🎨 Example Art References

**Style Inspiration**:
- Clash Royale (clean, cartoony)
- Kingdom Rush (simple, readable)
- Slay the Spire (minimalist)
- Dead Cells (bold pixel art)

**Keep It Simple**:
- This is a hackathon project - favor speed over perfection
- Clear silhouettes > complex details
- Bold colors > realistic shading
- Readability > realism

---

## 📊 Asset Priority Matrix

| Asset | Priority | Quantity | Time Est. | Impact |
|-------|----------|----------|-----------|--------|
| Bodies | HIGH | 4 | 2-3h | ⭐⭐⭐⭐⭐ |
| Weapons | HIGH | 5 | 1-2h | ⭐⭐⭐⭐ |
| Ability Icons | MEDIUM | 20 | 2-3h | ⭐⭐⭐ |
| Element Auras | LOW | 3 | 1h | ⭐⭐ |
| UI Elements | LOW | 10+ | 2-4h | ⭐⭐ |

**Total Time Estimate**: 6-10 hours for complete asset set

**Recommended Approach**:
- Day 1: Bodies + Weapons (3-5h) → Test immediately
- Day 2: Ability Icons (2-3h) → Full visual completeness
- Day 3: Polish + UI (2-4h) → Final touches

---

**Questions?** Check this guide or ask! Happy creating! 🎨

**Pro Tip**: Start with ONE complete set (Knight + Sword + Fire effect) to verify the pipeline works, then batch-create the rest!
