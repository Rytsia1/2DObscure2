# GAME DESIGN DOCUMENT | SKILL SYSTEM

# **SKILL SYSTEM SPECIFICATION**
*Character Progression • Skill Trees • Combat Integration • Unity Architecture*

**Document Version:** v1.0  
**Project:** Party-Based Turn-Based Roguelike RPG (2D Obscure)  
**Status:** Working Design Draft — Foundation & Global Architecture  
**Author:** Senior Game Designer (RPG Systems)  
**Date:** November 2026  

---

## 1. Overview & High Concept Integration

The Skill System is the primary mechanical framework governing character customization, combat action economy, build variety, and strategic synergy in the game. It bridges character level progression, equipment selection, reaction-based QTE combat, and dungeon exploration across the 8-dungeon campaign.

### 1.1 Core Game Pillars & System Context
- **Party Composition:** Exactly four playable characters in an active combat party. The player drafts or recruits characters across nine distinct classes.
- **Turn-Based Individual Action Economy:** Combat runs on individual character turns driven by Action Speed (DEX/SPD). Each turn grants 1 Action (Skill, Attack, Item, or Defend). Skills form the core tactical decision space.
- **Reaction-Based QTE Integration:** Every active offensive skill triggers an offensive QTE (bar timing for physical skills, rune-typing for magical spells) that dynamically scales direct damage (0.5× / 1.0× / 1.5×).
- **Sequential 8-Dungeon Progression:** Characters progress from Level 1 to Level 15 through eight sequential dungeons. Skill point allocation represents permanent character progression within a run, while combat buffs, debuffs, and stances reset between battles.
- **Hub & Preparation Camp:** Between dungeon runs and at designated rest sites, players interact with the Skill Tree to allocate Skill Points (SP), adjust active/passive loadouts, and respec builds to counter upcoming dungeon threats.

---

## 2. Design Goals

