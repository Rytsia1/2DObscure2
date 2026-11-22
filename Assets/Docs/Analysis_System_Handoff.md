# Handoff: ANALYSIS command (inspection, Codex, Drain/Reflect, per-status resistance)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design source:** `Assets/Docs/Felna_Analysis_System_Spec_v1.md`. Read it first. If this handoff and the spec disagree, the spec wins.
- Every number below is a **posture-B starting value**, not a final one. Keep its label, its pass test and its tuning direction as a comment next to the constant.
- **One stage at a time.** Don't start a stage until the previous stage's gate passes. Report gate results to the user after each stage.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## What exists today
- **Affinity model 1:** `Affinities` / `Effectiveness { Neutral, Weak, Resist, Immune }` in `Combat/Elements.cs`. It's applied in `Battle.Deal` around L893: Immune returns 0 damage, then the multiplier is applied.
- **Affinity model 2:** `EnemyAffinityProfile` / `AffinityType { Normal, Weakness, Resistant, Nullification }` in `Combat/Damage/EnemyAffinity.cs`. It's used by `DamageCalculator` and Null Break (`NullBreakController`).
- **No Dungeon 1 roster enemy has affinities set** (`Affinities.None`). The Enhanced variant helper copies `affinities` and `affinityProfile` from the base form.
- **Status resistance:** one universal value per unit, `StatusResistanceProfile` in `Combat/Status/`. It has additive sources, a 75% cap and multiplicative reductions. Bosses are mental-immune.
- **Save:** `CampaignSaveData`, `PersistenceCommitHandler` and `SaveManager` in `Data/SaveSystem.cs`. There is one Codex per save slot.
- **Combat menu:** `CombatScreen.ShowActions` (around L1797), `ShowSkills`, and `ChooseTarget`. The menu is a scroll view.
- **AI:** the encounter memory in `Combat/AI/` (`EncounterBlackboard`, `PlayerModel`) already records the effectiveness that has been shown.

