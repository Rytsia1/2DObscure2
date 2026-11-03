# CLASS SYSTEM
## Party-Based Turn-Based Roguelike RPG

**Version:** 1.0 — Design Draft  
**Status:** Working reference; effects and balance values not explicitly confirmed are marked **TBD / Draft**  
**Scope:** Class identity, progression, active skills, passive skills, combat integration  
**Classes:** Fighter, Barbarian, Martial Artist, Rogue, Ranger, Cleric, Wizard, Warlock, Druid

---

# 1. Overview

Class System defines each character's combat identity, skill access, and build options. A party contains four characters. Combat is turn-based, with individual character turns and reaction-based QTEs used to determine the execution quality of supported actions.

The system supports nine classes. Each class has a distinct core mechanic and a skill tree organized into five Tiers.

## 1.1 Design Principles

1. Every class should have a distinct tactical identity.
2. No class is mandatory for a successful party composition.
3. Active and passive skills should support more than one viable build where practical.
4. Skill effects must use shared combat systems rather than creating class-specific duplicates.
5. Skill values should be data-driven and balanceable without rewriting core combat code.
6. Descriptions in this document define intended gameplay behavior; unconfirmed numerical values remain TBD.

## 1.2 Global Class Rules

| System | Rule |
|---|---|
| Party size | 4 characters |
| Character level cap | 15 |
| Skill Tiers | Tier 1 at Level 1; Tier 2 at Level 4; Tier 3 at Level 7; Tier 4 at Level 10; Tier 5 at Level 13 |
| Skill loadout | 3 Active Skill slots + 3 Passive Skill slots per character |
| Skill Rank | Normal skills have Rank 1–3 |
| Skill Points (SP) | Used to unlock and upgrade skills; exact award schedule and costs TBD |
| Tier access | Determined by character level, not by SP |
| Resources | HP and MP only; no class-specific resource pool |
| Loadout changes | At Hub/Camp |
| Weapon compatibility | Strict class weapon compatibility; no Legendary/Mythic exception |
| Skill requirements | A skill may have a specific weapon requirement; requirements are stated per skill when confirmed |
| QTE | QTE quality modifies supported direct-action outcomes; it is separate from hit chance and Critical Hit |
| Numerical tuning | Damage coefficients, MP costs, durations, cooldowns, chances, and caps are TBD unless explicitly noted |

### 1.2.1 Skill Unlock and Rank

- Character level opens access to a Tier.
- SP is spent to unlock a skill and, where applicable, increase its Rank.
- Rank 1–3 is the standard skill progression.
- Skill Rank effects are skill-specific. Do not assume every skill gains damage, duration, or chance in the same way.
- The exact SP cost per unlock/rank and total SP progression remain TBD.
- Skill loadouts can be changed at the Hub/Camp; combat-time loadout swapping is not part of the baseline design.

### 1.2.2 Skill Categories

**Active Skill:** An action selected during a character's turn. It may deal damage, heal, apply a status, grant protection, reposition, or alter combat conditions. Active skills generally spend MP; each skill's exact cost is TBD.

**Passive Skill:** A persistent modifier while equipped. Passives may alter stats, skill performance, conditions, or class mechanics. A passive occupies one of the three Passive Skill slots.

**Special Core Skill:** A class mechanic may have a special acquisition or Rank rule. Barbarian's Berserk is the current example: it is an Active Skill with its full designed effect from acquisition and is not upgraded through SP. Its tuning remains TBD.

---

# 2. Combat System Integration

Class skills use the shared combat systems described in the related system documents.

## 2.1 Damage Types

- **Physical Damage:** Primarily weapon attacks and physical techniques.
- **Elemental Damage:** Fire, Ice, Lightning, Earth, Wind, Water, Holy, and Shadow.
- **Almighty Damage:** A special damage category with its own resolution rules, if a skill explicitly uses it.
- **Nature is a theme / ability identity, not a damage element.** Druid Nature-themed damage should use the damage category and source specified by the skill; the current damage system does not define Nature Damage or Nature Resistance.
- There is no separate Magical Damage category. “Magic” may describe an ability, but its actual damage category must be specified.

## 2.2 QTE, Hit, and Critical

- QTE grade affects supported direct damage according to the shared QTE system.
- QTE is separate from attack hit chance.
- Ranged Accuracy affects ranged basic attacks and ranged Active Skills; QTE execution does not automatically guarantee a hit.
- Critical Hit applies to Physical Damage under the shared rules. Elemental damage and DoT do not Critical by default.
- Each skill must state whether it invokes a QTE and whether it is single-hit, multi-hit, or multi-component.

## 2.3 Status Effects

Skills apply status effects only when their definition explicitly says so. Damage element does not automatically apply a matching status.

Status names used in this document—such as Burn, Freeze, Shock, Curse, Root, Slow, Stun, Shield, and Regeneration—must follow the shared Damage & Status Effect System. Any class-specific duration, chance, or exception is TBD until specified.

## 2.4 Boss Considerations

Boss resistance and immunity rules come from the shared Status Effect System. Skills that apply hard control, resurrection, large party-wide protection, or delayed burst may require special balance limits. Those limits are marked TBD rather than presumed.

---

# 3. Fighter

## 3.1 Class Identity — Weapon Techniques

Fighter is a reliable frontline combatant built around weapon mastery, tactical strikes, defensive stances, and counterplay. The Fighter can contribute consistent Physical Damage while protecting the party or exploiting enemy openings.

**Primary role:** Frontline damage / defense  
**Core mechanic:** Weapon Techniques  
**Build directions:** Offensive weapon specialist; shield defender; counter/tactical fighter  
**Weapon requirements:** Weapon-specific requirements may apply to individual skills. Exact compatibility and skill requirements remain TBD unless explicitly stated.

### Core Gameplay

- Select weapon techniques to match the current enemy situation.
- Use defensive skills to mitigate danger and counter attacks.
- Use armor or guard disruption to create openings for party damage.
- Fighter's passives improve weapon consistency, defense, tactical awareness, or counter performance.

