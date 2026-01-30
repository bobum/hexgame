# ABYSSAL TIDE - Game Design Document

**Version:** 0.3 (Draft)
**Last Updated:** 2026-01-30
**Genre:** Turn-Based Tactical Strategy / Open World
**Platform:** PC (Godot 4.x / C#)
**Source Material:** Neo-Pirate Caribbean Novel Series (9 books, 3 trilogies)

---

## Table of Contents

### Part I: Philosophy & Foundation
1. [Executive Summary](#1-executive-summary)
2. [Engine Philosophy](#2-engine-philosophy)
3. [Core Pillars](#3-core-pillars)

### Part II: Canonical Lore
4. [Timeline & History](#4-timeline--history)
5. [The Novel Trilogies](#5-the-novel-trilogies)
6. [Canonical Characters](#6-canonical-characters)
7. [Factions & Powers](#7-factions--powers)

### Part III: Engine Systems
8. [Game Structure](#8-game-structure)
9. [The Ship System](#9-the-ship-system)
10. [Captain Creation](#10-captain-creation)
11. [Crew System](#11-crew-system)
12. [Job & Ability System](#12-job--ability-system)
13. [Naval Combat Engine](#13-naval-combat-engine)
14. [Land Combat Engine](#14-land-combat-engine)
15. [Economy Engine](#15-economy-engine)
16. [Progression Engine](#16-progression-engine)
17. [Campaign & Story Engine](#17-campaign--story-engine)

### Part IV: Content & Modding
18. [Modding Architecture](#18-modding-architecture)
19. [Base Game Content](#19-base-game-content)
20. [Technical Requirements](#20-technical-requirements)

---

## 1. Executive Summary

**ABYSSAL TIDE** is a turn-based tactical strategy game set in the neo-Caribbean, based on a 9-book novel series spanning 70 years of alternate history. The game is designed as a **content-agnostic engine** that can tell any story within this universe - from the collapse of civilization to the submarine cold wars of the 22nd century.

### The Vision

The game combines **open-world naval exploration** (Sid Meier's Pirates!) with **tactical land missions** (XCOM) and **deep crew relationships** (Mass Effect/Fire Emblem). But more importantly, it's built so that **each novel can become a campaign mod**, and the community can create their own stories using the same tools.

### Engine + Content Model

```
┌─────────────────────────────────────────────────────────────┐
│                    THE ENGINE                                │
│  (What we build - the systems that never change)            │
│                                                             │
│  • Hex Grid & Navigation    • Combat (Naval + Land)         │
│  • Ship Management          • Crew & Job Systems            │
│  • Economy & Trade          • Campaign & Story Systems      │
│  • Modding API              • Save/Load                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    THE CONTENT                               │
│  (What changes - delivered as mods/campaigns)               │
│                                                             │
│  • Book 4: The Coral Crown     (Discovery era campaign)     │
│  • Book 5: The Broken Bridge   (Canal war campaign)         │
│  • Book 6: The Trident Pact    (Unification campaign)       │
│  • Books 7-9: Abyssal War      (Submarine era campaigns)    │
│  • Community Campaigns         (Fan-made content)           │
└─────────────────────────────────────────────────────────────┘
```

### The Star Trek Principle

Like the Enterprise, your ship houses a large crew of dozens - but the game focuses on your **Bridge Crew**: a growing roster of named officers and specialists with unique personalities, skills, and story arcs. The rest of the crew provides bonuses and flavor, but the Bridge Crew are the characters you deploy, develop, and potentially lose.

---

## 2. Engine Philosophy

### "Build the Engine, Not the Things"

The most critical insight for this project: **focus on systems, not content**. Content changes. Stories evolve. But a well-designed engine enables infinite content.

### What is "Engine"?

| Engine (Build Once) | Content (Mod/Data) |
|---------------------|-------------------|
| Hex grid rendering | Specific maps |
| Combat turn system | Enemy types, abilities |
| Crew relationship system | Specific characters |
| Dialogue tree system | Actual dialogue |
| Economy simulation | Item definitions, prices |
| Ship physics/movement | Ship models, stats |
| Campaign state machine | Specific story beats |
| Save/Load serialization | Save file format |

### Engine Design Principles

1. **Data-Driven Everything**
   - No hardcoded content
   - All game objects defined in JSON/YAML
   - Engine reads data, doesn't know what "AetherCorp" or "Corbin Shaw" means

2. **Event-Based Architecture**
   - Engine emits events (OnCombatStart, OnCrewDeath, OnDialogueChoice)
   - Content/mods subscribe to events
   - Loose coupling between systems

3. **Campaign Agnostic**
   - Engine doesn't know what year it is
   - Engine doesn't know who the protagonist is
   - Engine doesn't know what the "main quest" is
   - All of that comes from campaign data

4. **Mod-First Development**
   - Base game content uses the same mod API as community mods
   - If we can't do it with the mod system, fix the mod system
   - No "special" paths for official content

5. **Temporal Flexibility**
   - Engine supports different technology levels (sail-only, sail+submarine, etc.)
   - Engine supports different map scales (single island, full Caribbean, Atlantic)
   - Campaign data defines what's available

### The Three Layers

```
┌─────────────────────────────────────────┐
│  LAYER 3: CAMPAIGN CONTENT              │
│  (Specific stories, characters, items)  │
│  - "The Coral Crown" campaign           │
│  - Corbin Shaw character definition     │
│  - AetherCorp faction definition        │
└────────────────┬────────────────────────┘
                 │ Uses
                 ▼
┌─────────────────────────────────────────┐
│  LAYER 2: GAME SYSTEMS                  │
│  (Rules, mechanics, formulas)           │
│  - Combat damage formulas               │
│  - Job ability definitions              │
│  - Ship upgrade paths                   │
└────────────────┬────────────────────────┘
                 │ Uses
                 ▼
┌─────────────────────────────────────────┐
│  LAYER 1: CORE ENGINE                   │
│  (Rendering, input, serialization)      │
│  - Hex grid math                        │
│  - Turn management                      │
│  - UI framework                         │
│  - Mod loading                          │
└─────────────────────────────────────────┘
```

---

## 3. Core Pillars

### Pillar 1: Your Ship Is Home

The ship is not just transportation - it's your base, your identity, and your family's home. Every upgrade is visible. Every battle scar tells a story. The ship grows from a salvaged wreck to a legendary vessel.

### Pillar 2: Crew Are People, Not Units

Bridge Crew members have names, faces, backstories, and opinions. They react to your choices. They form relationships with each other. When they die, they're gone - and it matters.

### Pillar 3: Meaningful Tactical Choices

Combat (naval and land) is about positioning, terrain, and trade-offs. No grinding - every battle should feel like a puzzle with multiple solutions.

### Pillar 4: Freedom Within Structure

Open world exploration with emergent stories, but strong narrative throughlines. The player chooses when to engage with the main story.

---

# PART II: CANONICAL LORE

This section documents the canonical world from the novel series. The engine doesn't "know" any of this - it's all content. But it serves as the reference for official campaigns.

---

## 4. Timeline & History

### The Pre-Crisis Era (2025-2033)
Global economies strained by debt and fragile "just-in-time" supply chains. The calm before the storm.

### The Twin Shocks (2034-2043)

**The Great Atmospheric Shift (2034-2038):**
Hyper-energetic solar flares permanently altered Earth's magnetosphere. The weakened magnetic field caused:
- Thermospheric heating and expansion
- Chronic wind shear at high altitudes
- Frequent high-altitude microbursts
- 15-30% increase in aircraft fuel consumption
- Exponential maintenance costs for aircraft
- Air travel became economically unviable for cargo

**The Fuel Famine (2039-2043):**
Depletion of accessible hydrocarbons accelerated. Energy costs skyrocketed. Aviation industry collapsed. Manufacturing crippled.

### The Great Devaluation (2044-2048)
- Governments print money uncontrollably to manage crises
- Hyperinflation destroys faith in fiat currency
- ~2048: Major central bank defaults, triggering global collapse
- International trade halts
- Nations fracture into regional blocs

### The Age of Scarcity (2049-2055)
- Mega-corporations and regional blocs emerge as new powers
- Caribbean's unique resources discovered: Grav-Coral, Vita-Algae, CanaFiber
- Advanced sail becomes the only viable transport
- Piracy flourishes

### The Canal Wars (2056-2076)
- 2056: Panamanian authority dissolves, Canal Zone descends into chaos
- 2060: AetherCorp wins the struggle, temporarily reopens one lane
- 2061-2075: AetherCorp fights losing battle against maintenance costs, sabotage, and guerilla warfare
- 2076: **The Canal is Abandoned** - AetherCorp withdraws, jungle reclaims the locks

### The New Order (2077-Present)
- Panama Canal becomes "The Broken Bridge" - a monument to the old world
- All trade forced around Cape Horn
- Caribbean becomes the most vital maritime hub on Earth
- The Trident Confederation forms as a unified pirate nation
- Cold war between AetherCorp and the Confederation intensifies

---

## 5. The Novel Trilogies

The canonical source material spans three trilogies, each covering a distinct era. Each can become a campaign mod.

### TRILOGY 1: The Long Fall (2030-2048)
*Genre: Techno-thriller disaster epic*
*Theme: The end of the world as we know it*

| Book | Title | Focus | Protagonist |
|------|-------|-------|-------------|
| 1 | **SKYFIRE** | The Atmospheric Shift | Dr. Aris Thorne (climatologist) |
| 2 | **DRY RUN** | Fuel Famine & Devaluation | Maria Flores (trader), young Corbin Shaw |
| 3 | **BROKEN BANKS** | Central bank collapse | Thorne, Flores, Shaw, Marcus Valerius |

**Campaign Potential:** Survival/escape campaigns, resource management under collapse, minimal naval combat, focus on land missions and tough choices.

### TRILOGY 2: The Age of Scarcity & Sail (2050-2077)
*Genre: High-seas adventure saga*
*Theme: Discovery, freedom vs. corporate tyranny*

| Book | Title | Focus | Protagonist |
|------|-------|-------|-------------|
| 4 | **THE CORAL CROWN** | Rise of AetherCorp, Grav-Coral discovery | Corbin Shaw (captain of *Stargazer*) |
| 5 | **THE BROKEN BRIDGE** | Canal struggle and abandonment | Shaw + AetherCorp engineer |
| 6 | **THE TRIDENT PACT** | Unification of pirate factions | Aging Corbin Shaw |

**Campaign Potential:** Classic pirate gameplay, ship combat, raiding, building reputation, political maneuvering. This is the "golden age of piracy" era for the setting.

### TRILOGY 3: The Abyssal War (2080-2100+)
*Genre: Deep-sea techno-thriller, cold war*
*Theme: Freedom vs. control, legacy*

| Book | Title | Focus | Protagonist |
|------|-------|-------|-------------|
| 7 | **THE KRAKEN PROJECT** | Submarine arms race begins | Sofia (engineer), elder Corbin Shaw |
| 8 | **THE SERPENT'S PASSAGE** | Submarine transport service | Crew of *Void Kraken* |
| 9 | **ABYSSAL DAWN** | Final confrontation | Sofia, Shaw, AetherCorp chief |

**Campaign Potential:** Adds submarine gameplay, espionage, underwater combat, higher tech level. The "endgame" era.

### Campaign Era Support

The engine must support campaigns in ANY of these eras:

| Era | Surface Ships | Submarines | Air | Tech Level |
|-----|--------------|------------|-----|------------|
| Long Fall (2030s-40s) | Modern → Sail | No | Failing | High → Low |
| Age of Sail (2050s-70s) | Advanced Sail | No | None | Medium |
| Abyssal War (2080s+) | Advanced Sail | Yes | None | High (specialized) |

---

## 6. Canonical Characters

### Player vs. Canonical Characters

**Critical Design Decision:** The player is NOT Corbin Shaw. The player creates their OWN captain through the Oracle system. Canonical characters from the novels serve as:

| Role | Examples | Function |
|------|----------|----------|
| **First Mate / Partner** | Corbin Shaw (in Abyssal War era) | Your right hand, advisor, combat companion |
| **Mentor** | Dr. Thorne, Elder Shaw | Teach skills, provide quests, exposition |
| **Rival** | Other captains, Valerius | Competition, antagonism, possible ally |
| **Quest Giver** | Maria Flores, faction leaders | Drive story forward |
| **Legend** | Shaw (in post-Shaw eras) | Historical figure, inspiration |

This means:
- YOU are the main character of your story
- The novels' heroes are part of YOUR crew or YOUR world
- Different campaigns can position Shaw differently (young rival, peer captain, legendary mentor, historical figure)

### Corbin Shaw - "Nemo"
*The Legend of the Saga*

| Era | Role IN GAME | Relationship to Player |
|-----|--------------|----------------------|
| Long Fall | Young survivor | Fellow refugee, possible crew |
| Age of Sail | Captain of *Stargazer* | Rival captain, ally, mentor |
| Abyssal War | Founder of Confederation | **First Mate**, legendary partner |
| Post-Shaw | Historical legend | Statue in port, stories told |

**In the Base Game (Abyssal War ~2085):** Shaw is your First Mate. He's old, legendary, and has seen everything. He joins YOUR ship because he sees something in you. His experience complements your fresh perspective. He's the Obi-Wan to your Luke, the Spock to your Kirk.

**Character Arc (in novels):** Humble worker → Reluctant survivor → Pirate captain "Nemo" → Founder of a nation → Elder watching his legacy tested

**Virtues:** Honor, Sacrifice, Courage
**Legendary Ship:** *Stargazer* (may be encountered as NPC ship, or inherited in some campaigns)

### Dr. Aris Thorne
*The Cassandra*

Climatologist who predicted the Atmospheric Shift. Dismissed as alarmist until proven right. Retreats to Caribbean, becomes mentor figure.

**Role in Game:** Potential mentor character, source of scientific knowledge, connection to "why" the world changed.

### Maria Flores
*The Pragmatist*

Energy commodities trader who saw the collapse coming. Lost her paper wealth, retained her strategic mind. Helps organize post-collapse communities.

**Role in Game:** Economic advisor, quest giver for trade/intelligence missions.

### Marcus Valerius
*The Corporate Antagonist*

AetherCorp security chief who rose through the chaos. Ruthless, brilliant, believes order must be imposed. Not a cackling villain - genuinely believes corporate control is humanity's best chance.

**Role in Game:** Primary antagonist for Age of Sail and Abyssal War campaigns. Complex motivations.

### Sofia
*The Next Generation*

Daughter of an AetherCorp scientist rescued by Shaw. Raised in the Confederation, becomes the architect of the Kraken submarine fleet. Represents the future.

**Role in Game:** Key character in Abyssal War campaigns. Engineering/tech specialist.

---

## 7. Factions & Powers

### AetherCorp
*The New Empire*

- **Type:** Mega-corporation
- **Controls:** Grav-Coral harvesting, Aetherium refining, Treasure Fleets
- **Military:** Corporate navy, hunter-killer drones, advanced sensors
- **Philosophy:** Order through control, efficiency over freedom
- **Headquarters:** Yucatan coast (processing facilities)

**Game Role:** Primary antagonist faction. Their ships are tough, their resources vast, but they're not everywhere.

### The Trident Confederation
*The Pirate Nation*

- **Type:** Loose federation of pirate captains
- **Controls:** Hidden coves, trade routes, "The Code"
- **Military:** Fast raiders, boarding specialists, guerilla tactics
- **Philosophy:** Freedom, the Code, strength through unity
- **Headquarters:** Hidden - various pirate havens

**Game Role:** Can be allied with, joined, or opposed. Player may help form it (Book 6 campaign).

### The Free Ports
*The Neutral Ground*

- **Type:** Independent city-states
- **Cities:** Nassau, Kingston, Havana (rebuilt), Port-Royal
- **Controls:** Trade, information, repair facilities
- **Military:** Harbor defense only
- **Philosophy:** Neutrality, commerce above all

**Game Role:** Safe havens, trade hubs, quest sources. Attacking a Free Port turns everyone against you.

### Resource Nations
*The Desperate Powers*

- **Type:** Island nations controlling key resources
- **Examples:** Bahamas Vita-Algae Syndicate, Hispaniola CanaFiber Cooperative
- **Controls:** Specific commodity production
- **Military:** Defensive fleets, hire protection
- **Philosophy:** Survival through leverage

**Game Role:** Employers, clients, faction to protect or exploit.

### The Drowned
*The Wild Card*

- **Type:** Religious cult
- **Beliefs:** The sea changed for a reason; humanity must adapt or perish
- **Controls:** Unknown - they appear and disappear
- **Military:** Strange ships, fanatical crews, possible "gifts" from the deep
- **Philosophy:** Embrace the change, reject the old world

**Game Role:** Mysterious antagonists, possible allies for those willing to go dark. Source of weird tech/abilities.

### The Three Treasures (Economic Foundation)

| Resource | Historical Parallel | Source | Use |
|----------|-------------------|--------|-----|
| **Aetherium Fuel** | Gold | Grav-Coral (Yucatan reefs) | Power source, highest value |
| **Vita-Algae** | Sugar | Caribbean shallows | Pharmaceuticals, lubricants |
| **CanaFiber** | Cotton | Cuba, Hispaniola | Textiles, composites, bulk cargo |

**Why Sea Transport:**
- Grav-Coral is too heavy to fly
- Aetherium Fuel is too volatile for unstable atmosphere
- CanaFiber is too bulky
- Air travel is impossible regardless

---

# PART III: ENGINE SYSTEMS

These sections describe the **systems** the engine must provide. They are content-agnostic - they work regardless of what campaign is loaded.

---

## 8. Game Structure

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

## 9. The Ship System

### The Ship as Character

**The ship is not a vehicle. The ship is a character.**

Like the Enterprise in Star Trek, the Edelweiss in Valkyria Chronicles, or the Normandy in Mass Effect - your ship has:
- A **name** and **identity**
- A **history** that grows with you
- **Personality** expressed through customization
- **Capabilities** that define your playstyle
- **Crew** who call it home
- **Scars** from battles survived
- **Trophies** from victories won

The ship is your second protagonist. When people speak of you, they speak of your ship.

### Ship Philosophy: Three Archetypes

Ships aren't just stat blocks - they represent playstyles:

| Archetype | Philosophy | Strengths | Weaknesses |
|-----------|------------|-----------|------------|
| **Raider** | Strike fast, vanish | Speed, boarding, escape | Fragile, small cargo |
| **Trader** | Profit over glory | Cargo, range, endurance | Slow, weak weapons |
| **Warship** | Dominate the seas | Firepower, armor, crew | Slow, expensive, conspicuous |

Every ship falls somewhere on this triangle. Customization moves you around it.

### Ship Statistics (Core)

| Stat | Description | Affects |
|------|-------------|---------|
| **Hull Integrity** | Health pool. Your ship's life. | Survival, repair costs |
| **Hull Class** | Size category (Sloop → Galleon) | Everything - base for all stats |
| **Speed** | Base movement in hexes | Strategic travel, combat initiative |
| **Maneuverability** | Turning, evasion | Combat positioning, escape |
| **Cargo Capacity** | Tons of goods carried | Trade profit, mission loot |
| **Crew Capacity** | Sailors + Bridge Crew | Combat effectiveness, operations |
| **Armament Slots** | Weapon hardpoints (fore/aft/broadside) | Combat loadout options |
| **System Slots** | Internal module capacity | Customization depth |
| **Aetherium Tank** | Fuel for special systems | Burst speed, special weapons |
| **Stealth Profile** | How visible/detectable | Avoiding encounters, ambush |

### Ship Statistics (Derived)

| Stat | Derived From | Description |
|------|--------------|-------------|
| **Combat Rating** | Weapons + Crew + Hull | Overall threat assessment |
| **Trade Value** | Cargo + Speed + Range | Profit potential per voyage |
| **Boarding Strength** | Crew + Equipment + Ship design | Effectiveness at taking ships |
| **Survivability** | Hull + Armor + Escape options | Chance of surviving losing fight |

### Hull Classes

Ships come in distinct size classes. You can upgrade WITHIN a class extensively, but changing CLASS requires a new ship.

| Class | Size | Crew | Cargo | Weapons | Role |
|-------|------|------|-------|---------|------|
| **Skiff** | Tiny | 5-10 | 5t | 1 | Scout, courier, escape craft |
| **Sloop** | Small | 15-30 | 20t | 2-3 | Raider, fast trader |
| **Brigantine** | Medium | 40-60 | 50t | 4-5 | Balanced, versatile |
| **Schooner** | Medium | 50-80 | 80t | 4-6 | Fast trader, light combat |
| **Frigate** | Large | 80-120 | 60t | 6-8 | Warship, escort |
| **Galleon** | Huge | 150-250 | 200t | 8-12 | Heavy warship, bulk trader |
| **Catamaran** | Medium | 40-70 | 100t | 4-6 | Fast, stable, modern design |
| **Submarine** | Medium | 30-50 | 40t | 4 (torpedoes) | Stealth, special missions (Abyssal era) |

### Ship Acquisition

How you get ships:

| Method | Description | Ship Condition |
|--------|-------------|----------------|
| **Purchase** | Buy from shipyard | New or refurbished, clean history |
| **Capture** | Board and take enemy ship | Damaged, has history (good or bad) |
| **Commission** | Order custom-built | Exactly to spec, expensive, takes time |
| **Inherit** | Story/quest reward | Often legendary, comes with expectations |
| **Salvage** | Find derelict | Heavily damaged, cheap/free, mysterious |

**Ship History:** Captured and salvaged ships come with HISTORY. A ship that belonged to a famous pirate carries their reputation. A ship that massacred innocents is cursed in sailors' minds. This affects crew morale and NPC reactions.

### Modular Ship Systems

Every ship has SLOTS for modular systems. Larger ships have more slots.

#### Propulsion Systems
| System | Effect | Notes |
|--------|--------|-------|
| **Standard Sails** | Base speed | Default |
| **Racing Sails** | +20% speed, -10% durability | Fast but fragile |
| **Storm Sails** | -10% speed, weather immunity | Never shredded by storms |
| **Automated Rigging** | +10% speed, -2 crew requirement | High-tech, expensive |
| **Aetherium Booster** | Emergency +50% speed (burns fuel) | Escape or pursuit |

#### Armor & Defense
| System | Effect | Notes |
|--------|--------|-------|
| **No Armor** | Base hull only | Lightest, fastest |
| **Wooden Reinforcement** | +15% hull, -5% speed | Cheap, available everywhere |
| **Iron Plating** | +30% hull, -15% speed | Heavy but tough |
| **Composite Armor** | +25% hull, -5% speed | Modern, expensive |
| **Reactive Plating** | +20% hull, -50% damage from first hit | High-tech |

#### Weapons (Hardpoint Systems)
| Weapon | Range | Damage | Special |
|--------|-------|--------|---------|
| **Cannons** | Medium | Medium | Reliable, cheap ammo |
| **Carronades** | Short | Very High | Devastating broadside |
| **Long Guns** | Long | Medium | Snipe before engagement |
| **Railgun** | Long | High | Aetherium-powered, piercing |
| **Chain Shot** | Medium | Low | Destroys sails, cripples speed |
| **Grapeshot** | Short | Low (hull), High (crew) | Anti-boarding prep |
| **Harpoon Launcher** | Medium | Low | Grapple for boarding |
| **Torpedo Tubes** | Long | Very High | Submarine only, limited ammo |

#### Internal Systems
| System | Slot Cost | Effect |
|--------|-----------|--------|
| **Extended Cargo Hold** | 2 | +50% cargo capacity |
| **Armory** | 1 | +20% boarding damage |
| **Medical Bay** | 1 | Crew heals faster, revive downed |
| **Brig** | 1 | Hold prisoners for ransom |
| **Navigator's Station** | 1 | +10% speed, see weather further |
| **Hidden Compartments** | 1 | Smuggle contraband, hide from searches |
| **Reinforced Powder Magazine** | 1 | Immune to ammo explosion crits |
| **Luxury Quarters** | 2 | +crew morale, recruit better officers |
| **Workshop** | 2 | Craft/repair items at sea |
| **Diving Bell** | 2 | Access underwater salvage sites |
| **Aetherium Refinery** | 3 | Convert raw Grav-Coral to fuel (rare) |

### Ship Customization (Visual)

Your ship's appearance tells your story:

**Hull Appearance:**
- Paint scheme (colors, patterns)
- Battle damage (scars from major fights - optional to repair or keep)
- Weathering (pristine, seasoned, battered)
- Modifications (visible armor plating, extra rigging)

**Figurehead:**
- Unlocked through achievements, purchases, quests
- Examples: Sea serpent, Mermaid, Storm goddess, Abstract prow
- Some have minor gameplay effects (+1 crew morale, etc.)

**Sails:**
- Colors and patterns
- Symbol/sigil (faction, personal, intimidating)
- Quality appearance (new, patched, ragged)

**Flags:**
- National/faction flag
- Personal standard
- Signal flags
- The black flag (declare piracy)

**Trophies:**
- Mounted on deck/hull
- Taken from defeated enemies
- Examples: Enemy figurehead, captured banner, mounted weapon
- Each tells a story; crew and NPCs comment on them

### Ship Naming & Legend

**Ship Names:**
- You name your ship at acquisition
- Can rename at significant moments (major refit, new era of your career)
- Name appears in game UI, dialogue, and world

**Ship Reputation:**
Ships build reputation independent of (but linked to) captain:

| Reputation Type | How Earned | Effect |
|-----------------|------------|--------|
| **Feared** | Victories, destruction | Enemies may flee or surrender |
| **Respected** | Fair dealing, skill | Better recruitment, faction standing |
| **Cursed** | Atrocities, betrayals | Crew superstitious, hard to recruit |
| **Lucky** | Surviving impossible odds | Crew morale bonus |
| **Legendary** | Achieving great feats | Everyone knows your ship |

**Famous Ships in Lore:**
- *Stargazer* (Corbin Shaw's legendary vessel)
- *ACS Sovereign* (AetherCorp flagship)
- *Void Kraken* (First Confederation submarine)

### Ship Death & Legacy

When your ship is destroyed:
- Dramatic sinking scene
- Surviving crew evacuate (some may die)
- You lose the ship and its customization
- Any trophies, history, reputation - gone
- Must acquire a new ship (captured, purchased, provided by allies)

**The loss of a beloved ship should HURT.** It's losing a character.

### Ship as Home (The Management Layer)

Between missions, you manage your ship:

```
┌─────────────────────────────────────────────────────────────┐
│                    THE SHIP SCREEN                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   BRIDGE    │  │    DECK     │  │   CARGO     │         │
│  │             │  │             │  │             │         │
│  │ Navigation  │  │ Weapons     │  │ Trade goods │         │
│  │ Ship status │  │ Crew posts  │  │ Supplies    │         │
│  │ World map   │  │ Trophies    │  │ Contraband  │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   QUARTERS  │  │  MEDICAL    │  │  WORKSHOP   │         │
│  │             │  │             │  │             │         │
│  │ Crew roster │  │ Injured     │  │ Upgrades    │         │
│  │ Morale      │  │ Treatment   │  │ Repairs     │         │
│  │ Assignments │  │ Supplies    │  │ Crafting    │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Each location on the ship is a management interface:**
- **Bridge:** Navigation, ship status, strategic decisions
- **Deck:** Weapon loadout, crew assignments, visual customization
- **Cargo Hold:** Inventory, trade goods, smuggling
- **Crew Quarters:** Crew management, morale, relationships
- **Medical Bay:** Heal injured, manage supplies
- **Workshop:** Upgrades, repairs, crafting (if equipped)

### Ship Progression

Your ship grows with you:

| Progression Type | How It Works |
|------------------|--------------|
| **Upgrades** | Purchase/install better systems |
| **Repairs** | Fix damage, can choose to keep scars |
| **Refit** | Major overhaul - change multiple systems at once |
| **Capture Better Ship** | Transfer crew to captured vessel |
| **Commission New Ship** | Order custom-built at major shipyard |
| **Legendary Status** | After major achievements, ship becomes "known" |

### Ship Data Definition (For Modding)

Ships are defined in JSON:

```json
{
  "id": "ship_brigantine_standard",
  "name": "Brigantine",
  "class": "medium",
  "archetype_bias": { "raider": 0.3, "trader": 0.4, "warship": 0.3 },

  "base_stats": {
    "hull": 200,
    "speed": 8,
    "maneuverability": 6,
    "cargo": 50,
    "crew_min": 25,
    "crew_max": 60,
    "stealth": 4
  },

  "slots": {
    "weapon_fore": 1,
    "weapon_broadside": 2,
    "weapon_aft": 1,
    "system": 4
  },

  "default_systems": [
    "sails_standard",
    "armor_wooden_light"
  ],

  "visuals": {
    "model": "models/ships/brigantine.glb",
    "figurehead_slot": true,
    "sail_slots": 2,
    "trophy_slots": 3
  },

  "cost": 15000,
  "availability": ["free_ports", "aethercorp_yards"],
  "era": ["age_of_sail", "abyssal_war"]
}

---

## 10. Captain Creation

### The Oracle's Cards (Ultima IV Inspiration)

Character creation happens through an in-world narrative framing: **The Oracle of Nassau** - an old woman who reads the future in cards. She asks you questions, and your answers determine who you are.

### The Eight Virtues of the Sea

Your answers weight eight core virtues. Your highest virtues define your starting bonuses and available dialogue options throughout the game.

| Virtue | Description | Mechanical Benefit |
|--------|-------------|-------------------|
| **Courage** | Facing danger without flinching | +Combat initiative, boarding bonuses |
| **Compassion** | Caring for crew and captives | +Crew morale, healing effectiveness |
| **Honor** | Keeping your word, fair dealing | +Faction reputation gains, parley success |
| **Justice** | Punishing the guilty, protecting innocent | +Damage vs. pirates/slavers, defense bonuses |
| **Sacrifice** | Giving up gain for others | +Crew loyalty, reduced desertion |
| **Honesty** | Speaking truth, keeping promises | +Trade prices, information quality |
| **Spirituality** | Connection to the changed world | +Aetherium efficiency, navigation bonuses |
| **Humility** | Knowing your limits, learning from others | +XP gain, crew skill sharing |

### The Seven Dilemmas

The Oracle presents seven scenarios, each forcing a choice between two virtues. There are no wrong answers - only different paths.

**Example Dilemmas:**

**Dilemma 1: The Burning Ship**
> *"A merchant vessel burns on the horizon. Her crew calls for rescue, but storm clouds gather. Do you..."*
> - **A)** Risk the storm to save them all *(Courage vs. Prudence)*
> - **B)** Save who you can before the storm hits *(Compassion vs. Courage)*

**Dilemma 2: The Prisoner's Plea**
> *"You've captured a corporate officer. He offers secrets for his freedom. Your crew demands justice for fallen comrades. Do you..."*
> - **A)** Honor your crew's wishes *(Justice vs. Pragmatism)*
> - **B)** Take the deal - information saves lives *(Honesty vs. Justice)*

**Dilemma 3: The Stolen Medicine**
> *"A dying child needs Vita-Algae you promised to deliver elsewhere. The buyer is powerful and expects their cargo. Do you..."*
> - **A)** Keep your contract *(Honor vs. Compassion)*
> - **B)** Save the child *(Compassion vs. Honor)*

**Dilemma 4: The Informant**
> *"A crew member has been selling information to AetherCorp. Confronted, they weep - their family is hostage. Do you..."*
> - **A)** Execute them as a traitor *(Justice vs. Compassion)*
> - **B)** Forgive and plan a rescue *(Compassion vs. Security)*

**Dilemma 5: The Rival Captain**
> *"Your greatest rival lies wounded on a captured deck. Killing them ends a threat forever. Sparing them... they might change. Do you..."*
> - **A)** End it now *(Pragmatism vs. Mercy)*
> - **B)** Show mercy *(Humility vs. Justice)*

**Dilemma 6: The Secret Cargo**
> *"Your hold contains weapons bound for rebels fighting AetherCorp. A Free Port inspector boards. Do you..."*
> - **A)** Lie to protect the cause *(Sacrifice vs. Honesty)*
> - **B)** Confess and face consequences *(Honesty vs. Sacrifice)*

**Dilemma 7: The Final Question**
> *"The sea has taken everything from you once. It may again. Why do you sail?"*
> - **A)** For wealth and freedom *(Ambition)*
> - **B)** For revenge against those who wronged me *(Justice)*
> - **C)** To protect those who cannot protect themselves *(Compassion)*
> - **D)** To find the truth the corporations hide *(Spirituality)*

### Virtue Scores & Starting Profile

After all dilemmas:
- Two highest virtues become your **Defining Virtues** (visible to NPCs)
- These unlock unique dialogue options throughout the game
- Starting crew members have compatible/conflicting virtues
- Some story paths require specific virtue thresholds

**Example Starting Profiles:**

| Profile | Primary Virtues | Starting Bonus | Ship Name Suggestion |
|---------|-----------------|----------------|----------------------|
| **The Protector** | Compassion + Sacrifice | +1 Surgeon crew, healing items | *Mercy's Hand* |
| **The Avenger** | Justice + Courage | +1 Cutlass crew, weapon upgrade | *Retribution* |
| **The Seeker** | Spirituality + Honesty | +Navigator, chart of hidden location | *Truth's Wake* |
| **The Merchant** | Humility + Honor | +Starting doubloons, trade contacts | *Fair Dealing* |

### Visual Character Creation

After the Oracle determines your virtues, you customize appearance:

**Basic Customization:**
- Gender (affects nothing mechanically)
- Skin tone
- Hair style/color
- Facial features
- Scars/tattoos (can add more as game progresses)
- Voice set (for combat barks)

**Name:**
- Enter captain's name
- Title options unlock based on virtue profile

### The Captain as a Unit

Your captain is a Bridge Crew member who:
- **Cannot die** (defeat = captured, rescue mission required)
- Starts at Level 5 (ahead of other crew)
- Has unique **Captain-only abilities** based on virtues
- Must participate in boarding actions (can sit out land missions)
- Virtue scores can shift based on in-game choices

### Captain-Only Abilities (Examples)

| Virtue | Ability | Effect |
|--------|---------|--------|
| **Courage** | *Into the Breach* | Captain boards first, grants +2 initiative to party |
| **Compassion** | *Rally the Fallen* | Revive downed ally once per mission |
| **Honor** | *Parley* | Force conversation before combat (chance to avoid fight) |
| **Justice** | *Mark for Death* | Target enemy takes +50% damage from all sources |
| **Sacrifice** | *Take the Blow* | Intercept attack meant for adjacent ally |
| **Honesty** | *Read Intentions* | See enemy planned actions for one turn |
| **Spirituality** | *Sea's Blessing* | Party gains +20% accuracy for 3 turns |
| **Humility** | *Learn from Failure* | Missed attacks grant +10% to next attack |

---

## 11. Crew System

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

## 12. Job & Ability System

### FFT-Inspired Multi-Job System

Every Bridge Crew member can learn multiple jobs. Unlike simple class systems, abilities learned in one job can be equipped while using another job. This creates deep character customization and encourages experimentation.

### Job Basics

**Active Job:** Your current combat role, determines:
- Base stats (HP, MP, Speed, etc.)
- Equipment options
- Innate abilities
- Which ability set is "primary"

**Job Level:** Each job levels independently (1-20)
- Gain Job XP when using that job in combat
- Higher job levels unlock more abilities
- Mastering a job (Level 20) grants a permanent bonus

**Job Points (JP):** Earned in combat, spent to learn abilities
- JP earned goes to your active job
- More JP for kills, objectives, and skillful play

### The Job Tree

Jobs are organized into tiers. Higher-tier jobs require mastery of prerequisite jobs.

```
                    ┌─────────────────┐
                    │   TIER 3 JOBS   │
                    │  (Requires 2    │
                    │   Tier 2 jobs)  │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
    ┌────▼────┐        ┌────▼────┐        ┌────▼────┐
    │ Tempest │        │Surgeon  │        │ Shadow  │
    │ Captain │        │ General │        │ Admiral │
    └────┬────┘        └────┬────┘        └────┬────┘
         │                   │                   │
    ┌────┴────┐        ┌────┴────┐        ┌────┴────┐
    │Corsair +│        │Surgeon +│        │Sapper + │
    │Marksman │        │Navigator│        │Corsair  │
    └─────────┘        └─────────┘        └─────────┘
         │                   │                   │
                    ┌────────┴────────┐
                    │   TIER 2 JOBS   │
                    │ (Requires 1     │
                    │  Tier 1 job)    │
                    └────────┬────────┘
                             │
    ┌──────────┬──────────┬──┴───┬──────────┬──────────┐
    │          │          │      │          │          │
┌───▼───┐ ┌───▼───┐ ┌───▼───┐ ┌▼────┐ ┌───▼───┐ ┌───▼────┐
│Corsair│ │Sapper │ │Surgeon│ │Navi-│ │Quarter│ │Gunner  │
│       │ │       │ │       │ │gator│ │master │ │        │
└───┬───┘ └───┬───┘ └───┬───┘ └──┬──┘ └───┬───┘ └───┬────┘
    │         │         │        │        │         │
    └─────────┴─────────┴────┬───┴────────┴─────────┘
                             │
                    ┌────────┴────────┐
                    │   TIER 1 JOBS   │
                    │ (Starting jobs) │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
    ┌────▼────┐        ┌────▼────┐        ┌────▼────┐
    │ Cutlass │        │ Marksman│        │ Sailor  │
    │(Melee)  │        │ (Ranged)│        │(Support)│
    └─────────┘        └─────────┘        └─────────┘
```

### Tier 1 Jobs (Starting)

Every crew member starts with access to these three jobs:

#### Cutlass (Melee Fighter)
| Stat | Value | Description |
|------|-------|-------------|
| HP | High | Frontline durability |
| MP | Low | Limited ability use |
| Speed | Medium | Balanced initiative |
| Move | 4 | Standard mobility |
| Jump | 2 | Can climb moderate terrain |

**Innate:** *Close Quarters* - +15% damage in melee range

**Learnable Abilities:**
| Ability | JP Cost | Effect |
|---------|---------|--------|
| Slash | 50 | Basic melee attack |
| Riposte | 100 | Counter next melee attack |
| Cleave | 150 | Hit two adjacent enemies |
| Intimidate | 200 | Lower enemy attack for 3 turns |
| Blade Dance | 300 | Attack all adjacent enemies |
| Armor Break | 400 | Reduce enemy defense permanently |
| Killing Blow | 600 | 2x damage on targets below 25% HP |
| **MASTERY: Steel Skin** | - | +10% physical defense (permanent, all jobs) |

#### Marksman (Ranged Fighter)
| Stat | Value | Description |
|------|-------|-------------|
| HP | Low | Fragile |
| MP | Medium | Moderate ability use |
| Speed | High | Acts early |
| Move | 3 | Below average mobility |
| Jump | 1 | Prefers flat ground |

**Innate:** *Eagle Eye* - +1 range to all ranged attacks

**Learnable Abilities:**
| Ability | JP Cost | Effect |
|---------|---------|--------|
| Aimed Shot | 50 | Basic ranged attack |
| Overwatch | 100 | Shoot first enemy that moves |
| Headshot | 200 | +50% crit chance, -20% accuracy |
| Suppressing Fire | 250 | Enemy can't move next turn |
| Piercing Round | 350 | Ignores 50% armor |
| Double Tap | 450 | Two attacks at -25% damage each |
| Kill Zone | 600 | Overwatch triggers twice |
| **MASTERY: Keen Senses** | - | +5% accuracy (permanent, all jobs) |

#### Sailor (Support)
| Stat | Value | Description |
|------|-------|-------------|
| HP | Medium | Moderate durability |
| MP | High | Lots of ability use |
| Speed | Medium | Balanced initiative |
| Move | 4 | Standard mobility |
| Jump | 3 | Good vertical movement |

**Innate:** *Sea Legs* - Immune to movement penalties from terrain

**Learnable Abilities:**
| Ability | JP Cost | Effect |
|---------|---------|--------|
| First Aid | 50 | Heal ally 25% HP |
| Rally | 100 | Remove fear/debuffs from ally |
| Throw Rope | 150 | Pull ally to your position |
| Brace | 200 | Ally gains +25% defense for 1 turn |
| Quick Patch | 300 | Heal self 50% HP (once per battle) |
| Inspire | 400 | All allies gain +1 AP this turn |
| Lifeline | 500 | Prevent one death this battle |
| **MASTERY: Endurance** | - | +10% max HP (permanent, all jobs) |

### Tier 2 Jobs

Unlock by reaching Job Level 8 in a Tier 1 job.

#### Corsair (Requires: Cutlass 8)
Fast, dual-wielding skirmisher.

**Innate:** *Fleet of Foot* - Can move after attacking

**Key Abilities:**
- *Shadowstep* - Teleport behind target
- *Twin Fangs* - Two weapon attack
- *Evasion* - 50% chance to dodge next attack
- *Ambush* - +100% damage from stealth

#### Sapper (Requires: Marksman 8)
Demolitions and tech specialist.

**Innate:** *Saboteur* - Attacks can target objects and cover

**Key Abilities:**
- *Plant Explosive* - Delayed area damage
- *Disable Trap* - Remove hazards
- *EMP Grenade* - Disable tech enemies
- *Breach Charge* - Destroy walls/doors

#### Surgeon (Requires: Sailor 8)
Advanced healing and revival.

**Innate:** *Triage* - Healing abilities +50% effective

**Key Abilities:**
- *Resuscitate* - Revive downed ally
- *Stimulant* - Ally acts again immediately
- *Anesthetic* - Enemy sleeps for 2 turns
- *Field Surgery* - Full heal, but ally can't act next turn

#### Navigator (Requires: Sailor 8)
Tactical support and buffs.

**Innate:** *Foresight* - See enemy movement range

**Key Abilities:**
- *Chart Course* - Ally ignores terrain for 1 turn
- *Predict Weather* - Party gains evasion
- *Coordinated Strike* - Next ally attack auto-crits
- *Retreat Signal* - All allies can move without provoking

#### Quartermaster (Requires: Cutlass 8 OR Sailor 8)
Item specialist and utility.

**Innate:** *Deep Pockets* - Carry +2 items

**Key Abilities:**
- *Resupply* - Grant item to ally
- *Jury Rig* - Repair equipment mid-battle
- *Caltrops* - Create difficult terrain
- *Emergency Rations* - Heal and cure status

#### Gunner (Requires: Marksman 8)
Heavy weapons specialist.

**Innate:** *Steady Aim* - No accuracy penalty at max range

**Key Abilities:**
- *Cannon Shot* - High damage, 2-turn cooldown
- *Grapeshot* - Cone area attack
- *Incendiary Round* - Target burns over time
- *Mortar Strike* - Indirect fire over obstacles

### Tier 3 Jobs

Unlock by reaching Job Level 12 in TWO Tier 2 jobs.

#### Tempest Captain (Requires: Corsair 12 + Marksman-line job 12)
The ultimate combat leader.

**Innate:** *Storm's Eye* - Immune to all status effects

**Key Abilities:**
- *Lightning Assault* - Move + Attack + Move in one action
- *Thunder Strike* - Guaranteed critical hit
- *Tempest Blade* - Attack all enemies in 2-hex radius
- *Captain's Fury* - Triple damage, costs all MP

#### Surgeon General (Requires: Surgeon 12 + Navigator 12)
Master of life and death.

**Innate:** *Miracle Worker* - Revived allies have 75% HP

**Key Abilities:**
- *Mass Heal* - Heal all allies in range
- *Phoenix Protocol* - Dead ally auto-revives once
- *Plague Doctor* - Enemies in range poisoned
- *Gift of Life* - Sacrifice HP to fully heal ally

#### Shadow Admiral (Requires: Sapper 12 + Corsair 12)
Stealth and assassination.

**Innate:** *Invisible* - Start each battle in stealth

**Key Abilities:**
- *Assassinate* - Kill target below 50% HP
- *Smoke Screen* - All allies gain stealth
- *Sabotage Ship* - In naval, disable enemy system
- *Ghost Fleet* - Summon decoy units

### The Ability System

#### Ability Slots

Each character has slots to equip learned abilities:

| Slot | Type | Description |
|------|------|-------------|
| **Primary** | Active | Your current job's abilities (automatic) |
| **Secondary** | Active | One other job's ability set |
| **Reaction** | Passive | Triggers on specific conditions (e.g., Counter) |
| **Support** | Passive | Always-on bonus (e.g., +10% HP) |
| **Movement** | Passive | Movement ability (e.g., Jump +2) |

#### Cross-Job Synergies

The depth comes from combining abilities across jobs:

**Example Build: "The Immortal"**
- Active Job: Cutlass (frontline durability)
- Secondary: Surgeon abilities (self-healing)
- Reaction: Riposte (from Cutlass)
- Support: Endurance (from Sailor mastery)
- Movement: Fleet of Foot (from Corsair)

**Example Build: "The Sniper"**
- Active Job: Marksman (ranged damage)
- Secondary: Navigator abilities (buffs)
- Reaction: Overwatch (from Marksman)
- Support: Keen Senses (from Marksman mastery)
- Movement: Shadowstep (from Corsair)

### Ability Learning & JP Economy

**Earning JP:**
| Action | JP Earned |
|--------|-----------|
| Kill enemy | 30 JP |
| Assist on kill | 15 JP |
| Complete objective | 50 JP |
| Critical hit | 5 JP bonus |
| Survive mission | 20 JP |

**JP Costs Scale:**
- Tier 1 abilities: 50-600 JP
- Tier 2 abilities: 150-800 JP
- Tier 3 abilities: 300-1200 JP
- Mastery bonuses: Automatic at Job Level 20

**Grinding Prevention:**
- JP gains diminish against much weaker enemies
- Story missions grant bonus JP
- Crew who don't participate get 25% JP (training)

---

## 13. Naval Combat Engine

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

## 14. Land Combat Engine

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

## 15. Economy Engine

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

## 16. Progression Engine

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

## 17. Campaign & Story Engine

The Campaign Engine is the system that turns raw gameplay into narrative experiences. It's entirely data-driven - the engine doesn't know what story it's telling.

### Campaign Structure

A campaign is a ZIP file containing all data needed for a complete game experience:

```
campaign-coral-crown.zip
├── campaign.json          # Campaign manifest
├── world/
│   ├── map.json           # Strategic map definition
│   ├── ports.json         # Port locations and data
│   ├── factions.json      # Faction definitions
│   └── regions.json       # Territory definitions
├── story/
│   ├── main_quest.json    # Main storyline
│   ├── side_quests/       # Optional content
│   └── events/            # Random events
├── characters/
│   ├── protagonist.json   # Player character (if fixed)
│   ├── crew/              # Recruitable characters
│   └── npcs/              # Non-playable characters
├── missions/
│   ├── naval/             # Naval combat scenarios
│   └── land/              # Land mission maps
├── dialogue/
│   └── *.json             # Dialogue trees
└── assets/
    └── ...                # Art, audio specific to campaign
```

### Campaign Manifest (campaign.json)

```json
{
  "id": "official.coral-crown",
  "name": "The Coral Crown",
  "description": "Book 4 of the Neo-Pirate Caribbean series",
  "version": "1.0.0",
  "author": "Official",

  "era": {
    "year_start": 2055,
    "year_end": 2060,
    "tech_level": "age_of_sail",
    "submarines_available": false
  },

  "protagonist": {
    "type": "fixed",
    "character_id": "corbin_shaw",
    "starting_ship": "stargazer_early"
  },

  "world": {
    "map": "caribbean_full",
    "starting_location": "barrier_island_haven",
    "factions_active": ["aethercorp", "free_ports", "pirates_unaligned"]
  },

  "victory_conditions": [
    { "type": "story_flag", "flag": "coral_crown_complete" }
  ],

  "dependencies": [],
  "incompatible_with": []
}
```

### Story State Machine

The engine tracks story state through:

**Flags:** Boolean markers (e.g., `"met_valerius": true`)
**Variables:** Numeric values (e.g., `"aethercorp_reputation": -50`)
**Quest States:** Per-quest progress tracking

### Quest Definition

```json
{
  "id": "quest_discover_grav_coral",
  "name": "The Heavy Reef",
  "type": "main",

  "trigger": {
    "type": "location",
    "location": "yucatan_channel",
    "conditions": [
      { "flag": "act1_complete", "value": true }
    ]
  },

  "stages": [
    {
      "id": "investigate",
      "description": "Investigate the strange readings",
      "objectives": [
        { "type": "reach_location", "location": "reef_alpha" },
        { "type": "dialogue", "npc": "maria_flores", "topic": "coral" }
      ],
      "on_complete": { "advance_to": "dive" }
    },
    {
      "id": "dive",
      "description": "Dive to the reef",
      "objectives": [
        { "type": "mission", "mission_id": "mission_coral_dive" }
      ],
      "on_complete": { "advance_to": "escape" }
    },
    {
      "id": "escape",
      "description": "Escape the AetherCorp patrol",
      "objectives": [
        { "type": "naval_combat", "escape": true },
        { "type": "reach_location", "location": "safe_haven" }
      ],
      "on_complete": {
        "set_flag": "discovered_grav_coral",
        "set_variable": { "aethercorp_reputation": -20 },
        "unlock_quest": "quest_the_engineer"
      }
    }
  ],

  "rewards": {
    "xp": 500,
    "items": ["grav_coral_sample"],
    "reputation": { "free_ports": 10 }
  }
}
```

### Event System

Random and triggered events that create emergent narrative:

```json
{
  "id": "event_storm_approaching",
  "type": "random",

  "conditions": {
    "location_type": "open_sea",
    "season": ["hurricane_season"],
    "probability": 0.15
  },

  "dialogue": "dialogue_storm_warning",

  "choices": [
    {
      "text": "Push through",
      "requirements": { "captain_virtue_courage": 40 },
      "outcome": {
        "type": "skill_check",
        "skill": "navigation",
        "success": { "continue": true, "bonus_xp": 100 },
        "failure": { "damage": "ship_medium", "crew_injury": 2 }
      }
    },
    {
      "text": "Seek shelter",
      "outcome": {
        "type": "delay",
        "days": 3,
        "continue": true
      }
    },
    {
      "text": "Turn back",
      "outcome": {
        "type": "return_to_port",
        "continue": true
      }
    }
  ]
}
```

### Dialogue System

Branching dialogue with conditions:

```json
{
  "id": "dialogue_maria_coral",
  "speaker": "maria_flores",
  "portrait": "maria_serious",

  "nodes": [
    {
      "id": "start",
      "text": "You found what? Corbin, do you have any idea what you're holding?",
      "responses": [
        {
          "text": "It's just coral, isn't it?",
          "next": "explain"
        },
        {
          "text": "I know exactly what it is. That's why I came to you.",
          "conditions": { "flag": "knows_about_aetherium" },
          "next": "business"
        }
      ]
    },
    {
      "id": "explain",
      "text": "That 'coral' is worth more than your ship. AetherCorp has killed for less. This is Grav-Coral - the source of Aetherium Fuel.",
      "effects": { "set_flag": "knows_about_aetherium" },
      "responses": [
        {
          "text": "Then we need to hide it.",
          "next": "hide"
        },
        {
          "text": "Or we need to sell it.",
          "next": "sell"
        }
      ]
    }
  ]
}
```

### Protagonist Modes

Campaigns can define protagonist handling:

| Mode | Description | Use Case |
|------|-------------|----------|
| **Fixed** | Player IS a specific character (Corbin Shaw) | Novel adaptations |
| **Created** | Player creates captain through Oracle system | Sandbox campaigns |
| **Selected** | Player chooses from roster | Multi-protagonist stories |

### Time & Pacing

The engine tracks time at multiple scales:

- **Strategic Time:** Days pass during travel
- **Tactical Time:** Turns during combat
- **Story Time:** Triggered by quest completion, not real time

Campaigns can define time pressure or allow unlimited exploration.

---

# PART IV: CONTENT & MODDING

---

## 18. Modding Architecture

### Design Philosophy

ABYSSAL TIDE is built from the ground up to be moddable. The community should be able to:
- Create new missions and campaigns
- Add new crew members, ships, and items
- Modify game balance
- Create total conversions

### Mod Structure

Mods are packaged as ZIP files with a standardized structure:

```
my-awesome-mod.zip
├── mod.json              # Mod manifest (required)
├── data/
│   ├── missions/         # Mission definitions
│   ├── characters/       # Crew member definitions
│   ├── jobs/             # Job/ability definitions
│   ├── items/            # Equipment definitions
│   ├── ships/            # Ship definitions
│   ├── dialogue/         # Dialogue trees
│   └── balance/          # Stat overrides
├── assets/
│   ├── portraits/        # Character art
│   ├── sprites/          # Unit sprites
│   ├── audio/            # Sound effects, music
│   └── models/           # 3D models (ships, etc.)
├── scripts/              # Custom C# scripts (sandboxed)
└── localization/         # Translation files
```

### Mod Manifest (mod.json)

```json
{
  "id": "com.modder.awesome-mod",
  "name": "Awesome Mod",
  "version": "1.0.0",
  "author": "ModderName",
  "description": "Adds new missions in the Northern Caribbean",
  "gameVersion": ">=1.0.0",
  "dependencies": [],
  "conflicts": [],
  "loadOrder": 100,
  "features": {
    "missions": true,
    "characters": true,
    "items": false,
    "scripts": false
  }
}
```

### Data Definition Format

All game data is defined in JSON or YAML for easy modding.

#### Mission Definition Example

```json
{
  "id": "mission_smugglers_cove",
  "type": "land_raid",
  "name": "Smuggler's Cove",
  "description": "A hidden cove harbors stolen Aetherium...",

  "requirements": {
    "minReputation": { "trident": 20 },
    "maxReputation": { "aethercorp": 50 },
    "flags": ["act1_complete"]
  },

  "location": {
    "region": "northern_caribbean",
    "terrain": "beach_jungle",
    "mapSize": [20, 20]
  },

  "deployment": {
    "minCrew": 3,
    "maxCrew": 5,
    "captainRequired": false
  },

  "objectives": {
    "primary": [
      { "type": "reach_zone", "zone": "cargo_area" },
      { "type": "interact", "target": "aetherium_cache" }
    ],
    "secondary": [
      { "type": "kill", "target": "smuggler_leader", "reward": { "xp": 100 } }
    ],
    "hidden": [
      { "type": "find", "target": "treasure_map", "reveals": "mission_treasure_island" }
    ]
  },

  "enemies": {
    "spawns": [
      { "type": "smuggler_grunt", "count": 6, "zone": "patrol_area" },
      { "type": "smuggler_leader", "count": 1, "zone": "command_tent" }
    ],
    "reinforcements": {
      "trigger": "alarm_raised",
      "spawns": [{ "type": "smuggler_grunt", "count": 4 }]
    }
  },

  "rewards": {
    "doubloons": 500,
    "items": ["aetherium_canister_x3"],
    "reputation": { "trident": 10, "free_ports": 5 }
  },

  "dialogue": {
    "intro": "dialogue_smugglers_intro",
    "success": "dialogue_smugglers_success",
    "failure": "dialogue_smugglers_failure"
  }
}
```

#### Character Definition Example

```json
{
  "id": "crew_maria_santos",
  "name": "Maria Santos",
  "title": "The Storm Singer",

  "portrait": "assets/portraits/maria_santos.png",
  "sprite": "assets/sprites/maria_santos.tres",

  "background": {
    "origin": "havana",
    "age": 28,
    "backstory": "Former AetherCorp navigator who defected after witnessing corporate atrocities..."
  },

  "startingJob": "navigator",
  "startingLevel": 3,
  "startingAbilities": ["chart_course", "predict_weather"],

  "stats": {
    "hp": 45,
    "mp": 30,
    "speed": 12,
    "move": 4
  },

  "virtues": {
    "honesty": 70,
    "compassion": 60,
    "justice": 50
  },

  "recruitment": {
    "type": "story",
    "mission": "mission_aethercorp_defector",
    "dialogue": "dialogue_maria_recruitment"
  },

  "loyaltyMission": {
    "id": "mission_maria_loyalty",
    "unlockRequirement": { "trust": 60 },
    "reward": { "ability": "storm_caller" }
  },

  "voiceSet": "voice_female_determined",

  "relationships": {
    "likes": ["crew_carlos_vega"],
    "dislikes": ["crew_corporate_defector_b"],
    "romantically_available": true
  }
}
```

#### Job/Ability Definition Example

```json
{
  "id": "job_storm_caller",
  "name": "Storm Caller",
  "tier": 3,
  "description": "Masters of weather manipulation",

  "requirements": {
    "jobs": [
      { "id": "navigator", "level": 12 },
      { "id": "sailor", "level": 12 }
    ]
  },

  "stats": {
    "hp": "medium",
    "mp": "very_high",
    "speed": "medium",
    "move": 4,
    "jump": 2
  },

  "innate": {
    "id": "weather_sense",
    "name": "Weather Sense",
    "description": "Always know weather effects; immune to weather damage"
  },

  "abilities": [
    {
      "id": "call_lightning",
      "name": "Call Lightning",
      "jpCost": 400,
      "mpCost": 15,
      "range": 5,
      "damage": { "base": 40, "type": "lightning" },
      "effects": ["stun_1_turn"],
      "description": "Strike target with lightning"
    },
    {
      "id": "fog_bank",
      "name": "Fog Bank",
      "jpCost": 300,
      "mpCost": 10,
      "areaOfEffect": { "type": "circle", "radius": 3 },
      "duration": 3,
      "effects": ["reduces_accuracy_50", "blocks_overwatch"],
      "description": "Create concealing fog"
    }
  ],

  "mastery": {
    "name": "Eye of the Storm",
    "description": "+15% all damage during storms (permanent)",
    "effect": { "type": "damage_bonus", "condition": "weather_storm", "value": 0.15 }
  }
}
```

### Modding API

#### Core Systems Exposed

| System | API Access | Capabilities |
|--------|------------|--------------|
| **Mission System** | Full | Create, trigger, modify missions |
| **Character System** | Full | Add crew, modify stats, relationships |
| **Job System** | Full | Add jobs, abilities, balance changes |
| **Item System** | Full | Add weapons, armor, consumables |
| **Dialogue System** | Full | Add dialogue trees, conditions |
| **Map Generation** | Partial | Custom terrain, but not core hex system |
| **Combat System** | Events Only | Hook into combat events, not modify core |
| **Save System** | Read Only | Query state, not modify saves |
| **UI System** | Limited | Add panels, not modify core UI |

#### Event Hooks

Mods can subscribe to game events:

```csharp
// Example mod script
public class MyMod : AbyssalTideMod
{
    public override void OnLoad()
    {
        // Subscribe to events
        Events.OnMissionComplete += HandleMissionComplete;
        Events.OnCrewRecruited += HandleRecruitment;
        Events.OnCombatStart += HandleCombatStart;
    }

    private void HandleMissionComplete(MissionCompleteEvent e)
    {
        if (e.MissionId == "mission_smugglers_cove")
        {
            // Trigger custom follow-up
            MissionManager.Unlock("mission_my_custom_followup");
        }
    }
}
```

#### Available Events

| Event | When Fired | Data Available |
|-------|------------|----------------|
| `OnGameStart` | New game begins | Captain data, starting conditions |
| `OnMissionStart` | Tactical mission begins | Mission ID, deployed crew |
| `OnMissionComplete` | Mission ends | Success/fail, casualties, rewards |
| `OnCombatStart` | Any combat begins | Combatants, terrain |
| `OnCombatEnd` | Combat resolves | Winner, survivors |
| `OnCrewRecruited` | New crew joins | Crew data |
| `OnCrewDeath` | Permadeath occurs | Crew data, killer, location |
| `OnLevelUp` | Crew gains level | Crew ID, new level, job |
| `OnAbilityLearned` | Ability purchased | Crew ID, ability ID |
| `OnDialogueChoice` | Player makes choice | Dialogue ID, choice index |
| `OnPortEntered` | Ship docks | Port ID, services available |
| `OnTradeComplete` | Buy/sell finishes | Items, prices, profit |
| `OnReputationChange` | Faction rep changes | Faction, old value, new value |

### Mod Manager

The game includes a built-in mod manager:

**Features:**
- Browse installed mods
- Enable/disable mods
- Adjust load order
- Check compatibility
- View mod details and credits
- One-click install from ZIP
- Steam Workshop integration (future)

**Load Order Resolution:**
1. Base game data loads first
2. Mods load in order by `loadOrder` value
3. Later mods override earlier mods
4. Conflicts are logged and reported

### Sandboxing & Security

**Script Sandboxing:**
- Mod scripts run in restricted environment
- No file system access outside mod folder
- No network access
- No reflection/unsafe code
- CPU time limits per frame

**Asset Validation:**
- All mod assets scanned for validity
- Size limits enforced
- Format requirements checked

**User Trust Levels:**
- **Safe Mods:** Data-only, no scripts
- **Trusted Mods:** Scripts from verified authors
- **Unverified Mods:** Warning displayed, user must approve

### Mod Development Tools

**Included with Game:**
- Mission Editor (visual tool)
- Character Creator
- Ability Balance Calculator
- JSON Schema files for validation
- Example mod templates

**Documentation:**
- Full API reference
- Tutorial: "Your First Mission"
- Tutorial: "Adding a New Job"
- Tutorial: "Creating a Crew Member"
- Best practices guide

### Steam Workshop (Future)

Planned features:
- Upload mods directly from game
- Subscribe and auto-download
- Ratings and reviews
- Collections/mod packs
- Automatic updates

---

## 19. Base Game Content

The base game ships with a single complete campaign to demonstrate all engine features. Additional campaigns (novel adaptations) release as official DLC/mods.

### Launch Campaign: "Abyssal Tide"

**Era:** Early Abyssal War (~2085)
**Protagonist Mode:** Created (Oracle system)
**Scope:** Full Caribbean, ~30 hours main story

**Premise:** You're a small-time captain who stumbles into the cold war between AetherCorp and the Trident Confederation. Both sides want you - as an asset or eliminated. Choose your path.

**Features Demonstrated:**
- All combat systems (naval, land, boarding)
- Full job system
- Ship upgrades
- Faction reputation
- Multiple endings

### Future Official Campaigns (DLC)

| Campaign | Novel | Era | Unique Features |
|----------|-------|-----|-----------------|
| The Coral Crown | Book 4 | 2055 | Play as young Corbin Shaw |
| The Broken Bridge | Book 5 | 2060s | Canal guerilla warfare |
| The Trident Pact | Book 6 | 2076 | Political/diplomatic focus |
| The Kraken Project | Book 7 | 2080s | Submarine introduction |
| The Serpent's Passage | Book 8 | 2090s | Full submarine gameplay |
| Abyssal Dawn | Book 9 | 2100 | Endgame content |

### Modding Community Vision

The tools and API we ship allow community to create:
- New campaigns (original stories)
- New time periods (pre-collapse, far future)
- Total conversions (different settings entirely)
- Additional content for official campaigns

---

## 20. Technical Requirements

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
| **Modding API** | Medium | High | Not started |
| **Mod Manager UI** | Low | Medium | Not started |
| **Character Creator** | High | Medium | Not started |

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

### Gameplay Influences
| Game | What We Take |
|------|--------------|
| **Ultima IV: Quest of the Avatar** | Virtue-based character creation, moral dilemmas shape identity |
| **Final Fantasy Tactics** | Deep job system, ability cross-pollination, JP economy |
| **Sid Meier's Pirates!** | Open world Caribbean, ship capture, reputation |
| **XCOM 1/2** | Tactical land combat, permadeath weight, base management |
| **Fire Emblem** | Character relationships, permadeath consequences, class progression |
| **Mass Effect** | Crew loyalty missions, dialogue importance, ship as home |
| **FTL** | Ship management under pressure, crew as resource |
| **Valkyria Chronicles** | Beautiful tactical battles, named squad members |

### Technical/Architecture Influences
| System | What We Take |
|--------|--------------|
| **Skyrim/Bethesda** | Moddable architecture, community content ecosystem |
| **Paradox Games** | Data-driven design, event systems, campaign mods |
| **Unity Asset Store Model** | Content as packages, mix official + community |
| **Rimworld** | Storyteller system, emergent narrative from systems |

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
| 0.2 | 2026-01-30 | Added Captain Creation (Ultima IV-style virtues), Job & Ability System (FFT-style), Modding Architecture |
| 0.3 | 2026-01-30 | Major restructure: Engine Philosophy, Canonical Lore (9-book novel series), Campaign & Story Engine, "Build the Engine, Not the Things" approach |

