# FELNA — ON-DEFEAT TRANSFORMATION SYSTEM

**Document ID:** FELNA-TRANSFORM-001
**Version:** 1.1 (amended)
**Status:** Proposed — System Specification
**Related System:** Enhanced Common Enemy Variant System (`Felna_Enhanced_Common_Variant_Spec_v1_1.md`)
**Implementation plan:** `Enhanced_Variant_Transformation_Handoff.md`

---

## Change log

| Version | Date | Change |
|---|---|---|
| 1.0 | — | Original specification |
| 1.1 | 2026-11-22 | All §12 open decisions are resolved (§12.1). **User decisions:** no pre-roll indication, no counterplay, no extra reward. Added: the presentation of the transformation moment (§7.1), lethal-event edge cases (§6.1), exclusions (§3.1), and initiative/intent handling (§8.1). Amended passages are marked **(v1.1)** |

---

## 1. System Overview

The On-Defeat Transformation System allows an eligible Common enemy to transform into its Enhanced Common Variant when its HP reaches zero.

The transformation is triggered by a lethal damage event. Rather than immediately entering the defeated state, the enemy receives an independent random chance to transform.

This creates an unexpected second combat challenge without requiring a separate enemy sprite, animation set, or entirely new enemy identity.

**Core principle: One enemy identity, an unexpected second form.**

## 2. Confirmed Design Decisions

| Design Element | Specification |
|---|---|
| Randomization | Independent chance per enemy instance |
| Transformation timing | When HP reaches zero or below |
| HP after transformation | Full Enhanced Max HP |
| Trigger source | All lethal damage sources |
| Transformation limit | Once per enemy instance |
| Transformed classification | Enhanced Common Variant |
| Enemy assets | Reuses original sprite and animation assets |
| Skills | Existing skill identities with configured modifications |
| **(v1.1)** Pre-roll indication | None: pure surprise |
| **(v1.1)** Player counterplay | None: every eligible lethal event rolls |
| **(v1.1)** Extra reward | None: the encounter pays exactly as before |

## 3. Transformation Eligibility

Each enemy definition may specify whether it is eligible for On-Defeat Transformation.

An eligible enemy must have:
- A configured Enhanced Common Variant.
- A configured transformation probability.
- A valid Enhanced stat profile.
- A valid Enhanced skill configuration.
- A valid Enhanced attack pattern profile.

Enemies without an eligible Enhanced configuration are defeated normally.

The system does not automatically make every Common enemy eligible.

### 3.1 Eligibility in v1.1

