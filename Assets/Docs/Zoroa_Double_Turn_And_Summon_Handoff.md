# Handoff: Zoroa rework (2 full turns per round, plus the Harvest Call summon)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Order:** implement this only **after** `Assets/Docs/D1_Difficulty_Pass_Handoff.md` and `Assets/Docs/Charge_Pacing_Handoff.md` are in. This doc assumes their constants (`BossAtkMult`, `JustReleasedCharge`) exist.
- Every number below is a **posture-B starting value** (untested). Keep that label and its pass/fail test as a code comment next to each constant.
- Once you're done, report the test results and the playtest metrics to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

---

## Part 1: Zoroa takes 2 full turns per round

### Context
The user wants Zoroa to act twice per round: two **full turns**, so either turn can use any move. The cooldown speed should be adjusted so signature moves don't come twice as often.

Current engine facts:
- `Battle.StartRound` (`Combat/Battle.cs` ~L139) builds the queue from `TurnOrder.Build` (`Combat/Combatant.cs` ~L687), sorted by Speed and side/slot.
- `NextTurn` (~L104) allows one turn per combatant per round via `HashSet<Combatant> acted`, which `acted.Add(c)` enforces.
- `StartTurn` (~L148) ticks cooldowns, statuses/DoTs, the Martial Artist combo and Break/Poise recovery.
- Stun or Sleep skips the turn and decrements `stunTurns`.
- `EnemyAI.Plan` (`Combat/EnemyAI.cs` ~L41) commits one `Intent` per enemy at round start for the HUD.

### Rules (the design)
1. **Two turns in the order.**
   - Turn A at Zoroa's Speed. Turn B at Speed × `SecondTurnSpeedFactor`, so some heroes act between A and B.
   - The turn-order bar (`NextRoundOrder`) shows Zoroa twice, so the player can see it.
2. **Round-scoped upkeep, ticked once per round on turn A only:** cooldowns, status durations and DoT damage, Break gauge recovery, and the phase/"acted" bookkeeping. This is the "cooldown speed adjustment": every cooldown keeps its meaning in **rounds**, so Final Reaping (cd 3) still comes back after 3 rounds, not 1.5.
3. **Stun, Sleep and Broken cost the whole round.** If turn A is skipped, turn B is skipped too, and `stunTurns` decrements once per round. Breaking Zoroa removes 2 actions, which is a strong reward for Break play.
4. **Charges.**
   - A charge releases on Zoroa's **next turn, whichever it is**: A charges and B releases in the same round, or B charges and next round's A releases. This needs no change to the charge logic.
   - The heroes placed between A and B (see `SecondTurnSpeedFactor`) are the party's window to Break-interrupt an A-to-B charge. That window is shorter than a full round, so the charge intent must be on screen the moment the charge starts.
   - The `JustReleasedCharge` gap from `Charge_Pacing_Handoff.md` applies per turn as written: after a release, Zoroa's next turn can't start a charge.
5. **One Heavy per round across both of Zoroa's turns.** Extend the `heavyTaken` check in `PriorityBrain.Select` (`Combat/EnemyBrains.cs` ~L59) to include Zoroa's own other turn this round.
6. **Intent HUD.** Plan turn A's intent at round start (as now). Plan turn B's intent at the end of turn A and show it right away. Both intents are visible before the party acts in between.

### Numbers (posture B: starting values, untested)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `EnemyDef.turnsPerRound` | Zoroa P1 = 2, P2 = 2; everyone else 1 | — | If P1 feels unfair, set P1 = 1 and keep the double turn as the Phase 2 escalation |
| `SecondTurnSpeedFactor` | 0.5 | At least 2 of 4 heroes act between Zoroa's A and B turns in a typical party | Too few: 0.4. Turns feel disconnected: 0.6 |
| Zoroa damage multiplier (per hit, both turns) | **0.80**, replacing the ×1.20 `BossAtkMult` for Zoroa only (0.85 for the double turn, minus 0.05 to make room for summons from Part 2) | Fight lasts 10–14 rounds. With Good QTEs, the party ends with 1+ hero above 30% HP. With sloppy QTEs, at least 2 heroes drop | Too lethal: 0.70. Still easy: 0.90 |
| Zoroa HP multiplier | ×1.30 (unchanged) | Same fight-length test | Adjust only after the damage multiplier is settled |
| Zoroa MP | unchanged (600 / 850) | Arcane Charge happens at most once per phase | Raise MP by 25% if it charges MP more often. Every Arcane Charge turn is a free turn for the party |

