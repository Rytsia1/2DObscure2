# Handoff: Enhanced Common Variants and On-Defeat Transformation

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design sources (read first):** `Assets/Docs/Felna_Enhanced_Common_Variant_Spec_v1_1.md` and `Assets/Docs/Felna_On_Defeat_Transformation_Spec_v1_1.md`. Passages marked **(v1.1)** are binding. Where the specs and this handoff disagree, the specs win; report the disagreement.
- **Builds on the shipped Enemy AI** (`Combat/AI/`, `Felna_Enemy_AI_Architecture_v1_1.md`) and the earlier handoffs (difficulty pass, charge pacing, Zoroa double turn and summon). Don't regress them.
- Every number is a **posture-B starting value**: it is not final. Keep its label, pass test and tuning direction as a code comment next to each constant.
- Report the test results, the simulation metrics and anything you couldn't verify to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Context
- **Enhanced Common Variant:** the same sprite and skill identities as a common enemy, plus a configured stat profile, skill overrides, up to two patterns, and an intermediate AI tier. It stays Normal tier for scaling and QTE.
- **On-Defeat Transformation:** when an eligible common enemy hits 0 HP, it gets **one** independent roll to rise as its Enhanced form at full HP.
- **User decisions:**
  - no pre-roll indication;
  - no counterplay;
  - no extra reward;
  - **Accelerando allowed** on the alternative pattern (≥ 310 ms) as an approved exception to "Normal = Steady only".

### Code this reuses
- **Phase swap = transformation without the roll.** `Battle.TryAdvancePhase` (`Combat/Battle.cs` ~L1016) swaps a lethal combatant to another def with a fresh bar through `Combatant.AdvanceToPhase` (`Combat/Combatant.cs` ~L222). It already runs from both lethal paths: direct `Damage` (~L1091) and DoT at turn start (~L181).
- **Def builder:** `D1Enemy(...)` (`Data/Dungeon1Roster.cs` ~L67) builds defs and derives `hitPower` from `hitDamage`. `Unit.ScaleEnemyStats` applies Normal scaling once.
- **AI tiers:** `AiTier { Normal, Elite, Miniboss, Boss }` (`Data/Defs.cs` L72), with tier logic in `Combat/AI/AiProfile.cs`. The Normal focus cap checks `aiTier == AiTier.Normal` (`Combat/AI/EncounterBlackboard.cs` ~L216, `Combat/AI/UtilityScorer.cs` ~L133).
- **Accelerando arrays:** e.g. `ReaperAccelerando` (Elite minimum 310 ms). Normal QTE window: 90/240 ms with a 1000 ms lead (`Qte/QteConfig.cs` ~L42).
- **Summons:** `IsSummon` summons give no rewards. Zoroa's final bar removes summons without rewards; that is a **forced removal**.

## Changes
1. **`Data/Defs.cs`:**
   - Add `AiTier.Enhanced` **after Normal**, and check every place that stores or compares the enum by int.
   - `EnemyDef`: add `string enhancedVariant`, `float transformChance`, `bool isEnhanced`, and a `VariantStatMult` (HP, MP, ATK, MATK, DEF, MDEF, SPD; default 1).
2. **`Data/Dungeon1Roster.cs`:**
   - Add `static EnemyDef Enhanced(string baseId, VariantProfile p)`. It deep-copies the base def and its `EnemyAttack`s, then sets:
     - `id = baseId + "_enhanced"`, `name = base.name + " (Enhanced)"`;
     - `isEnhanced = true`, `aiTier = AiTier.Enhanced`, `tier = Tier.Normal`;
     - the same `sprite`, `tag` and `aiRole`;
     - `statMult` and `targetRule` (extend `TargetRule` with `LowestMdef` and `HighestThreat`);
     - the per-skill overrides.
   - Re-derive `hitPower` from the overridden `hitDamage` against the **base** ATK/MATK, exactly as `D1Enemy` does.
   - Register the 3 pilot variants (table below). On the 3 base defs, set `enhancedVariant` and `transformChance`.
   - Add a static validator, also run as a test, that enforces Variant Spec §13 on every `isEnhanced` def.