## 3.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Shield Bash** — Strikes a target with a shield, dealing Physical Damage and potentially disrupting its action or posture. The exact disruption effect and shield requirement are TBD.
- **Power Strike** — A committed weapon attack that deals increased Physical Damage compared with a basic attack. Exact coefficient and weapon requirement are TBD.
- **Guard Stance** — Adopts a defensive stance for a limited duration, improving the Fighter's defense or damage mitigation. Duration and modifier are TBD.

**Passive Skills**
- **Weapon Proficiency** — Improves the Fighter's effectiveness with compatible weapons. The affected stat or damage modifier is TBD.
- **Defensive Training** — Improves the Fighter's survivability through a defensive stat or damage-reduction modifier. Exact parameter is TBD.

### Tier 2 — Level 4

**Active Skills**
- **Cleaving Strike** — Sweeping weapon attack that can hit multiple enemies. Target count, range, and damage distribution are TBD.
- **Shield Wall** — Uses a shield to strengthen defense, potentially protecting the Fighter or nearby/allied targets. Target scope and duration are TBD.
- **Weapon Disarm** — Attempts to disarm an eligible enemy, reducing or disabling its weapon-based threat. Enemy eligibility, chance, and duration are TBD.

**Passive Skills**
- **Combat Discipline** — Improves combat consistency, such as reducing a penalty or strengthening a core combat parameter. Exact effect is TBD.
- **Shield Mastery** — Enhances shield-based skills or shield defense. Exact modifier and qualifying actions are TBD.

### Tier 3 — Level 7

**Active Skills**
- **Counterattack** — Prepares or triggers a retaliatory strike when a defined enemy attack condition is met. Trigger timing and damage are TBD.
- **Armor Break** — Deals damage and reduces the target's defense or armor for a duration. The affected defense and duration are TBD.
- **Tactical Strike** — A precise attack that gains an additional benefit when a tactical condition is met, such as a vulnerable or controlled target. Condition and bonus are TBD.

**Passive Skills**
- **Tactical Awareness** — Improves the Fighter's response to enemy actions or tactical conditions. Exact trigger and benefit are TBD.
- **Weapon Adaptation** — Improves flexibility across compatible weapon types. Whether this changes skill performance or grants a general modifier is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Whirlwind** — A spinning attack that deals Physical Damage to multiple nearby enemies. Target count and damage are TBD.
- **Iron Fortress** — Enters a high-defense stance that substantially improves mitigation or resistance to disruption. Duration and values are TBD.
- **Execution Strike** — Delivers a powerful attack against an enemy meeting a vulnerability condition. The qualifying condition and bonus are TBD.

**Passive Skills**
- **Iron Will** — Improves resistance to control, disruption, or damage under a defined condition. Exact effect is TBD.
- **Counter Specialist** — Improves Counterattack or other counter-based actions. Exact bonus and scope are TBD.

### Tier 5 — Level 13

**Active Skills**
- **Blade Tempest** — Performs a rapid series of weapon attacks. Hit count, target rules, and per-hit scaling are TBD.
- **Unbreakable** — Activates an exceptional defensive effect that helps the Fighter withstand heavy pressure. Exact protection, duration, and limits are TBD.
- **Final Verdict** — A high-impact finishing strike, potentially gaining power against weakened or vulnerable targets. Condition and scaling are TBD.

**Passive Skills**
- **Master of Arms** — Represents peak weapon mastery and improves a defined set of weapon-based combat parameters. Exact modifiers are TBD.
- **Unyielding Defense** — Strengthens defensive performance, particularly against heavy or repeated attacks. Exact effect is TBD.

---

# 4. Barbarian

## 4.1 Class Identity — Berserk State

Barbarian is an aggressive Physical Damage class that trades controlled defense for overwhelming pressure and resilience. Its signature state is **Berserk**.

**Primary role:** Frontline physical damage  
**Core mechanic:** Berserk State  
**Build directions:** Sustained aggression; heavy burst; high-risk damage with survival tools

### Berserk — Special Core Skill

- Berserk is an **Active Skill**, not an automatic state.
- Activating it uses the Barbarian's turn.
- Berserk grants its full designed effect when acquired; it is treated as Rank 3 for the current concept and is not upgraded using SP.
- Proposed bonus themes include Physical Damage, Critical Chance, and Damage Reduction, but exact effects, values, duration, and restrictions are TBD.
- Berserk does not create a separate resource pool.
- Berserk's final interaction with other buffs and status effects must follow shared stacking rules.

## 4.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Savage Strike** — A brutal basic technique that deals Physical Damage. Coefficient and any additional effect are TBD.
- **Berserk** — Activates the Barbarian's signature Berserk State, granting its defined combat bonuses for a limited duration. Exact parameters are TBD; this skill is not SP-upgraded.
- **Intimidating Roar** — Roars at enemies to apply an intimidation effect or weaken their combat performance. Exact status, target scope, and duration are TBD.

**Passive Skills**
- **Savage Instinct** — Barbarian's starter passive; improves a core offensive or survival parameter that supports aggressive play. Exact effect is TBD.
- **Bloodthirst** — Rewards aggressive combat through a defined benefit, potentially tied to dealing damage or defeating an enemy. Trigger and value are TBD.

### Tier 2 — Level 4

**Active Skills**
- **Reckless Swing** — A powerful attack with a risk/reward tradeoff. The intended downside and damage bonus are TBD.
- **Crushing Blow** — A heavy strike designed to deal high Physical Damage and/or pressure enemy Break. Exact Break Power and coefficient are TBD.
- **Blood Rush** — A rush of aggression that improves the Barbarian's momentum or attacks a target with a momentum-related benefit. Exact behavior is TBD.

**Passive Skills**
- **Unrelenting Fury** — Improves sustained offensive pressure. Exact modifier and trigger are TBD.
- **Pain Tolerance** — Improves the Barbarian's ability to withstand damage or maintain offense while injured. Exact effect is TBD.