Why about 0.85 for the double turn alone: in the difficulty-pass version, one turn at ×1.20 ATK averages about 53 damage per round in P1 (cycle: Soulbind, Soul Lance 42, charge, Final Reaping 135). Two turns at ×1.0 average about 88, which is ×1.66. At 0.85 that's about 75, or ×1.4. This is an estimate, so verify it with the simulation below.

### Changes
1. `Data/Defs.cs` `EnemyDef`: add `public int turnsPerRound = 1;`. Set it to 2 for `zoroa_p1` and `zoroa_p2` in `Data/Dungeon1Roster.cs` (static ctor, next to `nextPhase`).
2. `Combat/Combatant.cs`:
   - Add `int TurnsPerRound => enemy?.turnsPerRound ?? 1`, `int turnsTakenThisRound`, and `float TurnSpeed(int turnIndex)` (index 1 multiplies by `SecondTurnSpeedFactor`).
   - Change `TurnOrder.Build` to emit one entry per turn, sorted by that entry's speed, with the same tiebreaks.
   - On a phase change, carry `turnsTakenThisRound` over so P2 doesn't get extra turns in the round it starts.
3. `Combat/Battle.cs`:
   - Replace `acted.Add(c)` with a per-round turn count (`turnsTakenThisRound < TurnsPerRound`).
   - Keep `HasActed(c)` meaning "has taken at least one turn this round" (the Charlatan perk at ~L754 and the defeated-this-round rule at ~L998 both depend on it).
   - In `StartTurn`, run the round-scoped upkeep only when `turnsTakenThisRound == 0`.
   - On a skipped turn A (stun/sleep), mark both turns used.
4. `Combat/EnemyAI.cs` / `Combat/EnemyBrains.cs`:
   - Charge release stays as it is: `PriorityBrain.Decide` releases `self.Charge` on Zoroa's next turn.
   - Add the per-round Heavy rule.
   - Re-plan turn B's intent after turn A.
5. `Screens/CombatScreen.cs`: make sure the turn-order strip and the intent line handle a combatant appearing twice, for example by labelling "ZOR ①" / "ZOR ②".

### Tests
- **TurnOrder:** Zoroa appears twice, with the B entry placed by speed × 0.5. Ordinary enemies appear once.
- **Cooldowns:** Final Reaping is used at most once every 3 rounds over a 12-round simulated fight.
- **Stun:** a 1-turn stun on Zoroa removes both of its turns that round.
- **Charge:** a charge started on turn A releases on turn B of the same round, and one started on B releases on next round's A. The turn right after a release never starts a charge. Breaking Zoroa between A and B cancels the charge.
- **Heavy:** never two Heavy moves from Zoroa in one round.
- **Balance:** add a `BalanceSimulation` case that runs Zoroa against the level-11 reference party with all-Good and all-Miss QTEs over 50 seeds. Report rounds, party deaths, and damage per round compared with the single-turn baseline.
- Re-run `Dungeon1RosterTests` (Zoroa phase/intent tests ~L740–870) and update any that assumed one turn per round.

### Playtest script
1. **Skill test:** Zoroa once with Good QTEs, once sloppy. Compare against the pass test in the Numbers table.
2. **Readability:** an observer can tell from the turn bar and intent line that Zoroa acts twice and what each turn will do.
3. **Break reward:** break Zoroa. It should lose both turns, and the player should feel the payoff.
4. **Break window:** the party can Break-interrupt an A-to-B Final Reaping charge at least sometimes. If that's never possible, lower `SecondTurnSpeedFactor` so more heroes act in between.
5. **Abuse test:** check that stun-locking doesn't trivialise the fight. Stun control immunity (`DecrementControlImmunities`) should still apply once per round.

---

## Part 2: Zoroa summon skill ("Harvest Call")

### Context
Requested by the user: Zoroa can summon up to 2 enemies, in both phases. The skill has cooldown 5. Each summon has an 80% chance to be a common enemy and a 20% chance to be an Elite.

There is no summon system today. Battle enemies are built once in the `Battle` ctor (`Combat/Battle.cs` ~L41), and victory is `Enemies.All(e => !e.Alive)` (~L87).

### Rules
1. **One cast fills the field back to 2 living summons.** Each empty slot rolls on its own: 80% a random D1 **Normal** roster enemy, 20% a random enemy from the **v1.9 random Elite pool** (Reliquary Confessor, Graveglass Arcanist, Crownless Duelist, Tombstone Colossus, Mourning Bell Wraith; never stage bosses).
   - Zoroa only uses it when fewer than 2 summons are alive.
   - Summons are tracked by an `IsSummon` flag, so the cap counts only them.
