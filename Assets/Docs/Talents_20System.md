# Talents System

**Game Design Document --- Draft**

-   **Status:** Draft for design review
-   **System:** Character progression / Talents
-   **Related systems:** Class System, Battle System, QTE System, Status
    Effects, Character Progression

------------------------------------------------------------------------

## 1. Overview

The Talent System provides characters with passive bonuses that support
different combat roles and build directions. Talents complement a
character's Class Skills and do not replace class identity.

The system is divided into: - **Shared Talents:** Available to
characters of any class. - **Class-Specific Talents:** Designed around
the identity and mechanics of a particular class.

Only effects explicitly defined in the current design discussion are
treated as specified. Values labeled **proposed** or **TBD** are not
final.

## 2. Talent Selection and Progression

Characters receive Talent selections at these level milestones:

    Character Level Talent Selection
  ----------------- ------------------
                  3 Select a Talent
                  6 Select a Talent
                  9 Select a Talent
                 12 Select a Talent
                 15 Select a Talent

### 2.1 Talent Offer Structure

**Proposal, pending confirmation:** - 1 Shared Talent - 2 Class-Specific
Talents

### 2.2 Talent Rules

-   Talent selections are intended to be unique for a character;
    already-selected Talents are not offered again.
-   Talent choices contribute to character build specialization.
-   Talent respec is available in the Hub and uses a special currency.
-   Respec currency cost and exact restrictions are **TBD**.

## 3. Shared Talents

### 3.1 Alert

**Effect:** Reduces the Critical Chance of an enemy attacking the
character who owns Alert.

-   Proposed value: **−10 percentage points Critical Chance** for the
    attacking enemy against this character.
-   Applies to attacks targeting the Talent owner, not the entire party.
-   Final value is subject to balancing.

### 3.2 Healer

**Effect:** Increases the amount of HP restored by the character.

-   Proposed value: **+15% healing**.
-   Applies to healing from both Skills and Items.
-   Final value is subject to balancing.

### 3.3 Durable

**Effect:** Improves the character's ability to survive incoming damage.

-   Proposed values: **+10% Max HP** and **5% Damage Reduction**.
-   Both effects are proposed as one Talent.
-   Final values are subject to balancing.

### 3.4 Keen Mind

**Effect:** Focuses on MP efficiency.

-   Proposed value: **−10% MP Cost** for Skills.
-   Example: a Skill costing 10 MP costs 9 MP after the reduction.
-   MP-cost rounding and minimum-cost rules should be finalized during
    implementation and balancing.

### 3.5 Arcane Reserve

**Effect:** Increases the character's Max MP.

-   Exact bonus value: **TBD**.

### 3.6 Poisoner

**Effect:** Increases the effectiveness of Poison applied by the
character.

-   Exact potency, duration, or other Poison parameters affected:
    **TBD**.
-   Intended to improve Poison applied by its owner.

### 3.7 Crusher

**Effect:** Increases the character's chance to inflict Stun on enemies.

-   Exact chance bonus: **TBD**.
-   Does not itself guarantee Stun.

### 3.8 Slasher

**Effect:** Increases the character's chance to inflict Bleed on
enemies.

-   Exact chance bonus: **TBD**.
-   Does not itself guarantee Bleed.

### 3.9 Piercer

**Effect:** When the character attacks a target, a portion of the
target's Physical Resistance is ignored.

-   Exact resistance reduction or penetration value: **TBD**.
-   Scope across attack types and whether it applies to every hit:
    **TBD**.

### 3.10 Lucky

**Effect:** Gives the player a chance to retry a failed QTE.

-   Proposed activation chance: **20%** when a QTE fails.
-   The player must perform the retry; Lucky does not automatically
    convert the failed QTE into a success.
-   Maximum **1 activation per QTE**.
-   Maximum **3 activations per battle**.
-   The activation counter resets at the start of the next battle.
-   After reaching the battle limit, Lucky cannot activate again during
    that battle.

### 3.11 Inspiring Leader

