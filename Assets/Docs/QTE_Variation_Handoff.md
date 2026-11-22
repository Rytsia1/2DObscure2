# Handoff: QTE Variation (additive variants, Guard key, motifs, remix, status distortions)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design source:** `Assets/Docs/Felna_QTE_Variation_Spec_v1.md`. Read it first; it wins over this handoff.
- **User directive: additive only.** Don't change any established QTE. That covers:
  - the offense sweep bar, Charge, MovingTarget, Combo and Typing;
  - the defense tap, random arrows, feints, stutter, jitter, Brace, Ward and Struggle;
  - the existing keys, windows and prompts.
- **Extend, never alter.** Every existing constructor, enum value, key, prompt and code path keeps its behaviour, and new fields default to "established behaviour". **Every existing QTE test must pass unmodified. That is the gate for every stage.**
- **Never touch the windows.** Every variant uses `QteConfig.JudgeCategory` with the existing profiles and bonus clamps.
- Every number is a **posture-B starting value**. Keep its label, pass test and tuning direction as a code comment.
- Report the test results, the QTE telemetry and anything you couldn't verify to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Where the current system lives
- `Qte/Qte.cs`:
  - Offense classes: `TimingQte`, `ChargeQte`, `MovingTargetQte`, `ComboQte`, `TypingQte`.
  - Defense classes: `DefenseQte` (`cues[]`, `heavy[]`, feints, `stutter[]`, `intervals`), `WardQte`, `StruggleQte`.
  - `QteFactory.Offense` / `QteFactory.Defense`. The factory picks offense kinds in `KindFor`, and the defense path rolls arrows, feint and stutter randomly.
- `Qte/QteConfig.cs`: tier profiles, feint and stutter chances, jitter, `MinHitInterval`, and the judge.
- `Core/Controls.cs`: `QteKey = Key.Space` plus the pad South button.
- `Screens/QtePanel.cs`: rendering and prompts.
- First-time slowdown pattern: `run.Tutorial.FirstTime(...)` / `Prologue.FirstTimeSlow`.

## Changes
1. **`Core/Controls.cs`:**
   - Keep `QteKey` / `QtePress` untouched.
   - Add `GuardKey = Key.LeftShift`, `GuardPress` / `GuardRelease` (pad East), and a remap entry.
   - Add a `SingleKeyMode` setting in `Core/GameSettings.cs`. When it's on, Guard-key variants also accept the QTE key.
2. **`Data/Defs.cs`:**
   - Add `enum QteVariant { None, GuardCue, Slip, Flurry, Roll }`. `None` is the established cue.
   - Add `struct HitCue { QteVariant variant; DodgeDir dir; }`.
   - Add `enum RemixKind { Mirror, Upgrade, Tempo }`.
   - Add `class QteMotif { HitCue[] hits; int feintHit = -1; int stutterHit = -1; bool stutterSlowFirst; RemixKind[] remixes; }`.
   - Add an optional `EnemyAttack.motif`. `null` means today's behaviour.
3. **`Qte/Qte.cs`, `DefenseQte`:**
   - **Keep** the existing arrays, logic and constructor signature.
   - Add an **optional** constructor parameter, `QteVariant[] variants = null`. `null` or `None` means the established behaviour.
   - Implement each new variant:
     - **Guard cue:** a tap that listens to Guard.
     - **Slip:** track the hold from the cue's spawn, judge the release.
     - **Flurry:** two sub-impacts per cue; the grade is the worse of the two.
     - **Roll:** arrow and Guard within `ChordTolerance`; judge on the later input.
     - **Wrong input** spends the hit, the same way `wrong[]` does today.
   - Add `PressGuard()` / `ReleaseGuard()` next to `Press` / `PressDirection`.
4. **`Qte/Qte.cs`, `QteFactory.Defense`:**
   - Moves **without** a `motif` keep today's code path, byte for byte in behaviour.
   - With a motif, fill the **same** `cues[]` / `heavy[]` / `feints[]` / `stutter[]` / `intervals` arrays from the authored data instead of rolling them. Pass `variants[]`.
   - Apply the remix: roll with `rng` at the tier chance; Enhanced forms always use their remix. The telegraph shows "REMIX".
   - Add a validator (static, plus a test) for the spec §D.3 budget and the §D.2 rhythm floors. Syncopated is allowed only on boss signatures.
