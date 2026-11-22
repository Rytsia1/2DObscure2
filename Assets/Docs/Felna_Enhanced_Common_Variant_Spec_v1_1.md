# FELNA — ENHANCED COMMON ENEMY VARIANT SYSTEM

**Document ID:** FELNA-ENEMY-VARIANT-001
**Version:** 1.1 (amended)
**Status:** Proposed — System Specification
**Scope:** Enemy Design, Combat, AI, Encounter Generation, Enemy Scaling
**Parent Systems:** Enemy Roster, Enemy Scaling, Initiative & Turn Order, QTE, Advanced Enemy AI
**Companion documents:** `Felna_On_Defeat_Transformation_Spec_v1_1.md`, `Enhanced_Variant_Transformation_Handoff.md`, `Felna_Enemy_AI_Architecture_v1_1.md`

---

## Change log

| Version | Date | Change |
|---|---|---|
| 1.0 | — | Original specification |
| 1.1 | 2026-11-22 | **A.1** Approved Accelerando exception for the alternative pattern (§6.6). **A.2** New AI tier `Enhanced`, which still counts as Normal tier for every Normal restriction (§7.5). **A.3** The data model is a C# `EnemyDef` built from the base def; there is no JSON layer (§12.3). **A.4** Pilot roster of 3 families (§15.1). **A.5** In this version, Enhanced variants appear only through On-Defeat Transformation (§10.5). Amended passages are marked **(v1.1)** |

---

## 1. System Overview

### 1.1 Purpose

The Enhanced Common Enemy Variant System allows an existing Common enemy to have a more challenging variation without requiring a completely new enemy implementation.

An Enhanced Common Variant reuses the original enemy's:

- Sprite and visual identity.
- Animation assets.
- Existing skill identities.
- Existing combat effects, unless explicitly modified through an approved configuration.
- Core enemy implementation.

The variant introduces additional difficulty through:

- Modified combat statistics.
- Modified skill configurations.
- Alternative attack patterns.
- More sophisticated decision-making.
- Different combinations of existing combat capabilities.

### 1.2 Design Philosophy

**Greater difficulty through variation, not necessarily through additional content.**

An Enhanced Common enemy should feel like a more dangerous version of a familiar enemy.

The player should recognize the enemy but cannot assume that its combat behavior is identical to the ordinary version.

The system should preserve visual consistency, combat readability, mechanical fairness, and efficient content production.

### 1.3 Core Definition

An Enhanced Common Variant is a special configuration of an existing Common enemy.

It is not classified as an Elite enemy.

It remains within the Normal enemy tier for the purposes of:

- Enemy scaling.
- QTE profile.
- Encounter classification.
- Normal-tier mechanical restrictions, unless an explicit approved exception exists. **(v1.1)** The only approved exception is the Accelerando alternative pattern (§6.6).

---

## 2. Enemy Classification

The system distinguishes enemy identity, variant type, and difficulty tier.

| Classification | Description |
|---|---|
| Common / Normal | Original standard enemy |
| Enhanced Common | Modified version of an existing Common enemy |
| Elite | Dedicated Elite enemy or Elite-tier encounter |
| Miniboss | Special encounter classification using its configured scaling tier |
| Boss | Dedicated boss encounter |

### 2.1 Enhanced Common Classification

Enhanced Common is a variant designation, not a new scaling tier.

Example:

| Property | Common Hollow Squire | Enhanced Hollow Squire |
|---|---|---|
| Enemy Identity | Hollow Squire | Hollow Squire |
| Variant | Normal | Enhanced |
| Scaling Tier | Normal | Normal |
| QTE Tier | Normal | Normal |
| Encounter Type | Common | Enhanced Common |
| Sprite | Original | Same |
| Animation | Original | Same |
| Skill Kit | Original | Modified configuration |
| Attack Patterns | Standard | Enhanced configuration |
| **(v1.1)** AI Tier | Normal | Enhanced (counts as Normal for restrictions, §7.5) |

