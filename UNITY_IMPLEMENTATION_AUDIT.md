# UNITY IMPLEMENTATION AUDIT & GDD COMPLIANCE REVIEW

**Project:** 2DObscure (FELNA)
**Audit date:** 2026-11-19 (replaces the 2026-10-30 audit; the old version is in git history)
**Engine:** Unity 6000.3.16f1, URP 2D
**Measured against:** [Assets/Docs/GDD_Terbaru.md](Assets/Docs/GDD_Terbaru.md) v1.5 and the spreadsheets in `Assets/Docs/`
**Code inspected:** `Assets/Scripts` (~59k lines incl. tests), working tree including uncommitted changes
**Tests:** 475 / 475 EditMode passing (batch-mode run, 2026-11-19, including uncommitted work)

---

## 1. Summary

The systems are in good shape. Almost every GDD system exists in code with tests: character creation, races, classes, backgrounds, skills, talents, combat, QTE, damage, status effects, equipment, relics, modifiers, hub facilities, saving and music.

The **playable loop is not complete**, for two reasons:

1. **Characters never gain EXP or levels during play.** Everything tied to level (skill tiers 2–5, talents, ASI points, background ranks) is built but unreachable in a real run.
2. **Only Dungeon 1 has content.** Dungeons 2–8 reuse Dungeon 1's encounters, boss and (legacy) events, and there is no final boss or ending.

Of the 5 critical problems in the October audit, 3 are fixed (shop, save slots, render resolution), 1 is still open (EXP/levelling) and 1 is unchanged (single-dungeon content).

## 2. Status dashboard

Legend: **Done** = implemented and reachable in play. **Built, unreachable** = code and data exist but nothing in a normal run triggers it. **Partial** = some of it works. **Missing** = not in code.

