# Draft UI Implementation Guide
**AI Draft Arena - Cute & Whimsical UI**

---

## Overview

This guide covers the complete implementation of the Draft and Battle UI systems with a **cute and whimsical** aesthetic while maintaining **simplicity and clean design**.

**Style Guidelines:**
- Font: **DynaPuff** (cute, rounded, playful)
- Colors: Soft pastels with pops of vibrant colors
- Animations: Bouncy, smooth (DoTween with ease-out curves)
- Cards: Rounded corners, subtle shadows
- Icons: Clear, colorful, simple shapes

---

## Phase 1: Scripts Implementation

### 1.1 DraftCard.cs
**Purpose:** Individual card component displaying a troop combination

**Features:**
- Display 4 module icons (Body, Weapon, Ability, Effect)
- Show amount multiplier (×1, ×2, ×3, ×5)
- Display name and stats
- Hover animation (scale up, bounce)
- Click handling
- Selection glow effect

**Key Methods:**
- `SetCombination(TroopCombination)` - Populate card with data
- `OnHoverEnter()` - Scale up animation
- `OnHoverExit()` - Scale back down
- `OnCardClicked()` - Notify parent UI
- `ShowSelected()` - Enable selection glow
- `SetInteractable(bool)` - Enable/disable interaction

---

### 1.2 DraftUI.cs
**Purpose:** Draft screen controller managing 3 cards and timer

**Features:**
- Subscribe to DraftController events
- Display 3 draft cards
- Countdown timer with color transitions
- Warning animation at 5 seconds (pulse, shake)
- Selection feedback
- Screen fade in/out

**Event Subscriptions:**
- `OnPlayerOptionsGenerated` → Display cards
- `OnTimerUpdated` → Update timer display
- `OnTimerWarning` → Trigger warning animation
- `OnPlayerSelected` → Show selection feedback
- `OnDraftCompleted` → Fade out draft screen

---

### 1.3 BattleUI.cs
**Purpose:** Battle screen controller with timer and HP display

**Features:**
- Battle countdown timer (30s)
- Team HP bars (Player = Blue, AI = Red)
- HP text display
- Victory banner with fade-in
- Timer color transitions

**Event Subscriptions:**
- `OnBattleStarted` → Show battle screen
- `OnTimerUpdated` → Update timer display
- `OnBattleEnded` → Show victory banner

**Update Loop:**
- Poll HP from TargetingSystem each frame
- Update HP bars and text

---

## Phase 2: Unity Editor Setup

### 2.1 Canvas Configuration
**Location:** `Assets/_Project/Prefabs/prefab_canvas.prefab`

**Settings:**
- Render Mode: **Screen Space - Overlay**
- Canvas Scaler:
  - UI Scale Mode: **Scale With Screen Size**
  - Reference Resolution: **1920 × 1080**
  - Screen Match Mode: **Match Width Or Height**
  - Match: **0.5** (balance between width and height)

**Hierarchy:**
```
prefab_canvas
├─ DraftScreen (initially active)
└─ BattleScreen (initially hidden)
```

---

### 2.2 DraftCard Prefab Structure

**File:** `Assets/_Project/Prefabs/UI/DraftCard.prefab`

```
DraftCard (200×320 rect)
├─ CardBackground (Image - rounded rect)
│  └─ Shadow (Image - soft shadow effect)
├─ IconRow (Horizontal Layout Group)
│  ├─ BodyIcon (Image 54×54)
│  ├─ WeaponIcon (Image 54×54)
│  ├─ AbilityIcon (Image 54×54)
│  └─ EffectIcon (Image 54×54)
├─ AmountBadge (Panel - top right corner)
│  └─ AmountText (TMP "×3" - DynaPuff Bold)
├─ NameText (TMP "Fire Knight" - DynaPuff)
├─ StatsPanel (Horizontal Layout)
│  ├─ HPIcon (Image - heart)
│  ├─ HPText (TMP "8.0")
│  ├─ DMGIcon (Image - sword)
│  └─ DMGText (TMP "3.0")
├─ SelectionGlow (Image - yellow glow, initially disabled)
└─ Button (covers entire card)
```