An Enhanced Common enemy must not automatically inherit Elite statistics or Elite QTE timing simply because it is stronger than its standard counterpart.

---

## 3. Variant Construction

An Enhanced Common Variant consists of five configuration layers.

1. Base Enemy Definition.
2. Variant Stat Profile.
3. Variant Skill Configuration.
4. Variant Attack Pattern Profile.
5. Variant AI Profile.

These layers should be independently configurable.

### 3.1 Base Enemy Definition

The original Common enemy remains the source of identity and default behavior.

The variant references the original enemy definition rather than duplicating the entire enemy.

Example:

- Base Enemy: Hollow Squire.
- Variant: Enhanced Hollow Squire.
- Shared assets: Hollow Squire.
- Shared skill identities: Hollow Squire's existing skills.
- Configuration overrides: Enhanced profile.

### 3.2 Asset Reuse

The following assets must be reused:

- Sprite.
- Idle animation.
- Movement animation.
- Attack animation.
- Hit animation.
- Defeat animation.
- Existing visual effects, where applicable.

The variant may reuse an existing animation multiple times to support a modified multi-hit sequence, provided the animation timing and presentation remain valid.

No additional animation asset is required by this system.

An optional UI indicator may distinguish Enhanced enemies from ordinary enemies. **(v1.1)** Decided: once an enemy *is* Enhanced, its nameplate shows a persistent "ENHANCED" tag (see Transformation Spec §7.1). Nothing is shown before then.

---

## 4. Variant Stat System

### 4.1 Purpose

Enhanced Common enemies receive their own stat configuration while retaining Normal-tier dungeon scaling.

Their increased strength should be controlled by a dedicated Variant Stat Profile.

### 4.2 Supported Attributes

The following attributes may be modified:

- HP.
- MP.
- ATK.
- MATK.
- DEF.
- MDEF.
- SPD.

Break Gauge may also receive a variant-specific value if explicitly configured.

### 4.3 Recommended Calculation

First, calculate the enemy's normal dungeon-scaled statistics.

Then apply the Enhanced Common variant modifier.

`NormalScaledStat = floor(BaseStat × (1 + NormalGrowthRate × (DungeonNumber - 1)))`

`EnhancedStat = floor(NormalScaledStat × VariantModifier)`

The modifier is applied exactly once.

Effective SPD is calculated after the variant-adjusted SPD is obtained, following the established initiative rules.

### 4.4 Variant Modifier Configuration

Each Enhanced Common enemy can have its own modifier profile.

Example configuration:

| Attribute | Normal | Enhanced Modifier |
|---|---|---|
| HP | Original | Configurable |
| MP | Original | Configurable |
| ATK | Original | Configurable |
| MATK | Original | Configurable |
| DEF | Original | Configurable |
| MDEF | Original | Configurable |
| SPD | Original | Configurable |

No universal multiplier is established in this specification.

Different enemy roles should receive different stat distributions.

For example:

- Enhanced Vanguard: Higher HP and DEF.
- Enhanced Skirmisher: Higher SPD and ATK.
- Enhanced Caster: Higher MATK and MP.
- Enhanced Bruiser: Higher HP and ATK.

These are design archetypes, not mandatory stat templates.

**(v1.1)** The pilot profiles (§15.1) are *starting values*. Each one has a test and a tuning direction in the handoff. They are not universal multipliers.

---

## 5. Modified Skill Configuration System

### 5.1 Core Rule

Enhanced Common enemies reuse existing skill identities but may use modified skill configurations.

A modified configuration does not automatically create a new skill.

It represents a different configuration of an existing skill.

### 5.2 Configurable Skill Parameters

The following parameters may be modified when appropriate:

| Parameter | Modification |
|---|---|
| Base damage reference | Allowed |
| Hit count | Allowed |
| Hit sequence | Allowed |
| Target scope | Allowed |
| Target selection condition | Allowed |
| Break Power | Allowed |
| Status chance | Allowed through explicit configuration |
| MP cost | Allowed |
| Cooldown | Allowed |
| QTE pattern | Allowed, subject to QTE rules |
| Attack pattern association | Allowed |