### Tier 3 — Level 7

**Active Skills**
- **Rampage** — Performs a sequence of aggressive attacks. Hit count, target rules, and scaling are TBD.
- **Brutal Cleave** — A heavy area attack that hits multiple enemies. Target count and damage distribution are TBD.
- **Blood Frenzy** — Activates or exploits a heightened aggressive condition, potentially benefiting from damage dealt or low enemy HP. Trigger and duration are TBD.

**Passive Skills**
- **Berserker's Momentum** — Improves the Barbarian after an offensive action or while Berserk is active. Exact trigger and bonus are TBD.
- **Predator's Instinct** — Improves damage or effectiveness against vulnerable enemies. Vulnerability condition and modifier are TBD.

### Tier 4 — Level 10

**Active Skills**
- **Devastating Smash** — A crushing attack with high damage and/or Break pressure. Exact target scope and values are TBD.
- **War Cry** — A battle cry that grants a benefit to the Barbarian and/or applies a combat debuff to enemies. Target scope and effect are TBD.
- **Death Rush** — A forceful attack that pressures a target, potentially gaining a bonus against weakened enemies. Exact condition and effect are TBD.

**Passive Skills**
- **Unstoppable Rage** — Improves resistance to interruption or control while attacking or Berserk. Exact condition and effect are TBD.
- **Blood Pact** — A sacrifice- or survival-themed passive. Its actual cost, trigger, and benefit remain TBD; no HP sacrifice is assumed.

### Tier 5 — Level 13

**Active Skills**
- **Primal Carnage** — A devastating area attack focused on overwhelming physical force. Target count and scaling are TBD.
- **Wrath Incarnate** — A high-tier aggression skill that embodies the Barbarian's rage. Its precise effect and relation to Berserk are TBD.
- **Cataclysm** — A major finishing attack with high impact. Damage, target scope, and usage limits are TBD.

**Passive Skills**
- **Endless Fury** — Extends or strengthens the Barbarian's sustained aggression. Exact mechanic is TBD.
- **Avatar of Destruction** — The Barbarian's peak offensive passive. Its modifiers and conditions are TBD.

---

# 5. Martial Artist

## 5.1 Class Identity — Combo Flow

Martial Artist fights unarmed and builds a combo through reaction-based QTE performance. The class emphasizes timing, flow, and converting successful actions into a powerful finisher.

**Primary role:** Unarmed physical damage / Break pressure  
**Core mechanic:** QTE-based Combo Flow  
**Build directions:** Combo builder; QTE specialist; finisher-focused build

### Combo Flow — Current Rules

- Combo is built through attacks and their QTE outcomes, not by requiring a fixed sequence of specific skills.
- A successful QTE increases combo by the skill-defined amount.
- A failed QTE resets combo to 0.
- Combo resets if Martial Artist goes one full turn without attacking.
- Combo resets when Martial Artist becomes Broken.
- A finisher consumes the entire current combo.
- The starter passive **Martial Discipline** increases Physical Break Power from unarmed attacks.
- Maximum combo, exact gain per skill, Perfect QTE bonus, and combo damage thresholds remain TBD.

## 5.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Quick Jab** — A fast unarmed strike intended to build combo reliably. Combo gain and damage are TBD.
- **Rising Kick** — An upward kick that deals Physical Damage and may pressure Break. Exact effects are TBD.
- **Focused Breathing** — An active focus technique that prepares the Martial Artist for improved execution or combat performance. Exact buff, duration, and cost are TBD.

**Passive Skills**
- **Martial Discipline** — Starter passive; increases Physical Break Power from unarmed attacks.
- **Combat Rhythm** — Improves combo flow through a defined benefit, such as combo gain or retention. Exact effect is TBD.

### Tier 2 — Level 4

**Active Skills**
- **Palm Strike** — A focused palm attack that deals Physical Damage and may have a defined tactical effect. Exact effect is TBD.
- **Sweep Kick** — A sweeping kick that attacks or disrupts one or more targets. Target scope and effect are TBD.
- **Flowing Combo** — A technique that continues the Martial Artist's combo flow. Its combo gain, QTE profile, and any bonus are TBD.

**Passive Skills**
- **Combo Mastery** — Increases the benefit gained from maintaining combo. Exact threshold or modifier is TBD.
- **Agile Defense** — Improves evasion or defensive response while maintaining the Martial Artist's agile fighting style. Exact effect is TBD.

### Tier 3 — Level 7

**Active Skills**
- **Dragon Fist** — A powerful unarmed strike with high Physical Damage and/or Break pressure. Exact values are TBD.
- **Spinning Kick** — A rotating kick that can hit multiple targets or apply a tactical effect. Target scope is TBD.
- **Counterstrike** — A retaliatory unarmed attack after a valid trigger. Trigger timing and QTE rules are TBD.

**Passive Skills**
- **Inner Focus** — Improves a defined combat parameter while focused or maintaining combo. Exact trigger and modifier are TBD.
- **Pressure Point** — Improves the Martial Artist's ability to exploit enemy vulnerabilities. Exact debuff, Break, or damage interaction is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Hundred Fists** — A rapid series of unarmed attacks. Hit count, combo gain, and per-hit scaling are TBD.
- **Flying Knee** — A high-impact knee strike. Damage, Break Power, and QTE profile are TBD.
- **Perfect Counter** — A counter technique whose payoff depends on QTE execution. Timing window and effect are TBD.

**Passive Skills**
- **Unbroken Flow** — Helps preserve or improve combo flow under a defined condition. Exact rule is TBD.
- **Ki Control** — Improves the Martial Artist's control over combat power or resource efficiency. “Ki” is thematic; it does not introduce a separate resource pool.

### Tier 5 — Level 13

**Active Skills**
- **Dragon's Fury** — Combo finisher that consumes the entire current combo for a powerful attack. Damage scaling and combo conversion formula are TBD.
- **Ascendant Strike** — A high-tier strike with a defined benefit tied to execution or combo. Exact behavior is TBD.
- **Final Technique** — A peak Martial Artist technique. Damage, condition, and whether it consumes combo are TBD.