| # | System | Status | Evidence / gap |
| --- | --- | --- | --- |
| 1 | Main menu & screen flow | Done | [Game.cs](Assets/Scripts/Core/Game.cs) routes 27 `GameState`s; New Game, Continue, Load, Settings. |
| 2 | Save system (1 auto + 4 manual, hub-only) | Done | [SaveSystem.cs](Assets/Scripts/Data/SaveSystem.cs): `SaveManager`, `PersistenceCommitHandler`, `SaveValidator`, atomic `FileSaveStorage`. |
| 3 | Character creation (point buy, races, backgrounds) | Done | [CharacterCreation.cs](Assets/Scripts/Data/CharacterCreation.cs), [CharacterCreationScreen.cs](Assets/Scripts/Screens/CharacterCreationScreen.cs); 27-point buy, 7 races, 10 backgrounds. |
| 4 | Dragonborn lineage choice | **Partial** | 4 lineages in `Database.DragonbornLineages`, but the race is hard-wired to `lineageId = "crimson"` ([Database.cs:337](Assets/Scripts/Data/Database.cs:337)); no picker in the creation screen; lineage not stored per character. Stone lineage uses `Element.Physical`, GDD says Earth. |
| 5 | Classes (8) & skills tiers 1–5 with ranks | Done | Data for all 8 classes; [SkillTreeScreen.cs](Assets/Scripts/Screens/SkillTreeScreen.cs). Reachable now that levels give SP. |
| 6 | Talents (levels 3/6/9/12/15) & Scroll of Emptiness respec | Done | [TalentSystem.cs](Assets/Scripts/Data/TalentSystem.cs), [TalentScreen.cs](Assets/Scripts/Screens/TalentScreen.cs). Reachable now that heroes level. |
| 7 | EXP, levelling, level cap 15, SP, ASI | Done (2026-11-19) | `Unit.GainExp` + `RunState.AwardBattleExp` in [RunState.cs](Assets/Scripts/Run/RunState.cs); tuning in `RunConfig`. Standing heroes only; R031/R032 applied; cap 15; reward screen EXP bars. EXP/SP/ranks saved and rolled back. Numbers are placeholders. |
| 8 | Turn-based combat, initiative queue, enemy scaling | Done | [Battle.cs](Assets/Scripts/Combat/Battle.cs), [Combatant.cs](Assets/Scripts/Combat/Combatant.cs); scaling in `RunState` (Bab 10 §7–8). |
| 9 | QTE | Done (expanding) | [Qte.cs](Assets/Scripts/Qte/Qte.cs). Uncommitted work adds Charge, Moving Target, Combo, rune Ward and arrow-dodge-with-feint QTEs. |
| 10 | Break gauge / Broken | Done (enemies); player Poise in progress | Uncommitted: party-side Poise with recovery and Guard/defensive-QTE mitigation. |
| 11 | Damage system v1.0 | Done | [Combat/Damage/](Assets/Scripts/Combat/Damage/DamageCalculator.cs); Physical/Elemental/Almighty, floor per hit. |
| 12 | Status effects incl. control immunity | Done | [StatusEffectController.cs](Assets/Scripts/Combat/Status/StatusEffectController.cs). |
| 13 | Enemy AI | Done for Dungeon 1 | [EnemyAI.cs](Assets/Scripts/Combat/EnemyAI.cs), [EnemyBrains.cs](Assets/Scripts/Combat/EnemyBrains.cs); Zoroa has a 2-phase transition with music. |
| 14 | Equipment & inventory (8 slots, 89 items, cursed gear) | Done | [CatalogueDatabase.cs](Assets/Scripts/Data/CatalogueDatabase.cs), [InventorySystem.cs](Assets/Scripts/Data/InventorySystem.cs), Inventory/Equipment screens usable mid-run. |
| 15 | Relics (31, capacity 5, permanent) | Done | [RelicManager.cs](Assets/Scripts/Run/RelicManager.cs). New Game starts with no relics (matches the 2026-11-19 decision). |
| 16 | Dungeon modifiers | Done | [DungeonModifierManager.cs](Assets/Scripts/Run/DungeonModifierManager.cs); uncommitted change makes all modifiers last until dungeon exit. |
| 17 | Hub facilities | Done | [HubServiceScreens.cs](Assets/Scripts/Screens/HubServiceScreens.cs): Shop, Armory, Blacksmith, Temple (Recovery 300), Magic Shop; Training Ground opens Skills. |
| 18 | Dungeon shop node & run economy | Done | `NodeType.Shop`, [ShopSystem.cs](Assets/Scripts/Data/ShopSystem.cs), [ShopScreen.cs](Assets/Scripts/Screens/ShopScreen.cs). |
| 19 | Node map generation | Done (Dungeon 1) | [MapGenerator.cs](Assets/Scripts/Run/MapGenerator.cs): seeded, 8 floors, 3 stages in Dungeon 1. |
| 20 | **Dungeons 2–8 content** | **Missing** | `DefaultSingleStagePlan` gives acts 2–8 one stage with boss `d1_boss`. Encounters only from [Dungeon1Roster.cs](Assets/Scripts/Data/Dungeon1Roster.cs); no enemies, bosses, art or music for Lilia, Akasuna, Pharam, Berzelius, Lacrima, Vortigern, Helelios. |
| 21 | Events | Done (Dungeon 1) / Missing (2–8) | [Dungeon1EventPool.cs](Assets/Scripts/Data/Dungeon1EventPool.cs); acts 2+ only get the legacy shared pool (`dungeonAct == 0`). |
| 22 | **Final boss, ending, run summary** | **Missing** | Act 8 clear → `GameState.Victory` → generic [EndScreen.cs](Assets/Scripts/Screens/EndScreen.cs). GDD Bab 2 §9.2 has not defined the ending yet. |
| 23 | Music & SFX | Partial | 8 tracks (menu, map, 3 battle, hub, Zoroa P1/P2); procedural + baked SFX. Boss themes exist only for Zoroa. No AudioMixer asset; volumes handled in code. |
| 24 | Rendering & UI | Done | 640×360 reference resolution (the October 320×180 conflict is resolved), pixel font, palette, Figma-built screens, mouse hit-testing fixed. |
| 25 | Character / enemy animation | Partial | Heroes: idle, act, hit, ko frames. Enemies: idle + attack only (no hit/ko); Dungeon 1 + Zoroa only. |
| 26 | Pause | Partial | Pause/abandon modal on the map; no pause found in combat. |
| 27 | Controller input | Partial (unverified) | [Controls.cs](Assets/Scripts/Core/Controls.cs) reads gamepad input; not covered by tests. |
| 28 | Story / lore delivery | Partial | Opening sequence screen exists; nothing story-related after it. GDD Bab 4 §8 still lists protagonist and layer history as open. |

**Score (28 systems):** 18 Done, 8 Partial (rows 4, 10, 21, 23, 25–28), 2 Missing (dungeons 2–8, ending). Updated 2026-11-19 after the EXP system landed (rows 5–7); §3.1 below is kept as the record of what was built.

## 3. Critical gaps in detail

### 3.1 Progression loop — DONE 2026-11-19 (original gap description kept below)

What exists: `Unit.LevelUp()` increments level, calls `SkillLoadout.SyncLevelUnlocks` (+2 SP) and grants ASI points at 4/8/12/14. `SkillLoadout.Respec` assumes 2 SP at level 1 and +2 per level. `RelicManager.ModifyExpReward` applies R031 (+15%) and R032 (−15%). The reward screen layout has per-hero XP gauges.

What is missing:
- An EXP amount on `RewardOffer` (per encounter tier) and on events where the GDD gives EXP.
- Per-hero EXP storage, an EXP curve, and calling `LevelUp()` when a threshold is crossed.
- A level-15 cap.
- Calling `ModifyExpReward`.
- Showing EXP gain and level-ups on the reward screen (the XP gauge currently shows HP).
- Saving EXP in `SavedCharacterData` and committing it on dungeon clear (the commit boundary already lists EXP).

