# Handoff: Dungeon 1 difficulty pass (Poise breaks, stronger Elites and Zoroa)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- Every number below is a **posture-B starting value** (untested). Keep that label and its pass/fail test as a code comment next to each constant.
- Once you're done, report the test results and the playtest metrics from the Verification section to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Context
Playtest feedback (2026-11-22): the game got a little too easy, the player's gauge (Poise) rarely breaks, the Zoroa fight is easier than before, and the party gets strong very fast.

What the code shows:
- **Poise almost never breaks.** No Dungeon 1 enemy sets ability scores, so every one of them, Zoroa included, has `physicalBreakPower = 10 + STRmod*2 = 10` (`Run/RunState.cs` ~L198, enemy `Derived` block). A Good defensive QTE halves that (`Combatant.PlayerPoiseMitigatedMult`), and StaggerRes (10 + VIT*4) cuts it again as a % reduction (`Combatant.ApplyBreakDamage`). A hero loses about 4–5 of 60 Poise per hit. Even Zoroa's 4-hit Final Reaping takes off only about 20. Only Gravefall's flat `breakPower = 30` actually threatens a break.
- **The party gets strong fast.** EXP is set so a run ends Dungeon 1 at level 13 (`Run/Rewards.cs` `RunConfig`), and the new Prologue adds a free level 2 (`Run/Prologue.cs`). The party reaches Zoroa around level 10–11. Zoroa's numbers are still Dungeon 1 base values.
- **Zoroa got easier.** Elite nodes used to bring back the stage bosses; now they draw from 5 new Elites (v1.9 pool, `d1_e8`–`d1_e12`) that also pay 350 EXP. Together with the Prologue level, the party reaches Zoroa stronger than it did before.

Decided with the user: **keep the EXP pacing as it is and buff the enemies instead.** For Poise, use **both** a per-tier break power base **and** per-move `breakPower` on signature moves.

## Numbers (posture B: starting values, untested)
| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| Enemy base break power by tier | Normal 12 / Elite 20 / Boss 28 | In a D1 playtest, a hero breaks 1–2 times per Elite/Zoroa fight when QTEs are missed, and about 0 times when they are hit | Too many breaks: lower Elite/Boss by 4. Still no breaks: raise by 4 |
| Per-move `breakPower` (flat, added per target hit) | Final Reaping P1 20 / P2 25, Soulstorm 10, Crownless Duelist Triple Feint 8, Funeral Chorus 8, Reaper/Ossuary Knight stage-boss heavy move 15 | Final Reaping with all QTEs missed breaks a 60-Poise hero; with all Good QTEs it does not | Adjust the Final Reaping value first |
| D1 Elite stat multiplier (HP, ATK, MATK) | ×1.25 | The median Elite fight takes about 4–6 rounds | Too long: ×1.15 |
| D1 Boss stat multiplier | HP ×1.30, ATK/MATK ×1.20 | Zoroa takes about 10–14 rounds for a level 10–11 party, and at least one hero drops if QTEs are sloppy | Adjust the HP multiplier first, then ATK |
| Normal enemies | unchanged | — | Add ×1.10 only if normal fights still feel trivial after this pass |

## Changes
1. **Tier break power.** `Run/RunState.cs` enemy `Derived` block (~L198): replace the base `10` in `physicalBreakPower` and `magicBreakPower` with a per-tier constant. Use `d.tier` (Normal 12 / Elite 20 / Boss 28) and keep the `+ mods.str*2` / `+ mods.intel*2` terms. Put the three constants next to the Poise knobs in `Combat/Combatant.cs` (L44–52).
2. **Per-move break power.** Reuse the existing `EnemyAttack.breakPower` field (`Data/Defs.cs`). It is already applied in `Combat/Battle.cs` ~L391 via `BreakHit`, and only Gravefall uses it today. In `Data/Dungeon1Roster.cs`, set `breakPower` on the moves in the table above. The Battle.cs code runs per target inside the hit loop, so all-target moves (Soulstorm, Funeral Chorus) hit every hero's Poise. That is intended.
3. **Elite and Boss stat buff.** In the `Dungeon1Roster.Enemy(...)` helper (`Data/Dungeon1Roster.cs` L20–40), scale `stats` by tier **after** the `hitPower = dmg / stat` coefficients are computed. Authored damage then scales with ATK automatically, with one knob per tier. Add constants `EliteStatMult = 1.25f`, `BossHpMult = 1.30f`, `BossAtkMult = 1.20f`. Leave DEF, SPD and MP alone.
   - This only affects D1 roster enemies. `Unit.ScaleEnemyStats` and its exact-value tests (`CursedEquipmentAndSystemRevisionTests`) stay as they are.
   - Add a `ponytail:` comment: these multipliers diverge from `Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx`, and the workbook should be updated if they stick.
   - `Data/PrologueRoster.cs` uses the same `internal` `Enemy()` helper. If any Prologue enemy is Elite/Boss tier, exempt the Prologue (apply the multiplier only for `Dungeon1Roster.Enemies`), so the tutorial's difficulty does not change.
4. **Tests.**
   - `Tests/Editor/Dungeon1RosterTests.cs`: TestCase rows ~L60–63 (Tombstone Colossus, Zoroa p1/p2) and the Zoroa `1840` HP asserts (~L576, 743, 754, 765, 783, 802) should derive from the multiplier constants instead of literals.
   - Add one test: with all-Miss QTEs, Zoroa Final Reaping breaks a default 60-Poise hero, and with all-Good QTEs it does not.

## Not doing
- No EXP or level-curve changes. The user chose to keep them. `BalanceSimulation.RandomPathThroughDungeon1ReachesLevel13` must stay green.
- No changes to player Poise gauge size or recovery (60 / 20 per turn).

## Verification
1. Run the EditMode tests (unity-cli skill): `Dungeon1RosterTests`, `BalanceSimulation`, `CursedEquipmentAndSystemRevisionTests`, then the full EditMode suite. All should pass.
2. Playtest script, one D1 run:
   - **Skill test:** fight Zoroa twice, once hitting QTEs and once deliberately missing them. Missing should cause at least one hero Break.
   - **New player test:** a Break should be readable. The existing "is staggered! (BROKEN)" log and the gauge UI should show it.
   - **Abuse test:** guard-spamming should still prevent Breaks (Guard ×0.5 is unchanged), but it costs tempo against the buffed HP pools.
3. Report the round counts and number of Breaks per Elite and Zoroa fight. Tune the HP multipliers first, then break power.