Every modification must be explicitly defined in the variant configuration.

A modified value must never be inferred automatically from the enemy's increased statistics.

### 5.3 Skill Identity Restrictions

The following should remain unchanged by default:

- Skill identity.
- Skill name.
- Core skill category.
- Damage element.
- Existing status effect identity.
- Fundamental skill purpose.
- Required animation compatibility.

Changing an effect's identity, adding a new status effect, or introducing a new mechanical effect requires a separate skill design approval.

### 5.4 Hit Count Restrictions

Enhanced Common enemies must respect the established multi-hit restrictions:

- No more than 6 hits per attack.
- Existing Dungeon 1 enemy designs currently remain at no more than 4 hits.
- Status application on multi-hit attacks must respect the maximum of 3 status-applying hits.
- A status normally applies once per attack unless explicitly specified otherwise.

For the initial implementation, the recommended limit is **4 hits per attack**, matching the existing enemy design constraints.

### 5.5 Multi-Target Restrictions

Multi-target attacks must respect the existing per-target damage coefficient cap of 60% of the equivalent single-target attack.

Changing an attack from single-target to multi-target requires explicit balance validation.

### 5.6 Resource Restrictions

Modified MP costs and cooldowns must be explicitly configured.

The variant must never gain automatic MP regeneration unless a specific existing or separately approved mechanic grants it.

Insufficient MP must be handled through valid fallback behavior rather than inventing an alternative skill.

---

## 6. Attack Pattern System

### 6.1 Purpose

Attack Pattern Variation is one of the primary ways to distinguish Enhanced Common enemies from their original versions.

The variant may use an alternative pattern while retaining its existing skill kit.

### 6.2 Pattern Limits

Recommended initial restriction:

- Maximum two attack patterns per Enhanced Common enemy.
- One standard pattern.
- One alternative pattern.

The standard pattern preserves the original enemy's behavior.

The alternative pattern provides the enhanced combat variation.

### 6.3 Pattern Examples

| Pattern | Purpose |
|---|---|
| Steady | Standard, predictable attack |
| Accelerando | Faster sequence of attacks. **(v1.1)** Allowed for the alternative pattern only (§6.6) |
| Syncopated | Irregular timing sequence. **(v1.1)** Not allowed for Enhanced Commons |
| Charge | Preparation followed by a stronger release, only if supported by the skill design. **(v1.1)** Not allowed for Enhanced Commons |

These patterns must use valid existing combat mechanics and animation-compatible sequences.

### 6.4 Pattern Selection

Pattern selection may consider:

- Current HP.
- Current MP.
- Skill availability.
- Cooldowns.
- Target condition.
- Number of active party members.
- Existing enemy conditions.
- Configured pattern priority.

Pattern selection must occur during the appropriate round preparation process.

Dangerous actions must retain their required telegraph behavior.

### 6.5 Pattern Predictability

Patterns should not be selected through unrestricted random behavior.

Use configured conditions and controlled selection rules.

The player should be able to recognize and learn the enemy's behavior through observation.

### 6.6 Approved Accelerando Exception (v1.1)

GDD rule: Normal enemies use Steady patterns only. As an explicit approved exception:
- an Enhanced Common's **alternative** pattern may be **Accelerando**;
- every hit interval must be **≥ 310 ms**, the Elite minimum (Dungeon 1 Elite Accelerando floor).

Within that exception:
- The **standard** pattern stays Steady.
- The QTE **timing window** stays the **Normal** profile (90 ms Perfect / 240 ms Good, 1000 ms lead). Only the hit spacing accelerates.
- Syncopated and Charge remain disallowed.

---

## 7. Enhanced Common AI Profile

### 7.1 AI Classification

