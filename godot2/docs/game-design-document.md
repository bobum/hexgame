# ABYSSAL TIDE - Game Design Document

**Version:** 1.2 (Draft)
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
14. [Away Party System](#14-away-party-system)
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

### Unified World Map

The entire game takes place on **one seamless hex map** at a consistent scale. There is no "strategic map" vs "tactical map" - just one world where ships sail on water hexes and parties walk on land hexes.

**Single Scale Philosophy:**
```
┌─────────────────────────────────────────────────────────────┐
│  ONE MAP - ONE SCALE - SEAMLESS PLAY                        │
│                                                             │
│  • Ship sails on water hexes                                │
│  • Party walks on land hexes                                │
│  • Cities are building clusters you can see                 │
│  • No loading screens between naval and land                │
│  • No zoom levels or abstraction layers                     │
│  • What you see is where you are                            │
└─────────────────────────────────────────────────────────────┘
```

**Map Specifications:**
- Hex scale: ~50-100 meters per hex
- Full Caribbean: Thousands of hexes across multiple streaming regions
- Terrain: Ocean, coast, beach, jungle, urban, ruins, mountains
- Features: Buildings, docks, roads, rivers, vegetation, elevation

### Ocean Boundaries - Island-Based Regions

The Caribbean is too large for a single map. Instead, the world uses **Ocean Boundaries** where each region is an island (or island cluster) naturally separated by open ocean:

```
┌─────────────────────────────────────────────────────────────┐
│                    THE CARIBBEAN                            │
│                                                             │
│         ┌─────────┐                    ┌─────────┐         │
│         │ NASSAU  │                    │ PUERTO  │         │
│         │ (start) │                    │ RICO    │         │
│         └─────────┘                    └─────────┘         │
│    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~      │
│         ┌─────────────────┐       ┌─────────┐              │
│         │      CUBA       │       │ VIRGIN  │              │
│         │  (2-3 regions)  │       │ ISLANDS │              │
│         └─────────────────┘       └─────────┘              │
│    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~      │
│    ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐     │
│    │ YUCATAN │  │ JAMAICA │  │SHATTERED│  │ LESSER  │     │
│    │ COAST   │  │         │  │ ISLES   │  │ANTILLES │     │
│    └─────────┘  └─────────┘  └─────────┘  └─────────┘     │
│                                                             │
│  ~~~ = Open Ocean (strategic travel, not hex-by-hex)       │
│  Each island region: ~200x200 hexes (self-contained)       │
│  Only ONE region loaded at a time + Ocean Map overlay      │
└─────────────────────────────────────────────────────────────┘
```

**Why Ocean Boundaries?**

This approach eliminates cross-region complexity by design:

| Problem | Ocean Boundary Solution |
|---------|------------------------|
| Neighbor cells across regions | **Gone** - ocean hexes have no land neighbors to stitch |
| Cross-region pathfinding | **Gone** - A* stays within island, ship travel between |
| Roads crossing boundaries | **Gone** - roads don't cross ocean |
| Rivers crossing boundaries | **Gone** - rivers end at coastline |
| Seamless streaming complexity | **Gone** - explicit "sail to destination" transition |
| Rendering seams at borders | **Gone** - ocean IS the natural border |

**Two-Level System:**
1. **Ocean Map** (Strategic) - Low-res overview of entire Caribbean for inter-island navigation
2. **Island Region** (Tactical) - Full detail hex map for the current island, with existing LOD/chunking

**Region Data:**
- Each region is a self-contained island map file
- Contains: terrain, buildings, roads, spawn points, encounter zones
- NO edge stitching required - coastline is the natural boundary
- Procedural + hand-crafted hybrid (key locations authored, surroundings generated)

### Movement Modes

The player has three movement modes:

**Naval Mode - Coastal (Ship on Island Region):**
- Active when on water hexes within a loaded island region
- Ship token represents your vessel + full crew
- Hex-by-hex movement around the island
- Encounters: Coastal patrols, fishing boats, local pirates

**Shore Mode (Away Party as Unit):**
- Active when on land hexes
- Party token represents selected crew members
- Movement: Based on terrain, party composition
- Encounters: Combat, social, tech, medical (see Section 14)

**Naval Mode - Open Ocean (Strategic Travel):**
- Active when sailing between islands
- Uses simplified **Ocean Map** (not hex-by-hex)
- Ship travels along sea lanes between ports
- Encounters: Naval combat, storms, discoveries, random events
- Triggers region load/unload

**The Land Transition:**
```
SHIP on water hex adjacent to land
          │
          ▼
    "GO ASHORE?"
     [Yes] [No]
          │
          ▼
   SELECT AWAY PARTY
   (Choose crew, gear)
          │
          ▼
   PARTY TOKEN appears on beach hex
   Ship stays anchored (or crew left behind)
          │
          ▼
   Party moves on land hexes
   (Walk into town, explore, complete objectives)
          │
          ▼
   Return to ship hex → Automatically re-board
```

**The Sea Travel Transition (Between Islands):**
```
SHIP at port or open water
          │
          ▼
    "SET SAIL" → Opens OCEAN MAP
          │
          ▼
   ┌─────────────────────────────────────┐
   │  OCEAN MAP (Strategic View)         │
   │  ┌───┐                              │
   │  │YOU│ ═══════▶ [JAMAICA]           │
   │  └───┘    ↑                         │
   │      Sea Lane                       │
   │  Travel time: 3 days                │
   │  Danger level: Moderate             │
   │  [CONFIRM] [CANCEL]                 │
   └─────────────────────────────────────┘
          │
          ▼
   SEA TRAVEL SEQUENCE
   - Time passes (days)
   - Random encounters roll
   - Weather events
   - Crew management
          │
          ▼
   ARRIVE AT DESTINATION
   - Unload previous island region
   - Load new island region
   - Ship appears at destination port
```

### Time & Turns

**Time Passage:**
- Time advances as you move (1 hex = variable based on terrain/mode)
- Naval: ~2 hours per hex (wind-dependent)
- Land: ~30 minutes per hex
- Resting, trading, dialogue: Time costs vary

**Turn-Based vs Real-Time:**
- Movement: Real-time with pause option
- Encounters: Fully turn-based when triggered
- Naval Combat: Turn-based on same hex map
- No separate "battle map" - fight where you stand

### Encounters on the Map

Encounters trigger based on hex type and content:

| Hex Content | Trigger | Resolution |
|-------------|---------|------------|
| **Enemy Ship** | Enter same hex or adjacent | Naval combat or flee |
| **Port/Town** | Enter building hex | Social/trade interface |
| **Patrol** | Enter guarded hex (land) | Combat/stealth/social check |
| **Ruin** | Enter ruin hex | Exploration encounters |
| **Event Marker** | Enter marked hex | Story event triggers |
| **Hazard** | Enter hazard hex | Environmental check |

**Line of Sight:**
- You can SEE enemies before you reach them
- Plan routes to avoid or engage
- Fog of war for unexplored hexes
- Some enemies patrol (move on their own)

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

## 10. Captain Creation & The Prologue

### Philosophy: Character Creation IS Gameplay

Unlike traditional RPGs where you answer abstract questions, **ABYSSAL TIDE creates your character through play**. The prologue is simultaneously:

1. **Character creation** - Your choices determine your virtues
2. **Tutorial** - Each stage teaches a game mechanic
3. **Origin story** - You learn how you became a captain
4. **Relationship building** - You forge your bond with your First Mate

This is inspired by Ultima IV's gypsy, but instead of answering hypothetical questions, you LIVE the dilemmas with your friend at your side.

### Your Friend (The Future First Mate)

At the start of the game, you define two characters:

**Yourself:**
- Name
- Appearance (gender, face, hair, skin tone)
- Voice set

**Your Friend:**
- Name
- Appearance
- Personality type (affects their dialogue and reactions):
  - **The Conscience** - Questions ruthless choices, praises mercy
  - **The Pragmatist** - Questions idealism, praises practical choices
  - **The Loyalist** - Supports whatever you decide, emphasizes friendship

Your friend is not a blank slate - they have opinions. But they're YOUR friend. They stay with you regardless of your choices. Their reactions show you how the world might see you, but they never leave.

### The Eight Virtues

Your choices in the prologue build these virtue scores:

| Virtue | Description | Mechanical Benefit |
|--------|-------------|-------------------|
| **Courage** | Facing danger without flinching | +Combat initiative, boarding bonuses |
| **Compassion** | Caring for crew and captives | +Crew morale, healing effectiveness |
| **Honor** | Keeping your word, fair dealing | +Faction reputation gains, parley success |
| **Justice** | Punishing the guilty, protecting innocent | +Damage vs. criminals, defense bonuses |
| **Sacrifice** | Giving up gain for others | +Crew loyalty, reduced desertion |
| **Cunning** | Using wits over force | +Stealth, smuggling, escape success |
| **Ambition** | Reaching for more | +Trade profits, recruitment options |
| **Resilience** | Enduring hardship | +HP, survival, recovery speed |

### The Prologue: "Before the Storm"

**Setting:** You and your friend are crew on a merchant vessel called the *Wavecutter*. You're not captains - just skilled sailors trying to survive. Over eight stages, circumstances will transform you.

---

#### Stage 1: The Wreck (Tutorial: Salvage/Exploration)

*The Wavecutter investigates a sinking ship. You and your friend are sent to salvage what you can.*

**Situation:** Below decks, you find survivors trapped behind debris - but also a lockbox of valuable cargo. Water is rising. You can't save both.

**Friend:** "There's people back there... but that cargo could change our lives. Water's coming fast. What do we do?"

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Save the survivors | Leave the cargo, rescue the trapped sailors | **Compassion** |
| Take the cargo | Secure the valuables, the survivors are too far gone | **Ambition** |
| Try for both | Risky - might save both, might lose everything | **Courage** |

*[Teaches: Movement, interaction, time pressure, risk/reward]*

---

#### Stage 2: The Stowaway (Tutorial: Dialogue/Choice)

*Back on the Wavecutter, you discover a young stowaway hiding in the hold. A runaway from an AetherCorp labor platform. Corporate patrol boats are signaling to search your ship.*

**Friend:** "If they find her, we're all in chains. But if we turn her over..."

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Hide and protect her | Risk everything to shelter the child | **Sacrifice** |
| Turn her over | Corporate favor, no risk to ship | **Cunning** (self-preservation) |
| Help her escape | Lower a boat, give her a chance alone | **Compassion** |

*[Teaches: Dialogue trees, faction reputation consequences]*

---

#### Stage 3: The Storm (Tutorial: Ship Management)

*A massive storm hits. The captain freezes. Someone has to give orders or the ship goes down.*

**Friend:** "Captain's useless. Crew's looking at us. Do we take over or..."

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Take command | Step up, give orders, save the ship | **Courage** |
| Support the captain | Try to help them function, share the burden | **Honor** |
| Rally the crew | Get everyone working together, no single leader | **Humility** |

*[Teaches: Ship systems, crew management, emergency decisions]*

---

#### Stage 4: The Mutiny (Tutorial: Combat Basics)

*After the storm, tensions explode. A faction of the crew wants to take the ship. The captain is held at knifepoint. Violence is inevitable.*

**Friend:** "This is it. Which side are we on?"

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Defend the captain | Fight the mutineers, restore order | **Honor** |
| Join the mutiny | The captain nearly killed us all, time for change | **Justice** |
| Try to negotiate | Step between, find a middle ground | **Compassion** |

*[Teaches: Basic combat, positioning, resolving conflict]*

---

#### Stage 5: The Aftermath (Tutorial: Trade/Economy)

*The mutiny is over. The Wavecutter limps into a Free Port for repairs. You have cargo to sell, but the port is full of desperate refugees who need medicine you're carrying.*

**Friend:** "We could sell to the highest bidder. Or... those people need help. What do we do?"

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Sell to the highest bidder | Maximize profit, it's just business | **Ambition** |
| Give to the refugees | They need it more than we need money | **Sacrifice** |
| Sell at fair price to refugees | Balance compassion with practicality | **Honor** |

*[Teaches: Trading, economy, reputation effects]*

---

#### Stage 6: The Prisoner (Tutorial: Boarding/Tactical Combat)

*Pirates attack the Wavecutter. During the desperate fight, you capture their leader. He has information that could lead to their hidden base - and their treasure.*

**Friend:** "He knows where they keep their loot. But he won't talk easy. What do we do?"

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Interrogate harshly | Do what's necessary to get answers | **Cunning** |
| Offer a deal | His freedom for the information | **Cunning** |
| Hand him to authorities | Let the law handle it | **Justice** |
| Let him go | We're not torturers or jailers | **Compassion** |

*[Teaches: Boarding combat, prisoner mechanics, moral weight]*

---

#### Stage 7: The Captain Falls (Tutorial: Naval Combat)

*Another attack - AetherCorp this time, hunting the stowaway you helped (or the pirates seeking revenge, or authorities for your choices). In the battle, the captain is killed.*

**Friend:** "Captain's dead. Ship's in chaos. Crew's looking at YOU."

**[Naval combat plays out - player commands the defense]**

*After the battle, surviving crew gathers.*

| Choice | Action | Virtue Gained |
|--------|--------|---------------|
| Claim command | "I'll lead us. Follow me." | **Ambition** + **Courage** |
| Suggest your friend | "They should lead. I'll support them." | **Humility** |
| Call for a vote | "We decide together who leads." | **Justice** |

*Your friend refuses command if offered:* "No. I saw you in that fight. The crew saw you. This is YOUR ship now. I'm with you - but I'm not the captain. You are."

*[Teaches: Naval combat, command decisions]*

---

#### Stage 8: The Naming (Character Finalization)

*The Wavecutter is yours now. Battered, scarred, but seaworthy. Your friend stands beside you on the deck. The crew waits.*

**Friend:** "She's yours now. Ours. But she needs a new name - a new beginning. What do we call her?"

*[Player names the ship]*

**Friend:** "[Ship name]. I like it. So, Captain... where do we sail?"

**You:** *[Player chooses starting region on map]*

**Friend:** "Then let's see what's out there. Together."

*[End of Prologue - Game begins properly]*

---

### Virtue Results

After the prologue, your virtue scores are tallied:

- **Two highest virtues** become your **Defining Virtues**
- These unlock unique dialogue options throughout the game
- They affect how NPCs perceive you initially
- They determine your starting **Captain Ability**

**Example Profiles Based on Choices:**

| Profile | Primary Virtues | Captain Ability | Ship Name Tendency |
|---------|-----------------|-----------------|-------------------|
| **The Protector** | Compassion + Sacrifice | *Rally the Fallen* | Mercy-themed |
| **The Avenger** | Justice + Courage | *Mark for Death* | Vengeance-themed |
| **The Opportunist** | Ambition + Cunning | *Read the Angles* | Fortune-themed |
| **The Survivor** | Resilience + Courage | *Against All Odds* | Endurance-themed |

### Your First Mate

Your friend emerges from the prologue as your **First Mate** - not because you recruited them, but because you survived together. They know exactly who you are because they watched you become it.

**First Mate Role:**
- Permanent Bridge Crew member (cannot be dismissed)
- Cannot die (if defeated, captured - triggers rescue mission)
- Gives advice based on their personality type
- Comments on your choices, remembering the prologue
- Has their own skill progression and abilities
- Romance option (optional, player choice)

**First Mate in Combat:**
- Always available for deployment
- Specialization based on their personality type:
  - Conscience → Surgeon/Support
  - Pragmatist → Quartermaster/Utility
  - Loyalist → Player's choice during prologue

### The Captain as a Unit

Your captain is a Bridge Crew member who:
- **Cannot die** (defeat = captured, rescue mission required)
- Starts at Level 3 (after prologue experience)
- Has unique **Captain Ability** based on defining virtues
- Must participate in boarding actions (can sit out some land missions)
- Virtue scores can shift based on ongoing choices

### Captain Abilities (Based on Defining Virtues)

| Virtue Pair | Ability | Effect |
|-------------|---------|--------|
| Courage + Justice | *Into the Breach* | First to board, +2 initiative to party |
| Compassion + Sacrifice | *Rally the Fallen* | Revive downed ally once per mission |
| Honor + Compassion | *Parley* | Force conversation before combat |
| Justice + Cunning | *Mark for Death* | Target takes +50% damage |
| Sacrifice + Courage | *Take the Blow* | Intercept attack meant for ally |
| Cunning + Ambition | *Read the Angles* | See enemy planned actions |
| Resilience + Courage | *Against All Odds* | +50% defense when below 25% HP |
| Humility + Compassion | *Shared Strength* | Adjacent allies share healing |

### Visual Customization

**At Prologue Start:**
- Gender (affects nothing mechanically)
- Skin tone
- Face shape (5-6 options)
- Hair style/color
- Starting outfit

**After Prologue (unlocks over time):**
- Scars (from battles survived)
- Tattoos (from ports visited)
- Captain's coat/hat (purchased or earned)
- Trophies worn (from victories)

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

Every crew member starts with access to these three jobs. With the Party-as-Unit system, jobs provide:
- **Party Stat Bonuses** - Passive contributions to the five party stats
- **Mission Abilities** - Powerful abilities usable once per mission (or limited uses)
- **Passive Bonuses** - Always-active benefits

Jobs still level up (1-10), unlocking more powerful abilities and increasing stat contributions.

---

#### Cutlass (Combat Specialist)

The frontline fighter. Cutlass crew members excel at direct confrontation, intimidation, and protecting the party in dangerous situations.

**Role:** Combat encounters, intimidation, boarding actions

**Base Stats:**
| Stat | Base | Growth/Level |
|------|------|--------------|
| HP | 120 | +12 |
| STR | 14 | +2 |
| DEX | 10 | +1 |
| CON | 12 | +2 |

**Party Stat Contribution:**

| Party Stat | Base Contribution | Per Job Level |
|------------|-------------------|---------------|
| **Combat** | +12 | +2 |
| **Stealth** | +4 | +0.5 |
| **Tech** | +2 | +0 |
| **Medical** | +3 | +0 |
| **Social** | +6 | +1 (intimidation) |

---

**MISSION ABILITIES:**

| Ability | JP | Unlock | Uses/Mission | Effect |
|---------|----| -------|--------------|--------|
| **Vanguard** | 0 | Lv1 | Passive | Party takes 15% less injury damage from Combat encounters. |
| **Intimidate** | 100 | Lv2 | 2 | +20 to a Social check when threatening or demanding. |
| **Breach** | 150 | Lv3 | 2 | Auto-succeed a Combat check to break through a barrier, door, or blockade. Alert +1. |
| **Bodyguard** | 200 | Lv4 | 1 | Designate one crew member. They cannot be injured this mission (Cutlass takes double). |
| **Shock & Awe** | 300 | Lv5 | 1 | Before a Combat encounter: +30 Combat for this fight. |
| **Hold the Line** | 400 | Lv7 | 1 | During retreat/extraction: Party cannot be caught. Auto-escape one pursuit. |
| **Boarding Master** | 500 | Lv9 | Passive | +10 Combat during all boarding actions. |

---

**PASSIVE BONUSES:**

| Ability | JP | Unlock | Effect |
|---------|----| -------|--------|
| **Thick Skin** | 100 | Lv2 | This crew member's injuries are one severity level lower. |
| **Combat Instincts** | 200 | Lv4 | +5 Combat contribution (stacks with base). |
| **Fearless** | 250 | Lv5 | Party immune to morale penalties when this crew member is present. |
| **Veteran** | 400 | Lv8 | +3 to ALL party stats while in party. |

---

**MASTERY BONUS (Job Level 10):**
*Steel Resolve* - This crew member contributes +5 Combat to ANY party they join (permanent, all jobs).

---

#### Marksman (Ranged/Stealth Specialist)

The precision specialist. Marksmen provide ranged superiority, ambush capability, and reconnaissance. Essential for stealth-focused parties.

**Role:** Stealth encounters, ambushes, scouting, ranged superiority

**Base Stats:**
| Stat | Base | Growth/Level |
|------|------|--------------|
| HP | 80 | +8 |
| STR | 8 | +1 |
| DEX | 14 | +2 |
| CON | 8 | +1 |

**Party Stat Contribution:**

| Party Stat | Base Contribution | Per Job Level |
|------------|-------------------|---------------|
| **Combat** | +8 | +1.5 |
| **Stealth** | +10 | +2 |
| **Tech** | +4 | +0.5 |
| **Medical** | +2 | +0 |
| **Social** | +3 | +0.5 |

---

**MISSION ABILITIES:**

| Ability | JP | Unlock | Uses/Mission | Effect |
|---------|----| -------|--------------|--------|
| **Scout Ahead** | 0 | Lv1 | 2 | Reveal all encounters in adjacent hexes before moving. |
| **Ambush** | 100 | Lv2 | 2 | If undetected, +25 Combat for the next Combat encounter. |
| **Covering Fire** | 150 | Lv3 | 2 | +15 to a Stealth check (suppressing enemies while party moves). |
| **Precision Shot** | 250 | Lv4 | 1 | Auto-succeed one Combat encounter against a single target (assassination). Alert +2. |
| **Spotter** | 200 | Lv4 | Passive | All party members contribute +2 extra Combat (you coordinate fire). |
| **Ghost** | 400 | Lv6 | 1 | Party can skip one hex entirely (move through without triggering encounter). |
| **Overwatch** | 350 | Lv7 | 1 | After resolving any encounter, immediately resolve a free Combat attack if enemies remain. |
| **Sniper's Nest** | 500 | Lv9 | 1 | For remainder of mission: +20 Combat, but Marksman cannot move with party (extracted at end). |

---

**PASSIVE BONUSES:**

| Ability | JP | Unlock | Effect |
|---------|----| -------|--------|
| **Steady Hands** | 100 | Lv2 | +5 Stealth contribution. |
| **Silent Killer** | 200 | Lv4 | Combat encounters you ambush don't raise Alert. |
| **Eagle Eye** | 250 | Lv5 | Scout Ahead range increases to 2 hexes. |
| **Deadeye** | 400 | Lv8 | +5 Combat contribution. |

---

**MASTERY BONUS (Job Level 10):**
*Keen Senses* - This crew member contributes +5 Stealth to ANY party they join (permanent, all jobs).

---

#### Sailor (Support/Utility Specialist)

The jack-of-all-trades. Sailors keep the party healthy, interact with ship systems, and provide crucial support in all situations.

**Role:** Medical encounters, Tech encounters, ship systems, general support

**Base Stats:**
| Stat | Base | Growth/Level |
|------|------|--------------|
| HP | 100 | +10 |
| STR | 10 | +1 |
| DEX | 10 | +1 |
| CON | 10 | +1 |
| INT | 12 | +2 |

**Party Stat Contribution:**

| Party Stat | Base Contribution | Per Job Level |
|------------|-------------------|---------------|
| **Combat** | +6 | +1 |
| **Stealth** | +6 | +1 |
| **Tech** | +8 | +1.5 |
| **Medical** | +10 | +2 |
| **Social** | +6 | +1 |

---

**MISSION ABILITIES:**

| Ability | JP | Unlock | Uses/Mission | Effect |
|---------|----| -------|--------------|--------|
| **First Aid** | 0 | Lv1 | 3 | Heal one crew member to Wounded (from Injured or Critical). Uses 1 Med Kit. |
| **Patch Up** | 100 | Lv2 | 2 | Heal one crew member one injury level. No supplies needed. |
| **Jury Rig** | 150 | Lv3 | 2 | +15 to a Tech check (improvised solution). |
| **Rally** | 200 | Lv4 | 1 | Remove all negative status effects from party. +10 to all stats for next encounter. |
| **Sabotage** | 250 | Lv5 | 1 | Auto-succeed one Tech check (disable alarms, unlock doors, etc.). |
| **Emergency Treatment** | 350 | Lv6 | 1 | Stabilize an Incapacitated crew member (they survive but can't contribute). |
| **Inspire** | 400 | Lv7 | 1 | All party stats +15 for the rest of the mission. |
| **Damage Control** | 500 | Lv9 | 1 | *Naval only:* Prevent one critical hit to your ship. |

---

**PASSIVE BONUSES:**

| Ability | JP | Unlock | Effect |
|---------|----| -------|--------|
| **Field Medic** | 100 | Lv2 | First Aid heals to full (Healthy) instead of Wounded. |
| **Scrounger** | 200 | Lv4 | Find +1 Med Kit and +1 Ammo at end of successful missions. |
| **Reassuring Presence** | 250 | Lv5 | Party is immune to fear/morale checks. |
| **Jack of All Trades** | 400 | Lv8 | +3 to ALL party stats while in party. |

---

**MASTERY BONUS (Job Level 10):**
*Survivor's Grit* - This crew member contributes +5 Medical to ANY party they join (permanent, all jobs).

---

### How Jobs Affect Party Stats

When forming an away party, each crew member contributes to party stats based on:

```
Crew_Contribution = Job_Base + (Job_Level × Per_Level_Bonus) + Passive_Bonuses + Equipment

Party_Stat = Sum of all Crew_Contributions + Gear_Bonuses + Synergy_Bonuses
```

**Example Party Calculation (Combat Stat):**

| Crew Member | Job | Level | Base | Level Bonus | Passives | Equipment | Total |
|-------------|-----|-------|------|-------------|----------|-----------|-------|
| Captain | Cutlass | 5 | 12 | +10 | +5 | +8 (Rifle) | 35 |
| First Mate | Sailor | 4 | 6 | +4 | 0 | +5 (SMG) | 15 |
| Rosa | Marksman | 6 | 8 | +9 | +5 | +8 (Rifle) | 30 |
| **TOTAL** | | | | | | | **80** |

---

### Multi-Jobbing (Simplified)

Crew members can learn multiple jobs, but only ONE job is active at a time.

**Switching Jobs:**
- Can switch between missions (not during)
- All learned abilities remain available regardless of active job
- Stat contributions come from ACTIVE job only
- Passive bonuses from mastered jobs apply always

**Example:**
Rosa has Marksman (Lv 8) and Cutlass (Lv 4).
- As Marksman: Contributes high Stealth/Combat, can use all Marksman abilities
- As Cutlass: Contributes high Combat, can use all Cutlass abilities
- Either way: If she mastered Marksman, she always gets +5 Stealth bonus

---

### XP & Job Leveling

**Earning XP:**
| Action | XP Earned |
|--------|-----------|
| Complete mission | 50 XP |
| Complete optional objective | 25 XP |
| Successful encounter (based on your contribution) | 10-20 XP |
| Flawless mission (no injuries) | +25 XP bonus |
| Mission MVP | +25 XP bonus |

**XP to Level:**
| Level | Total XP Required |
|-------|-------------------|
| 1 → 2 | 100 |
| 2 → 3 | 150 |
| 3 → 4 | 200 |
| 4 → 5 | 300 |
| 5 → 6 | 400 |
| 6 → 7 | 500 |
| 7 → 8 | 650 |
| 8 → 9 | 800 |
| 9 → 10 | 1000 |
| **Total to Master** | **4,100 XP** |

**Estimated Missions to Master:** 40-50 missions (one job)

---

### Job Synergies

Certain job combinations in a party provide bonuses:

| Synergy | Requirement | Bonus |
|---------|-------------|-------|
| **Assault Team** | 2+ Cutlass | +10 Combat, +5 intimidation Social |
| **Sniper Team** | 2+ Marksman | +10 Stealth, Ambush costs 1 use instead of 2 |
| **Medical Team** | 2+ Sailor | +10 Medical, First Aid doesn't consume Med Kits |
| **Balanced Squad** | 1 of each job | +5 to ALL stats |
| **Strike Force** | Cutlass + Marksman | Ambush bonus increases to +35 Combat |
| **Field Hospital** | Sailor + any Sailor passive | Emergency Treatment can be used twice |
| **Spec Ops** | Marksman + Sailor | Ghost can be used twice |

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

### Naval Weapons (Post-Collapse 2085)

Ships in 2085 are sail-powered due to atmospheric instability making aircraft unreliable, but they carry modern and salvaged weapons. Ammunition and Aetherium are precious - most fights are decided by positioning and boarding rather than long-range duels.

**Weapon Categories:**

| Category | Examples | Ammo Cost | Notes |
|----------|----------|-----------|-------|
| **Ballistic** | Deck guns, autocannons | Medium | Pre-collapse military salvage |
| **Aetherium** | Railguns, pulse cannons | Very High | Devastating but expensive |
| **Kinetic** | Harpoons, bolt throwers | Low | Boarding-focused, recoverable |
| **Improvised** | Molotov launchers, nail guns | Cheap | Scavenger weapons |
| **Missiles** | Guided rockets | Extreme | Rare, one-shot kills |

**Specific Weapons:**

| Weapon | Range | Damage | Ammo | Special |
|--------|-------|--------|------|---------|
| **Deck Gun (76mm)** | 8 hex | 40 | Shell | Reliable, military standard |
| **Autocannon** | 6 hex | 25 | Burst | Rapid fire, anti-personnel |
| **Naval Railgun** | 12 hex | 70 | Aetherium | Pierces armor, expensive |
| **Pulse Cannon** | 6 hex | 35 | Aetherium | EMP effect, disables systems |
| **Harpoon Gun** | 5 hex | 15 | Harpoon | Grapple, enables boarding |
| **Bolt Thrower** | 4 hex | 20 | Bolts | Targets sails/rigging |
| **Flak Battery** | 4 hex | 30 | Shells | Anti-personnel, deck sweeper |
| **Guided Missile** | 15 hex | 100 | Missile | Rare, usually 1-2 per ship max |
| **Fire Launcher** | 3 hex | 20/turn | Fuel | Sets fires, area denial |

### Targeting Systems

Choose what to target (affects damage distribution):
- **Hull:** Structural damage, risk sinking. Best for: Deck guns, railguns
- **Sails/Rigging:** Reduce speed, enable escape/pursuit. Best for: Bolt throwers, autocannons
- **Deck:** Kill crew, prepare for boarding. Best for: Flak, fire launchers
- **Weapons:** Disable armaments. Best for: Precision shots (railgun)
- **Engine/Systems:** If ship has auxiliary power. Best for: Pulse weapons (EMP)

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

### Naval Combat Math

The naval combat system draws inspiration from **Car Wars** (phased movement, turning costs), **BattleTech** (to-hit modifiers, location targeting), and **Seas of Havoc** (deck-building, ship asymmetry). The goal is tactical depth without excessive complexity.

#### Movement Formulas

**Base Movement Points (MP):**
```
MP = Ship_Base_Speed × Wind_Modifier × Hull_Condition
```

**Wind Modifiers:**
| Wind Relation | Modifier | Description |
|---------------|----------|-------------|
| Running (with wind) | 1.0 | Full speed |
| Broad Reach (45°) | 0.9 | Slight penalty |
| Beam Reach (90°) | 0.75 | Significant reduction |
| Close Hauled (135°) | 0.5 | Half speed |
| In Irons (against) | 0.25 | Near stationary |

**Turning Costs:**
```
Turn_Cost = Ship_Turn_Rate × Turn_Degrees / 60

Where:
- Turn_Rate varies by ship class (1-3 MP per 60°)
- Sloop: 1 MP per 60°
- Brigantine: 1.5 MP per 60°
- Frigate: 2 MP per 60°
- Galleon: 3 MP per 60°
```

#### To-Hit Formula (Inspired by BattleTech)

**Base To-Hit:**
```
Hit_Chance = Base_Accuracy + Modifiers

Base Accuracy by Weapon:
- Deck Gun (76mm): 65%
- Autocannon: 55% (burst compensates)
- Naval Railgun: 60%
- Pulse Cannon: 65%
- Harpoon Gun: 70% (designed for grappling)
- Bolt Thrower: 60%
- Flak Battery: 75% (area effect)
- Guided Missile: 85% (but rare/expensive)
- Fire Launcher: 80% (area, short range)
```

**To-Hit Modifiers Table:**
| Condition | Modifier |
|-----------|----------|
| Target stationary | +15% |
| Target moving slow (1-3 MP used) | +5% |
| Target moving fast (4+ MP used) | -10% |
| Attacker stationary | +10% |
| Attacker moved | -5% |
| Attacker turned this phase | -10% |
| Point-blank range (1 hex) | +20% |
| Short range (2-3 hex) | +10% |
| Medium range (4-6 hex) | +0% |
| Long range (7-10 hex) | -15% |
| Extreme range (11+ hex) | -30% |
| Rough seas | -10% |
| Storm conditions | -25% |
| Targeting: specific system | -20% |
| Crew quality (per tier) | ±5% |

**Hit Chance Bounds:** 5% minimum, 95% maximum

#### Weapon Arcs (Inspired by Car Wars)

Ships have six firing arcs based on hex directions:

```
        Bow (Forward)
            ╱ ╲
     Port  ╱   ╲  Starboard
    Bow   ╱     ╲  Bow
         │ SHIP │
    Port  ╲     ╱  Starboard
    Stern  ╲   ╱  Stern
            ╲ ╱
        Stern (Rear)
```

**Arc Weapon Availability:**
| Arc | Deck Guns | Support Weapons | Special Mounts |
|-----|-----------|-----------------|----------------|
| Bow | 1-2 | 0-2 | Railgun, Missile |
| Port Broadside | 2-6 | 2-4 | Harpoons |
| Starboard Broadside | 2-6 | 2-4 | Harpoons |
| Stern | 0-1 | 1-2 | Fire Launcher |
| Turret (360°) | 0-1 | 0-1 | Flak, Autocannon |

**Broadside Advantage:**
Firing a full broadside (all port OR all starboard weapons):
- Requires target to be within 90° arc
- +10% accuracy from concentrated fire
- All weapons fire simultaneously

#### Damage Formula

**Base Damage:**
```
Damage = Weapon_Power × (1 + Random_Variance)
       × Critical_Multiplier × Range_Falloff

Where:
- Random_Variance = ±15% (0.85 to 1.15)
- Critical_Multiplier = 2.0 on natural 95%+ roll
- Range_Falloff = 1.0 (short), 0.9 (medium), 0.75 (long)
```

**Damage Distribution by Target:**
| Target Choice | Hull | Sails | Crew | Weapons |
|---------------|------|-------|------|---------|
| Hull (default) | 70% | 10% | 15% | 5% |
| Sails | 15% | 70% | 10% | 5% |
| Deck (crew) | 20% | 10% | 60% | 10% |
| Weapons | 15% | 5% | 20% | 60% |

**Armor Penetration:**
```
Effective_Damage = Damage × Penetration_Factor - Armor

Where Penetration_Factor varies by weapon:
- Deck Gun (76mm): 1.0
- Autocannon: 0.8 (light rounds)
- Naval Railgun: 1.8 (armor-piercing, Aetherium-powered)
- Pulse Cannon: 1.2 (energy bypass)
- Harpoon/Bolt: 0.6 (not designed to penetrate)
- Flak Battery: 0.5 (anti-personnel)
- Guided Missile: 2.0 (explosive penetrator)
- Fire Launcher: 0.3 (damage over time, not penetration)
```

#### Ship Status Thresholds

**Hull Integrity:**
| HP Percentage | Status | Effect |
|---------------|--------|--------|
| 100-75% | Seaworthy | No penalties |
| 74-50% | Damaged | -10% speed, -5% accuracy |
| 49-25% | Critical | -25% speed, -15% accuracy, fires spread |
| 24-1% | Sinking | -50% speed, must flee or abandon |
| 0% | Lost | Ship sinks in 2 turns |

**Crew Status:**
| Crew Percentage | Status | Effect |
|-----------------|--------|--------|
| 100-75% | Full Strength | No penalties |
| 74-50% | Undermanned | -10% reload speed, reduced boarders |
| 49-25% | Skeleton Crew | -25% reload, cannot board, -10% accuracy |
| 24-1% | Desperate | Ship actions halved, surrender likely |

#### Boarding Combat Resolution

**Boarding Strength:**
```
Boarding_Power = Crew_Count × Crew_Quality × (1 + Leader_Bonus)
              × Morale_Modifier × Weapon_Modifier

Where:
- Crew_Quality: 0.8 (green) to 1.5 (elite)
- Leader_Bonus: +0.1 to +0.3 per Bridge Crew member
- Morale_Modifier: 0.7 to 1.3
- Weapon_Modifier: 1.0 (standard) to 1.3 (cutlasses/pistols)
```

**Boarding Resolution:**
```
Attacker_Roll = Boarding_Power × (0.8 + 0.4 × Random)
Defender_Roll = Defender_Power × (0.8 + 0.4 × Random)

If Attacker_Roll > Defender_Roll × 1.5: Decisive Victory
If Attacker_Roll > Defender_Roll: Victory (casualties)
If Attacker_Roll > Defender_Roll × 0.75: Stalemate (both retreat)
Else: Defeat (attacker retreats with heavy losses)
```

---

## 14. Away Party System

Land-based gameplay uses a **Party-as-Unit** system rather than individual tactical combat. Your away party moves as a single token through location hexes, resolving encounters through combined party stats and player choices.

### Design Philosophy

Instead of XCOM-style individual tactics (which is an entire game unto itself), land gameplay focuses on:
- **Party composition** - Who you bring matters
- **Equipment choices** - Gear affects party capabilities
- **Approach decisions** - Multiple ways to handle each encounter
- **Meaningful consequences** - Injuries, deaths, and story impacts

This keeps the depth of crew management while dramatically reducing complexity.

---

### The Party-as-Unit Concept

```
┌─────────────────────────────────────────────────────────────┐
│  AWAY PARTY: "Ironhull Infiltration"                        │
│  ═══════════════════════════════════════════════════════    │
│                                                             │
│  CREW:                          PARTY STATS:                │
│  ► Captain (You) - Cutlass      ┌─────────────────────┐     │
│  ► First Mate - Sailor          │ Combat:    65       │     │
│  ► Rosa - Marksman              │ Stealth:   42       │     │
│  ► Doc Williams - Sailor        │ Tech:      38       │     │
│                                 │ Medical:   55       │     │
│  GEAR BONUSES:                  │ Social:    35       │     │
│  ► Assault Rifles (+10 Combat)  └─────────────────────┘     │
│  ► Med Kit (+15 Medical)                                    │
│  ► Silencers (+8 Stealth)       SUPPLIES:                   │
│                                 ► Ammo: 3 units             │
│                                 ► Medkits: 2 units          │
│                                 ► Grenades: 1 unit          │
└─────────────────────────────────────────────────────────────┘
```

Your party is represented by **one token** that moves through location hexes. All combat and challenges resolve using **Party Stats** rather than individual tactical actions.

---

### Party Stats

Five stats represent your party's combined capabilities:

| Stat | Derived From | Used For |
|------|--------------|----------|
| **Combat** | STR + DEX + Weapons + Job bonuses | Fighting, intimidation, breaching |
| **Stealth** | DEX + INT + Gear + Job bonuses | Sneaking, ambushes, avoiding detection |
| **Tech** | INT + Gear + Job bonuses | Hacking, sabotage, disabling systems |
| **Medical** | INT + SPR + Gear + Job bonuses | Healing injuries, stabilizing, extraction |
| **Social** | SPR + LCK + Job bonuses | Talking, bribing, negotiating, bluffing |

**Calculating Party Stats:**

```
Party_Combat = Sum of (Each crew member's Combat contribution)
             + Weapon bonuses
             + Synergy bonuses

Where individual Combat contribution:
  = (STR + DEX) ÷ 2 + Job_Combat_Bonus + Equipment_Bonus
```

**Example Calculation:**

| Crew Member | Combat Contrib | Stealth Contrib | Notes |
|-------------|----------------|-----------------|-------|
| Captain (Cutlass) | 18 | 8 | High STR, melee bonus |
| First Mate (Sailor) | 12 | 10 | Balanced |
| Rosa (Marksman) | 15 | 14 | High DEX |
| Doc Williams (Sailor) | 10 | 10 | Support focus |
| **Gear Bonuses** | +10 | +8 | Rifles, silencers |
| **TOTAL** | **65** | **50** | |

---

### Locations as Hex Clusters

Locations (cities, bases, ruins) are represented as **clusters of hexes** on the world map. Each hex within a location has:
- A **type** (market, checkpoint, residential, restricted, etc.)
- Possible **encounters**
- **Connections** to adjacent hexes

**Example: Ironhull (AetherCorp Outpost)**

```
                    [Restricted]
                         │
    [Market]────[Plaza]────[Lab District]
        │          │            │
   [Docks]────[Gate]────[Admin]────[Compound]
        │                              │
    [Your                         [Objective]
     Ship]                     (Dr. Vasquez)
```

**Hex Types:**

| Type | Common Encounters | Notes |
|------|-------------------|-------|
| **Docks** | Inspection, smuggling check | Entry/exit point |
| **Market** | Social encounters, shopping, intel | Safe zone usually |
| **Gate/Checkpoint** | Security check, combat or stealth | Bottleneck |
| **Residential** | Civilians, hiding spots, witnesses | Low security |
| **Admin/Office** | Social, tech encounters | Medium security |
| **Restricted** | Combat, stealth, tech | High security |
| **Objective** | Mission-critical encounter | Varies |

---

### Movement & Alert Level

**Moving Through Hexes:**
- Party moves one hex at a time
- Each hex may trigger an encounter
- Some hexes require passing a check to enter

**Alert Level (0-5):**

The location has an overall Alert Level that increases when things go wrong:

| Alert | Status | Effect |
|-------|--------|--------|
| 0 | **Unaware** | Normal patrols, easy checks |
| 1 | **Suspicious** | +10 to all check difficulties |
| 2 | **Searching** | +20 difficulty, extra patrols |
| 3 | **Alert** | +30 difficulty, reinforcements arrive |
| 4 | **Lockdown** | +40 difficulty, can't enter some hexes |
| 5 | **Hostile** | Combat encounters in every hex |

**Alert Increases:**
- Failed Stealth check: +1
- Combat (even if won): +1 or +2
- Alarm triggered: +2
- Body discovered: +1
- Witnessed by civilian: +1 (unless handled)

**Alert Decreases:**
- Time passing (some missions): -1 per X turns
- Disabling alarm system (Tech check): -1
- "All clear" event: -1

---

### Encounter Types

Each hex can trigger one of several encounter types:

#### Combat Encounters

Your party faces an enemy group. Resolved by comparing Combat stats.

```
┌─────────────────────────────────────────────────────────────┐
│  COMBAT: Security Patrol                                    │
│  ═══════════════════════════════════════════════════════    │
│                                                             │
│  YOUR PARTY              ENEMY GROUP                        │
│  Combat: 65              Combat: 45                         │
│  ────────────────────────────────────────                   │
│  Advantage: +20 (Strong)                                    │
│                                                             │
│  APPROACH:                                                  │
│  [A] Direct assault                                         │
│      → Victory likely, Alert +2                             │
│                                                             │
│  [B] Ambush (requires Stealth 40) ✓                         │
│      → Victory likely, Alert +1, bonus loot                 │
│                                                             │
│  [C] Avoid entirely (requires Stealth 55) ✗ Risky           │
│      → Skip combat, no Alert change                         │
│                                                             │
│  [D] Retreat to previous hex                                │
│      → No combat, may trigger different encounter           │
└─────────────────────────────────────────────────────────────┘
```

**Combat Resolution:**

```
Combat_Difference = Party_Combat - Enemy_Combat
Roll = Random(1-100)

If Combat_Difference >= 20:  (Strong advantage)
  Victory on Roll <= 90
  Flawless Victory on Roll <= 50

If Combat_Difference >= 0:   (Advantage)
  Victory on Roll <= 75
  Flawless Victory on Roll <= 25

If Combat_Difference >= -20: (Disadvantage)
  Victory on Roll <= 50
  Flawless Victory on Roll <= 10

If Combat_Difference < -20:  (Severe disadvantage)
  Victory on Roll <= 25
  Flawless Victory: Impossible
```

**Combat Outcomes:**

| Outcome | Result |
|---------|--------|
| **Flawless Victory** | No injuries, +bonus loot, minimal Alert |
| **Victory** | Minor injuries (1-2 crew lose 10-25% HP), Alert +1 |
| **Costly Victory** | Significant injuries (all crew lose 20-40% HP), Alert +2 |
| **Pyrrhic Victory** | Major injuries, 1 crew incapacitated, Alert +2 |
| **Defeat** | Party forced to retreat, 1-2 crew incapacitated, Alert +3 |
| **Rout** | Party scattered, all crew injured, possible captures |

---

#### Stealth Encounters

Avoiding detection, sneaking past guards, or remaining hidden.

```
┌─────────────────────────────────────────────────────────────┐
│  STEALTH: Guard Post                                        │
│  Required: Stealth 45                                       │
│  Your Stealth: 50 ✓                                         │
│  ────────────────────────────────────────                   │
│                                                             │
│  [A] Sneak past (Stealth check)                             │
│      → Success: Pass undetected                             │
│      → Failure: Combat encounter, Alert +1                  │
│                                                             │
│  [B] Create distraction (uses 1 Grenade)                    │
│      → Auto-success, but Alert +1                           │
│                                                             │
│  [C] Wait for shift change (costs 1 Turn)                   │
│      → Stealth requirement drops to 35                      │
└─────────────────────────────────────────────────────────────┘
```

**Stealth Check:**
```
Roll = Random(1-100)
Success if Roll <= (Party_Stealth - Difficulty + 50)
```

---

#### Tech Encounters

Hacking terminals, disabling alarms, sabotaging equipment.

```
┌─────────────────────────────────────────────────────────────┐
│  TECH: Security Terminal                                    │
│  Required: Tech 40                                          │
│  Your Tech: 38 ✗ Risky                                      │
│  ────────────────────────────────────────                   │
│                                                             │
│  [A] Hack the system (Tech check)                           │
│      → Success: Disable cameras, Alert -1                   │
│      → Failure: Alarm triggered, Alert +2                   │
│                                                             │
│  [B] Brute force (Combat, destroys terminal)                │
│      → Auto-success, but Alert +1, no intel gained          │
│                                                             │
│  [C] Find another way (skip this hex's bonus)               │
│      → No risk, no reward                                   │
└─────────────────────────────────────────────────────────────┘
```

---

#### Social Encounters

Talking, bribing, intimidating, or deceiving NPCs.

```
┌─────────────────────────────────────────────────────────────┐
│  SOCIAL: Checkpoint Guard                                   │
│  ────────────────────────────────────────                   │
│  "Papers? What's your business in the lab district?"        │
│                                                             │
│  [A] Bluff - "Maintenance crew, here's our work order"      │
│      Requires: Social 45 | Your Social: 35 ✗ Risky          │
│                                                             │
│  [B] Bribe - "Maybe this helps with the paperwork"          │
│      Cost: 200 doubloons | Auto-success                     │
│                                                             │
│  [C] Intimidate - "You really want to make this difficult?" │
│      Requires: Combat 60 | Your Combat: 65 ✓                │
│      → Success: Pass, but guard remembers you               │
│      → Failure: Combat encounter                            │
│                                                             │
│  [D] Show legitimate papers (if you have them)              │
│      → Auto-success                                         │
└─────────────────────────────────────────────────────────────┘
```

---

#### Medical Encounters

Handling injuries, rescuing people, or dealing with hazards.

```
┌─────────────────────────────────────────────────────────────┐
│  MEDICAL: Injured Crew Member                               │
│  Rosa is incapacitated and bleeding out.                    │
│  ────────────────────────────────────────                   │
│                                                             │
│  [A] Field surgery (Medical 50 required)                    │
│      Your Medical: 55 ✓                                     │
│      → Success: Rosa stabilized at 25% HP                   │
│      → Failure: Rosa dies                                   │
│                                                             │
│  [B] Use Med Kit (consumes 1 Med Kit)                       │
│      → Auto-success: Rosa at 50% HP                         │
│                                                             │
│  [C] Carry her (party movement slowed)                      │
│      → Rosa survives but can't contribute to checks         │
│                                                             │
│  [D] Leave her behind                                       │
│      → Rosa is captured or dies (permanent)                 │
│      → Party moves faster                                   │
└─────────────────────────────────────────────────────────────┘
```

---

### Equipment & Loadout

Before deploying, you select gear that affects party stats:

**Weapons (affect Combat, sometimes Stealth):**

| Weapon | Combat Bonus | Stealth Mod | Special |
|--------|--------------|-------------|---------|
| Pistols | +5 | +0 | Concealable |
| SMGs | +8 | -5 | Loud |
| Assault Rifles | +12 | -10 | Loud, +5 vs armored |
| Shotguns | +10 | -10 | +10 in close quarters |
| Sniper Rifle | +8 | +0 | +15 for ambushes |
| Crossbows | +6 | +5 | Silent |
| Melee (machetes, etc.) | +5 | +5 | Silent, last resort |
| Silenced weapons | +8 | +5 | Expensive ammo |

**Armor (affect Combat, Stealth, survival):**

| Armor | Combat Bonus | Stealth Mod | Injury Reduction |
|-------|--------------|-------------|------------------|
| Light/None | +0 | +5 | 0% |
| Ballistic Vest | +3 | +0 | 20% |
| Combat Armor | +5 | -5 | 35% |
| Heavy Armor | +8 | -15 | 50% |

**Gear (consumables and tools):**

| Item | Effect | Uses |
|------|--------|------|
| Med Kit | Auto-success on Medical check OR heal 50% HP | 1 |
| Grenade | +20 Combat for one fight OR create distraction | 1 |
| Hacking Tool | +15 Tech for one check | 3 |
| Disguise Kit | +15 Social for one check | 2 |
| Smoke Bomb | Auto-escape from combat OR +20 Stealth | 1 |
| Bribe Money | Auto-success on bribable Social checks | Varies |

---

### Crew Synergies

Certain crew combinations provide bonuses:

| Synergy | Requirement | Bonus |
|---------|-------------|-------|
| **Battle Buddies** | 2+ crew with Support Level A+ | +10 Combat |
| **Silent Professionals** | All crew have Stealth training | +10 Stealth |
| **Medical Team** | 2+ Sailors in party | +15 Medical |
| **Tech Specialists** | Crew with INT 14+ | +10 Tech |
| **Silver Tongues** | Crew with SPR 14+ | +10 Social |
| **Full Squad** | Maximum party size (5) | +5 all stats |
| **Small Team** | Only 2 crew | +10 Stealth, -10 Combat |

---

### Injuries & Consequences

**During Mission:**

| Injury Level | Effect | Recovery |
|--------------|--------|----------|
| **Healthy** | Full contribution | - |
| **Wounded** (75-50% HP) | -25% stat contribution | Heals after mission |
| **Injured** (50-25% HP) | -50% stat contribution | 1-2 missions to recover |
| **Critical** (<25% HP) | No contribution, must be carried | 3+ missions to recover |
| **Incapacitated** (0 HP) | Dead or captured (context) | Permanent or rescue mission |

**After Mission:**

- **Flawless:** All crew healthy, bonus XP
- **Standard:** Minor wounds heal immediately
- **Rough:** Injured crew need recovery time (sit out next mission)
- **Disaster:** Crew deaths are permanent, captures require rescue

---

### Mission Flow Example

**Mission: Extract Dr. Vasquez**

```
TURN 1: Start at [Docks]
        → Inspection encounter (Social)
        → Bluff successful, proceed

TURN 2: Move to [Market]
        → Intel encounter (Social)
        → Learn guard patrol patterns (+5 Stealth for this mission)

TURN 3: Move to [Gate]
        → Checkpoint encounter (Stealth or Social)
        → Bribe guard (200 doubloons), proceed

TURN 4: Move to [Lab District]
        → Patrol encounter (Combat or Stealth)
        → Ambush successful, Alert +1

TURN 5: Move to [Compound]
        → Security system (Tech)
        → Hack successful, cameras disabled

TURN 6: Reach [Objective]
        → Story encounter: Find Elena
        → Choice: What about David Chen?

TURN 7-9: Extraction (reverse path)
        → Alert Level 2, encounters are harder
        → Combat encounter at Gate
        → Victory with injuries

MISSION COMPLETE
        → Elena extracted
        → Rosa injured (2 mission recovery)
        → 350 XP earned
        → Alert Level: 3 (AetherCorp knows something happened)
```

---

### Why This System Works

| Benefit | Explanation |
|---------|-------------|
| **Depth preserved** | Party composition, gear, and crew relationships all matter |
| **Quick resolution** | Encounters resolve in 30 seconds, not 30 minutes |
| **Meaningful choices** | Multiple approaches reward different builds |
| **Crew still at risk** | Injuries and deaths have weight |
| **Reuses hex system** | Locations are just hex clusters |
| **Easy to expand** | Add encounters, locations, and gear without new systems |
| **Story-friendly** | Narrative moments integrate naturally |

---

### Comparison: Old vs New

| Aspect | Old (Tactical Grid) | New (Party-as-Unit) |
|--------|---------------------|---------------------|
| Combat time | 20-40 minutes | 1-3 minutes |
| Development effort | ~12 months | ~3 months |
| Individual positioning | Yes (complex) | No (abstracted) |
| Party composition matters | Yes | Yes |
| Equipment matters | Yes | Yes |
| Crew death risk | Yes | Yes |
| Player choices | Per-action | Per-encounter |
| Replayability | High (tactics vary) | High (approaches vary) |

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

### MVP vs Full Game

The game releases in stages. The MVP is a complete, polished experience - not a demo.

| Aspect | MVP | Full Game |
|--------|-----|-----------|
| **Map Size** | ~100 hexes, 1 region | 500+ hexes, full Caribbean |
| **Ports** | 3-4 | 20+ |
| **Story Length** | 8-12 hours | 30+ hours |
| **Ships Available** | 3 hull classes | 8+ hull classes |
| **Jobs** | Tier 1 only (3 jobs) | All 3 tiers (9+ jobs) |
| **Endings** | 2 | Multiple branches |

---

### MVP Campaign: "The Gray Tide"

**Era:** Book 7 (~2085) - The Abyssal War
**Timeline Position:** Side story during *The Kraken Project*
**Relationship to Novels:** Canonical but separate - your story, not Shaw's

#### The Complete MVP Flow

```
┌─────────────────────────────────────────────────────────────┐
│  PROLOGUE: "Before the Storm" (Character Creation)          │
│  8 stages with your friend, choices shape your virtues      │
│  End: You become captain, friend becomes First Mate         │
│  You name your ship                                         │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  ACT 1: "Small Fish"                                        │
│  Your small ship in the Shattered Isles (tutorial region)   │
│  Learn: Trading, navigation, minor encounters               │
│  Goal: Make enough to survive, establish yourself           │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  THE ENCOUNTER: Corbin Shaw finds you                       │
│  The legendary captain needs something done quietly         │
│  He can't use Confederation ships - too visible             │
│  He offers a job: [THE TASK]                                │
│  You can refuse (game continues, harder path)               │
│  Or accept (main story path)                                │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  ACT 2: "Shaw's Task" (Core MVP Content)                    │
│                                                             │
│  The task requires:                                         │
│  • TRADE: Acquire specific cargo as cover story             │
│  • NAVAL: Evade or fight AetherCorp patrols                 │
│  • LAND: Infiltrate a location to [objective]               │
│  • CHOICE: Moral gray area - what Shaw asked isn't clean    │
│                                                             │
│  You learn WHY Shaw needed this done                        │
│  You learn something about the larger war                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  ACT 3: "Consequences"                                      │
│                                                             │
│  Completing the task draws attention:                       │
│  • AetherCorp knows someone helped Shaw                     │
│  • Other factions are curious about you                     │
│  • You have information/cargo that's valuable               │
│                                                             │
│  Final choice:                                              │
│  • HELP SHAW: Deliver the goods, become a secret ally       │
│  • SELL OUT: Take AetherCorp's offer (they found you)       │
│  • WALK AWAY: Try to disappear with what you have           │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│  ENDING                                                     │
│                                                             │
│  Based on your choice:                                      │
│  • SHAW PATH: You're now part of something bigger           │
│  • CORP PATH: You have power, but at what cost?             │
│  • INDEPENDENT: You're free, but alone and hunted           │
│                                                             │
│  All endings: "Your name means something now..."            │
│  Cliffhanger: The larger war is just beginning              │
│  "To be continued..."                                       │
└─────────────────────────────────────────────────────────────┘
```

#### Shaw's Task: "The Defector"

Shaw needs you to extract **Dr. Elena Vasquez**, an AetherCorp bioweapons researcher who wants to defect. She has evidence of Project Stormbreak - a plan to poison the Vita-Algae supply of independent settlements, forcing them to depend on AetherCorp or starve.

**The Gray:** Elena's husband Marco and daughter Sofia (age 12) are still inside the AetherCorp compound. If Elena disappears, they become hostages - or worse. Elena knows this. She's choosing the lives of thousands over her own family. And she's asking you to help her make that choice.

---

##### The Encounter: Meeting Shaw

**Location:** Haven - The Rusty Anchor tavern, back room
**Trigger:** Player reaches Reputation Level 2 OR completes 3 trade runs

*Shaw finds you. He's not what the legends describe - older, tired, but his eyes miss nothing.*

**Shaw:** "You're the captain of the [Ship Name]. Small ship. Crew that survived the Wavecutter mutiny. You've been making quiet runs, staying out of trouble. I need someone who can stay out of trouble a little longer."

**Shaw's Pitch:**
- A scientist wants out of AetherCorp
- She has information that could save thousands of lives
- The Confederation can't be seen involved - it would mean open war before they're ready
- He needs a nobody. A small ship that won't draw attention.
- Payment: 5,000 doubloons + a favor from Corbin Shaw (valuable in this world)

**Player Choices:**

| Choice | Response | Effect |
|--------|----------|--------|
| **Accept** | "What do you need me to do?" | Main story proceeds |
| **Ask questions first** | "Why can't you do this yourself?" | Shaw explains the political situation, +Cunning |
| **Negotiate** | "5,000 isn't enough for this risk." | Can negotiate to 7,500, Shaw respects it, +Ambition |
| **Refuse** | "Find someone else." | Shaw leaves. Side content only. Can change mind later at Haven. |

---

##### Mission 1: "The Cover" (Trade Mission)

**Objective:** Establish a legitimate reason to approach Ironhull (AetherCorp outpost)

**Shaw's Instructions:**
*"You can't just sail into an AetherCorp port asking questions. You need a reason to be there. There's a merchant in Drift - Old Reyes - who has a shipment of machine parts AetherCorp wants. Buy the cargo, deliver it to Ironhull. That's your cover."*

**Mission Flow:**

1. **Travel to Drift** (Pirate haven)
   - Optional: Random pirate encounter (flee or fight)

2. **Find Old Reyes** (Dialogue)
   - Reyes is suspicious - why does a small-timer want his cargo?
   - **Choices:**
     - Lie convincingly (Cunning check) - Lower price
     - Tell partial truth ("Got a buyer in Ironhull") - Normal price
     - Intimidate (Courage check) - Lower price but Reyes remembers
     - Pay extra for no questions - Higher price but clean

3. **Purchase Cargo:** 800-1,200 doubloons depending on approach

4. **Travel to Ironhull**
   - AetherCorp patrol encounter (scripted)
   - They scan your cargo - machine parts check out
   - Tense moment, then cleared to proceed

**Mission Rewards:**
- Access to Ironhull port
- 200 XP
- Introduction to AetherCorp security patterns

---

##### Mission 2: "The Contact" (Infiltration/Social)

**Objective:** Make contact with Dr. Vasquez without alerting AetherCorp security

**Location:** Ironhull - AetherCorp research outpost

**The Setup:**
Ironhull is a working port but heavily monitored. Scientists occasionally visit the market district during off-hours. Elena will be there tomorrow evening - but so will security.

**Mission Flow:**

1. **Deliver the Machine Parts** (Cover maintained)
   - Get paid for legitimate delivery (+1,500 doubloons)
   - Establishes you as a trader, not a threat

2. **Gather Intel** (Exploration)
   - Talk to locals: Learn guard patrol patterns
   - Bribe a dock worker: Learn where scientists shop
   - Observe the market: Identify security checkpoints
   - *Each approach gives different intel bonuses for Mission 3*

3. **The Meeting** (Timed Social Encounter)
   - Elena appears at a fabric stall (cover: buying clothes for Sofia)
   - Security detail nearby but not watching closely
   - **You have 3 dialogue exchanges before security gets suspicious**

   **Exchange 1 - Recognition:**
   - "Looking for something specific, ma'am?" (Innocent opener)
   - Elena: "Something... durable. For a long journey."

   **Exchange 2 - Confirmation:**
   - "I hear the southern islands are nice this time of year." (Shaw's code phrase)
   - Elena's eyes widen. She knows.
   - Elena: "I've heard. But traveling is... complicated. Family obligations."

   **Exchange 3 - Critical Choice:**

   | Choice | What You Say | Elena's Response | Consequence |
   |--------|--------------|------------------|-------------|
   | **Promise safety** | "Everyone travels together. No one left behind." | Relief, gratitude. "Then I'll send word when I'm ready." | You've implied you'll extract the family too. Can you? |
   | **Be honest** | "I can get you out. I can't promise more." | Pain, then resolve. "I know. I've made my choice." | Elena trusts your honesty. Family stays behind. |
   | **Push her** | "The information is what matters. You're just the carrier." | Anger, hurt. "You're just like them." | Elena cooperates but doesn't trust you. Complications later. |

4. **Exfiltration from Market**
   - Security notices you've been talking too long
   - **Quick Choice:**
     - Buy something and leave casually (Cunning check)
     - Cause a distraction (Courage check)
     - Just walk away (Security suspicion +1)

**Mission Rewards:**
- Contact established
- Intel for Mission 3
- 250 XP
- Moral weight: Your promise (or lack thereof) will matter

---

##### Mission 3: "The Extraction" (Land Combat Mission)

**Objective:** Extract Dr. Elena Vasquez from AetherCorp compound

**Timeline:** 2 days after contact - Elena has arranged to be in the auxiliary lab (less security)

**Approach Options:**

The compound has three entry points. Intel gathered in Mission 2 affects difficulty:

| Entry Point | Base Difficulty | Intel Bonus |
|-------------|-----------------|-------------|
| **Main Gate** | Hard (heavy security) | Bribed guard (-1 enemy squad) |
| **Service Tunnel** | Medium (tight spaces) | Maintenance schedule (avoid 2 patrols) |
| **Sea Wall** | Medium (climbing, exposure) | Tide charts (bonus movement) |

**Landing Party:** Select 3-4 crew (including yourself)
- Recommended: 1 Cutlass, 1 Marksman, 1 Sailor
- Your First Mate insists on coming

---

**Phase 1: Infiltration**
- Reach the auxiliary lab without raising full alarm
- Stealth possible but not required
- **Alarm Levels:**
  - **0 (Silent):** No reinforcements
  - **1 (Suspicious):** +1 enemy squad at extraction
  - **2 (Alert):** +2 enemy squads, Elena panics
  - **3 (Lockdown):** Mission becomes extraction under fire, Elena may die

**Phase 2: The Lab**
- Find Elena in the auxiliary research wing
- She has a data chip with Project Stormbreak evidence
- **Complication:** Her assistant, young researcher **David Chen**, is there

  **David:** "Dr. Vasquez? What's happening? Who are these people?"

  | Choice | Action | Consequence |
  |--------|--------|-------------|
  | **Knock him out** | Quick, quiet | David wakes up, reports everything. AetherCorp knows it was extraction, not kidnapping. |
  | **Take him too** | "You're coming with us." | Unwilling hostage. Slows movement. But he can't report. |
  | **Let Elena decide** | "He's your assistant." | Elena hesitates, then tells him to stay quiet. He might. Or might not. |
  | **Kill him** | No witnesses | Elena is horrified. "What kind of people ARE you?" Trust permanently damaged. Fast and clean though. |

**Phase 3: Extraction**
- Return to entry point with Elena (and David?)
- Difficulty based on alarm level and entry choice
- **Combat encounters:** 2-4 fights depending on stealth

**Extraction Combat Encounters:**

| Alarm Level | Enemies |
|-------------|---------|
| 0 | 1 Security patrol (4 guards) |
| 1 | 2 Patrols (8 guards) |
| 2 | 2 Patrols + 1 Marine squad (12 enemies) |
| 3 | Full response: 3 squads + Sergeant Knox (boss) |

**Phase 4: The Choice at the Wall**

*If you promised to save Elena's family:*

As you reach the extraction point, Elena stops.

**Elena:** "Marco and Sofia. You said—"

**Your First Mate:** "Captain, we don't have time. The compound is waking up."

The family quarters are 200 meters away. Going there means fighting through more security. Not going means breaking your word.

| Choice | Action | Consequence |
|--------|--------|-------------|
| **Keep your word** | "We're getting them. Move." | Additional combat encounter + timed escape. If successful: Elena and family extracted. If failed: Casualties likely. |
| **Break your word** | "I said I'd try. We can't." | Elena extracted. Her family becomes AetherCorp leverage. Elena never fully trusts you. Shaw understands but is disappointed. |
| **Offer alternative** | "We come back for them. I promise." | Elena extracted. Side mission unlocks post-MVP: Family Rescue |

*If you were honest about not saving the family:*
- Elena is grim but prepared
- No additional choice required
- Extraction proceeds

---

**Mission Rewards:**
- Elena extracted (or killed if mission fails badly)
- Project Stormbreak data
- 500 XP
- Bridge Crew unlock: **David Chen** (if taken and survives) - Scientist, unique abilities
- Heavy narrative consequences based on choices

---

##### Mission 4: "The Run" (Naval Combat/Chase)

**Objective:** Escape the Shattered Isles with Elena and the data

**The Setup:**
AetherCorp knows someone took their scientist. Patrols are out. Your description is circulating. The direct route to the rendezvous with Shaw is blocked.

**Mission Flow:**

1. **Choose Your Route:**

| Route | Distance | Danger | Advantage |
|-------|----------|--------|-----------|
| **The Shallows** | Short | Medium | Your small ship can navigate; their frigates can't |
| **The Storm Belt** | Medium | High (weather) | Patrols avoid it; you might not survive it either |
| **The Long Way** | Long | Low (but more encounters) | Safest, but more chances to be spotted |

2. **Naval Encounters (varies by route):**

**The Shallows Route:**
- 1 Patrol Boat encounter (can be evaded with good navigation)
- Reef hazard (ship damage if failed check)
- Pirate ambush at the narrows (fight or pay toll)

**The Storm Belt Route:**
- Storm damage event (crew injuries, ship damage)
- Single Cutter encounter (they're desperate too)
- Navigation challenge (fail = blown off course, +1 encounter)

**The Long Way Route:**
- 2-3 Patrol Boat encounters
- Merchant ship (potential witness - bribe, threaten, or ignore)
- Final Frigate encounter near rendezvous (unavoidable fight or chase)

3. **The AetherCorp Ultimatum:**

Midway through any route, you receive a radio transmission:

**AetherCorp Commander:** "Unknown vessel carrying stolen corporate property. This is Commander Reis of the ACS Vigilance. You have one hour to surrender Dr. Vasquez and the data she stole. Do this, and your crew goes free. Refuse, and we will hunt you to the edge of the world."

*If Elena's family was left behind:*
**Reis:** "Dr. Vasquez. Your husband asked me to tell you: Sofia misses her mother. Come home. We can still fix this."

| Choice | Action | Consequence |
|--------|--------|-------------|
| **Ignore** | Maintain radio silence | No immediate effect. Reis wasn't bluffing - pursuit intensifies. |
| **Defiant response** | "Come and get us." | +Courage. Reis respects it. Still hunting you. Elena appreciates it. |
| **Elena decides** | Hand her the radio | Elena's choice depends on your earlier actions and her family status. 10% chance she surrenders if family is hostage AND you broke promises. |

4. **Final Chase/Battle:**

Before reaching Shaw's rendezvous, one final encounter:

**ACS Cutter "Ironside"** - Tier 2 ship, aggressive captain

Options:
- **Fight:** Winnable but costly naval battle
- **Outrun:** Speed check + navigation; small ships have advantage
- **Trick:** Lure them into the shallows/storm (requires earlier route knowledge)
- **Decoy:** If David Chen is aboard, he can broadcast fake distress signal to draw them off

---

**Mission Rewards:**
- Reach Shaw's rendezvous
- 400 XP
- Ship upgrade opportunity (Shaw provides)
- Reputation: AetherCorp now knows your name

---

##### The Handoff: Shaw's Rendezvous

**Location:** Open water, coordinates provided
**Shaw's Ship:** The *Tempest* - A Confederation frigate, impressive but showing its years

**The Meeting:**

Shaw comes aboard your ship. He wants to meet Elena personally.

**Shaw to Elena:** "Dr. Vasquez. I know what you've given up to be here. I can't give you your family back. But I can promise their sacrifice won't be meaningless."

**Elena:** "Just stop them. Stop Stormbreak. That's all I ask."

Shaw reviews the data. His expression darkens.

**Shaw to You:** "This is worse than I thought. They're not just planning to poison settlements - they're going to blame it on the Confederation. Use it as justification for full military action. We'd be fighting a war while our own people think we're the monsters."

**Shaw's Offer:**

"You've done more than I asked. The smart thing would be to take your money and disappear. But I could use someone like you. Someone AetherCorp doesn't know. Someone who can go places the Confederation can't."

"This is bigger than one scientist or one data chip. I'm building something - a network of people who can fight this war in the shadows. No flags. No uniforms. Just people doing what needs to be done."

"I'm not asking for an answer now. Finish your business in the Isles. When you're ready - if you're ready - find me."

**Payment:** 5,000 doubloons (or 7,500 if negotiated) + Shaw's Favor (unique item, unlocks options later)

---

##### Act 3: Consequences

After the handoff, returning to the Shattered Isles triggers the final act.

**Immediate Effects:**

1. **Haven:** Word has spread that you "crossed" AetherCorp
   - Some people avoid you (fear)
   - Some people seek you out (respect)
   - New mission opportunities from anti-Corp factions

2. **Ironhull:** You are HOSTILE
   - Cannot dock
   - AetherCorp ships attack on sight in their waters
   - Bounty on your head: 2,000 doubloons

3. **Drift:** Pirates are impressed
   - Better prices
   - Access to black market
   - Potential recruits

**The Approach:**

Within a few days, three parties contact you:

---

**Option A: Shaw's Network**

*Message delivered by unmarked skiff:*

"The data checked out. Stormbreak is real. We're moving against it, but we need more. There's a supply depot on Cayo Muerto that ships Stormbreak materials. It needs to disappear. Quietly. Are you in? - S"

**Accepting Shaw Path:**
- Leads to raid on Cayo Muerto (bonus mission)
- Firmly allies you with Confederation
- Sets up full game story

---

**Option B: AetherCorp's Offer**

*Commander Reis contacts you directly:*

"Captain. You've made an enemy of AetherCorp. That's usually fatal. But I'm a practical man. Dr. Vasquez was a traitor. The data she stole was... sensitive. We want it back. More importantly, we want to know what Shaw is planning."

"Work for us. Feed us information. In return: your bounty disappears, your record is clean, and you'll have access to ports and resources you can't imagine. AetherCorp takes care of its friends."

**Accepting Corp Path:**
- Become a double agent (or genuine turncoat)
- Access to AetherCorp equipment and ports
- Eventually forced to betray Shaw or blow cover
- Different ending

---

**Option C: Independent**

*Your First Mate:*

"Captain, I've been thinking. We've got a ship, a crew, and a reputation now. We don't owe Shaw anything - we did the job, we got paid. And I don't trust AetherCorp as far as I could throw their frigate."

"What if we just... didn't pick a side? The Isles are full of opportunities. We could build something here. Something that belongs to us."

**Staying Independent:**
- Refuse both offers
- Focus on building your own operation
- Both factions become neutral-hostile (suspicious)
- Hardest path but most freedom
- Can still choose a side later

---

##### Endings (MVP)

**Ending A: "The Shadow Fleet"**
*Chose Shaw Path*

You raid Cayo Muerto. The depot burns. Stormbreak is delayed, not stopped - but you've bought time. Shaw's message arrives: "Well done, Captain. The war is just beginning. When it's over, people will know what you did. For now, stay in the shadows. I'll be in touch."

*Epilogue: Your ship sails into the darkness. In the distance, AetherCorp searchlights sweep the water. They're looking for ghosts. They're looking for you.*

**Ending B: "The Corporate Captain"**
*Chose AetherCorp Path*

Reis is pleased. Your first "assignment" - report on Confederation ship movements. Easy money. Clean conscience? That's harder. Elena's face haunts you. But you're alive, your crew is fed, and in this world, that's not nothing.

*Epilogue: Your ship enters Ironhull, flying AetherCorp colors. The guards salute. You've joined the winning side. At least, that's what you tell yourself.*

**Ending C: "The Free Captain"**
*Stayed Independent*

Neither side trusts you. Both sides want you. You sail the Shattered Isles, taking jobs from whoever pays, answering to no one. It's dangerous. It's uncertain. But it's yours.

*Epilogue: Your ship anchors in a hidden cove. Your crew gathers around a fire. Your First Mate raises a bottle. "To the [Ship Name]. To freedom. And to whatever comes next." You drink. The stars are bright. The future is unwritten.*

---

**All Endings Include:**

*"Your name means something now. In the taverns of Haven, in the corridors of Ironhull, in the shadows where Shaw builds his network - people know the captain of the [Ship Name]. What happens next is up to you."*

*"THE GRAY TIDE - Chapter 1 Complete"*
*"To be continued..."*

---

##### Choices Summary & Consequences

| Choice Point | Options | Long-term Impact |
|--------------|---------|------------------|
| Shaw negotiation | Accept/Question/Negotiate/Refuse | Payment amount, Shaw's respect |
| Reyes approach | Lie/Truth/Intimidate/Pay | Drift reputation, future prices |
| Elena's family promise | Promise/Honest/Dismiss | Trust, ending options, side mission |
| David Chen | KO/Take/Let decide/Kill | Crew member, witness report, Elena trust |
| Family rescue attempt | Keep word/Break word/Promise later | Elena loyalty, Shaw respect, side mission |
| Escape route | Shallows/Storm/Long way | Encounters, ship damage, time |
| Final choice | Shaw/AetherCorp/Independent | Ending, faction status, game sequel setup |

#### MVP Scope Details

**Map: The Shattered Isles**
- A fictional archipelago in the southern Caribbean
- ~100 sea hexes
- 3-4 ports:
  - **Haven** (Free Port - neutral, starting area)
  - **Ironhull** (AetherCorp outpost - hostile/cautious)
  - **Drift** (Pirate haven - rough but free)
  - **The Bones** (Ruins - salvage site)

**Ships Available:**
- **Sloop** (starting class) - Fast, fragile, small crew
- **Brigantine** (upgrade) - Balanced, more weapons
- **Schooner** (upgrade) - Fast trader, good cargo

**Enemies:** See detailed enemy roster below.

**Bridge Crew (MVP):**
- Your First Mate (from prologue)
- 2-3 recruitable characters in the Isles
- Max roster: 5-6 for MVP

**Jobs (MVP):**
- Tier 1 only: Cutlass, Marksman, Sailor
- 5-6 abilities each
- Enough depth to demonstrate the system

---

### Enemy Roster

Enemies are divided into **Naval** (ship-to-ship combat) and **Ground** (land missions) categories. Each enemy type has variants for different difficulty tiers.

#### Faction: AetherCorp

The dominant corporate power, AetherCorp controls Aetherium production and fields professional military forces. They are the primary antagonist faction.

**Naval - AetherCorp Fleet:**

| Ship Class | Tier | Hull | Speed | Weapons | Crew | Threat Level |
|------------|------|------|-------|---------|------|--------------|
| **Skiff** | 1 | 80 | Fast | 1× Autocannon, 1× Harpoon | 8 | Low |
| **Patrol Boat** | 1 | 150 | Medium | 2× Deck Gun, 1× Flak | 15 | Medium |
| **Cutter** | 2 | 200 | Fast | 2× Deck Gun, 2× Autocannon, Missiles (2) | 20 | Medium-High |
| **Frigate** | 2 | 350 | Medium | 4× Deck Gun, 1× Railgun, Flak | 40 | High |
| **Destroyer** | 3 | 500 | Medium | 6× Deck Gun, 2× Railgun, Missiles (4), Flak | 60 | Very High |
| **Cruiser** | 3 | 800 | Slow | 8× Deck Gun, 2× Railgun, Missiles (8), 2× Flak | 100 | Extreme |

**AetherCorp Ship Behavior:**
- Patrol in pairs or groups
- Call for reinforcements when outmatched
- Prefer to disable and board (capture cargo/prisoners)
- Won't pursue into shallow waters or reefs

**Ground - AetherCorp Personnel:**

| Unit Type | Tier | HP | Armor | Weapon | Special | Count |
|-----------|------|-----|-------|--------|---------|-------|
| **Security Guard** | 1 | 40 | Vest (15) | Pistol, Baton | Calls backup | 2-4 |
| **Corp Marine** | 1 | 60 | Combat (25) | Assault Rifle | Frag grenades | 3-5 |
| **Marine Sergeant** | 2 | 80 | Combat (25) | SMG, Shotgun | Commands squad, +10% ally accuracy | 1 |
| **Heavy Gunner** | 2 | 100 | Combat (25) | LMG | Suppressing fire, immobile when firing | 1-2 |
| **Combat Medic** | 2 | 50 | Combat (25) | Pistol | Heals 30 HP/turn to allies | 1 |
| **Shock Trooper** | 2 | 80 | Powered (40) | Pulse Rifle | EMP grenades, breaches doors | 2-3 |
| **Sniper** | 2 | 45 | Light (10) | Sniper Rifle | Overwatch, +50% damage vs stationary | 1 |
| **Enforcer** | 3 | 120 | Powered (40) | Railgun, Shock Baton | Armor-piercing, melee counter | 1-2 |
| **Commander** | 3 | 100 | Powered (40) | Pulse Pistol | Buffs all allies +15% damage/accuracy | 1 |

**AetherCorp Ground Tactics:**
- Use cover effectively
- Marines advance while heavies suppress
- Medics stay in rear
- Shock troopers flank
- Commanders stay protected, buff allies

---

#### Faction: Pirates / Raiders

Desperate survivors, opportunists, and criminals. Less organized than AetherCorp but unpredictable and numerous.

**Naval - Pirate Vessels:**

| Ship Class | Tier | Hull | Speed | Weapons | Crew | Threat Level |
|------------|------|------|-------|---------|------|--------------|
| **Dinghy** | 1 | 40 | Very Fast | Small arms only | 4 | Very Low |
| **Skiff** | 1 | 60 | Fast | 1× Bolt Thrower, Molotovs | 6 | Low |
| **Raider** | 1 | 120 | Fast | 2× Autocannon, Harpoon, Fire Launcher | 12 | Medium |
| **Corsair** | 2 | 200 | Medium | 2× Deck Gun, 2× Harpoon, Flak | 25 | Medium |
| **Marauder** | 2 | 300 | Medium | 4× Deck Gun, Fire Launcher, Harpoons | 35 | High |
| **Dreadnought** | 3 | 450 | Slow | 6× Deck Gun, 2× Salvaged Railgun | 50 | High |

**Pirate Ship Behavior:**
- Aggressive, prefer to board and capture
- Flee when outgunned (hull < 40%)
- Use fire weapons liberally
- Ambush from island cover
- Some may parley (pay toll to pass)

**Ground - Pirate Personnel:**

| Unit Type | Tier | HP | Armor | Weapon | Special | Count |
|-----------|------|-----|-------|--------|---------|-------|
| **Scav** | 1 | 30 | None | Pistol or Knife | Flees at 50% HP | 3-5 |
| **Raider** | 1 | 50 | Salvage (20) | SMG, Machete | Aggressive, charges melee | 2-4 |
| **Gunner** | 1 | 45 | Light (10) | Shotgun | Close range specialist | 2-3 |
| **Brute** | 2 | 90 | Salvage (20) | Boarding Axe, Pistol | High melee damage, slow | 1-2 |
| **Sharpshooter** | 2 | 40 | None | Hunting Rifle | Long range, flees if approached | 1 |
| **Firebug** | 2 | 50 | Light (10) | Flamethrower, Molotovs | Area denial, sets fires | 1 |
| **Pirate Captain** | 2 | 80 | Combat (25) | Dual Pistols | +20% ally morale, won't flee | 1 |
| **Berserker** | 3 | 70 | None | Powered Gauntlet | Rage: +50% damage when wounded | 1 |

**Pirate Ground Tactics:**
- Swarm tactics, overwhelm with numbers
- Brutes charge while gunners flank
- Will flee if captain dies
- Unpredictable - may surrender or fight to death
- Sometimes have hostages

---

#### Faction: Scavengers

Not truly hostile - desperate survivors picking through the bones of civilization. Will fight if cornered but prefer to avoid conflict.

**Naval - Scavenger Boats:**

| Ship Class | Tier | Hull | Speed | Weapons | Crew | Threat Level |
|------------|------|------|-------|---------|------|--------------|
| **Raft** | 1 | 20 | Slow | None | 2-3 | Minimal |
| **Salvage Skiff** | 1 | 50 | Medium | 1× Bolt Thrower | 5 | Very Low |
| **Hauler** | 1 | 100 | Slow | 1× Deck Gun (often broken) | 8 | Low |

**Scavenger Behavior:**
- Flee on contact
- Will trade if approached peacefully
- Might have valuable salvage
- Occasionally ambush if desperate
- Good source of information

**Ground - Scavenger Personnel:**

| Unit Type | Tier | HP | Armor | Weapon | Special | Count |
|-----------|------|-----|-------|--------|---------|-------|
| **Scavenger** | 1 | 25 | None | Crossbow, Knife | Hides, non-aggressive | 2-4 |
| **Pack Leader** | 1 | 40 | Salvage (20) | Pistol | Commands group to flee or fight | 1 |
| **Feral** | 2 | 60 | None | Teeth, Claws | Diseased bite (poison), animalistic | 1-2 |

**Scavenger Tactics:**
- Avoid combat if possible
- Use terrain to escape
- Ferals are unpredictable (collapsed mentally)
- Can be recruited or bribed

---

#### Environmental / Wildlife Threats

The post-collapse Caribbean has dangers beyond human enemies.

**Naval Hazards:**

| Threat | Tier | Description | Effect |
|--------|------|-------------|--------|
| **Storm** | 1-3 | Weather event | Movement penalties, damage over time, visibility reduced |
| **Reef** | 1 | Shallow waters | Hull damage if entered, blocks large ships |
| **Debris Field** | 1 | Floating wreckage | Slows movement, may hide salvage or enemies |
| **Sargasso** | 2 | Dense seaweed mass | Traps ships, requires cutting free (takes turns) |
| **Rogue Wave** | 2 | Sudden large wave | Heavy damage, can capsize small ships |
| **Whirlpool** | 3 | Dangerous current | Pulls ships toward center, damage + movement loss |

**Ground Hazards:**

| Threat | Tier | HP | Description | Behavior |
|--------|------|-----|-------------|----------|
| **Guard Dog** | 1 | 20 | Trained attack animal | Alerts enemies, fast, weak |
| **Feral Dog Pack** | 1 | 15 each | Wild dogs | Hunt in packs (4-6), flee if 2+ killed |
| **Gator** | 2 | 60 | Swamp predator | Ambush from water, grab attack |
| **Coral Wasp Swarm** | 2 | 40 | Mutated insects | Poison damage, hard to hit, attracted to noise |
| **Reef Shark** | 2 | 50 | In shallow water areas | Attacks wounded targets, blood frenzy |
| **Bull Shark** | 3 | 80 | Aggressive predator | Attacks anything in water |

---

#### Elite / Boss Enemies

Unique enemies for story missions and climactic battles.

**Naval Bosses:**

| Name | Faction | Ship | Special Abilities |
|------|---------|------|-------------------|
| **The Taxman** | AetherCorp | Modified Destroyer "Revenue" | Calls reinforcements every 3 turns, EMP pulse disables player weapons for 1 turn |
| **Captain Raze** | Pirate | Dreadnought "Cinderheart" | All weapons are fire-based, leaves burning hexes, immune to fire |
| **The Ghost** | Independent | Stealth Frigate "Specter" | Cloaking (invisible until attacks), always acts first |
| **Commodore Vale** | AetherCorp | Cruiser "Dominance" | Command aura (+25% to all allied ships), personal shield (absorbs first 100 damage) |

**Ground Bosses:**

| Name | Faction | Type | HP | Armor | Special Abilities |
|------|---------|------|-----|-------|-------------------|
| **Sergeant Knox** | AetherCorp | Heavy Infantry | 150 | Powered (40) | Minigun (suppresses 3 targets), calls airstrikes (3 turn cooldown) |
| **"Stitches"** | Pirate | Combat Medic | 100 | Combat (25) | Heals self 20/turn, resurrects fallen pirates once |
| **The Collector** | Independent | Slaver | 120 | Salvage (20) | Shock net (immobilizes), attempts to capture not kill |
| **Dr. Vance** | AetherCorp | Scientist | 60 | None | Deploys combat drones (3), hacks player equipment, must be captured alive (story) |
| **Ironjaw** | Pirate | Berserker Chief | 180 | Salvage (20) | Enrage (double damage at <50% HP), intimidate (enemies flee), regenerates 10 HP/turn |

---

#### Enemy Scaling

Enemies scale based on player progression and story act:

| Act | Enemy Tier | Typical Encounters |
|-----|------------|-------------------|
| **Prologue** | Tutorial | Scripted, cannot lose |
| **Act 1** | Tier 1 | Scavengers, lone pirates, security guards |
| **Act 2** | Tier 1-2 | Pirate groups, AetherCorp patrols, marine squads |
| **Act 3** | Tier 2 | Coordinated enemies, bosses, mixed forces |
| **Endgame** | Tier 2-3 | Elite enemies, capital ships, boss encounters |

**Difficulty Modifiers:**

| Difficulty | HP Modifier | Damage Modifier | AI Behavior |
|------------|-------------|-----------------|-------------|
| **Story** | -25% | -25% | Passive, makes mistakes |
| **Normal** | 0% | 0% | Standard tactics |
| **Veteran** | +25% | +15% | Aggressive, coordinates |
| **Ironman** | +50% | +25% | Perfect tactics, no mercy |

---

### Full Game Campaign: "Abyssal Tide"

**Era:** Book 7-8 (~2085-2090)
**Scope:** Full Caribbean, 30+ hours
**Structure:** MVP story continues + expands

The full game:
- Continues from MVP ending
- Opens up the full Caribbean map
- Introduces more factions, characters, complexity
- Multiple major story arcs
- The Kraken Project events in background
- Player can influence the larger war

---

### Future Official Campaigns (DLC)

Each novel becomes an official campaign mod:

| Campaign | Novel | Era | Protagonist | Unique Features |
|----------|-------|-----|-------------|-----------------|
| The Coral Crown | Book 4 | 2055 | New captain (Shaw is NPC) | Discovery era, no Confederation |
| The Broken Bridge | Book 5 | 2060s | New captain | Canal guerilla warfare |
| The Trident Pact | Book 6 | 2076 | New captain | Political focus, forming Confederation |
| The Kraken Project | Book 7 | 2080s | New captain | Submarine introduction |
| The Serpent's Passage | Book 8 | 2090s | New captain | Full submarine gameplay |
| Abyssal Dawn | Book 9 | 2100 | New captain | Endgame content, final war |

**Note:** In novel campaigns, you play a NEW captain in that era - not the novel protagonists. Shaw and other novel characters are NPCs you encounter. This keeps the player as the hero while staying true to the novels.

---

### Modding Community Vision

The same tools we use to build official campaigns are available to modders:

**Community Can Create:**
- New campaigns (original stories in the universe)
- New time periods (pre-collapse survival, far future)
- Alternate history (what if the Canal never fell?)
- Total conversions (different settings entirely)
- Additional content for official campaigns (side quests, characters)

**We Provide:**
- Full campaign editor
- Character/ship/item definition tools
- Dialogue tree editor
- Mission builder
- Documentation and tutorials
- Curated mod showcase

---

## 20. Technical Requirements

### Map System Architecture

The game uses an **Ocean Boundaries** approach: each island is a self-contained hex map, with inter-island travel handled by a strategic ocean map. This dramatically simplifies the streaming architecture:

```
┌─────────────────────────────────────────────────────────────┐
│  LEVEL 1: OCEAN MAP (Strategic)                             │
│  ═══════════════════════════════                            │
│  • Coarse grid covering entire Caribbean (~100x75 hexes)    │
│  • Each hex = ~30km (open ocean navigation)                 │
│  • Islands are region markers (not detailed hexes)          │
│  • Used for: destination selection, sea lane routing        │
│  • Always in memory (~7,500 hexes, trivial)                 │
├─────────────────────────────────────────────────────────────┤
│  LEVEL 2: ISLAND REGION (Tactical)                          │
│  ════════════════════════════════                           │
│  • ONE island region loaded at a time                       │
│  • Each region: ~200x200 hexes (self-contained island)      │
│  • Full detail: terrain, buildings, roads, encounters       │
│  • Uses existing chunk rendering with LOD                   │
│  • No cross-region neighbors (ocean is the boundary)        │
├─────────────────────────────────────────────────────────────┤
│  LEVEL 3: CHUNK RENDERING (Existing)                        │
│  ═════════════════════════════════════                      │
│  • Within loaded island, divide into render chunks          │
│  • LOD system reduces detail at distance                    │
│  • Frustum culling hides off-screen chunks                  │
│  • Currently handles 1024 cells at 30 FPS                   │
└─────────────────────────────────────────────────────────────┘
```

**Why Ocean Boundaries Simplifies Everything:**
- NO neighbor stitching at region edges (coastline IS the edge)
- NO cross-region pathfinding (A* stays on current island)
- NO seamless streaming complexity (explicit sea travel transition)
- NO boundary rendering artifacts (ocean is the natural seam)
- Single region in memory = predictable performance

### Hex Grid (Current Implementation)

**Core Features (Built):**
- Cube coordinates (q, r, s)
- Terrain types with elevation (0-6 levels)
- Water rendering with coastal transitions
- Rivers and roads
- Chunked rendering with LOD
- Building placement
- Vegetation (trees, features)
- 30 FPS @ 1024 cells, ~917K triangles

**Needs Extension:**
- Region boundary handling (seamless loading)
- Region file format (save/load region data)
- Unit tokens on hex grid (ship, party)
- Encounter zone markers
- Dynamic content spawning

### Ocean Map System (To Build)

**Ocean Map Data:**
```csharp
public class OceanMap
{
    // Coarse strategic grid - entire Caribbean
    public int Width = 100;              // ~3,000 km
    public int Height = 75;              // ~2,250 km
    public float HexScale = 30000f;      // 30km per hex

    public OceanHex[,] Hexes;            // Ocean depth, currents, danger
    public IslandRegion[] Regions;       // Metadata for each island
    public SeaLane[] TradeLanes;         // Common shipping routes
}

public class IslandRegion
{
    public string Id;                    // "nassau", "jamaica", etc.
    public string DisplayName;           // "Nassau, Bahamas"
    public Vector2I OceanMapPosition;    // Where on ocean map
    public string RegionFile;            // "regions/nassau.region"
    public List<Port> Ports;             // Entry points
    public bool IsDiscovered;            // Fog of war
}
```

**Island Region Data Format:**
```
/regions/
  nassau.region           # MVP starting area (New Providence)
  jamaica.region          # Kingston, Port Royal
  cuba_havana.region      # Western Cuba
  cuba_santiago.region    # Eastern Cuba
  hispaniola.region       # Haiti/DR
  puerto_rico.region
  virgin_islands.region
  shattered_isles.region  # Fictional, main story area
  ...
```

**Per-Region Contents:**
- Terrain heightmap + type map
- Building/structure placements
- Road/path network
- Coastal buffer (5-10 hexes of ocean around island)
- Port locations (entry/exit points)
- Encounter spawn points
- Story event markers
- Faction control zones
- Resource locations

**Region Lifecycle (Simple!):**
```
1. Player selects destination on Ocean Map
2. Sea Travel sequence plays (encounters, time passage)
3. On arrival:
   - Save current region state to disk
   - Unload current region (QueueFree)
   - Load new region from file
   - Place ship at destination port
4. Player explores island (existing HexGrid code)
5. Repeat when sailing to new destination
```

**No Complex Streaming Logic Needed:**
- No boundary detection (player explicitly chooses to sail)
- No edge stitching (ocean separates everything)
- No adjacent region preloading (one region at a time)
- No pathfinding across regions (A* stays on island)

### Required Systems (To Build)

| System | Priority | Complexity | Status | Notes |
|--------|----------|------------|--------|-------|
| **Ocean Map System** | Critical | Low | Not started | Simple strategic map, ~7.5K hexes |
| **Sea Travel Screen** | Critical | Medium | Not started | Inter-island navigation UI |
| **Region Serializer** | Critical | Low | Not started | Save/load CellData[] per island |
| **Unit Token System** | Critical | Medium | Not started | Ship + Party tokens on hex |
| **Movement Controller** | Critical | Medium | Not started | Hex movement, coastal vs land |
| **Encounter Trigger System** | High | Medium | Not started | Land + sea encounters |
| **Away Party UI** | High | Medium | Not started | Party selection, stats display |
| **Naval Combat (same map)** | High | High | Not started | Ship vs ship on coastal hexes |
| **Party Stats Calculator** | High | Low | Not started | Aggregate party skills |
| **Turn Manager** | High | Medium | Partial | Existing foundation |
| **Dialogue System** | Medium | Medium | Not started | Conversation trees |
| **Save/Load System** | Medium | Medium | Not started | Simplified with ocean boundaries |
| **Economy/Trade** | Medium | Medium | Not started | Port-based trading |
| **AI Pathfinding (hex)** | Medium | Medium | Not started | Single-island only (simpler!) |
| **Faction System** | Low | Medium | Not started | Territory control |
| **Character Creator** | High | Medium | Not started | Initial crew setup |

**Complexity Reductions from Ocean Boundaries:**
- ~~Cross-region pathfinding~~ → Eliminated (A* stays on island)
- ~~Boundary stitching~~ → Eliminated (coastline is boundary)
- ~~Seamless streaming~~ → Replaced with explicit sea travel
- ~~Multi-region memory management~~ → One region at a time
- Save/Load: High → Medium (simpler region lifecycle)

### Unit Token System

**Ship Token:**
- 3D model on water hexes
- Represents: hull, crew, cargo
- Click to select, right-click to move
- Shows health, status indicators
- Smooth hex-to-hex movement animation

**Party Token:**
- 3D model/icon on land hexes
- Represents: away party (combined)
- Same interaction as ship
- Shows party composition indicator
- Returns to ship when reaching ship hex

**Enemy Tokens:**
- Same system for AI units
- Patrol paths on hex grid
- Detection radius (can see player)
- Behavior: patrol, pursue, flee

### Art Requirements

**2D Assets:**
- Character portraits (Bridge Crew)
- UI elements (party selection, stats display)
- Icons (items, abilities, resources, encounter types)
- Minimap elements

**3D Assets:**
- Ship models (5-10 types) - placed ON water hexes
- Ship tokens for distant view (simplified)
- Party token model (group representation)
- Building prefabs for ports/towns (ON land hexes)
- Dock/pier structures (at coast hexes)
- Terrain textures (tropical expansion)
- Vegetation variety (palms, jungle, mangroves)

### Audio Requirements

- Sea ambience (waves, wind, seabirds)
- Land ambience (jungle, town, ruins)
- Movement sounds (ship creaking, footsteps)
- Combat sounds (cannons, gunfire, melee)
- UI feedback (clicks, notifications)
- Music (exploration, combat, port, story)
- Voice acting (optional, for key story moments)

### Sea Travel System

The Sea Travel System handles inter-island navigation, providing gameplay during the transition between island regions:

**Ocean Map Interface:**
```
┌─────────────────────────────────────────────────────────────┐
│  CARIBBEAN SEA                           [Wind: NE 15kts]   │
│  ═══════════════                                            │
│                    ○ Puerto Rico                            │
│        ┌───┐                                                │
│        │YOU│══════════════▶ ○ Virgin Islands               │
│        └───┘    Route                                       │
│     Nassau      ─ ─ ─▶ ○ Jamaica                           │
│                                                             │
│     ○ Cuba                ○ Hispaniola                     │
│                                                             │
│  ───────────────────────────────────────────────────────── │
│  Destination: Virgin Islands                                │
│  Distance: ~800 km  |  Est. Time: 4 days  |  Danger: Low   │
│  [SET SAIL]  [CANCEL]                                       │
└─────────────────────────────────────────────────────────────┘
```

**Sea Travel Events:**
During the voyage, random events can occur based on:
- Distance traveled
- Danger level of route
- Current crew/ship condition
- Faction hostilities

| Event Type | Example | Resolution |
|------------|---------|------------|
| **Storm** | Tropical storm ahead | Navigate around (+1 day) or through (damage risk) |
| **Encounter** | Merchant vessel spotted | Hail, ignore, or attack |
| **Discovery** | Uncharted island | Investigate (side region) or note for later |
| **Crew Issue** | Morale dropping | Address or ignore (consequences) |
| **Naval Combat** | Pirates intercept! | Fight or flee (triggers combat) |

**Region Loading During Travel:**
```
Player clicks [SET SAIL]
        │
        ▼
   Start sea travel animation/UI
        │
        ▼
   BEGIN ASYNC: Load destination region file
        │
        ├──▶ While loading, play sea travel events
        │    (events take real time, mask load time)
        │
        ▼
   COMPLETE: Destination region ready
        │
        ▼
   Unload current region
   Swap to destination region
   Place ship at port
```

### Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| **Ocean Map Hexes** | ~7,500 | Always loaded, minimal memory |
| **Island Region Hexes** | ~40,000 | One region at a time |
| **Rendered Hexes** | 2,000-4,000 | With existing LOD/culling |
| **Frame Rate** | 30+ FPS | On mid-range hardware |
| **Region Load Time** | <3 sec | Async, masked by sea travel |
| **Region File Size** | 1-2 MB | Compressed CellData[] |
| **Memory (Island)** | ~300 MB | HexCells + chunks + meshes |
| **Memory (Ocean Map)** | ~10 MB | Coarse grid + metadata |
| **Save File Size** | <50MB | All region states + player data |

---

## Appendix A: Influences Reference

### Gameplay Influences
| Game | What We Take |
|------|--------------|
| **Ultima IV: Quest of the Avatar** | Virtue-based character creation, moral dilemmas shape identity |
| **Final Fantasy Tactics** | Deep job system, ability cross-pollination, JP economy, Faith/Brave modifiers |
| **Sid Meier's Pirates!** | Open world Caribbean, ship capture, reputation |
| **XCOM 1/2** | Tactical land combat, permadeath weight, base management, action point economy |
| **Fire Emblem** | Character relationships, permadeath consequences, class progression, weapon triangle, True Hit system, doubling attacks, support bonuses |
| **Mass Effect** | Crew loyalty missions, dialogue importance, ship as home |
| **FTL** | Ship management under pressure, crew as resource |
| **Valkyria Chronicles** | Beautiful tactical battles, named squad members |

### Combat System Influences
| Game | What We Take |
|------|--------------|
| **Fire Emblem (series)** | Simple Atk-Def damage formula, weapon triangle (+15%/-15%), True Hit system (RNG smoothing), attack speed doubling, support bonuses, displayed vs actual hit rates |
| **Final Fantasy Tactics** | Faith modifier for abilities (caster × target), terrain height advantages, status effect durations, ability JP costs |
| **BattleTech (tabletop)** | To-hit modifier tables, location-based damage, movement penalties, piloting skill rolls, hex-based arcs |
| **Car Wars** | Phased movement, turning costs based on vehicle type, weapon arcs (bow/stern/broadside), momentum and speed management |
| **Seas of Havoc** | Ship asymmetry (captain + ship cards), broadside mechanics, deck-building progression, worker placement for repairs/upgrades |

### Technical/Architecture Influences
| System | What We Take |
|--------|--------------|
| **Skyrim/Bethesda** | Moddable architecture, community content ecosystem |
| **Paradox Games** | Data-driven design, event systems, campaign mods |
| **Unity Asset Store Model** | Content as packages, mix official + community |
| **Rimworld** | Storyteller system, emergent narrative from systems |

### Combat Math Design Philosophy

The combat system is designed around these principles:

1. **Readability**: Players should be able to predict outcomes before committing. No hidden dice rolls on basic attacks. Damage ranges are narrow (±10%) so surprises are tactical, not random.

2. **Meaningful Choices**: Every modifier in the to-hit formula represents a tactical decision (positioning, cover, support). No "stat check" battles where higher numbers always win.

3. **Bounded Outcomes**: Hit chances are capped (5%-95%) so there's always hope and always risk. Critical hits are powerful (3x) but capped at 50% chance maximum.

4. **Asymmetric Balance**: Naval and land combat use different formulas reflecting their different natures:
   - **Naval**: Momentum-based, positional (arcs matter), crew as resource
   - **Land**: Action-point based, cover-centric, individual abilities matter

5. **Moddable Math**: All formulas use clearly named constants that can be overridden by campaign data. A "grittier" mod can reduce HP scaling; a "heroic" mod can increase critical multipliers.

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
| 0.4 | 2026-01-30 | Ship as Character (deep customization, ship reputation, ship death matters), clarified Shaw is NOT First Mate but legendary figure |
| 0.5 | 2026-01-30 | Complete Prologue system replacing Oracle - character creation through gameplay with your friend (future First Mate). Defined MVP: "The Gray Tide" campaign with Shaw encounter and singular task. |
| 0.6 | 2026-01-30 | Comprehensive Combat Math: Naval combat formulas (movement/wind, to-hit modifiers, weapon arcs, boarding resolution) inspired by Car Wars, BattleTech, Seas of Havoc. Land combat formulas (True Hit system, armor interaction, doubling, support bonuses) inspired by Fire Emblem, FFT. Updated weapons to post-collapse 2085 tech: modern firearms, Aetherium weapons, ballistic/improvised options, ammunition economy. Added Combat Design Philosophy to Appendix A. |
| 0.7 | 2026-01-30 | Complete Enemy Roster: AetherCorp (naval fleet + ground forces), Pirates/Raiders, Scavengers, Environmental/Wildlife hazards, Elite/Boss enemies. Includes stats, behaviors, tactics, and difficulty scaling. |
| 0.8 | 2026-01-30 | Complete Tier 1 Job Abilities: Cutlass (10 active, 3 reaction, 4 passive), Marksman (10 active, 3 reaction, 4 passive), Sailor (10 active, 3 reaction, 4 passive). Includes JP costs, unlock levels, detailed effects, cross-equipping system, and JP economy. All abilities integrate with combat math formulas. |
| 0.9 | 2026-01-30 | Complete Shaw's Task: "The Defector" - Full 4-mission story arc with Dr. Elena Vasquez extraction. Includes: The Encounter (Shaw meeting), Mission 1 "The Cover" (trade), Mission 2 "The Contact" (infiltration), Mission 3 "The Extraction" (land combat with family choice), Mission 4 "The Run" (naval chase). Three endings (Shaw/Corp/Independent), branching choices, consequences table. Core MVP narrative complete. |
| 1.0 | 2026-01-30 | **MAJOR SCOPE REVISION:** Replaced tactical grid Land Combat with **Away Party System**. Party moves as single unit through location hex clusters. Five party stats (Combat, Stealth, Tech, Medical, Social) derived from crew + gear. Encounters resolve through stat checks + player choices, not individual tactics. Reduces development time ~75% while preserving depth of party composition, equipment, and consequences. |
| 1.1 | 2026-01-30 | Revised Job System for Party-as-Unit: Jobs now provide Party Stat Contributions + Mission Abilities (limited uses per mission) + Passive Bonuses. Removed tactical abilities (Overwatch, Cleave, etc.). Added Job Synergies for party composition bonuses. Simplified leveling (1-10 instead of 1-20). Naval boarding uses same system as land. |
| 1.2 | 2026-01-30 | **Unified Single-Scale Map:** Entire game on one seamless hex map - ship sails water hexes, party walks land hexes, no loading screens. Added Region Streaming system for Caribbean-scale world (12-16 regions, ~200x200 hexes each, streamed as player moves). Updated Technical Requirements with two-level loading (Region Streaming + Chunk Rendering), Unit Token system, and performance targets. |
| 1.3 | 2026-01-30 | **Ocean Boundaries Architecture:** Replaced complex seamless streaming with island-based regions. Each island is a self-contained region naturally separated by ocean - eliminates cross-region pathfinding, boundary stitching, and neighbor synchronization. Added Ocean Map (strategic ~7.5K hex overview) for inter-island navigation. Added Sea Travel System with events/encounters during voyages. Only one island region loaded at a time = predictable performance. Dramatically simplified technical requirements while fitting the Caribbean setting perfectly. |