**Component Settings:**
- **CardBackground Image:**
  - Sprite: Rounded rectangle (create in Unity: GameObject → UI → Image)
  - Color: White (#FFFFFF) - will tint per element
  - Material: Optional soft shadow material

- **IconRow Layout:**
  - Spacing: 8px
  - Child Alignment: Middle Center
  - Child Control Size: Width & Height
  - Padding: 10px all sides

- **AmountBadge:**
  - Anchors: Top Right
  - Offset: (-15, -15)
  - Width: 50, Height: 50
  - Circular background
  - AmountText: Font Size 24, Bold, Center aligned

- **NameText:**
  - Font: DynaPuff Regular
  - Font Size: 20
  - Alignment: Center
  - Color: Dark gray (#333333)
  - Position: Below icons

- **StatsPanel:**
  - Horizontal layout with spacing 5
  - Icons: 24×24 sprites
  - Text: Font Size 16, DynaPuff Light

- **SelectionGlow:**
  - Anchors: Stretch (fill parent)
  - Color: Yellow (#FFEB3B) with 40% alpha
  - Sprite: Rounded rect matching card shape
  - Initially disabled

- **Button:**
  - Target Graphic: CardBackground
  - Transition: Color Tint
  - Highlighted: 10% lighter
  - Pressed: 20% darker
  - Navigation: None (cards are close together)

---

### 2.3 DraftScreen Prefab Structure

**File:** `Assets/_Project/Prefabs/UI/DraftScreen.prefab`

```
DraftScreen (Full screen)
├─ Background (Image - soft gradient)
├─ TitlePanel (Top center)
│  ├─ TitleDecoration (Image - stars/sparkles)
│  └─ TitleText (TMP "CHOOSE YOUR CHAMPION!" - DynaPuff Bold)
├─ CardContainer (Center)
│  ├─ DraftCard_0
│  ├─ DraftCard_1
│  └─ DraftCard_2
├─ TimerPanel (Top center, below title)
│  ├─ TimerBackground (Rounded panel)
│  ├─ TimerIcon (Image - clock/hourglass)
│  └─ TimerText (TMP "15" - DynaPuff Bold)
└─ PromptText (Bottom center - TMP "Click a card to select!")
```

**Component Settings:**
- **Background:**
  - Anchors: Stretch
  - Color: Soft gradient (light blue to light purple)
  - Sprite: Optional gradient texture

- **TitleText:**
  - Font: DynaPuff Bold
  - Font Size: 48
  - Color: White with soft shadow
  - Outline: 2px dark color
  - Position Y: -80 from top

- **CardContainer:**
  - Horizontal Layout Group
  - Spacing: 60px
  - Child Alignment: Middle Center
  - Anchors: Middle Center

- **TimerPanel:**
  - Position Y: -180 from top
  - Background: White rounded rect with shadow
  - Width: 120, Height: 80

- **TimerText:**
  - Font: DynaPuff Bold
  - Font Size: 56
  - Color: White (changes to yellow/red based on time)
  - Alignment: Center

- **PromptText:**
  - Font: DynaPuff Regular
  - Font Size: 24
  - Color: White with transparency
  - Position Y: 60 from bottom
  - Animation: Subtle float up/down (optional)

---

### 2.4 BattleScreen Prefab Structure

**File:** `Assets/_Project/Prefabs/UI/BattleScreen.prefab`

```
BattleScreen (Full screen, initially hidden)
├─ TimerPanel (Top center)
│  ├─ TimerBackground (Circular badge)
│  └─ TimerText (TMP "30" - DynaPuff Bold)
├─ PlayerHPPanel (Top left)
│  ├─ PlayerHPBar (Slider)
│  │  ├─ Background (rounded bar)
│  │  ├─ Fill (blue gradient)
│  │  └─ Handle (none)
│  └─ PlayerHPText (TMP "24.0 HP")
├─ AIHPPanel (Top right)
│  ├─ AIHPBar (Slider)
│  │  ├─ Background (rounded bar)
│  │  ├─ Fill (red gradient)
│  │  └─ Handle (none)
│  └─ AIHPText (TMP "18.0 HP")
└─ VictoryBanner (Full screen, initially hidden)
   ├─ BannerBackground (Image - semi-transparent)
   ├─ VictoryPanel (Center panel)
   │  ├─ Decoration (Image - stars/confetti)
   │  └─ WinnerText (TMP "VICTORY!" - DynaPuff Bold)
   └─ CanvasGroup (for fade animation)
```

**Component Settings:**
- **TimerPanel:**
  - Position Y: -60 from top
  - Circular background, width/height: 100
  - TimerText: Font Size 52, Center aligned

- **PlayerHPPanel:**
  - Anchors: Top Left
  - Position: X: 60, Y: -160
  - Width: 250

- **PlayerHPBar (Slider):**
  - Direction: Left to Right
  - Min Value: 0, Max Value: 100 (set dynamically in code)
  - Whole Numbers: No
  - Interactable: No
  - Fill: Blue gradient (#2196F3 to #64B5F6)
  - Background: Dark gray with rounded corners
  - Height: 24

- **AIHPPanel:**
  - Anchors: Top Right
  - Position: X: -60, Y: -160
  - Width: 250

- **AIHPBar:**
  - Same as PlayerHPBar
  - Fill: Red gradient (#F44336 to #EF5350)

- **VictoryBanner:**
  - Initially disabled
  - CanvasGroup Alpha: 0 (fades in via script)

- **WinnerText:**
  - Font: DynaPuff ExtraBold
  - Font Size: 96
  - Color: Yellow (#FFEB3B) with white outline
  - Shadow effect
  - Alignment: Center

---

## Phase 3: Reveal Phase

**Location:** Between Draft and Battle phases in MatchController

**Simple Implementation:**
1. After draft completes, show "Reveal Panel"
2. Display:
   - Left side: Player's selected card (enlarged)
   - Right side: AI's selected card (enlarged)
   - Center: "VS" text
3. 2-second delay
4. Fade out reveal panel
5. Start battle

**Reveal Panel Structure:**
```
RevealPanel (Full screen)
├─ Background (semi-transparent dark)
├─ PlayerRevealCard (DraftCard - scale 1.2)
├─ VSText (TMP "VS" - DynaPuff Bold, huge)
└─ AIRevealCard (DraftCard - scale 1.2)
```

**Animation:**
- Cards slide in from sides (0.5s)
- Hold for 2 seconds
- Fade out (0.3s)
- Total duration: ~3 seconds

---

## Phase 3.5: UIManager Setup

**Script:** `UIManager.cs` - Handles screen transitions based on match phases

**Purpose:**
- Subscribes to MatchController phase changes
- Shows/hides DraftUI and BattleUI based on current phase
- Ensures victory banner is hidden when starting new draft
- Prevents UI overlap issues

**Setup Instructions:**

1. **Add UIManager to Scene:**
   - Create new GameObject in Hierarchy: "UIManager"
   - Add Component → UIManager.cs
   - Position: Doesn't matter (no visual representation)

2. **Assign Inspector References:**
   - `matchController` → MatchController in scene
   - `draftUI` → DraftScreen (DraftUI component)
   - `battleUI` → BattleScreen (BattleUI component)

3. **Initial State:**
   - Both DraftScreen and BattleScreen can be active or inactive in editor
   - UIManager will manage visibility at runtime
   - Recommended: Leave both inactive initially for clean startup

**Phase Transition Behavior:**
- **MatchStart:** Hide all screens
- **Draft:** Hide BattleUI (+ reset), Show DraftUI (+ reset)
- **Spawn:** Keep Draft visible (brief phase)
- **Battle:** Hide DraftUI, Show BattleUI (+ reset)
- **RoundEnd:** Keep BattleUI visible (shows victory banner)
- **MatchEnd:** Keep BattleUI visible (final results)

---

## Phase 4: Inspector Reference Assignment Checklist

### DraftCard Component
- [ ] `bodyIcon` → IconRow/BodyIcon (Image)
- [ ] `weaponIcon` → IconRow/WeaponIcon (Image)
- [ ] `abilityIcon` → IconRow/AbilityIcon (Image)
- [ ] `effectIcon` → IconRow/EffectIcon (Image)
- [ ] `amountText` → AmountBadge/AmountText (TMP_Text)
- [ ] `nameText` → NameText (TMP_Text)
- [ ] `hpText` → StatsPanel/HPText (TMP_Text)
- [ ] `dmgText` → StatsPanel/DMGText (TMP_Text)
- [ ] `cardBackground` → CardBackground (Image)
- [ ] `selectionGlow` → SelectionGlow (Image)
- [ ] `button` → Button (Button)

### DraftUI Component
- [ ] `cards` → List<DraftCard> (3 elements)
- [ ] `timerText` → TimerPanel/TimerText (TMP_Text)
- [ ] `promptText` → PromptText (TMP_Text)
- [ ] `titleText` → TitlePanel/TitleText (TMP_Text)
- [ ] `canvasGroup` → CanvasGroup on root

### BattleUI Component
- [ ] `battleController` → BattleController in scene
- [ ] `timerText` → TimerPanel/TimerText (TMP_Text)
- [ ] `playerHPBar` → PlayerHPPanel/PlayerHPBar (Slider)
- [ ] `aiHPBar` → AIHPPanel/AIHPBar (Slider)
- [ ] `playerHPText` → PlayerHPPanel/PlayerHPText (TMP_Text)
- [ ] `aiHPText` → AIHPPanel/AIHPText (TMP_Text)
- [ ] `victoryBanner` → VictoryBanner (GameObject)
- [ ] `winnerText` → VictoryPanel/WinnerText (TMP_Text)
- [ ] `bannerCanvasGroup` → VictoryBanner/CanvasGroup

### UIManager Component
- [ ] `matchController` → MatchController in scene
- [ ] `draftUI` → DraftScreen (DraftUI component)
- [ ] `battleUI` → BattleScreen (BattleUI component)

---

## Phase 5: Testing Checklist

### Draft Phase Tests
- [ ] Draft screen appears on match start
- [ ] 3 cards display with icons and text
- [ ] Timer counts down from 15 seconds
- [ ] Timer text turns yellow at 10s, red at 5s
- [ ] Warning animation plays at 5s (pulse/shake)
- [ ] Hovering card scales up (1.05x)
- [ ] Clicking card shows selection glow
- [ ] Timer stops on selection
- [ ] Auto-select triggers at 0s if no selection
- [ ] Draft screen fades out after selection

### Reveal Phase Tests
- [ ] Reveal panel appears after draft
- [ ] Player card shows on left
- [ ] AI card shows on right
- [ ] "VS" text visible in center
- [ ] Cards slide in smoothly
- [ ] 2-second hold duration
- [ ] Panel fades out before battle

### Battle Phase Tests
- [ ] Battle screen appears after reveal
- [ ] Timer counts down from 30 seconds
- [ ] HP bars start at max (based on troop count)
- [ ] HP bars update as troops take damage
- [ ] HP text updates (e.g., "18.5 HP")
- [ ] Timer text turns yellow at 10s, red at 5s
- [ ] Victory banner appears on battle end
- [ ] "VICTORY" shows for player win
- [ ] "DEFEAT" shows for player loss
- [ ] Banner fades in smoothly

### UI Manager Tests
- [ ] Draft screen is completely hidden during battle
- [ ] Battle screen is completely hidden during draft
- [ ] Victory banner is hidden when starting new draft
- [ ] No timer overlap between draft and battle
- [ ] Stats display as integers (no decimals)
- [ ] Screen transitions are smooth
- [ ] No UI elements persist across phase changes

---

## Phase 6: Color Palette (Cute & Whimsical)

### Primary Colors
- **Background Gradient:** Light Blue (#B3E5FC) to Lavender (#E1BEE7)
- **Card Background:** White (#FFFFFF) with element tint
- **Text Primary:** Dark Gray (#424242)
- **Text Secondary:** Medium Gray (#757575)

### Element Colors (for card tinting)
- **Fire:** Soft Red (#FF8A80)
- **Water:** Soft Blue (#82B1FF)
- **Nature:** Soft Green (#B9F6CA)

### UI Accents
- **Player HP:** Blue (#2196F3)
- **AI HP:** Red (#F44336)
- **Timer Normal:** White (#FFFFFF)
- **Timer Warning:** Yellow (#FFEB3B)
- **Timer Urgent:** Red (#FF5252)
- **Selection Glow:** Yellow (#FFEB3B) 40% alpha

### Shadows & Outlines
- **Card Shadow:** Black (#000000) 20% alpha, offset (0, -4)
- **Text Outline:** Dark Gray (#212121) 2px
- **Text Shadow:** Black (#000000) 15% alpha, offset (2, -2)

---

## Phase 7: DoTween Animation Parameters

### Card Hover
```csharp
transform.DOScale(1.05f, 0.2f).SetEase(Ease.OutBack);
```

### Card Select Glow
```csharp
selectionGlow.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
```

### Timer Warning Pulse
```csharp
timerText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 0.5f);
```

### Screen Fade In
```csharp
canvasGroup.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
```

### Screen Fade Out
```csharp
canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad);
```

### Victory Banner Entrance
```csharp
canvasGroup.alpha = 0f;
canvasGroup.DOFade(1f, 0.6f).SetEase(Ease.OutCubic);
winnerText.transform.localScale = Vector3.zero;
winnerText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.2f);
```

### Reveal Card Slide In
```csharp
// Left card (player)
card.anchoredPosition = new Vector2(-800, 0);
card.DOAnchorPos(new Vector2(-300, 0), 0.5f).SetEase(Ease.OutBack);

// Right card (AI)
card.anchoredPosition = new Vector2(800, 0);
card.DOAnchorPos(new Vector2(300, 0), 0.5f).SetEase(Ease.OutBack);
```

---

## Phase 8: Placeholder Assets Creation

### Icon Sprites (until real icons are made)

**Create in Unity:**
1. GameObject → UI → Image
2. Set Image to solid color
3. Set size to 64×64
4. Right-click in Hierarchy → Convert to Sprite
5. Save in `Assets/_Project/Sprites/UI/Icons/`

**Placeholder Icons:**
- **Body_Placeholder:** Brown square (#8D6E63)
- **Weapon_Placeholder:** Gray square (#9E9E9E)
- **Ability_Placeholder:** Purple square (#9C27B0)
- **Effect_Fire:** Red square (#FF5252)
- **Effect_Water:** Blue square (#448AFF)
- **Effect_Nature:** Green square (#69F0AE)

---

## Implementation Order

1. **Script: DraftCard.cs** → Implement → Review → Commit ✅
2. **Script: DraftUI.cs** → Implement → Review → Commit ✅
3. **Script: BattleUI.cs** → Implement → Review → Commit ✅
4. **Prefab: DraftCard** → Create in Unity → Test → Commit ✅
5. **Prefab: DraftScreen** → Create in Unity → Test → Commit ✅
6. **Prefab: BattleScreen** → Create in Unity → Test → Commit ✅
7. **Script: UIManager.cs** → Implement → Add to scene → Wire up ✅
8. **Integration: Test full flow** → Fix issues → Test → Commit
9. **Polish: Animations** → Add DoTween effects → Test → Commit

---

## Next Steps

After completing this guide:
1. Review design decisions
2. Clarify any uncertainties
3. Begin implementation phase 1 (scripts)
4. Test each component in isolation
5. Integrate into main game flow
6. Polish and refine

**Estimated Total Time:** 10-14 hours
**Style:** Cute, whimsical, clean, and functional!
