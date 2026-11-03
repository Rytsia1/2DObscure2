# Background System
**Project:** Tugas GenAI  
**Document Version:** Draft v0.1  
**System Type:** Character Starting Perk  
**Status:** Working Draft — balancing values are provisional

---

## 1. Overview

Background represents a character's life and experience before becoming an adventurer. Each character selects one Background, which grants a passive perk that develops as the character levels up.

Backgrounds provide a distinct gameplay identity without determining a character's Race, Class, or Skill Loadout.

### Design Principles

- Each character has one Background.
- A Background grants one passive perk.
- Perks improve at designated character-level milestones.
- A newly unlocked Rank replaces the previous Rank; effects from different Ranks do not stack.
- Background perks should remain useful across multiple Classes.
- Background perks do not determine Race, Class, or Skill Loadout.
- Values and effects in this document are proposals for evaluation, not confirmed final balance.

## 2. Rank Progression

The game's current documented Character Level Cap is 15. Background progression uses the same five milestone levels as the Skill Tree.

| Perk Rank | Unlock Level |
|---|---:|
| Rank I | 1 |
| Rank II | 4 |
| Rank III | 7 |
| Rank IV | 10 |
| Rank V | 13 |

Rank V is the highest Background Perk Rank within the current level cap.

## 3. Backgrounds and Perks

### 3.1 Acolyte — Faithful Devotion

**Identity:** Spiritual devotion, protection, and recovery.

Acolyte gains strength through faith and devotion. Its perk supports keeping the character and party alive.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Faithful Heart | Healing dealt increases by 5%. |
| II | 4 | Divine Resilience | Healing dealt increases to 8%. |
| III | 7 | Blessed Recovery | Healing dealt increases to 10%. Healing received also increases by 5%. |
| IV | 10 | Sanctified Spirit | Healing dealt increases to 12%, and healing received increases by 8%. |
| V | 13 | Faith Unbroken | Healing dealt increases to 15%, and healing received increases by 10%. |

**Design note:** Acolyte can benefit any Class with access to healing; it is not exclusive to Cleric.

### 3.2 Charlatan — Master of Deception

**Identity:** Manipulation, deception, and exploiting enemy weaknesses.

Charlatan turns deception into a combat advantage when enemies are impaired or affected by debuffs.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | False Promise | Damage dealt to enemies affected by a debuff increases by 3%. |
| II | 4 | Cunning Exploit | The damage bonus increases to 5%. |
| III | 7 | Deceptive Advantage | The damage bonus increases to 7%. |
| IV | 10 | Master Manipulator | The damage bonus increases to 10%. |
| V | 13 | Perfect Deception | The damage bonus increases to 12%. The character's first attack against an enemy that has not yet acted in the battle deals an additional 5% damage. |

**Design note:** The debuff condition uses the game's defined debuff classification. The Rank V first-attack bonus is a separate effect.

### 3.3 Guard — Guardian's Discipline

**Identity:** Defense, vigilance, and protection.

Guard has experience protecting people, places, or territory. Its perk provides consistent physical defense.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Watchful Guard | Physical Damage Taken is reduced by 3%. |
| II | 4 | Steady Defense | Physical Damage Taken is reduced by 5%. |
| III | 7 | Defensive Training | Physical Damage Taken is reduced by 7%. |
| IV | 10 | Guardian's Resolve | Physical Damage Taken is reduced by 9%. |
| V | 13 | Unyielding Guardian | Physical Damage Taken is reduced by 10%. While HP is below 30%, Physical Damage Taken is reduced by an additional 5%. |

**Design note:** This perk applies to Physical Damage, not automatically to all damage types.

### 3.4 Hermit — Solitary Wisdom

**Identity:** Solitude, endurance, and self-control.

Hermit has learned to endure difficult circumstances without relying on others. Its perk improves resistance to status effects.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Quiet Endurance | Status Effect Resistance increases by 5%. |
| II | 4 | Solitary Resolve | Status Effect Resistance increases to 8%. |
| III | 7 | Inner Balance | Status Effect Resistance increases to 10%. |
| IV | 10 | Unshaken Mind | Status Effect Resistance increases to 12%. |
| V | 13 | Tranquil Spirit | Status Effect Resistance increases to 15%. When the character successfully resists a status effect, restore MP equal to 3% of Max MP, at most once per turn. |

**Design note:** This perk follows the game's Status Effect Resistance rules and does not grant automatic immunity.

### 3.5 Farmer — Hardy Harvest

**Identity:** Hard work, physical endurance, and a simple life.

Farmer is accustomed to demanding work and harsh conditions. Its perk increases durability and post-battle recovery.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Hardy Worker | Max HP increases by 3%. |
| II | 4 | Sturdy Constitution | Max HP increases to 5%. |
| III | 7 | Seasoned Laborer | Max HP increases to 7%. |
| IV | 10 | Resilient Farmer | Max HP increases to 9%. |
| V | 13 | Bountiful Harvest | Max HP increases to 10%. After battle, restore HP equal to 3% of Max HP. |

**Design note:** Post-battle recovery does not revive a character whose HP has reached 0.

### 3.6 Noble — Noble Authority

**Identity:** Social status, leadership, and confidence.