5. **`Qte/Qte.cs`, `QteFactory.Offense`:**
   - `KindFor` is unchanged.
   - Add an optional variant layer for Basic Attack and single-hit physical skills: a Strike Signature lookup by the main-hand `WeaponType` (spec §C.3).
   - Implement the variants as **optional parameters or subclasses** of the existing classes:
     - `TimingQte(curve: Linear | EaseIn | EaseOut, passes: 1 | 2)`. The defaults, Linear and 1, are today's behaviour. Judgment stays by time.
     - `ChargeQte` Thrust: a zone-width parameter, same windows.
     - `ComboQte` Echo.
     - `TypingQte` Cadence.
   - Sword, Axe, Mace, None, Bow, Crossbow and casters get exactly today's QTE.
6. **`Qte/QteConfig.cs`:** add constants with posture-B comments: `FlurryGap`, `ChordTolerance`, `ClearZone`, `ShakePx`, the remix chances, and example Ritardando and Syncopated arrays.
7. **`Screens/QtePanel.cs`:**
   - Leave the existing visuals and prompts unchanged.
   - **Add:**
     - glyphs for the new variants (shape, colour and label);
     - the REMIX mark in the telegraph;
     - the status distortion layer (spec Part E: priority order, `ClearZone`, and the "Reduce QTE distortions" toggle);
     - prompts that name the key, for example "GUARD [SHIFT]".
8. **`Data/Dungeon1Roster.cs`:**
   - Author motifs for Dungeon 1 physical moves (examples below; the rest by the spec rules). A motif may only *fix* values that are random today (arrow, feint hit, stutter hit) and *add* new variants.
   - **Leave `PrologueRoster.cs` untouched.** The Prologue keeps teaching the established QTEs exactly as now.
   - Each new variant gets a one-time **first-encounter coach prompt** in Dungeon 1, using the existing `Tutorial.FirstTime(...)` slowdown pattern.

## Example motifs (starting values, not final)
| Move | Tier | Motif |
|---|---|---|
| Rusty Cleave (Squire) | Normal | Dodge →, with the arrow fixed instead of random |
| Grave Arrow (Archer) | Normal | Dodge ↑ |
| Rending Bite (Hound) | Normal | Dodge ← |
| Royal Halberd (Royal Guard) | Normal | Slip (a sweeping halberd) |
| Rusty Cleave, Repeated Assault (Enhanced Squire) | Enhanced | Tap, Flurry (remix: Upgrade) |
| Rending Bite, alternative (Enhanced Hound) | Enhanced | Tap, Guard cue, Tap at Accelerando 350/310 ms (remix: Tempo) |
| Accelerating Rend (Soul Reaper) | Miniboss | Dodge →, Dodge ←, Guard cue, Brace (Heavy); feint on hit 2; remix: Mirror |
| Graveward Charge (Ossuary Knight) | Miniboss | Brace (Heavy), unchanged, with a Ritardando lead-in |
| Triple Feint (Crownless Duelist) | Elite | Tap, Flurry, Roll →; feint on hit 1 |
| Final Reaping (Zoroa) | Boss | Dodge ↑, Slip, Roll ←, Brace (Heavy) at the existing Accelerando 350/298/280 ms; remixes: Mirror, Upgrade |

Moves not listed keep today's random cues until someone authors them.

## Numbers (posture B: starting values, not final)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `FlurryGap` | 150 ms | Flurry Good+ rate within 15 pp of Tap cues at the same tier (playtest) | 180 ms |
| `ChordTolerance` | 80 ms | Roll Good+ rate ≥ 60% | 110 ms |
| `ClearZone` | 150 ms before impact | Distorted cues' Good+ rate within 5 pp of undistorted | 220 ms |
| `ShakePx` | 4 px | Same as `ClearZone` | 2 px |
| Remix chance | Elite 25%, Boss 35% | ≥ 70% of players say "noticeable but fair" | ±10 pp |
| **Mastery metric** | — | Perfect rate on the same motif rises **≥ 10 pp from the 1st to the 3rd exposure** | Simplify motifs that don't improve |
| Normal-tier budget | 2 verbs / 2 hits | Normal-fight defense Good+ drops no more than 5 pp versus the stage-0 baseline | Cut to 1 verb |

