# Handoff: Felna Enemy AI (staged implementation, stages 0–10)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design sources (read both first):** `Assets/Docs/Felna_Enemy_AI_System_Spec_v1_1.md` and `Assets/Docs/Felna_Enemy_AI_Architecture_v1_1.md`. Passages marked **(v1.1)** are binding amendments. Where the docs and this handoff disagree, the docs win; report the disagreement.
- **Prerequisites already shipped:** `D1_Difficulty_Pass_Handoff.md`, `Charge_Pacing_Handoff.md` and `Zoroa_Double_Turn_And_Summon_Handoff.md`. Don't regress any of them.
- Every number below is a **posture-B starting value**: it is not final (Architecture §18). Keep that label, its pass test, and its tuning direction as a code comment next to each constant.
- **One stage at a time.** Don't start a stage until the previous stage's gate passes. Report the gate results (and the stage-0 baseline) to the user after each stage.
- **Ship point:** stages 0–3 already deliver most of the felt intelligence. After stage 3, stop and ask the user to playtest before continuing.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Context
The user wrote two design docs:
- the Enemy AI System Spec: hybrid rules plus utility, a blackboard, high adaptation, roles, and a telegraph lock;
- the Advanced Architecture: six layers, a strategic planner, 2-turn prediction, player modeling, synergy, a resource planner, behavior trees, a counterplay check, intelligence tiers, and a replay simulator.

The user's clarifications:
- **Memory is shared only within the current encounter.**
- **Intents are not published in initiative order.**
- **Shipped code wins over the docs where they conflict.** The docs are now amended to v1.1.

What the code has today:
- **AI and gates:** `PriorityBrain` (`Combat/EnemyBrains.cs`) is a strict priority list with hard gates: cooldown, MP, `aiMpBelow`, heal thresholds, one Heavy per round, `JustReleasedCharge` gap, Break gate, and summon cap.
- **Targets:** chosen by `TargetRule`. `Random` uses `b.Rng`; Elites use `LowestHp` (Elite Design Lock); the Archer uses `LowestDef`.
- **Planning:** `EnemyAI.Plan` runs at `Battle.StartRound` (~L157) in `Enemies` list order. Zoroa's turn B is re-planned after turn A (~L613).
- **Zoroa:** `turnsPerRound = 2`, Harvest Call (`IsSummon`, `Battle.Summon`), and Soulbind is instant.
- **HUD:** shows intent only for `NeedsTelegraph` moves (`EnemyAI.IntentText`), per GDD §8.9.
- **Unused brains:** the legacy `BasicBrain`, `TacticalBrain` and `BossBrain` serve only legacy fixture enemies. Leave them alone.

---