Enhanced Common enemies use an intermediate AI profile.

They are more capable than standard Common enemies but do not automatically receive the complete Elite tactical intelligence profile.

Recommended AI hierarchy:

| Enemy Type | AI Profile |
|---|---|
| Common | Basic Rule-Based AI |
| Enhanced Common | Tactical Rule-Based AI |
| Elite | Advanced Hybrid AI |
| Miniboss | Advanced Hybrid AI with encounter-specific configuration |
| Boss | Advanced Hybrid AI with boss-specific planning |

### 7.2 Enhanced Common Capabilities

Enhanced Common AI may use:

- Current battlefield evaluation.
- Skill-condition evaluation.
- Basic threat assessment.
- Target-condition evaluation.
- Resource awareness.
- Cooldown awareness.
- Alternative pattern selection.
- Limited tactical adaptation.

### 7.3 AI Limitations

Enhanced Common AI should not automatically receive:

- Full two-step predictive planning.
- Advanced player behavior recognition.
- Long-term player adaptation.
- Complex team-level strategic objectives.
- Advanced tactical synergy chains.
- Commander-level coordination.

These capabilities remain part of the more sophisticated AI profiles.

### 7.4 Coordination

Enhanced Common enemies may participate in basic encounter coordination.

Examples:

- Avoiding unnecessary duplicate target reservations.
- Following explicitly configured target priorities.
- Using a support skill when its conditions are met.
- Respecting existing enemy action commitments.

Coordination must remain flexible and must not override explicit skill targeting rules.

### 7.5 Mapping to the Shipped AI Tiers (v1.1)

Add `AiTier.Enhanced` between Normal and Elite in the Enemy AI architecture (Architecture §12 v1.1).

| Capability | Normal | **Enhanced** | Elite |
|---|---|---|---|
| Utility + role, read shared memory/Blackboard, publish reservations, synergy follow-up | ✓ | ✓ | ✓ |
| Configured targeting rule (§8) | — | ✓ | ✓ (locked rules) |
| Resource / cooldown awareness | — | ✓ (reserve horizon = its next turn only) | ✓ (H = 2) |
| Adaptation responses | Normal decay / threshold | Normal decay / threshold | Elite |
| Prediction H = 2, strategic objectives, synergy setup | — | — | ✓ |

**For every Normal-tier restriction, Enhanced counts as Normal.** This includes the Normal focus cap (`FocusCapNormal`).

---

## 8. Target Selection

Enhanced Common enemies may use configured target-selection rules.

Possible criteria include:

- Lowest current HP.
- Lowest HP percentage.
- Lowest DEF.
- Lowest MDEF.
- Highest threat.
- Specific status condition.
- Random valid target, when explicitly configured.

The selected targeting rule must be defined per enemy variant.

A variant must not silently inherit an Elite targeting rule merely because it uses some Elite AI capabilities.

All target selections must respect legal target restrictions and deterministic retargeting rules.

**(v1.1)** "Highest threat" means the hero with the highest observed impact in the **shared encounter memory** (AI Spec §5.3 v1.1). It is derived only from observable events.

---

## 9. QTE and Combat Integration

### 9.1 QTE Classification

Enhanced Common enemies retain the Normal QTE profile by default.

They do not automatically inherit Elite QTE timing.

Dungeon-specific timing adjustments must follow the existing QTE system.

**(v1.1)** The Accelerando exception (§6.6) changes hit spacing only, never the Normal timing window.

### 9.2 QTE Pattern Changes

A modified skill may use a different QTE sequence if its configuration explicitly defines one.

Examples:

- One-hit Defensive QTE.
- Two-hit Defensive QTE.
- Three-hit Defensive QTE.
- Modified timing pattern using an existing supported pattern.

The configuration must respect established QTE limits.

### 9.3 Telegraph Requirements

Enhanced Common enemies must follow the same telegraph fairness requirements as other enemies.

Dangerous actions requiring awareness must be telegraphed from round preparation.

