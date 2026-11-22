# Handoff: wire the 5 active Racial Feats into live combat (Skill menu)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design source:** `Assets/Docs/GDD_Terbaru.md`, race chapter §3.2 (Racial Feat), §4 (combat conventions), §5.2–5.7 (each feat). The GDD wins over this handoff.
- **Don't change** Human Determination or Halfling Second Chance. Both already trigger automatically.
- Report test results, and anything you couldn't verify, to the user.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Context
- **What works today:** Human (Determination) fires automatically from `Battle.cs`, and Halfling (Second Chance) from `CombatScreen.cs`.
- **What doesn't:** these 5 *active* feats exist only in the backend, in `Combat/Combatant.cs`, and only tests call them:

| Race | Method | Target | Limit |
|---|---|---|---|
| Elf | `ActivateFeyStep()` | self | CD 4 |
| Dwarf | `ActivateIronResolve()` | self | CD 3 |
| Dragonborn | `ActivateDraconicOverdrive()` | self | 1 per battle |
| Liichtian | `ActivateLightbound(allies)` | all active allies | 1 per battle |
| Merfolk | `ActivateOceansEmbrace(ally)` | one ally, including self | CD 4 |

- Their durations and cooldowns already tick in `Battle.StartTurn` (~L226–250).
- **User direction:** the feats go into the **Skill menu**, which scrolls. `CombatScreen.ShowSkills` (~L1816) already lives in a `FigmaUi.CreateVerticalScrollView`.

## GDD rules the wiring must respect
- A Racial Feat **is not a Skill**:
  - it costs no MP;
  - it takes no Active/Passive slot;
  - **it stays usable while Silenced.**
- An active feat **uses the owner's turn**. It never grants an extra turn.
- It can't be activated while the owner is Stunned, Asleep, Mind-Controlled or Down. Those states already skip the turn.
- A Racial Feat **never uses a QTE**.
- Every activation needs clear visual and combat feedback.

## Changes
1. **`Combat/BattleAction.cs`:**
   - Add `ActionKind.RacialFeat`.
   - Add `BattleAction.RacialFeat(Combatant actor, Combatant target = null)`.
   - Silence only blocks `ActionKind.Skill` (`Combat/Status/StatusEffectController.cs` ~L216), so this new kind stays usable while Silenced with no extra code.
2. **`Combat/Combatant.cs`:** add two helpers:
   - `RacialFeatState`: Ready / on cooldown n / used, plus the feat name and id, read from `unit.race.racialFeat` and the existing cooldown and used fields.
   - `bool CanUseRacialFeat(out string reason)`.
3. **`Combat/Battle.cs`:**
   - In `Validate`, accept `RacialFeat` only when all of these hold:
     - the actor is alive and able to act;
     - the feat is available;
     - for Merfolk, the target is a living ally.
   - In `Perform`, call the matching method:
     - Lightbound gets `Allies(actor)`;
     - Ocean's Embrace gets the chosen ally;
     - the other three take no target.
   - Then log one line, for example "Thorin invokes Iron Resolve!", and play an existing `Sfx` sting.
   - The action ends the turn normally, so the encounter-memory and `AiTrace` hooks see it.
   - **No QTE:** `QteFactory.Offense` and `QteFactory.Defense` return null for this kind.
4. **`Screens/CombatScreen.cs`, `ShowSkills(c)`:**
   - Add a **"RACIAL FEAT"** row at the **top** of the list, above the class skills. It appears only for the 5 active-feat races; Human and Halfling get no row.
   - Row label:
     - `"<FEAT NAME>  READY"`;
     - `"<FEAT NAME>  CD n"`;
     - `"<FEAT NAME>  USED (1/BATTLE)"`.
   - Tooltip: the GDD effect text. Cost line: `COST: TURN · NO MP`. When unusable, the row is disabled and shows its reason.
   - Targeting:
     - Ocean's Embrace: `ChooseTarget(Battle.Allies(c), …)`, self included.
     - Lightbound: no picker; it hits all allies.
     - Fey Step, Iron Resolve, Draconic Overdrive: self, no picker.
   - **Silence:** if the SKILL option in `ShowActions` is disabled while Silenced, keep it enabled whenever the feat is usable. Class skills inside stay disabled, as today.