**Passive Skills**
- **Grandmaster** — Represents mastery of unarmed combat and improves defined Martial Artist parameters. Exact effects are TBD.
- **Limitless Potential** — A peak passive that enhances a defined aspect of combo or QTE performance. Exact effect is TBD.

---

# 6. Rogue

## 6.1 Class Identity — Exploit

Rogue is a precision attacker who exploits enemy conditions, vulnerabilities, and openings. Its core identity is **Exploit**, not a universal Mark mechanic.

**Primary role:** Physical damage / opportunistic burst  
**Core mechanic:** Exploit enemy conditions  
**Build directions:** Debuff exploiter; execution specialist; evasive assassin

### Exploit — Design Concept

Proposed exploit conditions include:
- Target is affected by a debuff.
- Target is Broken.
- Target is at low HP.
- Target is preparing or charging an attack.

These are concept triggers, not a finalized global mechanic. Exact trigger definitions, bonuses, and whether several conditions can activate simultaneously remain TBD. Rogue's **Death Mark** is an individual skill and does not replace the Exploit class identity.

## 6.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Quick Slash** — A fast blade attack that deals Physical Damage. Exact coefficient is TBD.
- **Backstab** — A strike that gains a tactical advantage against an eligible target or from a defined position. Dagger requirement is a draft; final weapon requirement is TBD.
- **Smoke Bomb** — Creates a smoke effect that disrupts enemies and/or improves Rogue evasion. Target scope, duration, and status are TBD.

**Passive Skills**
- **Opportunist** — Starter passive; increases Critical Chance against debuffed enemies.
- **Evasive Instinct** — Improves Rogue evasion or defensive response. Exact modifier is TBD.

### Tier 2 — Level 4

**Active Skills**
- **Shadow Strike** — A shadow-themed attack that deals damage and may benefit from a tactical opening. Damage type and extra effect are TBD.
- **Poison Blade** — An attack that applies Poison on a successful, eligible hit. Poison chance, potency, and duration follow the shared status rules and remain TBD.
- **Feint** — A deceptive maneuver that creates an opening, weakens a target, or improves Rogue's next action. Exact effect is TBD.

**Passive Skills**
- **Dirty Fighting** — Improves the Rogue's effectiveness when using debuffs, conditions, or tactical openings. Exact effect is TBD.
- **Precision** — Improves a defined accuracy, critical, or damage parameter. Exact stat is TBD.

### Tier 3 — Level 7

**Active Skills**
- **Assassinate** — A high-impact attack against a target meeting a vulnerability condition. Condition and scaling are TBD.
- **Shadowstep** — Repositions the Rogue or grants an evasive shadow movement effect. Movement rules, duration, and target restrictions are TBD.
- **Weakpoint Strike** — Attacks a target's weak point, gaining a defined damage or Break benefit. Exact effect is TBD.

**Passive Skills**
- **Exploit Weakness** — Improves the payoff from Rogue's Exploit conditions. Exact bonus and stacking behavior are TBD.
- **Lethal Focus** — Improves a defined offensive parameter, potentially when targeting a vulnerable enemy. Exact trigger is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Phantom Dance** — A sequence of evasive or shadow-based attacks. Hit count, defensive effect, and damage are TBD.
- **Death Mark** — Applies a skill-specific mark that makes the target more vulnerable to Rogue's follow-up. Duration and effect are TBD; this is not the class's core mechanic.
- **Vanishing Act** — Temporarily reduces enemy focus or improves Rogue's evasion/positioning. Exact targeting and duration are TBD.

**Passive Skills**
- **Shadow Affinity** — Improves the effectiveness of eligible shadow-themed skills. Damage category and modifier scope are TBD.
- **Executioner** — Improves Rogue's effectiveness against targets near defeat. Threshold and bonus are TBD.

### Tier 5 — Level 13

**Active Skills**
- **Shadow Requiem** — A high-tier shadow attack or attack sequence. Damage, hit count, and conditions are TBD.
- **Thousand Cuts** — Performs a rapid multi-hit attack. Hit count, per-hit damage, and QTE handling are TBD.
- **Final Ambush** — A finishing ambush that exploits a target's vulnerable state. Condition and payoff are TBD.

**Passive Skills**
- **Master Assassin** — Rogue's peak assassin passive, improving a defined set of offensive or exploit parameters. Exact effects are TBD.
- **Death's Embrace** — A high-tier offensive passive associated with finishing or vulnerable targets. Exact trigger and modifier are TBD.

---

# 7. Ranger

## 7.1 Class Identity — Hunt Mark

Ranger is a ranged specialist who tracks and marks a target, then increases **Ranger's own damage** against that marked target. Hunt Mark does not grant the same damage bonus to the rest of the party.

**Primary role:** Ranged Physical Damage  
**Core mechanic:** Hunt Mark  
**Build directions:** Mark-focused single target; ranged area damage; accuracy and precision

### Hunt Mark — Design Concept

- Hunter's Mark applies Hunt Mark to an eligible target.
- Current draft assumes one active marked target at a time.
- Ranger's attacks gain a damage bonus against the marked target.
- The mark's duration, bonus, target switching, and refresh behavior remain TBD.
- Hunt Mark does not automatically increase party-wide damage.

### Ranged Accuracy

Hunter's Instinct increases Ranged Accuracy for ranged basic attacks and ranged Active Skills. Ranged Accuracy affects hit chance; QTE grade is a separate execution outcome.

## 7.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Quick Shot** — A fast ranged attack. Damage, accuracy interaction, and QTE profile are TBD.
- **Hunter's Mark** — Applies Hunt Mark to a target, enabling Ranger's personal damage bonus against it. Exact bonus and duration are TBD.
- **Evasive Shot** — Fires while taking an evasive action, combining ranged damage with a defensive benefit. Exact effect is TBD.