3. **`Run/RunState.cs`, the enemy `Unit` ctor:** after `ScaleEnemyStats`, apply `statMult` **once**: `floor(scaled × mult)`.
4. **`Combat/Combatant.cs`:**
   - Add `bool transformRolled`.
   - Add `ResetForTransform()`, which clears all statuses, stun, `Intent`, `Charge` and `JustReleasedCharge`, and sets MP to max.
   - Verify that `AdvanceToPhase` already gives full HP, Break refill and a cooldown reset, and extend it only where it doesn't.
5. **`Combat/Battle.cs`:**
   - Rename and generalize `TryAdvancePhase(t)` into `TryContinueAfterLethal(t, bool forced = false)`:
     - boss `nextPhase`: unchanged behaviour;
     - otherwise, if `!forced && t.enemy?.enhancedVariant != null && !t.transformRolled && !Excluded(t)`: set `t.transformRolled = true`. If `Rng.NextDouble() < t.enemy.transformChance`, call `AdvanceToPhase(variant)`, then `ResetForTransform()`, then fire the presentation event, log the line, and re-plan (below).
   - `Excluded(t)` is true for `IsSummon`, the Prologue (`run.IsPrologue`), and Boss-node encounters.
   - The forced summon cleanup on Zoroa's final bar passes `forced = true` (or bypasses this call).
   - **Stop the remaining hits** of the current action on a target that just transformed, and **discard overkill**.
   - **Victory:** make sure the outcome check runs after the roll.
   - **Intent:** if the transformed enemy's turn is still pending this round, call `EnemyAI.Plan(this, t)` at once, the same way the Zoroa turn-B re-plan does. Otherwise it plans at the next `StartRound`.
   - **Rewards and relics:** an instance counts as defeated once, when its final form dies. Make sure the R013/R022 "enemy defeated" hooks (~L996) don't fire on a successful transform.
6. **`Combat/AI/`:**
   - Add `AiTierExt.IsNormalTier(t)` (Normal or Enhanced) and use it everywhere the code checks `== AiTier.Normal` for restrictions (focus cap and similar).
   - `AiProfile.Build(Enhanced)`: Normal capabilities plus the configured `targetRule`, plus resource/cooldown awareness with reserve horizon = its next turn, at Normal decay and threshold. No prediction, objectives or synergy setup (Variant Spec §7.5).
   - `HighestThreat` reads the shared encounter memory.
7. **`Screens/CombatScreen.cs`:** the presentation (Transformation Spec §7.1):
   - the log line;
   - an existing `Sfx` sting;
   - a sprite flash or tint through the same hook used for the boss phase transition (`BossPhaseTransitioning` or an equivalent event);
   - a persistent **"ENHANCED"** nameplate tag.

## Pilot variants (posture B: starting values, not final)
| Family | Stat multipliers (after scaling) | Standard pattern (unchanged) | Alternative pattern (override of an existing skill) | Targeting | `transformChance` |
|---|---|---|---|---|---|
| Hollow Squire (Vanguard) | HP ×1.6, DEF ×1.3, ATK ×1.15 | Rusty Cleave 1 × 18, Steady | Rusty Cleave as Repeated Assault: `hitDamage {12,12}`, Steady | `LowestDef` | 0.10 |
| Soulbound Hound (Disruptor) | HP ×1.4, SPD ×1.25, ATK ×1.2 | Rending Bite 1 × 17. Soul Snarl unchanged (Fear 20%) | Rending Bite: `hitDamage {8,7,7}`, `intervals {0.350, 0.310}` (Accelerando) | `HighestThreat` | 0.10 |
| Cinder Wisp (Elemental) | HP ×1.4, MATK ×1.25, MP ×1.5 | Ember Bolt 1 × 22, Burn 20% | Ember Bolt: `hitDamage {13,13}`, Steady, Burn 30%, applied once per attack, MP 8 | `LowestMdef` | 0.10 |