The variant system must not introduce hidden Heavy attacks, secret charge releases, or unannounced dangerous actions.

### 9.4 Initiative

Enhanced Common enemies use the shared initiative queue.

Their SPD affects turn order only.

It does not grant additional actions or extra turns.

---

## 10. Encounter Generation

### 10.1 Encounter Classification

Enhanced Common enemies are distinct encounter variants but remain Normal-tier enemies.

They may appear in designated encounter configurations.

### 10.2 Spawn Eligibility

The following should be configurable:

- Whether an enemy can appear as Enhanced Common.
- Which dungeon layers permit the variant.
- Which encounter groups can contain it.
- Whether it can appear alongside its ordinary version.
- Whether it can appear in special nodes.
- Whether its appearance is random or explicitly placed.

### 10.3 Encounter Balance

Enhanced Common enemies should not be treated as ordinary Common enemies when evaluating encounter difficulty.

Encounter composition should account for:

- Increased enemy durability.
- Modified damage output.
- Increased attack complexity.
- QTE execution requirements.
- Additional tactical pressure.

### 10.4 Spawn Frequency

No universal spawn frequency or probability is established by this specification.

Those values should be defined separately through dungeon encounter design and balancing.

### 10.5 Appearance in v1.1

In this version, Enhanced variants appear **only through On-Defeat Transformation** (`Felna_On_Defeat_Transformation_Spec_v1_1.md`). There is no direct spawn in encounters. The §10.2 spawn options stay reserved for a future version.

---

## 11. Example Implementation: Hollow Squire

This example demonstrates how the system can be applied to an existing Dungeon 1 enemy.

### 11.1 Original Enemy

**Hollow Squire — Common**

| Attribute | Value |
|---|---:|
| HP | 145 |
| MP | 35 |
| ATK | 18 |
| DEF | 8 |
| MATK | 8 |
| MDEF | 6 |
| SPD | 9 |
| Break Gauge | 100 |

Original behavior:

- One-hit basic attack.
- Steady pattern.
- Normal QTE.
- Standard Common AI.

### 11.2 Enhanced Variant

**Hollow Squire — Enhanced**

Proposed configuration:

- Same sprite.
- Same animation.
- Same skill identity.
- Normal scaling tier.
- Normal QTE profile.
- Variant-specific stat modifiers.
- Two attack patterns.

### 11.3 Example Attack Patterns

**Pattern A — Steady Slash**

- Uses the original attack.
- One hit.
- Original skill configuration.

**Pattern B — Repeated Assault**

- Uses the same sword attack animation.
- Two consecutive hits.
- Two Defensive QTE markers.
- Explicitly configured damage per hit.
- No new status effect.

The repeated attack is a modified skill configuration, not a new skill identity.

The pattern must be balanced using its total potential damage and QTE interaction.

**(v1.1)** In the roster, the original attack is **Rusty Cleave** (1 × 18). Repeated Assault is a Rusty Cleave configuration: 2 × 12, Steady.

### 11.4 Tactical Behavior

The Enhanced Hollow Squire may:

- Prefer the configured vulnerable target.
- Select Repeated Assault when its conditions are satisfied.
- Use Steady Slash when the alternative pattern is unavailable.
- Reevaluate its available actions during round preparation.

This example is illustrative and does not alter the existing Hollow Squire design lock.

---

## 12. Data Architecture

The implementation should use reusable enemy definitions and separate variant configuration data.

### 12.1 Conceptual JSON Structure