**Passive Skills**
- **Hunter's Instinct** — Starter passive; increases Ranged Accuracy for ranged basic attacks and ranged Active Skills.
- **Keen Eye** — Improves aiming or target awareness. Exact accuracy, critical, or scouting effect is TBD.

### Tier 2 — Level 4

**Active Skills**
- **Piercing Arrow** — Fires an arrow that pierces or penetrates eligible targets/defenses. Exact penetration rule and target count are TBD.
- **Volley** — Fires a spread or volley of arrows at multiple enemies. Target count and damage distribution are TBD.
- **Crippling Shot** — A ranged attack that applies a weakening effect, such as reduced movement/Speed or another defined combat parameter. Exact status is TBD.

**Passive Skills**
- **Marksmanship** — Improves ranged attack performance. Exact modifier is TBD.
- **Predator's Focus** — Improves Ranger's performance against a tracked or marked target. Exact trigger and effect are TBD.

### Tier 3 — Level 7

**Active Skills**
- **Snipe** — A precision ranged attack with high single-target potential. Accuracy requirement, coefficient, and QTE profile are TBD.
- **Explosive Arrow** — Fires an arrow that explodes, dealing damage to a target area. Element/damage type and area rules are TBD.
- **Trap Setup** — Places a trap that triggers under defined enemy conditions. Trap type, trigger, duration, and damage/control are TBD.

**Passive Skills**
- **Improved Tracking** — Improves tracking-related benefits, such as Hunt Mark handling or exploration tracking. Exact scope is TBD.
- **Deadly Aim** — Improves precision damage or another defined ranged offensive parameter. Exact modifier is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Rain of Arrows** — An area attack that rains arrows over a target zone or enemy group. Target count and damage are TBD.
- **Shadow Shot** — A special ranged attack with a shadow-themed effect. Damage category, status, and utility are TBD.
- **Pinning Shot** — A ranged attack that pins or restricts an eligible target. Status, chance, duration, and boss behavior are TBD.

**Passive Skills**
- **Tactical Hunter** — Improves the Ranger's tactical options or ranged performance under a defined condition. Exact effect is TBD.
- **Eagle Eye** — Improves a defined precision or ranged accuracy parameter. Exact value and scope are TBD.

### Tier 5 — Level 13

**Active Skills**
- **Deadeye** — A high-tier precision attack against one target. Damage, accuracy, and QTE profile are TBD.
- **Nature's Wrath** — A nature-themed ranged attack. It does not imply a Nature Damage type; the actual damage category and effects are TBD.
- **Perfect Hunt** — A peak Hunt Mark technique that exploits the marked target. Exact mark interaction and damage are TBD.

**Passive Skills**
- **Master Hunter** — Ranger's peak hunting passive, improving defined ranged or tracking parameters. Exact effect is TBD.
- **Apex Predator** — Improves Ranger's effectiveness against a defined prey condition, potentially marked or vulnerable targets. Trigger and bonus are TBD.

---

# 8. Cleric

## 8.1 Class Identity — Divine Protection

Cleric is a hybrid healer that combines HP restoration, protection, and Holy offense. Cleric is not restricted to healing only; its skills can support the party defensively and contribute damage.

**Primary role:** Hybrid healer / protector / Holy offense  
**Core mechanic:** Divine Protection  
**Build directions:** Healing specialist; shield/protection support; Holy offense support

### Healing and Protection Rules

- **Divine Grace** increases Healing Power from Cleric skills.
- Divine Grace does **not** automatically increase Shield Power.
- Shield and damage-reduction effects use their own relevant modifiers.
- Resurrection, Guardian Angel, and Miracle may require use limits or conditions; those limits are TBD.
- Holy skills use the shared Elemental Damage rules for Holy, unless a skill explicitly defines another category.

## 8.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Divine Heal** — Restores HP to one ally. Healing amount, scaling, and MP cost are TBD.
- **Holy Light** — Deals Holy Damage to an eligible enemy. Damage, target rules, and QTE profile are TBD.
- **Blessing** — Grants a beneficial effect to an ally, such as protection or improved combat performance. Exact buff and duration are TBD.

**Passive Skills**
- **Divine Grace** — Starter passive; increases Healing Power from Cleric skills. It does not automatically increase Shield Power.
- **Sacred Vitality** — Improves a defined survivability parameter, such as HP or healing received. Exact effect is TBD.

### Tier 2 — Level 4

**Active Skills**
- **Prayer of Healing** — Restores HP to multiple party members. Target scope, healing distribution, and cost are TBD.
- **Divine Shield** — Grants Shield or another defined protective effect to an eligible target. Shield amount, duration, and stacking are TBD.
- **Purify** — Removes eligible negative status effects from a target. Cleanse scope and number of instances removed are TBD.

**Passive Skills**
- **Healing Hands** — Improves Cleric healing performance. Exact modifier and scope are TBD.
- **Protective Faith** — Improves protection effects, such as Shield or Damage Reduction, without assuming an increase to Healing Power. Exact effect is TBD.

### Tier 3 — Level 7

**Active Skills**
- **Sanctuary** — Creates an area or party protection effect that may include healing or damage mitigation. Exact mechanics and duration are TBD.
- **Holy Smite** — Deals Holy Damage to an enemy. Damage, scaling, and QTE profile are TBD.
- **Resurrection** — Revives a fallen ally. Target conditions, restored HP, MP cost, and usage restrictions are TBD.

**Passive Skills**
- **Divine Aegis** — Improves Shield or protection effectiveness. Exact modifier is TBD.
- **Spiritual Focus** — Improves a defined spell-related parameter, such as MP efficiency or accuracy. Exact effect is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Mass Restoration** — Restores HP to the party or multiple allies. Target scope, amount, and cost are TBD.
- **Guardian Angel** — Grants a powerful protective effect to an ally, potentially preventing or mitigating a critical threat. Exact effect and limits are TBD.
- **Judgment** — A Holy offensive skill that deals damage and may apply a defined secondary effect. Exact effect is TBD.