## Stages
| # | Stage | Gate |
|---|---|---|
| 0 | **One resolver.** Find every live damage path that reads affinity. Add `AffinityResolver.Get(Combatant target, Element e)` and route `Battle.Deal`, `DamageCalculator`, Null Break and the AI through it. **No behaviour change.** | Every existing test passes unchanged |
| 1 | **Drain and Reflect.** Add `Effectiveness.Drain` / `Reflect`, `Affinities.drain[]` / `reflect[]`, and `AffinityType.Drain = 4` / `Reflect = 5`. Resolve them in damage per spec Part E | Unit tests listed below under "Drain / Reflect" |
| 2 | **Per-status resistance.** Add `StatusResistanceProfile.perStatus` (`Dictionary<StatusId, float>`, range −0.5 to +0.75). Effective resistance = `clamp(universal + perStatus[S], 0, 0.75) × Π(1 − reductions)`. Immunity stays a separate flag. Add a tier helper (VULNERABLE / NORMAL / RESISTANT / IMMUNE). Add an `EnemyDef.statusResist` map | Existing status tests pass unchanged when every `perStatus` is 0. New tests cover the clamp, reductions, immunity and tier cut-offs |
| 3 | **Superseded by `Stage3_Coverage_Handoff.md`** (coverage rule changed, Arcane removed, skill retags). Original text: **Dungeon 1 table.** Author the spec Part F table in `Data/Dungeon1Roster.cs`. Add a validator, as a test, for the tier limits. Add a **coverage test**: every weakness must be dealable by ≥ 2 playable classes' early skills, checked against `Database.Skills`; if it fails, adjust the table and report what changed. Update the v1.9 xlsx, or report that it needs updating | Validator and coverage pass. `BalanceSimulation` encounter deaths stay within ±15% of the current baseline |
| 4 | **Codex.** Add `CampaignSaveData.codex`: entry id → element → known affinity, status → known tier, plus concealed trigger counters. Add discovery hooks: a hit resolved, a status attempt resolved, and Elf Keen Senses (one weakness per dungeon). Commit at the end of every battle, win or lose | Save/load round-trip. Discoveries persist across runs in the same slot but not across slots. Base, Enhanced and each boss phase are separate entries. Summons share their definition's entry. The concealed reveal happens on the 2nd trigger |
| 5 | **ANALYSIS UI.** Add ANALYSIS to `ShowActions` after GUARD. Add the ENEMIES / ALLIES tabs, the enemy panel (spec Part B), the ally panel (Part C), reveal feedback (floating word, log line, one-time toast, chime), the "?!" and "AFFINITIES SHIFTED" states, and the **target-picker tags** in `ChooseTarget` | Opening it never spends a turn: `Battle.Active`, the turn queue and MP are unchanged after open/close. It isn't available during QTEs or enemy turns. Every displayed enemy affinity equals `AffinityResolver` filtered by the codex. Allies always show exact values. Enemies never show raw stats |
| 6 | **AI.** Through the existing encounter memory, enemies avoid an element a hero has been *shown* to Drain or Reflect (that's for when gear adds those; it does nothing today) | AI spec v1.1 parity tests still pass |

## Drain and Reflect rules (spec Part E)
**Drain**
- The target takes 0 damage and heals by the NORMAL-equivalent damage, capped at its missing HP. It never revives.
- No Break damage. No status rider.

**Reflect**
- The target takes 0 damage.
- The attacker takes the NORMAL-equivalent damage, and then the **attacker's own** affinity for that element applies.
- No Defensive QTE on the reflected damage.
- The reflected damage is capped at `ReflectCapPct` of the attacker's Max HP.
- No Break or Poise damage either way. No status rider. It never reflects again.

**Both**
- Null Break ignores Drain and Reflect.
- Area attacks resolve per target.
- A kill by reflected damage is a normal defeat.

## Numbers (posture B: starting values, not final)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `ReflectCapPct` | 35% of attacker Max HP per hit | No hero at full HP dies to a single reflected hit (sim) | 25% |
| Drain heal | NORMAL-equivalent, capped at missing HP | A drain fight lasts ≤ 2 extra rounds when the party ignores it | Cap at 50% of NORMAL |
| Concealed threshold | 2 triggers | Players call the reveal "earned, not random" | 1 |
| Status tier cut-offs | −10% / +15% | Players predict status success from the tier ≥ 70% of the time | Move the cut-offs |
| **Learning metric** | — | In the 2nd fight against the same family, the first action exploits a known weakness ≥ 50% of the time | Louder reveal feedback |

## Tests
**Drain / Reflect** (stage 1)
- Drain heals and respects the cap.
- Reflect hits the attacker with the attacker's own affinity applied.
- No QTE on reflected damage.
- Reflected damage never loops.
- No Break damage from either.
- No status rider from either.
- Null Break has no effect on either.
- AoE resolves per target.

**Everything else**
- Every stage gate above.
- ANALYSIS opens, closes and reopens without spending the turn.
- Active status effects are always visible on both sides.
- Drain never revives.
- The Codex survives the On-Defeat Transformation, with a separate Enhanced entry.
- A Zoroa phase change shows "AFFINITIES SHIFTED" and auto-reveals affinities that are unchanged between phases.
- A seeded replay reproduces every discovery.

## Playtest
1. **New player:** they find ANALYSIS, understand `???`, and notice their first reveal without being told.
2. **Mastery:** across 3 runs, the learning metric improves.
3. **Abuse:** ANALYSIS is free, pauses nothing and leaks no hidden numbers.
4. **Readability:** after a DRAINED! or REFLECTED! hit, the player can say what happened and what to change.

## Results (fill in)
| Stage | Date | Sample | Key metrics | Gate |
|---|---|---|---|---|
| 0 (one resolver) | 2026-11-22 | Full EditMode suite (611 tests) | `AffinityResolver.Get` wired across `Battle.Deal`, `DamageCalculator`, Null Break, and AI; 0 behavior change. | PASS (608/608 pass, 3 skipped) |
| 1 (Drain & Reflect) | 2026-11-22 | `AnalysisSystemTests` + `DamageSystemTests` + `BattleTests` + `RacialFeatCombatTests` (53 tests) | Drain heals NORMAL-equivalent capped at missing HP & never revives; Reflect applies attacker affinity, caps at 35% Max HP, no QTE, no loop, no Break/Poise, no rider; Null Break ignores both; AoE resolves per target. Lightbound 75% cap & cross-battle non-stacking verified. | PASS (53/53) |
| 2 (per-status resistance) | 2026-11-22 | `AnalysisSystemTests` + `StatusEffectTests` (20 tests) | `perStatus` [-0.50, +0.75], `clamp(universal + perStatus, 0, 0.75) × Π(1-red)`, separate immunity flag, `-10% / +15%` tier cut-offs, `EnemyDef.statusResist` & `statusImmune`. | PASS (20/20) |
| 3 (Dungeon 1 table) | 2026-11-22 | `AnalysisSystemTests` + `Dungeon1RosterTests` + `EnhancedVariantTransformationTests` + `BalanceSimulation` (153 tests) | All 14 D1 enemies + 2 Zoroa phases authored; tier limits pass; coverage test passes (`Database.Skills` untouched, weaknesses restricted to `Physical`/`Fire`/`Dark` each covered by ≥2 playable classes). v1.9 xlsx reported as needing manual update (`openpyxl` unavailable). | PASS (153/153) |
| 4 (Codex) | 2026-11-22 | `AnalysisSystemTests` + `SaveSystemTests` + `EnhancedVariantTransformationTests` (38 tests) | Save/load JSON round-trip, slot isolation, defeat/rollback persistence, separate Base/Enhanced/Boss-phase entries, summons shared, concealed 2-trigger reveal (`?!` on 1st), Zoroa phase carry-over + `AFFINITIES SHIFTED`, Elf Keen Senses 1/dungeon, seeded replay determinism. | PASS (38/38) |
| 5 (ANALYSIS UI) | 2026-11-22 | `AnalysisSystemTests` + `BattleTests` (33 tests) | Free ANALYSIS command after GUARD & in target picker; ENEMIES/ALLIES tabs + unit cycle + scroll; Codex-filtered enemy panel & target-picker tags (`WEAK`/`RESIST`/`NULL`/`DRAIN`/`REFLECT`/`NORMAL`/`???`); full ally panel; floating words, log line, toast, chime. | PASS (33/33) |
| 6 (AI & full regression) | 2026-11-22 | Full EditMode suite (621 tests) | Enemies avoid elements shown in encounter memory to Drain/Reflect without peeking; AI Spec v1.1 parity tests pass. | PASS (618/618 pass, 3 skipped) |