**Effect:** Grants the party a temporary bonus to damage resistance
while the Talent owner remains alive.

-   Proposed bonus: **10% Damage Reduction**.
-   Target: **the entire party, including the Talent owner**.
-   Duration: **3 turns**.
-   The effect ends immediately if the Talent owner dies, even if the
    duration has not elapsed.
-   Final value and turn-timing behavior are subject to balancing and
    battle-system definitions.

## 4. Additional Shared Talent Concepts

These names were previously proposed for the Shared Talent pool. Their
effects are not finalized in this document:

-   Vitality
-   Battle Rhythm
-   Iron Will
-   Efficient Casting
-   Quick Reflex
-   Fortune Seeker
-   Keen Eye
-   Steady Hand
-   Survivor's Instinct

Confirm their effects and values before treating these entries as locked
design.

## 5. Class-Specific Talent Pools

Class-Specific Talents are intended to reinforce each class's core
identity.

  -----------------------------------------------------------------------
  Class                               Core Identity
  ----------------------------------- -----------------------------------
  Fighter                             Weapon Techniques

  Barbarian                           Berserk State

  Martial Artist                      QTE Combo Flow

  Rogue                               Exploit

  Ranger                              Hunt Mark; the mark affects the
                                      Ranger themself

  Cleric                              Divine Protection; hybrid healing,
                                      protection, and Holy offense

  Wizard                              Elemental Specialization; no
                                      permanent element specialization

  Warlock                             Curse Mastery; Curse effects are
                                      independent and can coexist

  Druid                               Nature's Control; hybrid nature
                                      damage, control, and healing
  -----------------------------------------------------------------------

### 5.1 Proposed Class-Specific Talent Names

These names were proposed in the design discussion. Detailed effects and
balancing values remain subject to review.

**Fighter** - Weapon Adept - Guard Specialist - Tactical Opening -
Weapon Precision - Master of Arms

**Barbarian** - Bloodlust - Unbreakable Rage - Berserk Fury - Raging
Momentum - Last Stand

**Martial Artist** - Flowing Strikes - Perfect Timing - Combo
Discipline - Finisher Mastery - Unbroken Rhythm

**Rogue** - Opportunist - Exploit Weakness - Dirty Fighting - Shadow
Step - Executioner

**Ranger** - Hunter's Instinct - Marked Prey - Deadly Precision -
Longshot - Predator's Patience

**Cleric** - Divine Grace - Sanctuary - Holy Retribution - Guardian's
Resolve - Last Benediction

**Wizard** - Elemental Adept - Focused Casting - Elemental Insight -
Arcane Efficiency - Elemental Mastery

**Warlock** - Lingering Malice - Curse Potency - Dark Knowledge - Soul
Harvester - Doom Weaver

**Druid** - Nature's Embrace - Wild Growth - Root Mastery - Nature's
Wrath - Primal Balance

## 6. Open Design Decisions

1.  Confirm whether each Talent milestone offers **1 Shared + 2
    Class-Specific Talents**.
2.  Set final numeric values for Alert, Healer, Durable, Keen Mind,
    Lucky, and Inspiring Leader.
3.  Define Arcane Reserve's Max MP bonus.
4.  Define which Poison properties Poisoner improves.
5.  Define Crusher's Stun chance bonus and Slasher's Bleed chance bonus.
6.  Define Piercer's Physical Resistance penetration value and attack
    scope.
7.  Confirm whether the other proposed Shared Talent names and effects
    belong to the final pool.
8.  Define complete effects and numeric values for all Class-Specific
    Talents.
9.  Define respec currency cost and restrictions.
10. Specify turn timing and refresh behavior for Inspiring Leader.

------------------------------------------------------------------------

## Change Log

  -----------------------------------------------------------------------
  Version                             Notes
  ----------------------------------- -----------------------------------
  0.1                                 Initial Talent System draft
                                      assembled from current design
                                      discussion. Includes Lucky's battle
                                      activation cap and Inspiring
                                      Leader's duration and life
                                      condition.

  -----------------------------------------------------------------------