```json
{
  "enemy_id": "hollow_squire",
  "variant_id": "hollow_squire_enhanced",
  "variant_type": "enhanced_common",
  "scaling_tier": "normal",
  "qte_profile": "normal",

  "assets": {
    "reuse_base_enemy_assets": true
  },

  "stat_profile": {
    "hp_multiplier": 1.0,
    "mp_multiplier": 1.0,
    "atk_multiplier": 1.0,
    "matk_multiplier": 1.0,
    "def_multiplier": 1.0,
    "mdef_multiplier": 1.0,
    "spd_multiplier": 1.0
  },

  "skill_configuration": {
    "source_skill_set": "hollow_squire_base",
    "allow_parameter_overrides": true
  },

  "attack_patterns": {
    "max_patterns": 2,
    "patterns": [
      "steady",
      "repeated_assault"
    ]
  },

  "ai_profile": "tactical_rule_based",

  "encounter": {
    "eligible_for_random_encounters": true,
    "eligible_dungeons": []
  }
}
```

The multipliers above are neutral placeholders, not recommended balance values.

### 12.2 Skill Override Structure

```json
{
  "variant_id": "hollow_squire_enhanced",

  "skill_overrides": [
    {
      "skill_id": "basic_sword_attack",
      "configuration_id": "repeated_assault",

      "hit_count": 2,
      "damage_per_hit": 12,

      "target_scope": "single_target",
      "qte_pattern": "steady",

      "status_effect": null
    }
  ]
}
```

The identifiers and numerical values are illustrative. Final values must reference the actual skill database and combat formula.

### 12.3 Implemented Form (v1.1)

The roster is C# data (`Data/Dungeon1Roster.cs`), so variants are too. The JSON above stays the *conceptual* schema.

- A variant is an `EnemyDef` produced by a roster helper, `Enhanced(baseId, profile)`. The helper:
  - copies the base definition (same sprite, tag, Normal tier, and skill names and elements);
  - sets `isEnhanced` and `aiTier = Enhanced`;
  - applies the explicit per-skill overrides, then re-derives the hit coefficients from the overridden damage values against the base ATK/MATK.
- The variant id is `<base>_enhanced`, and the display name is "<Name> (Enhanced)".
- Stat multipliers are applied **after** Normal scaling, exactly once (§4.3).

---

## 13. Validation Requirements

Every Enhanced Common Variant must pass the following checks before implementation approval.

| Validation | Requirement |
|---|---|
| Asset reuse | Uses valid base enemy assets |
| Animation compatibility | Every attack can be presented correctly |
| Stat scaling | Normal growth rates applied exactly once |
| QTE | Valid Normal-tier profile. **(v1.1)** Accelerando only on the alternative pattern, with intervals ≥ 310 ms |
| Skill identity | Existing skill references are valid |
| Hit count | Respects multi-hit restrictions |
| Status | Respects status application restrictions |
| AoE | Respects multi-target damage cap |
| MP | Valid resource cost and fallback |
| Cooldown | Valid cooldown behavior |
| Pattern count | No more than two patterns |
| Targeting | Explicit and legal |
| Initiative | Uses shared initiative queue |
| Telegraph | Required dangerous actions are communicated |
| AI | Does not bypass combat rules. **(v1.1)** `AiTier.Enhanced`, and counts as Normal for restrictions |
| Encounter | Properly classified and balanced |

---

## 14. Implementation Roadmap

### Phase 1 — Variant Data Structure
- Introduce variant identifiers.
- Connect variants to base enemy definitions.
- Support independent stat profiles.
- Support skill configuration overrides.

### Phase 2 — Stat and Scaling Integration
- Implement variant stat modifiers.
- Integrate Normal-tier dungeon scaling.
- Validate Effective SPD calculations.

### Phase 3 — Skill Configuration Overrides
- Support configurable hit counts.
- Support configurable damage references.
- Support target scope and targeting conditions.
- Support cooldown and MP overrides.
- Validate QTE compatibility.

### Phase 4 — Attack Pattern System
- Support a standard and alternative pattern.
- Implement conditional pattern selection.
- Validate animation reuse.
- Validate telegraph behavior.

### Phase 5 — Enhanced Common AI
- Implement tactical rule-based decisions.
- Add basic threat evaluation.
- Add resource and cooldown awareness.
- Add configured target priorities.

