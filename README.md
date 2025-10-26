# AI Draft Arena

**A Real-Time Arena Battler Where AI Learns to Counter You**

Built for **Supercell AI Game Hack 2025** | Made in under 40 hours | 90% AI-Generated Development

[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity.com)
[![Claude Code](https://img.shields.io/badge/Powered%20by-Claude%20Code-8A2BE2)](https://claude.com/claude-code)

---

## What is AI Draft Arena?

Imagine a real-time arena battler like Clash Royale, but your opponent doesn't just play better—it **adapts and evolves** to counter your exact strategy. Every round, the AI analyzes your choices and **generates custom troop combinations** in real-time, mixing and matching modular components like LEGO blocks to exploit your weaknesses.

This isn't pre-programmed counters. This is **true AI-driven adaptive gameplay**.

---

## The Innovation: AI That Thinks & Counters

### Real-Time Strategy Analysis
After each battle, the AI examines:
- Which body types you prefer (Knights, Archers, Scouts, Tanks)
- Your weapon choices (Sword, Bow, Hammer, Daggers, Staff)
- Which abilities you rely on (20 unique abilities from Berserk to Chain Lightning)
- Your elemental preferences (Fire, Water, Nature triangle)
- Whether you favor elite units or swarms

### Live Card Generation
The AI doesn't just pick from a fixed deck—it **creates new troop combinations** mid-match by:
1. Identifying your strategy patterns
2. Finding your vulnerabilities
3. Combining 4 modular components (Body + Weapon + Ability + Effect)
4. Generating counters that directly exploit your weaknesses

### Adaptive Difficulty
- Used Fire Knights? The AI generates Water Archers to hard-counter you.
- Played slow melee tanks? The AI creates fast ranged scouts to kite you.
- Sent swarms of weak units? The AI builds elite units with AOE attacks.

**4,800+ possible troop combinations** from pre-made modules. The AI never runs out of answers.

---

## Core Features

### 🎮 Real-Time Combat
No turns, no lanes—troops move freely across the battlefield in continuous real-time combat. Maximum 4 troops per side create strategic depth without overwhelming complexity.

### 🧩 Modular Troop System
Every troop is a unique combination of 4 modules:
- **BODY**: Determines HP, speed, range (Knight, Archer, Scout, Tank)
- **WEAPON**: Defines damage and attack pattern (Sword, Bow, Hammer, Daggers, Staff)
- **ABILITY**: Grants special powers (20 universal abilities like Shield Aura, Berserk, Slow, etc.)
- **EFFECT**: Adds elemental advantage (Fire > Nature > Water > Fire)
- **AMOUNT**: Spawns ×1 elite, ×2 pair, ×3 swarm, or ×5 horde

Mix any body with any weapon, any ability, and any element for infinite strategic possibilities.

### ⚔️ Element Triangle
Fire melts Nature. Water douses Fire. Nature overgrows Water. Each advantage grants **×1.5 damage**, making counter-picks crucial.

### 🧠 AI-Powered Opponent
Powered by Claude API, the AI:
- Analyzes your troop modules after each round
- Generates 2 new counter combinations per round
- Builds a growing pool of custom troops (up to 16 options by Round 7)
- Adapts its strategy in real-time based on your playstyle

### ⚡ Fast-Paced Matches
- **15-second draft phase**: Quick decision-making under pressure
- **30-second battles**: Fast, intense combat
- **Best of 7 rounds**: First to 4 wins takes the match
- **6-8 minute sessions**: Perfect for quick, replayable gameplay

### 🔁 Infinite Replayability
No two matches are the same. The AI's adaptive generation ensures every game evolves differently based on your unique strategy and counter-strategy dance.

---

## Technology Stack

### Development Tools (90% AI-Generated)
- **Claude Code**: Primary development assistant - wrote ~90% of the codebase
- **Unity Engine**: 2D real-time combat and rendering
- **Claude API**: Powers the AI opponent's adaptive troop generation

### Asset Generation (100% AI)
- **Layer.ai**: All 2D sprites, UI elements, and textures
- **Hyper3D Rodin AI**: All 3D models for troops and effects

### Architecture
- Modular component system using ScriptableObjects
- State machine-driven match flow
- Real-time combat with dynamic targeting
- JSON-based AI communication with validation and fallbacks

---

## How It Works

### 1. Draft Phase (15 seconds)
Pick 1 of 3 troop combinations. Each card shows all modules and their stats.

### 2. Spawn Phase (1 second)
Both players reveal their picks. Troops materialize on the battlefield.

### 3. Battle Phase (up to 30 seconds)
Troops fight autonomously in real-time:
- Find nearest enemy and engage
- Use weapons with unique attack patterns
- Trigger abilities dynamically
- Apply elemental damage modifiers
- Winner = eliminate all enemies OR highest total HP when time expires

### 4. AI Analysis Phase (during round end)
The AI examines your pick:
```
Player used: Nature Scout ×3 (fast melee swarm)
Detected patterns: Prefers speed, lacks ranged, weak to Fire
Generating counters...
  Counter 1: Fire Archer ×2 (ranged kiting with elemental advantage)
  Counter 2: Fire Tank ×1 (AOE Hammer to destroy swarms)
```

### 5. Repeat
AI adds its generated counters to the pool. Next draft has more options, including the AI's custom creations designed specifically to beat YOU.

---

## Gameplay Example

**Round 1**
- You pick: Fire Knight ×1 (tanky frontline with Shield Aura)
- AI picks: Water Archer ×2 (from base pool)
- Result: Your knight wins, but the AI learns...

**Round 2**
- AI generates: Water Tank ×1 (survives your Fire knight + Regeneration)
- AI generates: Nature Scout ×5 (fast swarm to overwhelm your single unit)
- You pick: Nature Scout ×3 (trying to adapt with speed)
- AI picks: Fire Tank ×1 (Hammer AOE destroys your swarm)
- Result: AI wins. The counter game begins...

**Round 3**
- The pool now has 10 options (4 base + 6 AI-generated)
- AI has learned your patterns: melee-heavy, Fire-element preference, elite-focused
- The AI's new generations directly target these weaknesses
- Can you adapt faster than the AI evolves?

---

## Best of 7 Rounds

First player to win 4 rounds wins the match. The AI gets smarter every round, building a custom arsenal tailored to counter YOUR playstyle.

---

## Why This Couldn't Exist Without AI

1. **Real-Time Content Generation**: Claude API generates valid, balanced troop combinations mid-match
2. **Adaptive Learning**: AI analyzes patterns across multiple data points to find meaningful weaknesses
3. **Natural Language to Game Logic**: AI reasoning ("Player is vulnerable to ranged") translates to JSON game data
4. **Development Speed**: 90% of code written by Claude Code in under 40 hours
5. **Asset Creation**: 100% of visual assets generated by AI (Layer.ai, Hyper3D Rodin)

Traditional development would require:
- Hundreds of pre-made cards designed by hand
- Static AI with scripted responses
- Months of balancing and testing
- A team of artists for asset creation

With AI assistance, one developer created an adaptive game system in a weekend hackathon.

---

## Technical Highlights

### Modular Architecture
- **ScriptableObjects** for all module definitions
- Component-based troop composition
- Universal ability system (all abilities work with any troop)
- Dynamic stat scaling based on spawn amount

### Real-Time Combat System
- Free 2D movement with collision detection
- Dynamic target acquisition
- Projectile physics and homing behavior
- Status effect system with durations and stacking

### AI Integration
- Player behavior analysis and pattern detection
- Structured prompt engineering for consistent generation
- JSON validation with fallback to procedural generation
- Asynchronous API calls with loading states

### Performance Optimization
- Object pooling for troops and projectiles
- Spatial partitioning for efficient collision detection
- Minimal GC allocations during combat

---

## Development Timeline

**Total Development Time**: ~38 hours

- **Hours 0-8**: Core framework and module system
- **Hours 8-14**: Draft system and UI
- **Hours 14-24**: Real-time combat mechanics
- **Hours 24-30**: Claude API integration and AI generation
- **Hours 30-36**: Ability system (20 universal abilities)
- **Hours 36-42**: Visual effects, particles, and juice
- **Hours 42-46**: Testing, balancing, and bug fixes
- **Hours 46-48**: Demo recording and submission prep

**90% of code written by Claude Code**, including:
- All core game systems
- Combat logic and AI pathfinding
- API integration and prompt engineering
- UI/UX implementation
- Ability system with 20 unique effects

---

## Play Instructions

### Controls
- **Mouse**: Click to select draft cards
- **Auto-Play**: Troops fight automatically once spawned

### Tips for Success
1. Watch the AI's patterns—it's watching yours
2. Mix elite units (×1) and swarms (×3, ×5) to keep the AI guessing
3. Remember the element triangle: Fire > Nature > Water > Fire
4. Ranged troops counter slow melee units
5. AOE abilities (Hammer, Splash Damage) devastate swarms
6. Don't get predictable—the AI will punish patterns

---

## Future Potential

With more development time, this system could expand to:
- **More modules**: Additional bodies, weapons, abilities (currently 33 modules total)
- **Multiplayer**: Human vs Human with AI suggesting counter-picks
- **Campaign mode**: AI adapts across matches, "remembering" your long-term patterns
- **Mod support**: Community-created modules that slot into the system
- **Meta-evolution**: AI generates entirely new module types based on player creativity

---

## Credits

### Development
- **Game Design & Programming**: Claude Code (AI-assisted development)
- **AI System**: Claude API (Anthropic)
- **Engine**: Unity 2022.3 LTS

### Assets
- **2D Art**: Layer.ai
- **3D Models**: Hyper3D Rodin AI
- **Fonts**: Google Fonts
- **Sound Effects**: AI-generated and open-source libraries

### Special Thanks
- **Supercell** for hosting the AI Game Hack 2025
- **Anthropic** for Claude Code and Claude API
- The open-source community for tools and inspiration

---

## License

This project was created for the Supercell AI Game Hack 2025. All AI-generated assets are used under their respective licenses.

---

## Play the Demo

**[Download Coming Soon]**

Built with ❤️ and 🤖 in 40 hours for Supercell AI Game Hack 2025

---

**"The best opponent is one that learns from you."**