Noble was raised in an aristocratic environment where influence and responsibility mattered. Its perk improves MP capacity and post-battle recovery.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Noble Bearing | Max MP increases by 3%. |
| II | 4 | Commanding Presence | Max MP increases to 5%. |
| III | 7 | Royal Confidence | Max MP increases to 7%. |
| IV | 10 | Noble Authority | Max MP increases to 9%. |
| V | 13 | Born to Lead | Max MP increases to 10%. After battle, restore MP equal to 5% of Max MP. |

**Design note:** MP capacity can benefit any character whose Active Skills use MP.

### 3.7 Sage — Learned Mind

**Identity:** Knowledge, research, and deep understanding.

Sage has spent considerable time studying. Its perk improves the efficiency of Active Skill MP usage.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Studied Knowledge | Active Skill MP Cost is reduced by 3%. |
| II | 4 | Efficient Casting | Active Skill MP Cost is reduced by 5%. |
| III | 7 | Deep Understanding | Active Skill MP Cost is reduced by 7%. |
| IV | 10 | Masterful Insight | Active Skill MP Cost is reduced by 9%. |
| V | 13 | Enlightened Mind | Active Skill MP Cost is reduced by 10%. After using an Active Skill, the character has a 10% chance to restore MP equal to 5% of Max MP. |

**Design note:** An Active Skill with an MP cost cannot become free through this perk; its minimum cost remains 1 MP.

### 3.8 Soldier — Veteran's Instinct

**Identity:** War experience, discipline, and combat ability.

Soldier has experience facing dangerous situations. Its perk improves consistent Physical Damage output.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Combat Training | Physical Damage increases by 3%. |
| II | 4 | Battle Experience | Physical Damage increases to 5%. |
| III | 7 | Veteran's Instinct | Physical Damage increases to 7%. |
| IV | 10 | Battle Hardened | Physical Damage increases to 9%. |
| V | 13 | War Veteran | Physical Damage increases to 10%. After the character receives damage from an enemy attack, its next Physical Damage instance increases by 5%, at most once per turn. |

**Design note:** The Rank V additional bonus applies to the next Physical Damage instance. Multiple enemy hits in the same turn do not stack the effect.

### 3.9 Wayfarer — Endless Journey

**Identity:** Travel, exploration, and adaptation.

Wayfarer is accustomed to long journeys and unfamiliar environments. Its perk improves Speed and can provide a temporary combat advantage.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Traveler's Instinct | Speed increases by 3%. |
| II | 4 | Roadwise | Speed increases to 5%. |
| III | 7 | Seasoned Traveler | Speed increases to 7%. |
| IV | 10 | Pathfinder | Speed increases to 9%. |
| V | 13 | Endless Journey | Speed increases to 10%. At the start of battle, the character has a 10% chance to gain 10% Speed for 2 turns. |

**Design note:** Speed affects turn order. This perk does not directly grant extra turns unless the turn-order system explicitly supports that behavior.

### 3.10 Scribe — Keeper of Records

**Identity:** Documentation, analysis, and recorded knowledge.

Scribe is accustomed to observing and recording information. Its perk improves accuracy and rewards repeated attacks against the same target.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Careful Observation | Accuracy increases by 3%. |
| II | 4 | Detailed Records | Accuracy increases to 5%. |
| III | 7 | Enemy Analysis | Accuracy increases to 7%. |
| IV | 10 | Tactical Notes | Accuracy increases to 9%. |
| V | 13 | Complete Record | Accuracy increases to 10%. After attacking the same enemy 3 times, the character's next attack against that enemy gains 5% Critical Chance. |

**Design note:** The Critical Chance bonus applies to the next attack against the same target. After triggering, the target's analysis counter resets.

## 4. Background Identity Summary

| Background | Primary Perk Focus | Potential Class Synergy |
|---|---|---|
| Acolyte | Healing and recovery | Cleric, Druid, support-oriented builds |
| Charlatan | Damage against debuffed targets | Rogue, Warlock, Ranger |
| Guard | Physical defense | Fighter, Cleric, Druid |
| Hermit | Status Effect Resistance | All Classes |
| Farmer | Max HP and recovery | Fighter, Barbarian, durable builds |
| Noble | Max MP and recovery | Wizard, Warlock, support-oriented builds |
| Sage | MP efficiency | Wizard, Cleric, Druid |
| Soldier | Physical offense | Fighter, Barbarian, Martial Artist |
| Wayfarer | Speed and turn order | Rogue, Ranger, support-oriented builds |
| Scribe | Accuracy and target analysis | Ranger, Rogue, Martial Artist |

## 5. Balancing and Implementation Notes

The following items require testing and confirmation before this draft is considered final:

1. **Percentage stacking:** Determine how Background bonuses stack with equipment, Class skills, Race abilities, and other modifiers.
2. **Damage calculation:** Confirm the exact position of damage bonuses and damage reduction in the combat formula.
3. **Status Effect Resistance:** Confirm the formula and whether resistance is additive or multiplicative.
4. **Healing rules:** Confirm whether healing dealt and healing received modifiers stack additively or multiplicatively.
5. **Post-battle recovery:** Confirm when recovery is applied and whether it applies between every battle or only after selected encounters.
6. **Speed and turn order:** Confirm how Speed affects turn order and whether temporary Speed modifiers can alter an already-established turn sequence.
7. **Accuracy and Critical Chance:** Confirm the definitions and calculation rules used by the combat system.
8. **MP cost rounding:** Apply the 1 MP minimum after MP Cost reduction and rounding.

The perk values and secondary effects in this document are provisional. They should be playtested against the game's final combat, attribute, equipment, and progression formulas.