## Architecture (new folder `Combat/AI/`)
| Type | Role | Reuses |
|---|---|---|
| `EncounterBlackboard` | **One per `Battle`**, created in the `Battle` ctor and discarded with it. Holds the party/enemy snapshot, published intents, lethal reservations, objectives, opportunities, **the single encounter memory, and all player profiles** (shared by every enemy, including summons) | `Battle.Party/Enemies`, `Combatant.Intent/Charge` |
| `PlayerModel` | Per-hero confidence for the 7 categories (Architecture §4.1 v1.1), plus observed per-turn output, most-frequent target, and elements with their *seen* effectiveness | event hooks |
| `StrategicPlanner` | Scores the 8 objectives each round, with hysteresis. An objective adds a bonus (≤ cap) to **action tags**: SingleDamage, AoE, Heal, MpRestore, Status, BreakPower, Protect | — |
| `Predictor` | 2-scenario projection over this enemy's next 2 turn opportunities (Architecture §6.6), plus cooldown/MP forecasts. Analytic, no search | `Rules` damage formulas (mean, no crit) |
| `UtilityScorer` | `score = aiPriority*10 + clamp(Immediate + Future + Role + Synergy + Objective + Adaptation − Resource − Repetition − Risk, −Clamp, +Clamp)`. Scored **only** over candidates that passed every mandatory gate | — |
| `AiProfile` | Data: `aiRole`, `aiTier`, role weights, decay, threshold, optional `conditionList` (minibosses + boss). Add `EnemyDef.aiRole` / `aiTier` and set them for all 15 roster enemies (Spec §6.2) | `Data/Defs.cs`, `Data/Dungeon1Roster.cs` |
| `CounterplayValidator` | The final filter, plus debug asserts: telegraph present when required; one Heavy per round (across Zoroa's two turns); no Stun + Heavy on the same target in a round; locked targets respected; published intents never re-scored | `NeedsTelegraph`, the `heavyTaken` logic |
| `PriorityBrain` (kept) | Becomes the **mandatory layer**. Pull today's `continue` gates into `IsLegal(m)` and the forced picks (Heals threshold, Arcane Charge threshold, pending Charge release, summon when allowed) into `ForcedChoice()` | `EnemyBrains.cs` L55–88 |
| `AiTrace` + replay | Per decision: candidates, every factor value, chosen move/target, and a reason string. A battle record (seed, party snapshot, ordered player actions) replays deterministically in an EditMode helper | `Battle.Rng`, `Tests/Editor/BalanceSimulation.cs` |

**Planning order** (Spec §4.5): Support, then Controller, then other roles, with spawn/formation slot breaking ties. It is **not initiative**. Replace the `foreach (var e in Enemies)` at `Battle.cs` ~L157 with this order. Zoroa's turn B re-plan (~L613) reads the blackboard as it is at that moment.

**Tie-break** (Architecture §8.5): explicit priority, then utility, then lower MP cost, then lower repetition, then data/slot order. **The roster AI path never calls `Rng`.** Replace `EnemyAI.Pick` / `RandomTarget` on the roster path; keep them for the legacy fixture brains only.

**Locked targeting** (Spec §3.5): `LowestHp`, `LowestDef` and the Confessor heal target stay mandatory. Utility may only skip a target that is **reserved as lethal**, then pick the next one by the **same rule**, and may break exact ties.

**Telegraph lock** (Spec §7.3 v1.1): `PriorityBrain.Decide` executes the published intent while it is legal and **never re-scores it**. If it's illegal: telegraphed moves use the existing `NextInPartyOrder` redirect; otherwise fall back to a legal action; otherwise skip (no replacement turn).

## Event hooks (in `Combat/Battle.cs`; observe only, no rule changes)
- after each `Perform`: actor, kind, skill/element, targets, damage, kills, and the QTE grade of resolved hits;
- `BreakHit` when it breaks a target;
- a charge cancelled by Broken (`Combatant` ~L631);
- Guard (~L438);
- heals and items used (observed as actions, **never** inventory counts);
- elemental effectiveness of each hit (this is the only way the AI learns affinities).

Apply decay once per round at `StartRound`, before planning.

## Adaptation responses (all inside the clamp; each logs a one-time "read", Spec §5.7)
| Recognized (confidence ≥ `RecognizeAt`) | Response | Example read (first trigger only) |
|---|---|---|
| Focus Fire by hero X on enemy E | E's allies: +Target on X | "The crypt turns on X." |
| Defensive X | −Target on X **while X is guarding**; +Status moves on X | "The Hollow sense X's guard." |
| Support Dependent X | +Target on X (Disruptor/Executioner count double) | "They mark the healer, X." |
| Elemental Specialist X exploiting an enemy weakness | Support: +Heal on the exposed ally. Attackers: +Target on X | "They shield what burns." |
| Break Specialist / repeated charge interrupts | Charge start gets −Adaptation unless own Break gauge ≥ `ChargeShieldGate`: **charges start when the shield is full** | "It guards its strength before it gathers." |
| Burst X | Raise the pressure weight of X in prediction, so supports protect low allies earlier | "They brace against X." |
| Enemy's own ineffective move×target (Resist seen, Miss, 0 net) | Repetition penalty | none (silent) |

## Coordination
- **Lethal reservation:** if the expected damage already committed by earlier planners is at least the target's HP, the target is reserved, and later planners get −Target on it.
- **Focus cap:** at most `FocusCapNormal` Normal-tier enemies plan single-target damage on the same hero per round. Elites and bosses are exempt.
- **Stun + Heavy:** check against published intents (the spec rule).
- **Synergy** (Architecture §9.4): derive the chain table from `StatusEffectDef` data and the Break rules, using only real follow-up mechanics. A setup adds utility for allies' follow-ups. It never changes damage, hits or status chances.

## Capability by tier (Architecture §12 v1.1)
| Capability | Normal | Elite | Miniboss (Soul Reaper, Royal Ossuary Knight) | Boss (Zoroa) |
|---|---|---|---|---|
| Utility + role, read memory/blackboard, publish reservations, synergy follow-up | ✓ | ✓ | ✓ | ✓ |
| Synergy setup, strategic objectives, prediction H = 2 | — | ✓ | ✓ | ✓ |
| `conditionList` workflow | — | — | ✓ | ✓ |
| Decay / `RecognizeAt` | 0.7 / 0.5 | 0.7 / 0.5 | 0.7 / 0.5 | 0.8 / 0.45 |

## Numbers (posture B: starting values, not final)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `UtilityClamp` | ±15 (1.5 priority levels) | Wasted Action Rate ≤ 5%; each role's move mix stays recognisable (200-seed sim) | Erratic: ±10. Rigid: ±20 |
| Decay per round | Normal/Elite 0.7, Boss 0.8 | Influence fades about 3 rounds after the behavior stops (boss about 5) | Adjust |
| Confidence `k` / `RecognizeAt` | 2 / 0.5 (boss 0.45) | Unit tests: 1 action never recognizes; 3 recent consistent actions do | Adjust `k` |
| Contradiction weight | 1.0 per opposite observation | 2 rounds of attacking after guarding drops Defensive below 0.5 | 1.5 |
| Repetition penalty | −3 per consecutive ineffective use | Repeated Action Rate is below the stage-0 baseline | −5 |
| Synergy bonus | +4 per real follow-up mechanic | Follow-ups observed; rule-output diff test is clean | Adjust |
| Role weights | Support heal +8, Executioner low-HP +6, Disruptor threat +6, Controller AoE +2 per extra target, Break bruiser BreakPower +6 | Role identity ≥ 60% of turns (sim) | Retune per role |
| `FocusCapNormal` | 2 | Normal-encounter party deaths ≤ baseline + 15% | 1 |
| `ChargeShieldGate` | 70% of own Break gauge | Telegraph Response Rate (interrupted / started) 20–50% | ±10 |
| Prediction mix | passive 0.5 + pressure 0.5 × confidence | Supports heal *before* a projected lethal round in ≥ 50% of scripted cases | Raise the pressure share |
| Objective cap / hysteresis | +6 / challenger must win by 3 | Objectives change in ≥ 30% of fights and never in > 50% of rounds | Increase the margin |
| Resource reserve | Keep MP for a signature/heal ready within H = 2 | No "can't afford signature" turns in the Zoroa sim | Reserve earlier |
| Decision budget | ≤ 2 ms per enemy plan, ≤ 10 ms per round (editor) | Profiler: Zoroa + 2 summons + 2 others | Disable prediction for Normal tier first |

## Stages and gates
| # | Stage | Exit gate |
|---|---|---|
| 0 | **Baseline + tooling:** `AiTrace`, replay helper, and `BalanceSimulation` reporting Spec §14.6 and Architecture §16.5 metrics per tier. Scripts: all-Good and all-Miss QTE, 200 seeds | The baseline is written into the **Results** section below, with sample size and date (posture C) |
| 1 | **Hybrid foundation:** split `PriorityBrain` into gates and `UtilityScorer`, with every modifier at 0 | Every existing test passes and the sim shows **identical decisions** to stage 0 |
| 1b | Turn on Immediate/Resource utility; deterministic targeting for `Random`-rule enemies; `aiRole`/`aiTier` on all 15 enemies | Role identity ≥ 60%; no `Rng` on the roster path (test) |
| 2 | **Blackboard:** planning order, lethal reservation, focus cap, Stun + Heavy check | Coordination tests; Speed-buff invariance test; deaths ≤ baseline + 15% |
| 3 | **Player modeling and shared encounter memory:** hooks, confidence, responses, reads | 1 action never recognizes; memory empty at the start of each battle; a summon reacts to a pre-spawn event; memory survives Zoroa's phase change; each read logs once. **Then stop for a user playtest** |
| 4 | **Prediction H = 2** (Elite and up) | Overkill Rate below baseline; with no observations, the choice equals the passive-party choice (no assumed player actions) |
| 5 | **Strategic planner** | Objective churn within bounds |
| 6 | **Synergy chains** (derived) | Follow-ups only on real mechanics; rule-output diff test is clean |
| 7 | **`conditionList` workflows** (Knight, Reaper, Zoroa) | Knight charge timing respects `ChargeShieldGate`; Reaper prefers execution targets |
| 8 | **Resource planner** | No unaffordable-signature turns; Arcane Charge at most once per phase (existing Zoroa test) |
| 9 | **`CounterplayValidator`** as the final filter plus asserts | Zero assert hits over 1000 simulated fights |
| 10 | **Boss integration:** Zoroa both turns, phases, summons, boss profile | Zoroa pass tests in `Zoroa_Double_Turn_And_Summon_Handoff.md` still hold; Telegraph Response Rate 20–50% |

## Tests
- **Unit tests:** every bullet in Spec §14 and Architecture §16, plus:
  - planning order and decisions are identical with and without a Speed buff;
  - memory is shared, survives phases, and resets between battles;
  - locked `LowestHp` keeps its rule except when a target is reserved as lethal;
  - confidence math matches Architecture §4.1;
  - no `Rng` call on the roster AI path;
  - a published intent is never re-scored.
- **Replay:** a recorded battle reproduces an identical `AiTrace`.
- **Playtest script:**
  1. **Readability:** an observer explains every read line and every death from the log.
  2. **Abuse:** spam one strategy for a whole fight. The enemies respond, but the strategy still works (no immunity).
  3. **New player:** play with no knowledge of adaptation. Deaths must still feel explainable.
  4. **Solved-fight check:** the same opening must not always win Zoroa.

## Tuning priority
Adjust in this order: the clamp, then `FocusCapNormal`, then `RecognizeAt`/`k`, then decay, then role weights, then the objective cap, then the prediction mix. **Don't touch damage or stats until the AI metrics pass.**

## Risks
- **Difficulty stacking:** the smarter AI lands on top of the difficulty pass, Zoroa's double turn and the summons. Gate every stage on deaths and completion against the stage-0 baseline. If they drop too far, reduce the clamp before touching stats.
- **Full determinism:** this is good for readability and replay, but watch for "solved" fights (playtest 4).
- **Scope:** 11 stages is a lot. Honour the stage-3 ship point.

## Results (fill in as stages complete)
| Stage | Date | Sample | Key metrics | Gate |
|---|---|---|---|---|
| 0 (baseline) | 2026-11-22 | 200 seeds × 4 tiers × 2 QTE (All Good / All Poor), D1WeaponArmor | Normal (`d1_e5` L4): Win 100/100%, Deaths 0.00/0.00, Rnds 4.1/5.1, Waste 0.0/0.0%, TopTgt 46.5/44.0%, Repeat 11.4/15.4%, Overkill 0.0/0.0%, PlanMs 0.008/0.010. Elite (`d1_e8` L7): Win 100/100%, Deaths 0.00/0.00, Rnds 5.0/7.0, Waste 0.0/0.0%, TopTgt 66.7/83.0%, Repeat 25.0/41.9%, MP 12.3/12.3%. Miniboss (`d1_e7` L9): Win 100/100%, Deaths 0.00/0.04, Rnds 10.1/14.2, Repeat 23.1/25.7%, ChgInt 0.0/10.2%, TelCnt 50.0/53.4%, Overkill 0.0/7.9%. Boss (`d1_boss` L11): Win 0/0%, Deaths 2.77/4.00, Rnds 39.5/21.8, Waste 10.0/8.4%, TopTgt 34.7/38.6%, Repeat 19.1/18.4%, ChgInt 6.9/0.0%, TelCnt 26.1/18.7%, MP 84.9/76.8%, Overkill 29.9/23.5%, Counter 99.2/100.0%, RndMaxMs 0.991. | PASS (replay trace identical, 133/133 roster tests pass) |
| 1 | 2026-11-22 | 120 fights (15 seeds × 4 tiers × 2 QTE) + 133 roster tests | `PriorityBrain` split into `IsLegal`, `ForcedChoice`, `UtilityScorer` (all modifiers = 0). 0/120 decision diffs vs Stage 0 (identical moves, targets, charges, rounds, deaths, outcomes). | PASS |
| 1b | 2026-11-22 | 200 seeds × 4 tiers × 2 QTE + 137 unit tests | `AiProfile` + `aiRole`/`aiTier` on all 15 enemies; Immediate/Role/Resource utility active; zero `Rng` calls on roster path. Role%: Normal 100%, Elite 75.0% (Good), Miniboss 100%, Boss 82.4/83.2%. Boss Waste% 10.0→8.6%, Counter% 100%. | PASS |
| 2 | 2026-11-22 | 200 seeds × 4 tiers × 2 QTE + 142 unit tests | `EncounterBlackboard` live: fixed planning order (Support→Controller→others, Speed-invariant), lethal reservations (incl. locked LowestHp), `FocusCapNormal = 2`, Stun+Heavy guard, telegraph lock (never re-scored). Normal TopTgt% 100→68.0% (Good)/76.7% (Poor), Normal deaths 0.00/0.05 (≤ baseline+15%), Miniboss Poor Overkill% 72.0→6.4%, Role% ≥ 71.5% across all tiers. | PASS |
| 3 | 2026-11-22 | 200 seeds × 4 tiers × 2 QTE + full EditMode suite (all passing) | `PlayerModel` + shared `EncounterTacticalMemory` + 7 adaptation responses + repetition penalty + one-time log reads + observe-only hooks. Boss Adapt% 15.6% (Good) / 8.9% (Poor), Repeat% 24.4→22.0%, TelCnt% 19.6→24.2%, PlanMs avg 0.027–0.088 ms. All 15 `EnemyAiTests` pass (1-action never recognizes, 3-action recognizes, contradiction/decay, empty at battle start, summon pre-spawn reaction, survives Zoroa phase change, read logs once, observable info only). | PASS — **Ship point reached; paused for user playtest before Stages 4–10** |
