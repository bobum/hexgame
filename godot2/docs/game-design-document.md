# ABYSSAL TIDE - Game Design Document

**Version:** 0.2 (Draft)
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
6. [Captain Creation](#6-captain-creation)
7. [Crew System](#7-crew-system)
8. [Job & Ability System](#8-job--ability-system)
9. [Naval Gameplay](#9-naval-gameplay)
10. [Land Gameplay](#10-land-gameplay)
11. [Economy & Resources](#11-economy--resources)
12. [Progression Systems](#12-progression-systems)
13. [Modding Architecture](#13-modding-architecture)
14. [Technical Requirements](#14-technical-requirements)

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

## 6. Captain Creation

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

## 7. Crew System

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

## 8. Job & Ability System

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

## 9. Naval Gameplay

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

## 10. Land Gameplay

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

## 11. Economy & Resources

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

## 12. Progression Systems

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

## 13. Modding Architecture

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

## 14. Technical Requirements

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
| **Skyrim/Bethesda** | Moddable architecture, community content ecosystem |

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