- **Eligible families:** only the Dungeon 1 pilot families with a configured variant. These are Hollow Squire, Soulbound Hound and Cinder Wisp (Variant Spec §15.1).
- **Never eligible, even if the family is:**
  - summoned enemies (Zoroa's Harvest Call);
  - Tutorial Prologue enemies;
  - any enemy in a **Boss-node** encounter.

## 4. Random Chance Resolution

Each eligible enemy instance receives one independent transformation roll.

The roll occurs when that particular enemy first reaches the lethal damage threshold.

Conceptually:

`TransformationRoll = Random(0, 1)`

`Transform = TransformationRoll < ConfiguredTransformationChance`

The exact random-number implementation is an engineering detail. The configured probability determines the actual chance.

**(v1.1)** The roll uses the battle's seeded random generator, so a recorded battle replays identically.

### Rules

1. Each enemy instance has its own transformation chance.
2. Different eligible enemies can produce different outcomes in the same encounter.
3. The transformation chance is evaluated only once per enemy instance.
4. A failed roll results in normal defeat.
5. A successful roll intercepts the normal defeat process and begins transformation.
6. The same enemy cannot receive another transformation roll after transforming.

**Important:** Numerical transformation probabilities are not finalized. They must be configured and balanced separately.

**(v1.1)** Starting value: **10% per eligible instance**, the same in every dungeon. This is a starting value, not a final one. Its test and tuning direction are in the handoff.

## 5. Lethal Damage Sources

Transformation eligibility is not restricted to direct player attacks.

Any damage source that causes an eligible enemy's HP to reach zero or below can trigger the transformation check.

Examples include:
- Direct physical attacks.
- Direct magical attacks.
- Damage-over-time effects.
- Counterattacks.
- Secondary damage effects.
- Environmental damage.
- Other combat damage sources.

The system evaluates the actual lethal damage event rather than relying on the identity of the attacker.

If an eligible enemy is defeated by an environmental effect, it can still transform.

**(v1.1)** A **forced removal** is not a lethal damage event and never rolls. Examples are the removal of remaining summons when Zoroa's final bar breaks, and end-of-encounter cleanup.

## 6. Transformation Execution

The transformation process follows this sequence:

1. Receive damage.
2. Calculate final damage.
3. Apply damage to current HP.
4. Check whether HP has reached zero or below.
5. Check whether the enemy is eligible for transformation.
6. If eligible and not previously checked, perform its transformation roll.
7. If the roll fails, proceed with normal defeat.
8. If the roll succeeds, cancel the normal defeat transition.
9. Activate the Enhanced Common Variant configuration.
10. Restore the enemy to its full Enhanced Max HP.
11. Resume combat under the Enhanced configuration.

The transformation check must happen before the enemy is finalized as defeated.

### 6.1 Lethal-Event Edge Cases (v1.1)

| Case | Rule |
|---|---|
| Overkill damage | Discarded. It never carries into the Enhanced form |
| Remaining hits of the killing action | Stop on the transformed enemy. Other targets of an area attack are unaffected |
| Last living enemy transforms | Victory is evaluated **after** the roll, so the battle continues |
| Lethal DoT at turn start | Rolls like any other lethal event, through the same path as a boss phase change |
| Second lethal event after transforming | Normal defeat (no roll) |

## 7. Enhanced Form Configuration

Upon successful transformation, the enemy adopts its configured Enhanced Common Variant profile.

| Attribute | Transformation Behavior |
|---|---|
| Sprite | Reuse original sprite |
| Animation | Reuse compatible existing animations |
| HP | Full Enhanced Max HP |
| MP | **(v1.1)** Set to full Enhanced Max MP |
| ATK | Enhanced configuration |
| MATK | Enhanced configuration |
| DEF | Enhanced configuration |
| MDEF | Enhanced configuration |
| SPD | Enhanced configuration |
| Break Gauge | Enhanced configuration, if explicitly defined. **(v1.1)** Refilled to its maximum; Broken is removed |
| Skills | Modified configurations of existing skills. **(v1.1)** All move cooldowns reset |
| Attack patterns | Enhanced attack pattern profile |
| AI | Enhanced Common AI profile (`AiTier.Enhanced`) |
| QTE | Normal profile by default unless explicitly configured otherwise |
| **(v1.1)** Status effects | **All cleared**: buffs, debuffs and the DoT that triggered the lethal event |
| **(v1.1)** Stun / charge / planned intent | Cleared |

The Enhanced Common designation does not automatically promote the enemy to Elite tier.

It retains Normal-tier scaling and QTE classification unless a separate rule explicitly changes them.

### 7.1 Presentation of the Transformation Moment (v1.1)

Because nothing is indicated before the roll, **the transformation itself must be unmistakable**. Use at least two feedback channels:

1. A combat log line, for example "The Hollow Squire refuses to fall. It rises, Enhanced!"
2. A sound cue (reuse an existing sting).
3. A short flash or tint on the sprite, reusing the boss phase-transition presentation hook.
4. A persistent **"ENHANCED"** tag on the enemy's nameplate for the rest of the encounter.

## 8. Combat and Initiative Compatibility

The transformation does not grant an additional action or extra turn.

SPD continues to determine initiative priority according to the existing initiative system. SPD changes do not reorder the active queue mid-round.

### Recommended implementation behavior

- Preserve the enemy's existing initiative position when transformation occurs.
- Do not grant an immediate action merely because transformation succeeded.
- If the enemy has already acted this round, it waits for its next scheduled turn.
- If its turn is still pending, it uses its Enhanced configuration when its existing position arrives.

These are proposed compatibility rules and should be confirmed during combat-system integration.

### 8.1 Confirmed Integration Rules (v1.1)

- The four rules above are confirmed.
- **Intent:** if the enemy's turn is still pending this round, it **re-plans immediately** with the Enhanced kit and publishes the new intent. Any required telegraph appears at once. This matches the AI spec's telegraph lock: a new form produces a new published intent, not a secret substitution.
- **Planning order:** the enemy keeps its planning slot (AI Spec §4.5 v1.1).
- **AI memory:** the encounter memory and player profiles carry over unchanged, because it is the same encounter (AI Spec §5.3 v1.1).

## 9. Transformation State Management

An enemy instance should track its transformation state independently.

Suggested states:

| State | Meaning |
|---|---|
| COMMON_ACTIVE | Original enemy is active |
| TRANSFORMATION_CHECKED | Its one-time chance has been evaluated |
| TRANSFORMING | Successful transformation is being executed |
| ENHANCED_ACTIVE | Enhanced form is active |
| DEFEATED | Enemy has been defeated permanently for this encounter |

### State transition

`COMMON_ACTIVE → TRANSFORMATION_CHECKED`

On a failed roll:

`TRANSFORMATION_CHECKED → DEFEATED`

On a successful roll:

`TRANSFORMATION_CHECKED → TRANSFORMING → ENHANCED_ACTIVE → DEFEATED`

The enemy must never transition from ENHANCED_ACTIVE back into TRANSFORMING.

**(v1.1)** In implementation these states collapse to a `transformRolled` flag plus the active definition (`isEnhanced`). TRANSFORMING is the synchronous swap inside the lethal-damage resolution.

## 10. Multiple Enemies in One Encounter

Each eligible enemy is evaluated independently.

For example, an encounter contains three eligible Common enemies:

- Enemy A: transformation succeeds.
- Enemy B: transformation fails.
- Enemy C: transformation succeeds.

The result is two Enhanced enemies and one normally defeated enemy.

There is no encounter-wide shared roll.

The system does not guarantee that any particular number of enemies will transform.

## 11. Compatibility and Restrictions

The transformation system must respect the following existing Felna systems:

- Damage calculation and damage resolution.
- Status effects and control effects.
- Break Gauge and Broken mechanics.
- QTE execution.
- Enemy skill configuration.
- Enemy AI and targeting.
- Dynamic initiative queue.
- Enemy defeat and encounter completion.

Transformation must not silently bypass existing combat restrictions.

In particular, transformation must not create an additional turn, bypass the initiative queue, or grant an unconfigured attack.

**(v1.1) Rewards:** EXP, gold and relic "enemy defeated" triggers fire **once per enemy instance**, when its final form is defeated. The encounter pays exactly what it would without a transformation.

## 12. Open Design Decisions

The following details remain unfinalized:

1. Numerical transformation chance for each eligible enemy.
2. Whether transformation probability changes by dungeon.
3. Whether transformation is permitted for enemies already affected by certain status effects.
4. Whether existing status effects and their remaining durations carry over.
5. Whether Break Gauge and Broken state carry over.
6. Whether MP is restored, retained, or recalculated.
7. Transformation animation, visual effects, and audio presentation.
8. Whether the player receives any visual indication before the transformation roll resolves.
9. Whether every enemy family can eventually receive an Enhanced Variant.

These details should be decided independently rather than assumed.

### 12.1 Resolutions (v1.1)

| # | Decision |
|---|---|
| 1 | 10% per eligible instance (a starting value, pending simulation) |
| 2 | No: the same in every dungeon for now |
| 3 | Yes: allowed regardless of status (no counterplay) |
| 4 | No: every status is cleared |
| 5 | No: Break refills and Broken is removed |
| 6 | Restored to full Enhanced Max MP |
| 7 | Log line, sound sting, sprite flash/tint, and an "ENHANCED" nameplate tag (§7.1) |
| 8 | No pre-roll indication (user decision) |
| 9 | Eventually yes, family by family. v1.1 covers the 3 pilot families only |

## 13. Final Design Principle

The On-Defeat Transformation System is a controlled random encounter escalation mechanic.

The player defeats a familiar enemy, but there is a chance that the enemy reveals a stronger form instead of disappearing.

The mechanic should create surprise without compromising combat readability, initiative consistency, or the established difficulty philosophy of Relentless Tactical Survival.

**One enemy. One chance. One unexpected second battle.**

---

## 14. Design Risk Note (v1.1)

Three choices remove the usual safety valves at once: no pre-roll indication, no counterplay, and no reward. That makes the transformation a pure-risk event. The playtest in the handoff measures perceived unfairness directly.

If players report it as unfair, revisit those choices **before** lowering the chance below 5%:
1. **Cheapest fix:** a mark on eligible enemies.
2. **Next:** a Broken-kill exception, where an enemy killed while Broken does not roll.
3. **Last:** a small reward.