1. **Distinct Gameplay Identity:** Every class features a unique core combat mechanic (e.g., Fighter's Weapon Techniques, Barbarian's Berserk, Martial Artist's Combo Flow, Wizard's Multi-Elemental Mastery, Warlock's Independent Curses).
2. **Multiple Viable Builds per Class:** Each class tree contains three distinct specialization branches, allowing at least 2–3 radically different playstyles per class.
3. **Encourage Strategic Party Synergy:** Skills are designed with cross-class combos in mind (e.g., physical Break combos, status exploitation, elemental vulnerability chaining, hybrid healing/warding).
4. **Flawless Integration with Existing Systems:** Full compliance with the *Consolidated Damage & Status Effect GDD v1.0*, *Race System PRD*, and *Equipment System GDD*.
5. **Campaign Progression Pacing:** Skill acquisition scales naturally from Level 1 to 15 across the eight sequential dungeons through a 5-Tier gating model.
6. **Meaningful Skill Point Decisions:** Total SP is strictly limited (38 SP max at Level 15). A player cannot max out all skills in a tree, forcing intentional trade-offs between deep specialization and hybrid versatility.
7. **Anti-Dominant Meta Design:** No single skill or build is universally optimal; enemy elemental affinities, resistance types, and boss mechanics require tactical loadout adaptation.
8. **Data-Driven Configuration:** Complete decoupling of skill logic from core combat code using Unity ScriptableObjects and serializable JSON schemas for rapid balancing.
9. **Deep Combat Interactions:** Skills directly interface with Physical/Elemental/Almighty damage types, 8 elements, QTE multipliers, Critical rolls, Null Break, Break Gauges, and 15 status effects.
10. **High Combat Feedback & Readability:** Intuitive UI tooltips, clear telegraphing of status chances/durations, and explicit visual/audio feedback on QTE grades and status applications.

---

## 3. Skill System Architecture & Action Economy

```
┌────────────────────────────────────────────────────────┐
│                      HUB / CAMP                        │
│  - Spend SP to unlock / upgrade Skill Tree Nodes       │
│  - Equip Loadout: 3 Active Skills + 3 Passive Skills   │
│  - Respec SP (Gold cost scaling with Level)           │
└──────────────────────────┬─────────────────────────────┘
                           │ Enters Combat
┌──────────────────────────▼─────────────────────────────┐
│                 TURN-BASED COMBAT                      │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 1. Active Turn: Choose 1 Action                  │  │
│  │    [Basic Attack] [Equipped Skill (1-3)] [Item]  │  │
│  └──────────────────────────┬───────────────────────┘  │
│                             │                          │
│  ┌──────────────────────────▼───────────────────────┐  │
│  │ 2. Resource & Cooldown Check                     │  │
│  │    - Check MP >= Skill Cost                      │  │
│  │    - Check Cooldown == 0                         │  │
│  └──────────────────────────┬───────────────────────┘  │
│                             │                          │
│  ┌──────────────────────────▼───────────────────────┐  │
│  │ 3. Target Selection                              │  │
│  │    Enemy, AllEnemies, Ally, AllAllies, Self      │  │
│  └──────────────────────────┬───────────────────────┘  │
│                             │                          │
│  ┌──────────────────────────▼───────────────────────┐  │
│  │ 4. QTE Resolution Window                         │  │
│  │    Physical: Timing Bar | Spell: Rune Typing     │  │
│  │    Result: Miss (0.5x), Good (1.0x), Perf (1.5x) │  │
│  └──────────────────────────┬───────────────────────┘  │
│                             │                          │
│  ┌──────────────────────────▼───────────────────────┐  │
│  │ 5. Damage / Heal / Status Execution Pipeline     │  │
│  │    - Critical Roll (Physical only)               │  │
│  │    - Defense / Elemental Resistance / Affinity   │  │
│  │    - Break Gauge Reduction                       │  │
│  │    - Independent Status Roll & Application       │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Passive Skills (3 Slots): Event-Driven Hooks     │  │
│  │ OnTurnStart, OnHit, OnCrit, OnDamageTaken, etc.  │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

### 3.1 Combat Action Economy
- **Turn Action:** On their turn, a combatant has **1 Action**. Casting an Active Skill consumes this action.
- **Resource Pools:** Characters possess **HP** and **MP** only. There are no secondary class-specific resource pools (e.g. Rage or Energy) as separate gauges. Unique class dynamics (such as Barbarian Berserk, Martial Artist Combo Flow, and Ranger Hunt Mark) are modeled via status instances, counters, and passive event triggers.
- **Skill Loadout System:**
  - A character may unlock numerous skills in their skill tree, but may only equip **3 Active Skills** and **3 Passive Skills** into battle.
  - Loadouts are locked during combat encounters.
  - Loadouts may be swapped freely in the Hub, at Camp nodes, and at Rest Sites.
- **Cooldown Management:**
  - Active skills have a turn-based cooldown ($0$ to $5$ turns).
  - Cooldown decrements at the start of the character's turn.
  - A skill with a cooldown of $N$ cannot be used again until $N$ full turns of that character have elapsed.

---

## 4. Skill Categories & Mechanics

Skills are classified into six structural categories. Each category serves a distinct functional role in the turn-based loop:

| Skill Category | Action Cost | Resource Cost | QTE Trigger | Cooldown | Loadout Slot | Description |
|---|---|---|---|---|---|---|
| **Active Skill** | 1 Turn Action | MP Cost | Yes (Offensive) | 0–3 turns | Active (1 of 3) | Direct combat attacks, offensive spells, active heals, or targeted tactical moves executed on character's turn. |
| **Passive Skill** | 0 Actions (Innate) | None | No | None | Passive (1 of 3) | Event-driven modifiers listening to combat events (`OnHitDealt`, `OnTurnStart`, `OnCrit`, etc.) providing stat boosts or conditional triggers. |
| **Ultimate Skill** | 1 Turn Action | High MP Cost | Yes (High Stakes) | 4–6 turns / Once per Battle | Active (1 of 3) | Apex Tier 5 abilities that dramatically alter combat momentum. Restricted by heavy MP cost and encounter limits. |
| **Class Signature** | Variable | 0 or Low MP | Contextual | 0–2 turns | Innate or Active | Foundational skills defining the class identity (e.g., Barbarian's *Berserk*, Ranger's *Hunt Mark*, Fighter's *Weapon Mastery*). |
| **Utility Skill** | 1 Turn Action | Moderate MP | No / Simple QTE | 1–3 turns | Active (1 of 3) | Non-damaging tactical tools: cleanses, dispels, aggro manipulation (Taunt/Stealth), evasion stances, and battlefield positioning. |
| **Support Skill** | 1 Turn Action | Moderate/High MP | Optional (Timing) | 1–4 turns | Active (1 of 3) | Ally-targeted abilities: HP restoration, shield barriers, stat buffs (AttackUp, DefenseUp), and elemental warding. |

### 4.1 Mechanical Interactions with Turn-Based Flow
- **Active & Utility/Support Skills:** Evaluated immediately upon selection. They check resource availability, deduct MP, initiate the QTE window, process damage/healing/status, and trigger target reaction animations.
- **Passive Skills:** Require no manual invocation. They subscribe to the `CombatEventDispatcher`. When specific trigger conditions occur (e.g., target drops below 30% HP, player lands a Perfect QTE, player takes physical damage), the passive automatically executes its payload.
- **Ultimate Skills:** Occupy an Active Skill loadout slot, forcing players to choose between balanced rotation consistency and high-impact burst potential.

---

## 5. Skill Progression & Economy

### 5.1 Level Progression & Tier Gating
Characters progress from Level 1 to the Level Cap of **Level 15** across the 8-dungeon campaign. The skill tree is partitioned into **Five Tiers**, unlocking at specific character level milestones:

| Tier | Character Level Required | Progression Context | Node Types Available |
|---|---|---|---|
| **Tier 1** | Level 1 | Starter / Baseline | Foundational Active Skills, Core Class Signature, Basic Passives |
| **Tier 2** | Level 4 | Early Dungeon Specialization | Branch Specialization Actives, Synergistic Passives, Basic Utility |
| **Tier 3** | Level 7 | Mid-Game Mastery | Advanced Actives, Cross-Branch Synergy Passives, Potent Buffs/Debuffs |
| **Tier 4** | Level 10 | Late-Game Specialization | High-impact Branch Keystones, Major Status Enablers, Null Break Skills |
| **Tier 5** | Level 13 | Endgame Apex | Class Ultimate Skills, Master Passives, Apex Specialization Caps |

### 5.2 Skill Point (SP) Acquisition Economy
Players earn Skill Points (SP) through two distinct vectors:
1. **Level-Up SP:**
   - Level 1: 2 SP starting pool.
   - Levels 2–15: 2 SP awarded per level-up ($14 \times 2 = 28\text{ SP}$).
   - Subtotal from leveling: **30 SP**.
2. **Dungeon Milestone SP:**
   - Clearing each of the 8 sequential dungeons awards **1 Milestone SP** directly to each party member.
   - Subtotal from dungeon exploration: **8 SP**.
3. **Total SP Available at Endgame (Level 15 + All Dungeons Cleared):** **38 SP**.

### 5.3 Skill Ranks & Investment Costs
To create meaningful progression, skills feature multi-rank upgrades:
- **Standard Active & Support Skills:** **3 Ranks (Rank 1–3)**.
  - Rank 1 (Unlock): 1 SP
  - Rank 2 (Upgrade): 1 SP
  - Rank 3 (Mastery): 2 SP
  - *Total to master a standard skill: 4 SP.*
- **Passive Nodes:** **2 or 3 Ranks** (1 SP per rank) or **1-Rank Keystones** (2 SP).
- **Ultimate Skills:** **1 Rank (Mastery upon unlock)**: Costs **3 SP** (requires Level 13+ and deep branch investment).

> **Balancing Principle:** With an average class tree containing 15–18 nodes totaling ~60–70 SP of potential investment, a player with 38 SP can:
> - Fully master **one complete branch** (~16–18 SP) and heavily invest in a **second branch** (~14–16 SP), OR
> - Build a broad **tri-hybrid build** across all three branches without reaching Tier 5 apex passives.
> This guarantees high build divergence and prevents homogenizing characters.

### 5.4 Progression Scopes: Permanent vs. Run vs. Combat

| Progression Layer | Scope | Persistence Rules |
|---|---|---|
| **Character Level & SP** | Permanent within Run | Persists across all 8 sequential dungeons. Lost upon complete party defeat (permadeath). |
| **Unlocked Skill Nodes** | Permanent within Run | Remains unlocked once purchased. Persists until player chooses to respec. |
| **Equipped Loadout** | Run / Hub Configurable | Persists between battles; can be freely reconfigured in Hub, Camps, and Rest nodes. |
| **Cooldowns & Stances** | Combat-Only | Reset completely at the end of each battle. Every combat encounter begins with all skills ready. |
| **Buffs, Debuffs & Statuses**| Combat-Only | All status instances, Burn/Poison/Bleed ticks, and stat modifiers expire when combat terminates. |
| **Break Gauge & HP/MP** | Encounter / Run | Break Gauges reset per battle. HP/MP carry over across dungeon nodes (recovered via items, rest, or cleric skills). |

### 5.5 Skill Respecification (Respec) Rules
- **Location:** Respec is available at the **Hub** between dungeon expeditions and at **Rest Sites** inside dungeons.
- **Cost:** Costs Gold scaling with character level:
  $$\text{Respec Cost} = \text{Character Level} \times 50\text{ Gold}$$
  *(e.g., Level 4 respec = 200 Gold; Level 10 respec = 500 Gold).*
- **Outcome:** 100% of all invested SP is refunded. Active/Passive loadouts are cleared and must be re-equipped before entering combat.

---

## 6. Skill Tree Architecture & Structural Rules

Every class possesses a dedicated Skill Tree structured into **three thematic specialization branches**.

```
                [ CLASS CORE / SIGNATURE (Tier 1) ]
                               │
       ┌───────────────────────┼───────────────────────┐
       ▼                       ▼                       ▼
  BRANCH A:               BRANCH B:               BRANCH C:
  Archetype 1             Archetype 2             Archetype 3
  
  [Tier 1: Active A1]     [Tier 1: Active B1]     [Tier 1: Active C1]
  [Tier 1: Passive A1]    [Tier 1: Passive B1]    [Tier 1: Passive C1]
       │                       │                       │
       ▼                       ▼                       ▼
  [Tier 2: Active A2]     [Tier 2: Active B2]     [Tier 2: Active C2]
  [Tier 2: Passive A2]    [Tier 2: Passive B2]    [Tier 2: Passive C2]
       │                       │                       │
       ├───────────────┬───────┴───────┬───────────────┤
       ▼               ▼               ▼               ▼
  [Tier 3: Active A3] [Tier 3: Cross-Synergy AB] [Tier 3: Active C3]
       │                               │               │
       ▼                               ▼               ▼
  [Tier 4: Keystone A4]           [Tier 4: Keystone B4] [Tier 4: Keystone C4]
       │                               │               │
       └───────────────────────┬───────┴───────────────┘
                               ▼
                    [ TIER 5: ULTIMATE SKILL ]
```

### 6.1 Node Types & Visual Grammar
1. **Active Node (Square):** Clickable combat actions requiring MP and turn action.
2. **Passive Node (Circle):** Stat boosts or triggered combat hooks.
3. **Keystone Node (Hexagon):** Build-defining game-changers that alter core mechanics.
4. **Ultimate Node (Diamond):** High-cost apex abilities at Tier 5.

### 6.2 Unlock Requirements & Prerequisites
To unlock a node, a character must satisfy three conditions:
1. **Level Check:** Character Level $\ge$ Tier Minimum Level ($1, 4, 7, 10, 13$).
2. **Prerequisite Edge:** The directly connected predecessor node must have at least Rank 1 unlocked.
3. **Branch Investment Threshold:** Higher-tier nodes require cumulative SP spent within that specific branch:
   - Tier 1: 0 SP required
   - Tier 2: 2 SP spent in branch
   - Tier 3: 5 SP spent in branch
   - Tier 4: 9 SP spent in branch
   - Tier 5 (Ultimate): 14 SP spent in associated branch(es)

---

## 7. Data-Driven Skill Data Structure (Unity Architecture)

To ensure high maintainability and runtime performance in Unity, the Skill System utilizes the **Flyweight Pattern**, cleanly separating static, immutable definitions from mutable runtime state.

### 7.1 Architecture Breakdown

```
┌────────────────────────────────────────────────────────┐
│             SkillDef (ScriptableObject)                │
│  - Immutable static data defined in Unity Editor / JSON│
│  - Contains BasePower, Scaling, TargetType, Elements,  │
│    QTE profile, Animation / VFX / SFX references.      │
│  - Lives in Resources / Addressables (Shared).         │
└──────────────────────────┬─────────────────────────────┘
                           │ Instantiated & Managed by
┌──────────────────────────▼─────────────────────────────┐
│                 SkillInstance (C# Class)               │
│  - Mutable runtime state attached to Combatant.        │
│  - CurrentRank (1 to 3)                                │
│  - CurrentCooldown (turns remaining)                   │
│  - Runtime stat overrides and temporary buffs          │
└────────────────────────────────────────────────────────┘
```

### 7.2 C# Core Data Contracts

```csharp
namespace Obscure.Skills
{
    public enum SkillCategory { Active, Passive, Ultimate, Signature, Utility, Support }
    public enum SkillType { PhysicalAttack, ElementalSpell, AlmightySpell, Buff, Debuff, Heal, Shield, Cleanse, Stance }
    public enum TargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self, RandomEnemy }
    public enum DamageCategory { None, Physical, Elemental, Almighty }
    public enum ElementType { None, Fire, Ice, Lightning, Earth, Wind, Water, Holy, Shadow }
    public enum QteType { None, TimingBar, SequenceTyping, RapidTap }

    [System.Serializable]
    public class SkillRankData
    {
        public int rank;
        public int spCost;
        public int mpCost;
        public int cooldown;
        public float basePower;
        public float statScaling;
        public float secondaryScaling;
        public float breakMultiplier;
        public int duration;
        public StatusApplicationDef[] statusApplications;
    }

    [System.Serializable]
    public class StatusApplicationDef
    {
        public StatusId statusId;
        public float applicationChance; // 0.0 to 1.0 (modified by target resistance)
        public int durationTurns;
        public float magnitude;
        public bool applyPerHit;        // true = roll per hit; false = roll once per skill execution
        public bool targetSelf;
    }

    [CreateAssetMenu(fileName = "NewSkillDef", menuName = "Obscure/Skill Definition")]
    public class SkillDef : ScriptableObject
    {
        [Header("Metadata")]
        public string skillId;
        public string skillName;
        [TextArea(2, 4)] public string skillDescription;
        public string classId;
        public int branchIndex; // 0, 1, or 2
        public int tier;        // 1 to 5
        public SkillCategory category;
        public SkillType skillType;
        public TargetType targetType;

        [Header("Damage & Typing")]
        public DamageCategory damageCategory;
        public ElementType element;
        public Ability primaryScalingStat = Ability.STR;
        public Ability secondaryScalingStat = Ability.None;
        public int hitCount = 1;
        public bool appliesNullBreak;
        public ElementType nullBreakTargetElement;

        [Header("QTE Configuration")]
        public QteType qteType;
        public string qteWord;              // for rune-typing spell QTE
        public float qteDifficulty = 1.0f;  // speed / timing window modifier

        [Header("Rank Progression")]
        public int maxRank = 3;
        public SkillRankData[] ranks;

        [Header("Prerequisites & Tags")]
        public string[] prerequisiteSkillIds;
        public string[] tags; // e.g. "WeaponTechnique", "Unarmed", "Arcane", "Curse"

        [Header("Presentation")]
        public string animationTrigger;
        public string vfxId;
        public string sfxCastId;
        public string sfxHitId;

        public SkillRankData GetRank(int rank) => ranks[Mathf.Clamp(rank - 1, 0, ranks.Length - 1)];
    }

    public class SkillInstance
    {
        public SkillDef Definition { get; private set; }
        public int CurrentRank { get; set; }
        public int CurrentCooldown { get; set; }

        public SkillInstance(SkillDef def, int rank = 1)
        {
            Definition = def;
            CurrentRank = rank;
            CurrentCooldown = 0;
        }

        public SkillRankData CurrentData => Definition.GetRank(CurrentRank);
        public bool IsReady => CurrentCooldown <= 0;
        public void OnTurnStart() { if (CurrentCooldown > 0) CurrentCooldown--; }
        public void TriggerCooldown() { CurrentCooldown = CurrentData.cooldown; }
    }
}
```

### 7.3 JSON Schema for Data-Driven Ingestion

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "SkillDefinition",
  "type": "object",
  "required": [
    "skillId", "skillName", "classId", "branchIndex", "tier",
    "category", "skillType", "targetType", "damageCategory",
    "element", "primaryScalingStat", "hitCount", "ranks"
  ],
  "properties": {
    "skillId": { "type": "string" },
    "skillName": { "type": "string" },
    "skillDescription": { "type": "string" },
    "classId": { "type": "string", "enum": ["fighter", "barbarian", "martial_artist", "rogue", "ranger", "cleric", "wizard", "warlock", "druid"] },
    "branchIndex": { "type": "integer", "minimum": 0, "maximum": 2 },
    "tier": { "type": "integer", "minimum": 1, "maximum": 5 },
    "category": { "type": "string", "enum": ["Active", "Passive", "Ultimate", "Signature", "Utility", "Support"] },
    "skillType": { "type": "string", "enum": ["PhysicalAttack", "ElementalSpell", "AlmightySpell", "Buff", "Debuff", "Heal", "Shield", "Cleanse", "Stance"] },
    "targetType": { "type": "string", "enum": ["SingleEnemy", "AllEnemies", "SingleAlly", "AllAllies", "Self", "RandomEnemy"] },
    "damageCategory": { "type": "string", "enum": ["None", "Physical", "Elemental", "Almighty"] },
    "element": { "type": "string", "enum": ["None", "Fire", "Ice", "Lightning", "Earth", "Wind", "Water", "Holy", "Shadow"] },
    "primaryScalingStat": { "type": "string", "enum": ["STR", "DEX", "VIT", "INT", "WIS", "CHA"] },
    "hitCount": { "type": "integer", "minimum": 1, "default": 1 },
    "appliesNullBreak": { "type": "boolean", "default": false },
    "nullBreakTargetElement": { "type": "string", "enum": ["None", "Fire", "Ice", "Lightning", "Earth", "Wind", "Water", "Holy", "Shadow", "Physical"] },
    "ranks": {
      "type": "array",
      "minItems": 1,
      "maxItems": 3,
      "items": {
        "type": "object",
        "required": ["rank", "spCost", "mpCost", "cooldown", "basePower", "statScaling"],
        "properties": {
          "rank": { "type": "integer", "minimum": 1, "maximum": 3 },
          "spCost": { "type": "integer", "minimum": 1 },
          "mpCost": { "type": "integer", "minimum": 0 },
          "cooldown": { "type": "integer", "minimum": 0 },
          "basePower": { "type": "number" },
          "statScaling": { "type": "number" },
          "breakMultiplier": { "type": "number", "default": 1.0 },
          "duration": { "type": "integer", "default": 0 },
          "statusApplications": {
            "type": "array",
            "items": {
              "type": "object",
              "required": ["statusId", "applicationChance", "durationTurns"],
              "properties": {
                "statusId": { "type": "string" },
                "applicationChance": { "type": "number", "minimum": 0.0, "maximum": 1.0 },
                "durationTurns": { "type": "integer", "minimum": 1 },
                "magnitude": { "type": "number", "default": 0.0 },
                "applyPerHit": { "type": "boolean", "default": false },
                "targetSelf": { "type": "boolean", "default": false }
              }
            }
          }
        }
      }
    },
    "prerequisites": { "type": "array", "items": { "type": "string" } },
    "tags": { "type": "array", "items": { "type": "string" } },
    "animationId": { "type": "string" },
    "vfxId": { "type": "string" },
    "sfxId": { "type": "string" }
  }
}
```

---

## 8. Combat Integration & Rules Alignment

All skill executions strictly adhere to the mathematical pipeline defined in the *Consolidated Damage & Status Effect GDD v1.0*.

### 8.1 Base Damage Formulas
The base raw damage for a skill hit is determined by its Damage Category:

$$\text{Base Physical Damage} = \text{SkillPower} + (\text{STR} \times \text{Scaling})$$
*(Note: Ranged weapon skills and Rogue finesse skills substitute $\text{DEX}$ for $\text{STR}$.)*

$$\text{Base Elemental Damage} = \text{SkillPower} + (\text{INT} \times \text{Scaling})$$
*(Note: Cleric Holy attacks and Druid elemental spells substitute $\text{WIS}$ for $\text{INT}$.)*

$$\text{Base Almighty Damage} = \text{SkillPower} + (\text{INT} \times \text{Scaling})$$

### 8.2 QTE Multipliers & Interaction
- Direct damage of all components (Physical, Elemental, Almighty) is scaled by the player's QTE execution:
  - **Perfect:** $1.5\times$
  - **Good:** $1.0\times$
  - **Miss:** $0.5\times$
- **Damage Over Time (DoT) Isolation:** QTE performance **never** affects DoT damage ticks (Burn, Bleed, Poison). DoT damage is calculated dynamically on the target's turn start based on source stats and relevant bonuses.

### 8.3 Critical Hit Pipeline
- **Restriction:** Critical Hits apply **exclusively to Physical Damage components**. Elemental and Almighty components cannot roll critical hits.
- **Roll Cadence:** Rolled independently per hit for multi-hit attacks.
- **Caps:**
  $$\text{CRIT}_{\text{final}} = \min(\text{CRIT}_{\text{base}} + \text{CRIT}_{\text{bonus}}, 95\%)$$
  $$\text{CRITDMG}_{\text{final}} = \min(150\% + \text{CRITDMG}_{\text{bonus}}, 300\%)$$
- **Order of Operations:** The Critical Multiplier is applied immediately after the QTE multiplier and before subtracting flat Physical Defense. Critical hits **cannot** bypass Physical Nullification.

### 8.4 Enemy Affinity & Null Break Resolution
Enemy affinities apply directly to matching damage components:
- **Weakness:** $1.5\times$ damage multiplier.
- **Normal:** $1.0\times$ damage multiplier.
- **Resistant:** $0.5\times$ damage multiplier.
- **Nullification:** $0$ damage. Nullification cannot be reduced or bypassed by damage bonuses.
- **Null Break Integration:**
  - When a skill with Null Break hits an enemy possessing Nullification for the specified element/damage type, Nullification is converted to **Normal** for the duration.
  - Duration scales by skill level: **Rank 1 = 1 turn, Rank 2 = 2 turns, Rank 3 = 3 turns**.
  - **Limit:** Exactly 1 active Null Break per enemy. It does not stack, refresh, or extend. Different enemies may each have an independent Null Break active.

### 8.5 Damage Modifier Stacking
Damage Bonuses and Damage Taken modifiers stack **multiplicatively**:
$$\text{Multiplier}_{\text{total}} = \prod (1 + \text{Bonus}_i)$$
*(Example: A +20% Physical Damage Bonus buff and a +30% All Damage Bonus buff yield $1.2 \times 1.3 = 1.56\times$ total multiplier).*

### 8.6 Multi-Hit & Multi-Type Resolution Pipeline
For any attack, the calculation proceeds per hit:
1. Calculate raw base damage for each component.
2. Apply relevant Damage Bonuses (Physical/Elemental/Element-Specific/All) multiplicatively.
3. Apply QTE Multiplier ($0.5\times$, $1.0\times$, $1.5\times$) to all direct components.
4. If Physical, roll Critical. If successful, apply $\text{CRITDMG}_{\text{final}}$.
5. Deduct flat Physical Defense (Physical only; damage can reach $0$, no minimum $1$ floor).
6. Apply Damage Taken modifiers.
7. Apply Target Affinity (Weakness/Normal/Resistant/Nullification). If target is a player, apply Player Elemental Resistance ($\text{Cap } 75\%$).
8. Sum component values in floating-point precision, then apply `Mathf.FloorToInt` to produce the final integer damage for that hit.

---

## 9. Status Effect Integration Rules

Status effects are determined explicitly by skill definitions. Dealing elemental damage **does not** automatically inflict a status effect.

### 9.1 Universal Status Effect Resistance
Status resistance reduces the **probability of application**, never the duration or potency of an applied status:

$$\text{Chance}_{\text{final}} = \text{Chance}_{\text{base}} \times (1 - \text{Resistance}_{\text{final}})$$
$$\text{Resistance}_{\text{final}} = \min(\sum \text{Resistance}_{\text{sources}}, 75\%)$$

Resistance reductions stack multiplicatively:
$$\text{Resistance}_{\text{after}} = \max(0, \text{Resistance}_{\text{initial}} \times \prod(1 - \text{Debuff}_i))$$

### 9.2 Status Effect Taxonomy & Interaction Matrix

| Status Effect | Category | Primary Mechanical Behavior | Tick Timing / Expiry | Boss Rule |
|---|---|---|---|---|
| **Burn** | Non-Mental DoT | Deals Fire Elemental DoT scaling with skill source stat. | Target turn start. Dynamic recalculation. | High Resistance. Can apply. |
| **Bleed** | Non-Mental DoT | Deals Physical DoT scaling with skill source stat. | Target turn start. Dynamic recalculation. | High Resistance. Can apply. |
| **Poison** | Non-Mental DoT | Deals skill-defined DoT (typically Nature theme / Shadow). | Target turn start. Dynamic recalculation. | High Resistance. Can apply. |
| **Freeze** | Non-Mental Debuff | Reduces target Speed (SPD) by a specified percentage; delays turns. | Decrements on target turn end. | High Resistance. Can apply. |
| **Shock** | Non-Mental Debuff | Target has a % chance to fail their Action on their turn. | Decrements on target turn end. | High Resistance. Can apply. |
| **Curse** | Non-Mental Debuff | Custom negative condition defined per Warlock skill. Independent instances. | Turn start/end per curse definition. | High Resistance. Can apply. |
| **Stun** | Non-Mental Control | Target loses their entire Action turn. | Expires after skipping turn. | High Resistance. Can apply. |
| **Mind Control** | Mental Effect | Target loses control; opposing party selects target's action. | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Confusion** | Mental Effect | Target acts, but has a % chance to target wrong ally/enemy. | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Fear** | Mental Effect | Combat Debuff: Target's Damage Dealt is reduced by % magnitude. | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Despair** | Mental Effect | Action Disruption: Target has a % chance to fail actions. | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Charm** | Mental Effect | Target cannot select the charmer as an attack target. | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Sleep** | Mental Effect | Target incapacitated. Breaks immediately upon receiving direct damage or DoT tick. | Breaks on damage or turn expiry. | **IMMUNE.** Cannot apply. |
| **Silence** | Mental Effect | Target cannot cast Skills (Basic Attack and Items remain valid). | Decrements on turn end. | **IMMUNE.** Cannot apply. |
| **Madness** | Mental Effect | AI chooses random action and random target (can hit friendly units). | Decrements on turn end. | **IMMUNE.** Cannot apply. |

### 9.3 Instance Independence Rule
- Multiple applications of the same status effect create **independent instances**.
- A new application **does not refresh or extend** the duration of existing instances.
- There is no upper limit on concurrent status instances on a target.
- For multi-hit skills, `applyPerHit = true` rolls application independently on each hit, potentially creating multiple instances from a single skill execution.

### 9.4 Boss Rules
- **Non-Mental Effects:** Bosses possess high base Universal Status Resistance (typically $50\%\text{--}70\%$), but are susceptible. Resistance reduction debuffs function normally against them.
- **Mental Effects:** Bosses are **100% IMMUNE** to all Mental Effects. Resistance reductions **cannot** bypass this immunity under any circumstances.

---

## 10. Break & Stagger Mechanics Integration

1. **Break Gauge:** Every enemy has a Break Gauge (default $40\text{--}120$ points depending on tier).
2. **Break Multiplier:** Every skill has a `breakMultiplier` property (baseline $1.0\times$). High-impact blunt or crushing skills possess higher multipliers ($1.3\times\text{--}1.8\times$).
3. **Break Execution:**
   $$\text{Break Damage} = \text{Base Damage} \times \text{breakMultiplier}$$
4. **Broken State:**
   - When the Break Gauge reaches $0$, the enemy enters the **Broken** state for $1$ turn.
   - Broken enemies suffer **Action Stun** (skip next turn).
   - Attacks against Broken enemies gain a **$1.5\times$ Break Damage Multiplier** plus any skill-specific `bonusVsBroken` multipliers.
   - Bosses recover their Break Gauge automatically when transitioning to Phase 2.

---

## 11. Proposed Class Skill Tree Archetypes (Overview)

The nine classes will be developed individually according to the approved architecture:

| Class | Core Identity & Mechanic | Branch 1 | Branch 2 | Branch 3 |
|---|---|---|---|---|
| **1. Fighter** | Frontline Weapon Master & Tactician | **Shield & Bulwark** (Tank/Guardian) | **Vanguard Striker** (Cleave/Physical Burst) | **Tactical Warlord** (Stance/Buffs/Control) |
| **2. Barbarian** | High-Risk Berserk & Physical Overwhelm | **Berserker Fury** (Rage/Damage Taken vs Dealt) | **Colossus Might** (Break/Stagger/Crushing) | **Bloodseeker** (Bleed/Lifesteal/Execute) |
| **3. Martial Artist** | Unarmed Combo Flow & Precision Finisher | **Iron Palm** (High Break/Stun/Impact) | **Flowing Crane** (Speed/Evasion/Counters) | **Dragon Qi** (Multi-Hit/Combo Finishers) |
| **4. Rogue** | Exploitation, Critical Hits & Weakness Striker | **Shadow Assassin** (Stealth/Critical Burst) | **Poison & Debilitation** (Poison/Status Sapping) | **Scoundrel Tricks** (Evasion/Confusion/Expose) |
| **5. Ranger** | Ranged Precision & Targeted Hunt Mark | **Sharpshooter** (Single-Target Sniper/Crit) | **Beast & Nature Scout** (Tracking/Survival/Traps)| **Barrage Marksman** (Multi-Hit/Volley/AoE) |
| **6. Cleric** | Hybrid Healer, Holy Smiter & Divine Shield | **Radiant Dawn** (Direct & AoE Healing/Regen) | **Divine Templar** (Holy Melee/Smite/Offense) | **Sacred Aegis** (Shields/Wards/Buffs) |
| **7. Wizard** | Multi-Elemental Specialist (Fire/Ice/Lightning/etc.) | **Pyromancy & Storm** (Fire Burn/Lightning Shock) | **Cryomancy & Depth** (Ice Freeze/Water Control) | **Earth & Arcane** (Earth Break/Almighty Missiles) |
| **8. Warlock** | Curse Master (Independent Curses & Dark Pacts) | **Affliction Hexes** (Independent Curse Stacking) | **Abyssal Destruction** (Shadow Burst/Curse Pop) | **Eldritch Pact** (Life Drain/Despair/Fear) |
| **9. Druid** | Nature's Control (Hybrid Support/Control/Shifting)| **Restoration Grove** (Healing/HoT/Cleansing) | **Feral Aspect** (Physical/Bleed Beast Strikes) | **Primal Storm** (Elemental Earth/Wind Control) |

---

## 12. Global Open Questions & Balancing Parameters

The following parameters represent proposed working values requiring review before class-by-class implementation:

1. **SP Gating vs. Tier Access:**
   - *Proposal:* Tiers strictly gated by Character Level ($1, 4, 7, 10, 13$).
   - *Alternative:* Tiers gated solely by total SP spent in the class tree. (Level gating is recommended to match dungeon pacing).
2. **Loadout Slot Quantity:**
   - *Proposal:* 3 Active Skills + 3 Passive Skills (as per GDD Draft v0.1 Section 4).
   - *Question:* Should Ultimate Skills occupy one of the 3 Active slots, or have a dedicated 4th "Ultimate" slot that unlocks at Level 13?
3. **Respec Economy:**
   - *Proposal:* Cost Gold ($\text{Level} \times 50$) at Hub/Rest sites.
   - *Question:* Should respec be completely free in the Hub to encourage experimentation between dungeon runs?
4. **Rounding Rule Confirmation:**
   - *Proposal:* Standardize all final damage rounding to `Mathf.FloorToInt` after summing all components of a hit.

---
*End of GDD Skill System v1.0 — Global Architecture & Foundation*