### Phase 6 — Encounter Integration
- Add Enhanced Common encounter entries.
- Configure eligible dungeon layers.
- Validate encounter composition.
- Test normal and enhanced versions together.

**(v1.1)** In this version, Phase 6 is replaced by the On-Defeat Transformation integration (§10.5).

### Phase 7 — QA and Balance
- Verify all mechanical restrictions.
- Compare encounter difficulty.
- Evaluate QTE execution pressure.
- Check whether the enhanced version remains recognizable and fair.

**(v1.1)** The executable plan is `Enhanced_Variant_Transformation_Handoff.md`.

---

## 15. Open Design Decisions

The following values are intentionally not finalized.

| Parameter | Status |
|---|---|
| Universal stat multipliers | TBD. **(v1.1)** None; profiles are per family |
| Per-enemy stat profiles | **(v1.1)** Pilot starting values (§15.1), pending simulation |
| Maximum Enhanced Common spawn count | **(v1.1)** N/A: no direct spawns in v1.1 |
| Enhanced Common spawn probability | **(v1.1)** N/A: appearance only via transformation chance |
| Dungeon eligibility | **(v1.1)** Dungeon 1 pilot families only |
| Allowed pattern catalog | **(v1.1)** Steady (standard) + Steady or Accelerando ≥ 310 ms (alternative) |
| Skill override boundaries per enemy | **(v1.1)** §5 limits; the pilot overrides are in §15.1 |
| Enhanced Common AI complexity | **(v1.1)** Decided (§7.5) |
| Visual indicator | **(v1.1)** "ENHANCED" nameplate tag after transformation only |
| Whether multiple Enhanced variants can coexist in one encounter | **(v1.1)** Yes, as independent transformation outcomes |

These parameters should be decided through enemy roster design and balance testing rather than assumed globally.

### 15.1 Pilot Families (v1.1, starting values, not final)

| Family (archetype) | Stat multipliers (after scaling) | Standard pattern | Alternative pattern (existing skill, reconfigured) | Targeting |
|---|---|---|---|---|
| Hollow Squire (Vanguard) | HP ×1.6, DEF ×1.3, ATK ×1.15 | Rusty Cleave 1 × 18, Steady | Rusty Cleave as **Repeated Assault**: 2 × 12, Steady | Lowest DEF |
| Soulbound Hound (Disruptor) | HP ×1.4, SPD ×1.25, ATK ×1.2 | Rending Bite 1 × 17, Steady. Soul Snarl unchanged (Fear 20%) | Rending Bite: 3 hits 8 / 7 / 7, **Accelerando 350 / 310 ms** | Highest observed threat |
| Cinder Wisp (Elemental caster) | HP ×1.4, MATK ×1.25, MP ×1.5 | Ember Bolt 1 × 22, Burn 20% | Ember Bolt: 2 × 13, Steady. Burn 30%, applied once per attack. MP cost 8 unchanged | Lowest MDEF |

All three respect §5: ≤ 4 hits, ≤ 3 status-applying hits, single target, no new statuses, no new skills.

---

## 16. Final System Definition

The Enhanced Common Enemy Variant System allows existing Common enemies to become more challenging through configurable stat profiles, modified skill configurations, alternative attack patterns, and intermediate tactical AI.

Enhanced Common enemies:

1. Reuse their original visual assets and animations.
2. Retain their original enemy identity and skill identities.
3. May use explicitly modified skill configurations.
4. Remain Normal-tier for scaling and QTE purposes.
5. May have up to two configured attack patterns. **(v1.1)** The alternative pattern may be Accelerando with intervals ≥ 310 ms.
6. Use an intermediate tactical AI profile. **(v1.1)** `AiTier.Enhanced`.
7. Follow all established combat, initiative, QTE, targeting, and telegraph rules.

**Core principle: One enemy identity, multiple combat configurations.**

This system expands encounter variety while reducing the need for additional enemy assets and entirely new skill implementations.

**Document status: Proposed — awaiting approval and numerical balancing.**