Until this lands, a full run is played at level 1, so balance for Dungeon 1 cannot be judged either.

### 3.2 Dragonborn lineage

GDD Bab 6 §5.5 says the lineage is chosen at character creation. Needed: a lineage step on the Dragonborn race page, `lineageId` on the character (not the shared `RaceDef`), racial bonus taken from the chosen lineage, `lineageId` saved and validated, and Stone set to `Element.Earth`. Existing test `GddTerbaru_DragonbornLineages_AreProperlyConfigured` asserts Stone = Physical and would need updating.

### 3.3 Dungeons 2–8

Per dungeon: a `DungeonPlanDef` with stages and its own boss, an enemy roster in the `Dungeon1Roster` format, an event pool with `dungeonAct = N`, enemy sprites, boss music, and a layer palette/background. GDD Bab 4 §6 fixes each layer's identity and boss; GDD Bab 21.5 #3 says per-layer enemy design is still to be written.

### 3.4 Naming

Dungeon names differ in [MapGenerator.cs:14](Assets/Scripts/Run/MapGenerator.cs:14), [SaveSystem.cs:58](Assets/Scripts/Data/SaveSystem.cs:58) and GDD Bab 4 §6. Pick one list and have both code locations read it.

## 4. Findings from the October audit that no longer apply

| October finding | Now |
| --- | --- |
| Shop system 100% absent | Hub shop, Armory, Magic Shop and dungeon shop node all exist. |
| Single `PlayerPrefs` save slot | 5-slot file-based save with validation and commit/rollback. |
| 320×180 renderer conflicts with GDD 640×360 | Renderer is 640×360. |
| No in-run inventory screen | Inventory and Equipment screens work mid-run. |
| Only 23 skills, no tiers 2–5 | All 8 classes have tier 1–5 skills with ranks. |
| 9 classes | The GDD and code both have 8 classes. |
| Only one dungeon generated | Still true for content; the 8-dungeon sequence, commit and hub return now work structurally. |
| No EXP earn loop | **Still true** (see §3.1). |

## 5. Design decisions and open questions

### 5.1 Settled since the last audit

- **Relics do not carry over into a New Game** (2026-11-19). The code already does this. Consequence: there is no meta-progression between New Games. GDD Bab 21.1 and 21.5 #1 still list this as open and should be updated.
- **Dragonborn lineages:** the code uses the GDD Bab 6 set (Crimson/Stone/Azure/Frost, Exhausted −20%). The competing set in GDD Bab 21.1 (Ember/Frost/Storm/Umbral, −15%) is not in code and can be removed from the GDD.

### 5.2 Still open (GDD Bab 21)

| Question | Why it matters |
| --- | --- |
| Run failure: GDD Bab 2 §9.1 says a wipe deletes the run and needs a New Game; the save spec (implemented) keeps committed progress and lets the player Continue. | Decides whether a wipe is a full reset. Relics already follow the save spec. |
| EXP curve, EXP per encounter, SP per level, skill MP costs, healing, recovery between battles (Bab 8 §14, Bab 21.5 #2) | Needed for §3.1 and for any balance pass. |
| Per-layer enemy composition, QTE patterns and boss signature attacks (Bab 21.5 #3) | Needed for §3.3. |
| Ending, victory screen, run summary (Bab 2 §9.2) | Needed for the final boss and Victory screen. |
| Official layer names (Bab 4 §6 canon note) | Needed for §3.4. |
| Name clashes: talent Last Stand vs MOD-023, talent Marked Prey vs MOD-022 | Player-facing confusion. |

## 6. Roadmap

1. ~~Progression loop~~ (§3.1) — done; retune `RunConfig` EXP numbers when design sets them.
2. **Dragonborn lineage picker** (§3.2). Small.
3. **One list of layer names** (§3.4). Trivial once decided.
4. **Dungeon 2 vertical slice** (Lilia, The Forgotten Prison): plan, roster, boss phases, events, art, music. This becomes the template.
5. **Dungeons 3–8**, with a balance pass after each.
6. **Helelios, ending, run summary.**
7. **Polish:** enemy hit/ko animations, combat pause, AudioMixer, controller testing, story beats.

## 7. Tests to add

- Progression: winning a battle grants EXP; crossing the threshold calls `LevelUp()`; level never exceeds 15; R031/R032 change EXP; EXP survives dungeon clear and is discarded on wipe.
- Dragonborn: choosing Azure gives +2 CHA and Lightning Overdrive bonus; lineage round-trips through save; Stone = Earth.
- Dungeon plans: every act 1–8 has a boss encounter that exists and is unique to that act (fails today, which is the point).
- New Game: snapshot has zero relics even when the previous save had relics (locks in the §5.1 decision).