## Stages (each gate includes "all existing QTE tests pass unmodified")
| # | Stage | Gate |
|---|---|---|
| 0 | QTE telemetry: Good+ and Perfect rates by motif, variant and tier, plus exposure count. Record today's baseline | Baseline recorded, with sample size and date |
| 1 | Guard key, Single-key mode, remap | The QTE key behaves exactly as before |
| 2 | Optional `variants[]` on `DefenseQte`; Guard cue, Slip, Flurry, Roll; the validator | **Parity test:** with no variants passed, outcomes are identical to today for the same seed. New tests per variant |
| 3 | Authored Dungeon 1 motifs, plus first-encounter coach prompts | Unauthored moves are unchanged; every motif passes the validator |
| 4 | Offense variants (curves, Double-pass, Thrust, Echo, Cadence) and weapon signatures | Unchanged families get exactly today's QTE; windows identical |
| 5 | Remix, plus the REMIX telegraph | The remix shows in the telegraph 100% of the time; Normal tier never remixes |
| 6 | Status distortions, plus the toggle | Judgment is identical with and without a distortion; `ClearZone` respected; only one distortion at a time |

## Tests
- For each variant: Perfect, Good, Partial and Poor at the window edges.
- Wrong-variant input spends the hit.
- Slip released too early, or never released.
- Flurry with only one press.
- Roll with only the arrow, only Guard, or outside the tolerance.
- Single-key mode works for every variant.
- Motif determinism: the same move gives the same QTE when there's no remix.
- The remix is always visible.
- Validator: complexity budget, rhythm floors, and Syncopated only on boss signatures.
- Distortions never change judgment.
- Replay: a seeded battle reproduces its QTE sequences exactly.

## Playtest script
1. **New player:** the Prologue is unchanged. When a player meets each new variant in Dungeon 1, the coach prompt alone should teach it: first-encounter Good+ ≥ 60%, third encounter ≥ 75%.
2. **Mastery:** fight the same Elite three times, then check the mastery metric.
3. **Readability:** an observer can name the verb for each glyph (≥ 80%).
4. **Stress:** spam both keys; Slip with key chatter; Roll with sloppy timing.
5. **Accessibility:** Single-key mode and "Reduce QTE distortions" both work end to end.

## Tuning priority
Adjust in this order: the Normal-tier budget, then `ChordTolerance`, then `FlurryGap`, then the remix chance, then `ClearZone` / `ShakePx`. **Never touch the windows.**

## Results (fill in)
| Stage | Date | Sample | Key metrics | Gate |
|---|---|---|---|---|
| 0 (baseline) | 2026-11-22 | 200 cues/tier (600 total), simulated ±65 ms reaction jitter, Standard assist | Normal: Good+ 95.5%, Perfect 53.5%. Elite: Good+ 90.0%, Perfect 45.0%. Boss: Good+ 83.0%, Perfect 41.5%. Overall Tap/Dodge: Good+ 89.5%, Perfect 46.7%. | PASS (22/22 `QteTests` pass) |
| 1 (controls & settings) | 2026-11-22 | EditMode test verification | Guard key (`LeftShift`), Single-key mode, and Reduce QTE distortions wired through `Controls`, `GameSettings`, `SettingsScreen`. | PASS |
| 2 (defense variants & validator) | 2026-11-22 | 200 cues/variant/tier (3000 cues), ±65 ms reaction jitter, Standard assist | `GuardCue` Good+ 100.0% / Perf 62.0%; `Slip` Good+ 100.0% / Perf 61.7%; `Flurry` Good+ 100.0% / Perf 56.0%; `Roll` Good+ 100.0% / Perf 64.7%. | PASS (30/30 `QteTests`, 7/7 Stage 2 `QteVariationTests`) |
| 3 (Dungeon 1 motifs) | 2026-11-22 | All D1 authored motifs + `PrologueRoster` check | All D1 motifs pass `QteMotifValidator`; `PrologueRoster` has 0 motifs; deterministic cues across seeds. | PASS (Stage 3 `QteVariationTests`) |
| 4 (weapon strike signatures) | 2026-11-22 | Sword, Dagger, DualDaggers, Spear, Echo Combo | Shape-only curves/passes/thrust/echo; `EffectivePerfect`/`EffectiveGood` unchanged. | PASS (Stage 4 `QteVariationTests`) |
| 5 (remixes & enhanced) | 2026-11-22 | `hollow_squire` vs `hollow_squire_enhanced` + D1 elite/boss | Normal never remixes; Enhanced always remixes (`Upgrade` Tap->GuardCue). | PASS (Stage 5 `QteVariationTests`) |
| 6 (status distortions) | 2026-11-22 | Fear/Slow/Despair/Poison/Bleed + `ClearZone` + toggle | Judgment & `QteOutcomeCategory` identical with/without distortion; `ClearZone` (≤150 ms) undistorted; `reduceQteDistortions` disables all. | PASS (30/30 `QteTests`, 12/12 `QteVariationTests`) |