**Passive Skills**
- **Blessed Recovery** — Improves recovery from healing or another defined restorative effect. Exact scope is TBD.
- **Unwavering Faith** — Improves Cleric's reliability or resistance to disruption. Exact effect is TBD.

### Tier 5 — Level 13

**Active Skills**
- **Divine Intervention** — A high-tier intervention that protects or restores allies in a critical situation. Trigger/activation, target scope, and limits are TBD.
- **Heaven's Wrath** — Deals Holy Damage to multiple enemies. Area, damage, and QTE profile are TBD.
- **Miracle** — Cleric's peak healing/protection ability. Exact effect, target scope, cost, and usage limit are TBD.

**Passive Skills**
- **Saint's Resolve** — A peak defensive/support passive. Exact modifiers and conditions are TBD.
- **Eternal Blessing** — A peak blessing-related passive that improves a defined support effect. Exact scope and values are TBD.

---

# 9. Wizard

## 9.1 Class Identity — Elemental Specialization

Wizard is a flexible spellcaster with skills that use different elements. The player does **not** choose one permanent element for the class; elemental focus emerges from the skills and build selected.

**Primary role:** Elemental / Arcane damage  
**Core mechanic:** Flexible elemental skill selection  
**Build directions:** Fire damage; Ice control; Lightning attacks; Arcane damage; mixed-element caster

### Elemental Rules

- Individual skills specify their damage element.
- A Wizard may use Fire, Ice, Lightning, Arcane, or another supported element according to the skill.
- Elemental Damage Bonus and element-specific bonuses follow the shared damage modifier rules.
- “Magic Damage” in early skill notes is descriptive only; the damage category must be Physical, Elemental, or Almighty as defined by the shared system.
- No permanent element specialization is imposed by class selection.

## 9.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Arcane Bolt** — Deals Arcane Elemental Damage to one target. Damage, scaling, and cost are TBD.
- **Fireball** — Deals Fire Elemental Damage to a target or area. Area size and damage distribution are TBD.
- **Frost Nova** — Deals Ice Elemental Damage in an area and applies Slow as defined by the skill. Slow strength and duration are TBD.

**Passive Skills**
- **Arcane Affinity** — Starter passive; reduces MP Cost of Arcane spells.
- **Elemental Knowledge** — Improves Elemental Damage. Exact modifier and scope are TBD.

### Tier 2 — Level 4

**Active Skills**
- **Lightning Strike** — Deals Lightning Elemental Damage to one target. Damage and any additional effect are TBD.
- **Ice Lance** — Deals Ice Elemental Damage with a penetration-oriented effect. The meaning of penetration—defense, targets, or another rule—is TBD.
- **Arcane Burst** — Deals Arcane Elemental Damage to multiple enemies or an area. Target scope is TBD.

**Passive Skills**
- **Spell Mastery** — Improves spell damage according to a defined scope. Exact modifier is TBD.
- **Mana Efficiency** — Reduces MP Cost of eligible spells. Scope and value are TBD.

### Tier 3 — Level 7

**Active Skills**
- **Meteor** — A large Fire Elemental Damage area attack. Area, damage, and any delay are TBD.
- **Blizzard** — Deals Ice Elemental Damage over an area and applies Slow or another defined Ice effect. Duration and values are TBD.
- **Chain Lightning** — Lightning Elemental Damage that jumps between eligible targets. Jump count and falloff are TBD.

**Passive Skills**
- **Elemental Focus** — Improves elemental spell performance. Exact modifier and conditions are TBD.
- **Arcane Precision** — Improves a defined spell accuracy or hit parameter. Exact scope is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Inferno** — Deals Fire Elemental Damage in an area and may apply Burn if the skill definition confirms it. Burn chance, potency, and duration are TBD.
- **Absolute Zero** — Deals Ice Elemental Damage and applies Freeze or another defined control effect. Exact control behavior and boss handling follow shared rules.
- **Arcane Overload** — A powerful Arcane Elemental Damage spell. Damage, cost, and any drawback are TBD.

**Passive Skills**
- **Spell Amplification** — Improves eligible spell damage. Exact scope and modifier are TBD.
- **Magical Resilience** — Improves resistance or mitigation against a defined damage category. The exact category and value are TBD; no separate Magical Damage category is assumed.

### Tier 5 — Level 13

**Active Skills**
- **Cataclysm** — A major elemental area attack. Element, target scope, and damage are TBD.
- **Apocalypse** — A high-tier spell with substantial damage. Element, area, and additional effects are TBD.
- **Grand Arcanum** — Wizard's peak Arcane spell. Damage, cost, target scope, and special rules are TBD.

**Passive Skills**
- **Archmage** — Wizard's peak spellcaster passive, improving defined spell damage and/or accuracy parameters. Exact modifiers are TBD.
- **Infinite Potential** — Improves a defined MP efficiency or spell progression parameter. Exact effect is TBD.

---

# 10. Warlock

## 10.1 Class Identity — Curse Mastery

Warlock is a dark spellcaster that specializes in curses, debuffs, and exploiting cursed targets.

**Primary role:** Debuff / damage over time / Dark Elemental Damage  
**Core mechanic:** Curse Mastery  
**Build directions:** Curse damage over time; debuff support; cursed-target burst

### Curse Rules

- Each Curse effect is independent.
- Different Curse effects may coexist on the same target.
- There is no automatic Curse stacking, merging, or combination.
- A new instance follows the shared Status Effect rules; do not assume it refreshes or extends another instance.
- Curse's actual effect is defined by the skill that applies it.
- Exact behavior when the target dies or becomes Broken remains TBD where not defined by the shared system.

## 10.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Shadow Bolt** — Deals Dark Elemental Damage to one target. Damage and cost are TBD.
- **Curse of Weakness** — Applies a Curse that reduces the target's Physical Damage output. Modifier and duration are TBD.
- **Corruption** — Applies a Curse-based Damage over Time effect. Damage type, stat scaling, tick timing, and duration are TBD.

