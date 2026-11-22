# Handoff: enemy charge pacing (stop Zoroa and stage bosses from charging every turn)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- Every number below is a **posture-B starting value** (untested). Keep that label and its pass/fail test as a code comment next to each constant.
- Once you're done, report the test results and the playtest metrics from the Verification section to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.
- Related: `Assets/Docs/D1_Difficulty_Pass_Handoff.md` (Poise break power and Elite/Boss stat buff). Read its "Interaction" note below before tuning.

## Context
Playtest feedback (2026-11-22): Zoroa, and some boss/Elite enemies, keep using charged attacks.

How the AI works: `PriorityBrain.Select` (`Combat/EnemyBrains.cs` L55–88) picks the highest-`aiPriority` move that is ready. Cooldowns are set on **release** (`Combat/Battle.cs` ~L359) and tick at the owner's turn start (`Battle.cs` ~L158). A telegraphed move spends one no-damage turn charging (`Battle.cs` ~L350), then releases.

Traced turn by turn:
- **Zoroa P1:** Final Reaping (prio 4, cd 3, telegraph) and Soulbind (prio 3, cd 3, telegraph, no damage) take turns, and their cooldowns run out just in time for each other. Zoroa's turns go charge, release, charge, release with no end, so **100% of turns are charge or release**. Soul Lance and Soul Rend are never used. Half of Zoroa's turns deal no damage, and Soulbind's release deals none either. This is also part of why Zoroa feels easy.
- **Zoroa P2:** Final Reaping and Soulstorm take turns the same way, again 100%. Soulbind, Soul Lance and Soul Rend are never used.
- **Royal Ossuary Knight (stage-2 boss):** Graveward Charge has `cooldown = 1`, so it is ready again on the turn after it releases, and the knight charges every other turn forever.
- Mourning Bell Wraith (Funeral Chorus, cd 3) already has normal turns between its charges. No change is needed there.

The existing test `Dungeon1RosterTests` ~L850 pins this exact sequence (`charge:Soulbind, Soulbind, charge:Final Reaping, Final Reaping`), so the behaviour is deliberate but untuned.

Decided with the user: **a global gap rule plus an instant Soulbind.**

## Numbers (posture B: starting values, untested)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| Charge gap: own turns after a charged release before another charge can start | 1 | In a Zoroa fight, 50–66% of Zoroa's turns are charge or release, and Soul Lance shows up at least once per 4 turns | Still feels charge-heavy: 2. Zoroa feels flat and has no signature moments: 0 for Boss tier only |
| Soulbind telegraph | removed (instant) | — | — |

Expected cycles after the change (verify these in a test):
- **P1:** Soulbind, Soul Lance, charge FR, release FR, and repeat. That's 2 of 4 turns charging or releasing, and 3 of 4 turns affect the party.
- **P2:** charge FR, release, normal move, charge Soulstorm, release, normal move, and repeat. That's 4 of 6 turns.
- **Ossuary Knight:** charge, release, Ossuary Slam, and repeat. That's 2 of 3 turns.

## Changes
1. **Gap rule (global, all roster enemies).**
   - Add `public bool JustReleasedCharge;` to `Combatant` (`Combat/Combatant.cs`, next to `Charge`/`Intent`). Add a constant `ChargeGapTurns = 1` with its posture-B comment.
   - In `Battle.cs` where a move is used (~L357–360, the `if (move != null)` block), set `actor.JustReleasedCharge = move.telegraph;`. A release sets it, and any other used move clears it.
   - In `PriorityBrain.Select`, add `if (m.telegraph && self.JustReleasedCharge) continue;` next to the cooldown check.
   - If the gap is ever raised above 1, replace the bool with a turn counter that ticks down in the same loop as cooldowns (`Battle.cs` ~L158). With a gap of 1, the bool is enough. Mark it with `// ponytail: bool = gap of 1; counter if ChargeGapTurns > 1`.
   - On a phase change (`Combatant.cs` ~L213, where `Intent = Charge = null`), reset `JustReleasedCharge = false`. That way Phase 2 can open with its signature charge.
   - Broken or stunned turns don't clear the flag. The gap is then still owed on the enemy's next real action. That is acceptable, so leave it as is.
2. **Instant Soulbind.** In `Data/Dungeon1Roster.cs`, remove `telegraph = true` from Soulbind in `zoroa_p1` and `zoroa_p2`. Keep its priority, cost, cooldown and Silence chance. It stays a no-QTE status move (the tests at ~L406–407 should still pass).
   - Add a `ponytail:` comment: Soulbind is no longer telegraphed, which diverges from the v1.9 xlsx.
3. **Intent HUD.** Round prep (`Combat/EnemyAI.cs` ~L39) calls `Select`, so the shown intent follows the new rule automatically. Check that the HUD never shows a charge intent the enemy then can't start.

## Tests (`Tests/Editor/Dungeon1RosterTests.cs`)
- Update the sequence assert at ~L850 to the new P1 cycle: `Soulbind`, `Soul Lance`, `charge:Final Reaping`, `Final Reaping`.
- Add one test: over 12 Zoroa turns in P1 and in P2, no charge starts on the turn right after a release, and the share of charge or release turns is at most 66%.
- Add one test: Royal Ossuary Knight follows charge, release, Ossuary Slam.
- Re-check the assumptions in ~L668–720 (knight charge/break-cancel) and ~L863 / ~L948 (Zoroa / Funeral Chorus intent charging).
- Run `BalanceSimulation` too. Its heuristic at ~L194 reads `IsCharging`.

## Interaction with the earlier difficulty pass
Zoroa now uses damaging moves much more often, on top of the ×1.20 ATK from `D1_Difficulty_Pass_Handoff.md`, so expect a noticeably harder fight. Playtest **this change first**, before raising any Zoroa multiplier further. If Zoroa now feels too hard, lower `BossAtkMult` back toward 1.0 before touching the gap.

## Verification
1. EditMode tests (unity-cli skill): `Dungeon1RosterTests`, `BalanceSimulation`, then the full suite. All should pass.
2. Playtest: one Zoroa fight. Log each Zoroa turn (charge, release, or normal) and count them.
   - **Pass:** P1 is about 50% charge or release, and P2 is about 66% or less.
   - **Readability:** every charge is still announced by "begins charging …" and the intent line.
   - **Skill test:** breaking Zoroa mid-charge still cancels it.
3. Report the turn counts and the fight's round count to the user.