2. **Available in both P1 and P2.** Cooldown 5 is counted in **rounds** (round-scoped upkeep from Part 1). The cooldown carries over the phase change, so P2 can't summon right after P1 did.
3. **Instant, not charged:** it's a self/status-style move with no damage and no QTE. It is announced in the log and the intent line ("HARVEST CALL: SUMMONS").
4. **Summons join at the next round** (`TurnOrder.Build` picks them up at `StartRound`). They never act in the round they appear.
5. **No EXP, no gold, no relic triggers on kill:** they're excluded from reward and "enemy defeated" relic hooks (R013/R022 at `Battle.cs` ~L996).
6. **They stay through the Phase 2 transition.**
7. **Zoroa's final bar breaking ends the battle.** Kill every living summon at that moment (no rewards), so `Outcome.Victory` triggers. Losing still means the whole party is down, as today.

### Numbers (posture B: starting values, untested)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| Harvest Call cooldown | 5 rounds (user-set) | — | — |
| Initial cooldown | 2 rounds | The party gets 2 rounds of pure Zoroa before the first summon | Too early: 3 |
| AI priority | P1: set Harvest Call to 3 and move Soulbind to 2 (still above Soul Lance). P2: place it between Soulstorm and Soulbind, renumbering as needed. Always below Final Reaping | It's used within 1 round of coming off cooldown, but never instead of a ready Final Reaping | Re-order |
| MP cost | 30 P1 / 35 P2 | Arcane Charge still at most about once per phase (Part 1 test) | Lower the cost, or raise Zoroa MP by 25% |
| Summon HP | 70% of the enemy's normal (post-multiplier) HP; other stats unchanged | Killing a common summon takes about 1–2 party turns, and an Elite summon about 3–4 | Too tanky: 60%. Ignored freely: 80% |
| Elite roll | 20% (user-set) | — | — |

### Changes
1. `Data/Defs.cs` `EnemyAttack`: add `public int summonCount;` (fill up to N). `Combatant`: add `public bool IsSummon;`.
2. `Data/Dungeon1Roster.cs`: add Harvest Call to `zoroa_p1` and `zoroa_p2` with the values above. Put the pools as static arrays next to the roster: Normal ids filtered by `Tier.Normal`, and the 5 Elite ids.
3. `Combat/Battle.cs`:
   - Add `public void Summon(Combatant summoner, EnemyDef def)`. It builds `new Combatant(new Unit(def, Dungeon), Side.Enemy, nextSlot, name, tag, def)`, sets `IsSummon = true`, scales HP to 70%, and reuses the duplicate-name/tag lettering from the ctor (~L41–46).
   - In the move-execution path (~L357), when `move.summonCount > 0`, roll with the battle `Rng` and call `Summon` for each empty slot.
   - On Zoroa's final defeat, kill the remaining summons before the outcome check. Skip reward and relic hooks for `IsSummon` (~L996).
4. `Combat/EnemyBrains.cs` `Select`: skip summon moves while living summons ≥ `summonCount`.
5. `Screens/CombatScreen.cs`: the enemy layout must fit Zoroa plus 2 summons (3 slots), and summons must be spawned in visibly (reuse the existing enemy-enter animation if there is one).

### Tests
- A cast with 0 summons alive spawns 2, with 1 alive spawns 1, and with 2 alive the AI doesn't choose it.
- Over 1000 seeded rolls, the Elite rate is 20% ± 3%, and stage bosses never appear.
- Summons don't act in the round they're summoned.
- Killing Zoroa's P2 bar wins the battle with summons still alive. Summon kills grant 0 EXP and 0 gold.
- Summons persist through the phase change, and the Harvest Call cooldown carries over.
- `BalanceSimulation`: the double-turn report from Part 1 now includes summons. Report rounds, deaths, and summons spawned per fight.

### Playtest additions
- **Choice test:** the player has a real decision between killing summons and focusing Zoroa. Neither should always be right.
- **Readability:** the intent line shows Harvest Call a turn ahead, and new enemies are visually distinct (summon marker).
- **Abuse test:** stalling Zoroa to farm summons gives nothing, because there are no rewards.

---

## Tuning priority
Adjust the Zoroa damage multiplier first, then summon HP %, then `SecondTurnSpeedFactor`, then P1 `turnsPerRound`. Change Zoroa HP last.