**Passive Skills**
- **Pact Affinity** — Starter passive; reduces MP Cost of Pact spells.
- **Lingering Curse** — Extends the duration of eligible Curses. Exact scope and duration modifier are TBD; it does not imply automatic stacking.

### Tier 2 — Level 4

**Active Skills**
- **Curse of Vulnerability** — Applies a Curse that increases damage received by the target. Modifier scope, duration, and stacking are TBD.
- **Soul Drain** — Deals damage and restores a portion of Warlock's HP. Damage, healing ratio, and damage category are TBD.
- **Shadow Burst** — Deals Dark Elemental Damage to an area. Target count and damage are TBD.

**Passive Skills**
- **Curse Mastery** — Improves the effectiveness of eligible Curses. Exact parameter is TBD.
- **Dark Knowledge** — Improves Dark Elemental Damage. Exact modifier is TBD.

### Tier 3 — Level 7

**Active Skills**
- **Curse of Agony** — Applies a Curse DoT whose tick damage increases according to a defined progression. Growth rule, cap, and duration are TBD.
- **Soul Harvest** — Deals bonus damage or gains another effect when the target is Cursed. Exact trigger and bonus are TBD.
- **Hex** — Applies a Curse that reduces the target's Elemental Damage output or another explicitly defined spell-related parameter. Exact stat and duration are TBD.

**Passive Skills**
- **Malicious Intent** — Increases Critical Chance against Cursed enemies. Critical applies only to eligible Physical Damage under shared rules; whether this passive also changes another mechanic is TBD.
- **Dark Ritual** — Improves Warlock damage against Cursed targets. Damage scope and modifier are TBD.

### Tier 4 — Level 10

**Active Skills**
- **Curse of Doom** — Applies a delayed Curse damage effect. Delay, damage, removal behavior, and duration are TBD.
- **Nightfall** — Deals Dark Elemental Damage in an area and applies an Accuracy debuff or other specified weakening effect. Exact debuff is TBD.
- **Soul Collapse** — Deals burst damage with a bonus against Cursed targets. Bonus and Curse conditions are TBD.

**Passive Skills**
- **Forbidden Power** — Improves Dark Elemental Damage or a defined high-risk spell effect. Exact behavior is TBD.
- **Eternal Affliction** — Improves Curse duration or persistence. Exact scope and value are TBD; Curse instances remain independent.

### Tier 5 — Level 13

**Active Skills**
- **Apocalypse** — Applies a high-tier Curse effect to multiple enemies, weakening them and/or dealing damage. Exact effects and target scope are TBD.
- **Soul Annihilation** — A burst attack against Cursed targets. Damage, conditions, and target scope are TBD.
- **Absolute Damnation** — A major Curse with a large or delayed effect. Exact mechanic, duration, and limits are TBD.

**Passive Skills**
- **Grand Curse Master** — Warlock's peak Curse passive, improving a defined set of Curse parameters. Exact effects are TBD.
- **Abyssal Dominion** — Improves Warlock's damage or control over Cursed enemies. Exact trigger and modifier are TBD.

---

# 11. Druid

## 11.1 Class Identity — Nature's Control

Druid is a hybrid class combining nature-themed damage, battlefield control, and healing. Its primary identity is tactical control, not being a dedicated healer like Cleric.

**Primary role:** Hybrid damage / control / healing  
**Core mechanic:** Nature's Control  
**Build directions:** Root/control; Nature-themed damage; hybrid healing and protection

### Nature Rules

- “Nature” is a skill theme and class identity, not a damage element in the shared damage taxonomy.
- Druid damage skills must specify their actual damage category, scaling, and any affinity interactions.
- Healing, Shield, Root, Slow, and Regeneration use the shared combat/status systems.
- Root/control duration and boss interaction require careful balancing.

## 11.2 Skill Tree

### Tier 1 — Level 1

**Active Skills**
- **Thorn Shot** — A nature-themed single-target attack. Its actual damage category, scaling, and additional effects are TBD.
- **Entangling Roots** — Applies Root or another defined movement/control restriction to an eligible target. Duration and boss handling follow shared rules and remain TBD.
- **Nature's Embrace** — Restores HP to one ally through nature-themed healing. Healing amount and scaling are TBD.

**Passive Skills**
- **Nature's Affinity** — Starter passive; increases the defined damage output of Druid's nature-themed magic skills. Exact damage category and modifier must be specified during balance.
- **Natural Recovery** — Improves Healing Power for Druid's Nature-themed healing skills.

### Tier 2 — Level 4

**Active Skills**
- **Vine Lash** — Deals nature-themed damage and applies Slow or another defined control effect. Actual damage category and status values are TBD.
- **Healing Bloom** — Restores HP to multiple party members. Target scope and healing distribution are TBD.
- **Barkskin** — Grants one ally Damage Reduction or another defined defensive effect. Modifier and duration are TBD.

**Passive Skills**
- **Thorn Mastery** — Improves the damage or effectiveness of eligible thorn-themed skills. Exact scope is TBD.
- **Deep Roots** — Improves Root duration or effectiveness. Exact modifier and boss restrictions are TBD.

### Tier 3 — Level 7

**Active Skills**
- **Nature's Wrath** — Deals nature-themed damage to multiple enemies. Actual damage category, area, and scaling are TBD.
- **Overgrowth** — Applies Root or a related control effect to multiple eligible enemies. Target count and duration are TBD.
- **Regrowth** — Applies Healing over Time to one ally. Tick timing, scaling, and duration are TBD.

**Passive Skills**
- **Wild Resilience** — Improves resistance or mitigation against defined damage categories or status effects. Exact scope is TBD.
- **Flourishing Life** — Improves Regeneration or Healing over Time effectiveness. Exact modifier is TBD.

### Tier 4 — Level 10

