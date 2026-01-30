# ABYSSAL TIDE - Game Design Document

**Version:** 0.1 (Draft)
**Last Updated:** 2026-01-30
**Genre:** Turn-Based Tactical Strategy / Open World
**Platform:** PC (Godot 4.x / C#)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Core Pillars](#2-core-pillars)
3. [Setting & Narrative](#3-setting--narrative)
4. [Game Structure](#4-game-structure)
5. [The Ship](#5-the-ship)
6. [Crew System](#6-crew-system)
7. [Naval Gameplay](#7-naval-gameplay)
8. [Land Gameplay](#8-land-gameplay)
9. [Economy & Resources](#9-economy--resources)
10. [Progression Systems](#10-progression-systems)
11. [Technical Requirements](#11-technical-requirements)

---

## 1. Executive Summary

**ABYSSAL TIDE** is a turn-based tactical strategy game set in the neo-Caribbean of 2085. Players captain a ship and crew through an open world of island nations, corporate fleets, and pirate havens. The game combines **open-world naval exploration** (Sid Meier's Pirates!) with **tactical land missions** (XCOM) and **deep crew relationships** (Mass Effect/Fire Emblem).

### The Star Trek Principle

Like the Enterprise, your ship houses a large crew of dozens - but the game focuses on your **Bridge Crew**: a growing roster of named officers and specialists with unique personalities, skills, and story arcs. The rest of the crew provides bonuses and flavor, but the Bridge Crew are the characters you deploy, develop, and potentially lose.

### Core Loop

```
┌─────────────────────────────────────────────────────────────┐
│                      OPEN WORLD                              │
│  Sail the Caribbean → Discover locations → Trigger events   │
└─────────────────────┬───────────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        ▼                           ▼
┌───────────────────┐     ┌───────────────────┐
│   NAVAL COMBAT    │     │   LAND MISSION    │
│                   │     │                   │
│ Ship-to-ship      │     │ Deploy landing    │
│ Hex-based tactics │     │ party (4-6 crew)  │
│ Boarding actions  │     │ Tactical hex      │
│                   │     │ combat on islands │
└─────────┬─────────┘     └─────────┬─────────┘
          │                         │
          └───────────┬─────────────┘
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    SHIP MANAGEMENT                           │
│  Upgrade ship → Manage crew → Sell cargo → Repair/refit    │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Core Pillars

### Pillar 1: Your Ship Is Home

The ship is not just transportation - it's your base, your identity, and your family's home. Every upgrade is visible. Every battle scar tells a story. The ship grows from a salvaged wreck to a legendary vessel.

### Pillar 2: Crew Are People, Not Units

Bridge Crew members have names, faces, backstories, and opinions. They react to your choices. They form relationships with each other. When they die, they're gone - and it matters.

### Pillar 3: Meaningful Tactical Choices

Combat (naval and land) is about positioning, terrain, and trade-offs. No grinding - every battle should feel like a puzzle with multiple solutions.

### Pillar 4: Freedom Within Structure

Open world exploration with emergent stories, but strong narrative throughlines. The player chooses when to engage with the main story.

---

## 3. Setting & Narrative

### The World of 2085

**The Great Atmospheric Shift (2045):** Solar activity caused catastrophic atmospheric changes. Air travel became impossible. Satellites fell. The sky turned hostile.

**The Fuel Famine (2050-2060):** Oil ran out. Nuclear plants melted down without maintenance. Civilization contracted to coastlines where wind and current provided power.

**The Great Devaluation (2065):** Digital currencies collapsed. Banks failed. The old world's wealth became worthless paper. What remained was tangible: ships, cargo, and armed crews.

**The Broken Bridge (2070):** The Panama Canal, damaged by earthquakes and unmaintained, became impassable. All Atlantic-Pacific trade now routes through the Caribbean - through pirate waters.

### The Neo-Caribbean (2085)

| Faction | Description | Relationship |
|---------|-------------|--------------|
| **AetherCorp** | Megacorporation controlling Aetherium refinement. Operates armored treasure fleets. | Primary antagonist (but nuanced) |
| **The Free Ports** | Independent city-states (Nassau, Havana, Kingston). Neutral trade hubs. | Safe havens, quest givers |
| **Trident Confederation** | Organized pirate nation. Has a code. Offers letters of marque. | Potential allies or rivals |
| **Resource Nations** | Island nations with Vita-Algae farms or CanaFiber plantations. Desperate for protection. | Clients, employers |
| **The Drowned** | Cultists who worship the changed sea. Operate strange vessels. | Wild card antagonists |

### Main Narrative Arc

The player starts as a small-time salvager who discovers something in a wreck - something AetherCorp will kill to recover. The main story is about uncovering what AetherCorp found in the deep ocean and why the Atmospheric Shift really happened.

**Story Beats (High Level):**
1. **Act 1:** Escape and survival. Build your first real crew. Establish reputation.
2. **Act 2:** Choose allegiances. The Trident Confederation, a Free Port alliance, or go independent.
3. **Act 3:** Confront AetherCorp. Discover the truth. Make a choice that shapes the Caribbean's future.

---

## 4. Game Structure

### Open World Map

The game world is a **strategic hex map** of the Caribbean:
- ~500-1000 sea hexes representing ocean zones
- ~50-100 island/location hexes (ports, ruins, resource sites)
- Weather systems that move across the map
- Faction-controlled territories that shift based on player actions

### Time & Turns

**Strategic Layer (Open World):**
- Time passes as you sail (1 hex = ~1 day)
- Events trigger based on time, location, and reputation
- Story missions appear when conditions are met
- Day/night cycle affects visibility and some events

**Tactical Layer (Combat):**
- Fully turn-based hex combat
- Initiative system determines unit order
- No time pressure - think as long as you need

### Exploration

**Sailing:**
- Plot courses on the strategic map
- Random encounters based on region danger level
- Discover uncharted locations
- Weather affects travel time and combat conditions

**Ports:**
- Dock to trade, repair, recruit, and gather information
- Each port has unique shops, NPCs, and rumors
- Reputation affects prices and available options
- Story missions often begin/end in ports

---

## 5. The Ship

### Ship as Central Hub

The ship serves multiple functions:
- **Transport:** Moves the party across the strategic map
- **Base:** Where crew lives, trains, and recovers
- **Combat Unit:** Fights in naval battles
- **Identity:** Visual representation of player's journey

### Ship Statistics

| Stat | Description |
|------|-------------|
| **Hull Integrity** | Health pool. Damaged in combat, repaired in port. |
| **Speed** | Hexes moved per turn on strategic map. Affects combat initiative. |
| **Maneuverability** | Turning radius in combat. Affects evasion. |
| **Cargo Capacity** | How much loot/trade goods you can carry. |
| **Crew Capacity** | Maximum crew size (background + bridge crew). |
| **Armament Slots** | Number and size of weapon hardpoints. |
| **Aetherium Tank** | Fuel capacity for special systems. |

### Ship Upgrades

**Hull Types (Progression):**
1. **Salvaged Sloop** - Starting vessel. Fast but fragile. 2 weapon slots.
2. **Refitted Brigantine** - Balanced. 4 weapon slots. Small cargo.
3. **Armored Schooner** - Tough. 4 weapon slots. Medium cargo.
4. **War Galleon** - Massive. 8 weapon slots. Large cargo. Slow.
5. **Hybrid Catamaran** - Fast, maneuverable, good cargo. Expensive.

**Systems (Modular):**
- **Sails:** Affects speed and maneuverability
- **Armor Plating:** Reduces incoming damage
- **Weapons:** Cannons, railguns, harpoons, boarding systems
- **Engine Room:** Aetherium-powered boosts
- **Medical Bay:** Crew recovery speed
- **Crew Quarters:** Morale and capacity bonuses

### Ship Customization

Players can rename their ship and customize:
- Sail colors/patterns
- Figurehead
- Hull markings
- Trophy displays (from major victories)

---

## 6. Crew System

### The Two-Tier Crew

**Background Crew (Anonymous):**
- Represented as a number (e.g., "47 sailors")
- Provides baseline ship operation
- Affects ship stats (more crew = faster repairs, better sailing)
- Lost in battles, recruited in ports
- No individual identity

**Bridge Crew (Named Characters):**
- 4-6 starting, grows to 12-20 over the game
- Unique names, portraits, backstories
- Class/specialization with skill trees
- Deployed in land missions and boarding actions
- Permadeath - death is permanent and mourned
- Relationships with player and each other

### Bridge Crew Recruitment

**How You Find Them:**
- Story missions introduce key characters
- Rescued from enemy ships or prisons
- Hired in ports (random pool refreshes)
- Defectors from enemy factions
- Found in exploration (castaways, survivors)

**Recruitment Moment:**
When you encounter a potential Bridge Crew member, you get:
- Brief introduction/backstory
- Their ask (what they want from you)
- Your response options (shapes initial relationship)

### Bridge Crew Classes

| Class | Role | Land Combat | Naval Combat |
|-------|------|-------------|--------------|
| **Cutlass** | Frontline fighter | Melee damage, high HP | Boarding leader |
| **Marksman** | Ranged damage | Rifles, overwatch | Deck sniper |
| **Sapper** | Demolitions/tech | Explosives, hacking | Targets ship systems |
| **Corsair** | Mobility/flanking | Fast, dual-wield | Grappling, first aboard |
| **Navigator** | Support/tactics | Buffs, debuffs | Ship maneuver bonuses |
| **Surgeon** | Healer | Healing, revive downed | Crew survival bonuses |
| **Quartermaster** | Utility | Traps, supplies | Cargo/loot bonuses |

### Character Progression

**Experience:** Gained from missions, combat, and story moments.

**Skill Trees:** Each class has 3 branches:
- **Core:** Class-defining abilities
- **Specialist:** Unique playstyle options
- **Hybrid:** Cross-class utility

**Personal Quests:** Each Bridge Crew member has optional loyalty missions that:
- Reveal backstory
- Unlock ultimate ability
- Cement relationship (or end it, if failed)

### Relationships

**With Captain (Player):**
- Trust meter (0-100)
- Affected by decisions, dialogue, personal quests
- High trust: Combat bonuses, special dialogue
- Low trust: Reduced effectiveness, may leave or betray

**With Each Other:**
- Bonds form between crew who fight together
- Rivalries can develop
- Deaths affect bonded characters

### Permadeath

When a Bridge Crew member dies:
- Final moment is shown (last words if possible)
- Funeral/memorial scene on ship
- Other crew react based on relationships
- Their bunk/post on ship remains empty (memorial option)
- They do not come back

---

## 7. Naval Gameplay

### Strategic Sailing

On the open world map:
- Click to set destination
- Ship moves along route (can be interrupted by events)
- Events include: enemy sails, storms, debris, discoveries
- Can choose to engage or avoid most encounters

### Naval Combat Initiation

Combat begins when:
- Player chooses to attack a target
- Enemy chooses to attack player
- Random encounter forces engagement
- Story mission requires battle

**Pre-Battle:**
- See enemy ship type and estimated strength
- Choose to fight, flee, or parley
- If fighting, combat map generates based on weather/location

### Naval Combat Map

**Hex Grid:**
- ~20x20 hex arena
- Wind direction indicated (affects sailing)
- Obstacles: rocks, shallows, debris
- Potentially includes shoreline for coastal battles

**Turn Structure:**
1. **Initiative Phase:** Ships ordered by speed + modifiers
2. **Movement Phase:** Each ship moves (wind-dependent)
3. **Action Phase:** Fire weapons, use abilities
4. **Boarding Phase:** Resolve boarding attempts
5. **End Phase:** Fires spread, crew recovers

### Ship Movement

- Ships must move (sailing ships can't hover)
- Turning costs extra movement
- Wind affects speed:
  - **With wind:** Full speed
  - **Across wind:** 75% speed
  - **Against wind:** 50% speed, requires tacking
- Running aground damages hull

### Naval Weapons

| Weapon | Range | Damage | Special |
|--------|-------|--------|---------|
| **Cannons** | Medium | Medium | Reliable, cheap |
| **Railgun** | Long | High | Requires Aetherium, piercing |
| **Carronades** | Short | Very High | Devastating at close range |
| **Harpoon** | Medium | Low | Prevents escape, enables boarding |
| **Chain Shot** | Medium | Low | Targets sails, reduces speed |

### Targeting Systems

Choose what to target:
- **Hull:** Damage ship, risk sinking
- **Sails:** Reduce speed, enable escape/pursuit
- **Deck:** Kill crew (reduces effectiveness, prepares for boarding)
- **Weapons:** Disable specific armaments

### Boarding Actions

**Initiating:**
1. Harpoon target OR pull alongside
2. Declare boarding action
3. Select Bridge Crew members to board (3-6)
4. Boarding mini-map appears

**Boarding Combat:**
- Small hex map representing connected decks
- Your selected crew vs. enemy defenders
- Objectives: Kill captain, reach cargo hold, plant explosives
- Victory: Capture ship (can take or scuttle)
- Defeat: Retreat to your ship (casualties stay behind)

### Surrender & Parley

**Enemy Surrender:**
- Triggered when hull/crew critical
- Options: Accept (capture), Refuse (destroy), Terms (negotiate)
- Reputation consequences for each choice

**Player Surrender:**
- Can always offer surrender
- Enemy may accept, demand ransom, or refuse
- Capture scenario if accepted (escape opportunity)

---

## 8. Land Gameplay

### Mission Types

| Type | Description | Typical Size |
|------|-------------|--------------|
| **Raid** | Assault enemy position for loot | 4-6 crew |
| **Rescue** | Extract prisoner/ally from location | 4-5 crew |
| **Sabotage** | Destroy target (fuel depot, ship in drydock) | 3-4 crew |
| **Infiltration** | Sneak in, steal item/intel, sneak out | 2-3 crew |
| **Defense** | Hold position against waves | 6 crew |
| **Exploration** | Investigate ruins, find secrets | 4-5 crew |

### Landing Party Selection

Before each land mission:
1. See mission briefing (type, estimated difficulty, terrain)
2. Select crew members (within mission limits)
3. Choose loadout for each (weapons, equipment)
4. Deploy to mission map

**The Ship Stays Behind:**
- Landing party is on their own
- Extraction point marked on map
- If things go wrong, must reach extraction to escape

### Land Combat Map

**Hex Grid:**
- Mission-specific size (~15x15 to 30x30)
- Terrain: jungle, urban, industrial, ruins, beach
- Cover system (half-cover, full-cover)
- Elevation matters (high ground advantage)
- Destructible objects

**Turn Structure:**
1. **Player Phase:** All player units act (any order)
2. **Enemy Phase:** All enemies act
3. (Repeat until victory or defeat)

### Combat Actions

Each crew member has:
- **Movement Points:** How far they can move
- **Action Points:** 2 per turn (attack, ability, item)
- **Reaction:** One interrupt action (overwatch, counter)

**Basic Actions:**
- Move (costs MP)
- Attack (costs 1 AP)
- Use Ability (costs 1-2 AP)
- Use Item (costs 1 AP)
- Hunker Down (costs 2 AP, +50% defense)
- Overwatch (costs 2 AP, shoot first enemy that moves)

### Hit Chance & Damage

**To-Hit Calculation:**
```
Base Accuracy (class/weapon)
+ Range Modifier (closer = better)
+ Elevation Modifier (+10% high ground)
+ Cover Modifier (-25% half, -50% full)
+ Flanking Modifier (+25% from side/rear)
+ Status Effects
= Final Hit Chance (capped 5%-95%)
```

**Damage:**
```
Weapon Base Damage
× Critical Multiplier (if crit)
- Armor Reduction
= Final Damage
```

### Land Mission Objectives

**Primary:** Must complete to succeed
**Secondary:** Optional, bonus rewards
**Hidden:** Discovered during mission

**Extraction:**
- Reach extraction point with at least one crew member
- Downed crew can be carried (costs movement)
- Left-behind crew are captured or killed

### Consequences

**Success:**
- Loot acquired
- XP for participants
- Reputation change

**Partial Success:**
- Some objectives complete
- Casualties may occur
- Reduced rewards

**Failure:**
- Surviving crew extracted
- Casualties permanent
- Reputation hit
- Story consequences

---

## 9. Economy & Resources

### The Three Treasures

| Resource | Role | Source | Use |
|----------|------|--------|-----|
| **Aetherium** | Fuel/Power | Raided from convoys, bought at high price | Powers special ship systems, high-value trade |
| **Vita-Algae** | Medicine/Value | Farmed on islands, raided | Heals crew, trade commodity |
| **CanaFiber** | Materials | Plantations, salvage | Ship repairs, basic trade |

### Currency

**Doubloons:** Universal currency, accepted everywhere.
- Earned from: Selling cargo, mission rewards, ransoms
- Spent on: Crew wages, repairs, upgrades, supplies

### Trade System

**Buy Low, Sell High:**
- Each port has different prices based on supply/demand
- Player can speculate on cargo
- Routes develop based on player knowledge

**Smuggling:**
- Some goods are contraband in certain ports
- Higher profit, higher risk
- Reputation consequences if caught

### Ship Expenses

**Ongoing Costs:**
- Crew wages (weekly)
- Ship maintenance (degradation over time)
- Supply consumption (food, water)
- Aetherium (if using powered systems)

**One-Time Costs:**
- Repairs (after battle damage)
- Upgrades
- New weapons
- Recruitment fees

---

## 10. Progression Systems

### Captain Progression

**Reputation:**
- With each faction (affects missions, prices, dialogue)
- General infamy (affects random encounters)
- Specialist reputations (e.g., "The Merciful," "The Terror")

**Captain Abilities:**
- Unlocked by achievements
- Passive bonuses to ship/crew
- Examples: "Inspiring Presence" (+morale in battle), "Sea Sense" (+weather prediction)

### Crew Progression

**Experience → Levels → Skills**
- Each Bridge Crew member levels independently
- New abilities every 2-3 levels
- Class promotion at level 5 and 10

**Equipment:**
- Weapons (affect damage, range, abilities)
- Armor (affects defense, movement)
- Accessories (special effects)

### Ship Progression

**Upgrade Path:**
- Save money → Buy better hull or systems
- Or: Capture enemy ship (if better than yours)
- Cosmetic trophies from major victories

**Named Ships:**
- Defeating famous enemy ships grants their "legend"
- Can inherit their name/reputation

### World Progression

**Faction Power:**
- Player actions shift faction control of regions
- Winning battles for a faction strengthens them
- Affects available missions and world state

**Story Flags:**
- Key decisions are tracked
- Affect available endings
- Some changes are permanent (characters dead, locations destroyed)

---

## 11. Technical Requirements

### Map System (Current Implementation)

**Hex Grid:**
- Cube coordinates (q, r, s)
- Terrain types with elevation
- Rivers and roads
- Chunked rendering with LOD

**Generation:**
- Procedural terrain generation exists
- Need to extend for:
  - Island generation
  - Port placement
  - Faction territory assignment

### Required Systems (To Build)

| System | Priority | Complexity | Status |
|--------|----------|------------|--------|
| **Turn Manager** | High | Medium | Partial |
| **Unit/Crew System** | High | High | Not started |
| **Combat System** | High | High | Not started |
| **Ship Management UI** | High | Medium | Not started |
| **Strategic Map** | High | High | Not started |
| **Dialogue System** | Medium | Medium | Not started |
| **Save/Load** | Medium | Medium | Not started |
| **Economy/Trade** | Medium | Medium | Not started |
| **AI (Enemy/Crew)** | Medium | High | Not started |
| **Mission Generator** | Low | High | Not started |
| **Faction System** | Low | Medium | Not started |

### Art Requirements

**2D Assets:**
- Character portraits (Bridge Crew)
- UI elements
- Icons (items, abilities, resources)

**3D Assets (Using Current System):**
- Ship models (5-10 types)
- Building prefabs for ports
- Terrain textures (tropical expansion)
- Unit models for land combat

### Audio Requirements

- Sea ambience
- Combat sounds (cannons, swords, gunfire)
- UI feedback
- Music (exploration, combat, port, story)
- Voice acting (optional, for key moments)

---

## Appendix A: Influences Reference

| Game | What We Take |
|------|--------------|
| **Sid Meier's Pirates!** | Open world Caribbean, ship capture, reputation |
| **XCOM 1/2** | Tactical land combat, permadeath weight, base management |
| **Fire Emblem** | Character relationships, permadeath consequences, class progression |
| **Mass Effect** | Crew loyalty missions, dialogue importance, ship as home |
| **Final Fantasy Tactics** | Class system depth, tactical positioning |
| **FTL** | Ship management under pressure, crew as resource |
| **Valkyria Chronicles** | Beautiful tactical battles, named squad members |

---

## Appendix B: Open Questions

1. **Multiplayer?** - PvP naval battles? Co-op missions? Or single-player only?
2. **Difficulty Modes?** - Ironman only? Or save scum allowed?
3. **New Game Plus?** - What carries over?
4. **Platform Expansion?** - Console? Mobile?
5. **Procedural vs Authored?** - How much of the story is fixed vs. emergent?

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 0.1 | 2026-01-30 | Initial draft |