5. **AI targeting:** single-target target picking skips **Phased** heroes (GDD: untelegraphed single-target attacks choose another target). That means `PriorityBrain.PickTarget`, the utility target scoring and the lethal reservation in `Combat/AI/*`.
   - AoE still hits a Phased hero.
   - Telegraphed attacks keep the existing redirect in `Battle.Intercept` (~L325).
6. **HUD:** show these as status chips on the hero, so each effect stays visible for its whole duration: Phased, Iron Resolve, Draconic Overdrive, Exhausted, Lightbound, and Ocean's Embrace (the shield).

## Bugs found while reading (fix each and cover it with a test)
1. **Cooldowns block one turn too few.**
   - **GDD:** cooldown N means the feat can't be used for the N owner turns after activation.
   - **Code:** `FeyStepCooldown = 4`, decremented at the *start of the next* turn. So the feat is usable again on turn T+4, after only 3 blocked turns. Iron Resolve (3) and Ocean's Embrace (4) have the same problem.
   - **Fix:** set N+1 on activation, or decrement at the end of the turn instead.
   - **Test:** activate on turn T. The feat is unavailable on T+1 through T+N and available on T+N+1.
2. **Lightbound raises the wrong resistance.**
   - **Code:** it adds to `ResistanceProfile.buffResistance`, which is an *elemental* resistance component (`Combat/Damage/ElementalResistance.cs`).
   - **GDD:** +20% **Status Effect Resistance**, additive, still under the 75% cap, for 2 Liichtian turns.
   - **Fix:** apply it to status resistance, and remove it when the effect expires (check the `LightboundTurns` expiry block in `Battle.StartTurn`).
3. **Ocean's Embrace applies the wrong effect.**
   - **Code:** it gives the target `DefenseUp 20%`.
   - **GDD:** the target's **Defensive QTE window +10%** while the Shield is active, counted inside the +25% window cap. It ends when the Shield breaks or after 3 Merfolk turns, then heals 10% Max HP once.
   - **Fix:** replace `DefenseUp` with a defensive window delta for the target, added to the `totalTimingBonus` clamp in `QteFactory.Defense`. Make sure the effect also ends when the Shield breaks, not only when the 3 turns run out.
4. **Draconic Overdrive may double-dip.**
   - **Code:** it adds both `AllDamageBonus ×1.35` and `AttackUp 0.35`. `AttackUp` also scales Break power through `Combatant.PhysicalBreak`.
   - **GDD:** for 3 Dragonborn turns, All Damage +35%, lineage element +25%, **Break Power +50%** and counter multiplier ×1.25. Afterwards, **Exhausted** for 2 turns: −20% damage, −30% Break Power, and it can't be cleansed.
   - **Fix:** verify each number in a test. Remove `AttackUp` if it stacks with the damage bonus. Add an explicit Break +50% and the counter ×1.25 if they're missing.

## Tests (`Tests/Editor/`)
- **For each of the 5 feats, used through `BattleAction.RacialFeat`:**
  - it uses the turn and grants no extra turn;
  - it spends no MP and shows no QTE;
  - it works while Silenced;
  - it's blocked when on cooldown or already used, with the reason shown in the menu;
  - its effect matches the GDD numbers.
- **The 4 bug fixes above:** one test each.
- **AI:**
  - with a Phased hero, single-target plans choose someone else;
  - an AoE still hits the Phased hero;
  - a telegraphed single-target attack is redirected.
- **Menu:**
  - the feat row appears only for the matching race;
  - Human and Halfling have no feat row;
  - the list still scrolls when a hero has many skills.

## Playtest
1. **New player:** they find the feat in the Skill menu without being told, and can say what it did from the log and the status chip.
2. **Abuse:** once bug 1 is fixed, Fey Step can't be chained back to back.
3. **Readability:** an observer can see the feat's chip for its whole duration.