**Active Skills**
- **Wrath of the Forest** — Deals nature-themed area damage and applies Root or another defined control effect. Target scope and duration are TBD.
- **Ancient Sanctuary** — Provides party healing and/or Damage Reduction. Exact effect, duration, and target scope are TBD.
- **Thorn Prison** — Restricts an eligible target and deals nature-themed damage. Control type, damage category, duration, and boss handling are TBD.

**Passive Skills**
- **Ancient Knowledge** — Improves nature-themed damage and Healing Power. Exact categories and modifiers are TBD.
- **Nature's Protection** — Improves Druid's Shield or protection effects. Exact scope is TBD.

### Tier 5 — Level 13

**Active Skills**
- **World Tree's Blessing** — Restores HP to multiple allies and applies Regeneration. Target scope, amount, and duration are TBD.
- **Primal Cataclysm** — Deals high nature-themed damage to all or multiple enemies. Actual damage category and scaling are TBD.
- **Nature's Dominion** — Creates a controlled battlefield effect involving Root and nature-themed damage. Area, target limits, duration, and boss behavior are TBD.

**Passive Skills**
- **Master of the Wild** — Druid's peak passive, improving nature-themed damage and control effectiveness. Exact modifiers are TBD.
- **Eternal Bloom** — Improves Healing Power and/or Regeneration duration. Exact scope and values are TBD.

---

# 12. Skill List Summary

Each Tier contains three Active Skills and two Passive Skills. The table below summarizes the skill count per class.

| Class | Active Skills | Passive Skills | Core Mechanic |
|---|---:|---:|---|
| Fighter | 15 | 10 | Weapon Techniques |
| Barbarian | 15* | 10 | Berserk State |
| Martial Artist | 15 | 10 | QTE Combo Flow |
| Rogue | 15 | 10 | Exploit |
| Ranger | 15 | 10 | Hunt Mark |
| Cleric | 15 | 10 | Divine Protection |
| Wizard | 15 | 10 | Elemental Specialization |
| Warlock | 15 | 10 | Curse Mastery |
| Druid | 15 | 10 | Nature's Control |

\* Barbarian's Berserk is included in the Active Skill count, but is a special core skill and is not upgraded with SP under the current design.

---

# 13. Design and Implementation Requirements

## 13.1 Skill Data

Each skill should be represented by data rather than hard-coded class logic. A skill definition should minimally contain:

- `skillId`
- `displayName`
- `classId`
- `tier`
- `skillType` (`active` or `passive`)
- `description`
- `unlockLevel`
- `spUnlockCost`
- `rankMax` (normally 3)
- `rankUpgradeCosts`
- `mpCost` (Active Skills, if applicable)
- `targetType`
- `damageComponents` (if applicable)
- `scalingStat`
- `skillPower` / scaling coefficient
- `qteProfile` (if applicable)
- `statusEffects` (if applicable)
- `duration` / `cooldown` / `usageLimit` (if applicable)
- `weaponRequirements` (if applicable)
- `balanceParameters`
- `icon` / visual and audio identifiers

Fields may be adjusted to match the project's actual implementation architecture.

## 13.2 Runtime Validation

The combat system should validate:
- Character owns and has equipped the skill.
- Skill is in an available Active/Passive slot as applicable.
- Character meets the skill's Tier/level requirements.
- Character has sufficient MP, if required.
- Target is valid and within skill constraints.
- Required weapon is equipped.
- Skill-specific cooldown, usage limit, and status conditions are satisfied.

Invalid actions should not consume a turn or MP unless a skill/system rule explicitly says otherwise.

## 13.3 Balance Parameters

The following are not globally finalized and must be defined per skill or in shared systems:

- Skill Power and stat scaling.
- MP Cost.
- Rank 1–3 progression.
- Target count, area, and range.
- QTE type and difficulty.
- Status chance, duration, potency, and resistance interaction.
- Cooldown or usage limit, if used.
- Weapon requirements.
- SP cost and SP acquisition schedule.
- Boss restrictions for control, revive, and high-impact effects.

## 13.4 Testing Considerations

- Verify each class can function without requiring one specific race.
- Verify active/passive slot limits.
- Verify Tier access at levels 1, 4, 7, 10, and 13.
- Verify SP unlock and Rank progression once costs are finalized.
- Verify skill weapon restrictions against strict class equipment compatibility.
- Verify QTE, hit chance, Critical, Break, status effects, healing, and Shield use shared system rules.
- Verify no passive or skill causes an unintended infinite trigger loop.
- Verify class-specific core mechanics reset or persist at the correct combat lifecycle boundaries.

---

# 14. Open Design Items

The following items remain open and should not be treated as finalized:

1. SP gained per level and from milestones/expedition events.
2. SP costs for unlocking and upgrading skills.
3. Whether every skill uses Rank 1–3 or some skills have special progression.
4. Exact skill damage coefficients, MP Costs, durations, and values.
5. Exact weapon requirements for individual skills.
6. Exact cooldown/usage-limit model, if any, for high-impact skills.
7. Barbarian Berserk's final bonus package and duration.
8. Martial Artist maximum combo, combo gain by skill, Perfect QTE gain, and finisher scaling.
9. Rogue Exploit trigger definitions and simultaneous trigger behavior.
10. Ranger Hunt Mark bonus, duration, target switching, and refresh rules.
11. Cleric Resurrection / Guardian Angel / Miracle limits.
12. Wizard skill-specific element and damage details for any skills still described generically.
13. Warlock Curse interactions with target death, Broken state, and independent instances.
14. Druid Root/control behavior and boss restrictions.
15. Final skill names, descriptions, visual effects, and audio feedback where still draft.

---

# 15. Related Documents

- **Game Design Document** — Core game loop, party, progression, and dungeon structure.
- **Race System** — Race abilities and class-independent race rules.
- **Damage & Status Effect System** — Damage categories, formula, QTE, Critical, affinities, resistance, and status behavior.
- **Inventory & Equipment System** — Equipment compatibility, stats, affixes, and persistence.
- **Shop System** — Hub facilities, gold, and run economy.

---

**End of Class System — Working Design Draft**