**Two patterns:** the standard and alternative entries are two `EnemyAttack` configs of the same skill name. Each is selected by the AI under its own condition (for example, the alternative while MP or cooldown allow), so there are at most 2 patterns per variant.

| Knob | Pass test | If it fails |
|---|---|---|
| `transformChance` 0.10 | 1–3 transformations per D1 run on average (200-map sim), and playtesters call it "a surprise", not "routine" | Routine: 0.05. Never noticed: 0.15 |
| Enhanced HP multiplier | A transform adds ≤ 2 rounds to the encounter on average (level-matched sim party) | Lower the HP multiplier first |
| Damage of the alternative pattern | An Enhanced turn deals ≤ 1.5× the base form's average turn | Lower the per-hit damage |
| Hound Accelerando 350 / 310 | Good/Perfect rate within 10 pp of the Soul Reaper's Accelerando among playtesters | Slow to 350 / 330 |

## Tests (`Tests/Editor/`)
- **Roll rules:**
  - exactly one roll per instance;
  - a lethal hit after transforming means defeat;
  - DoT-lethal rolls;
  - forced removal never rolls;
  - summons, Prologue and Boss-node enemies never roll;
  - over 10000 seeded trials, the rate is 10% ± 1%.
- **State after transform:**
  - full Enhanced HP and MP;
  - statuses, Broken, stun, Intent, Charge and `JustReleasedCharge` cleared;
  - cooldowns reset;
  - overkill and the remaining hits of the killing action discarded;
  - AoE co-targets unaffected.
- **Initiative and intent:**
  - same queue slot;
  - no extra turn;
  - waits if it already acted;
  - if pending, a new intent is published immediately;
  - planning slot unchanged.
- **Victory:** the last enemy transforming continues the battle, and victory comes only on its final defeat.
- **Rewards:** EXP, gold and relic triggers are identical with and without a transform.
- **Data validator** (Variant Spec §13) on all variants:
  - ≤ 2 patterns, ≤ 4 hits, ≤ 3 status-applying hits;
  - Accelerando ≥ 310 ms and only on the alternative pattern;
  - `tier == Normal`, Normal QTE window, no new statuses or skills;
  - stats = `floor(scaled × mult)`, applied once.
- **AI:**
  - Enhanced counts toward `FocusCapNormal`;
  - the Enhanced capability set matches Variant Spec §7.5;
  - encounter memory carries over the transform;
  - `AiTrace` records the re-plan.
- **Replay:** a recorded battle with a transformation reproduces identically (seeded `Rng`).
- **`BalanceSimulation`:** add transformations per run, extra rounds per transform, and party deaths with and without the pilot. Compare against the current AI baseline.

## Playtest script
1. **New player** (not told about the mechanic): after the first transformation, ask "What happened?" (target ≥ 80% correct) and "Did it feel fair?". **Watch this closely:** there is no mark, no counterplay and no reward.
2. **Readability:** an observer picks out the Enhanced unit at a glance afterwards.
3. **Stress:** a multi-hit kill, a DoT kill, the last enemy transforming, two transforms in one fight, and a transform during Zoroa-free Elite encounters with allies.
4. **Skill:** the Hound's Accelerando pattern is readable at the Normal QTE window.

## Tuning priority
Adjust `transformChance` first, then the Enhanced HP multiplier, then the alternative-pattern damage, then the Accelerando interval.

## Risk
This is a pure-risk event, by the user's decision. If playtest 1 reports unfairness, raise it with the user **before** lowering the chance below 0.05. The cheapest fixes are, in order:
1. a mark on eligible enemies;
2. a Broken-kill exception;
3. a small reward.

## Results (fill in)
| Date | Sample | Transforms per run | Extra rounds per transform | Party deaths (vs. baseline) | Unfairness reports |
|---|---|---|---|---|---|
| | | | | | |
