# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT

**Project:** 2DObscure (FELNA) — 2D party-based turn-based roguelike RPG in Unity
**Updated:** 2026-11-19
**Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera), C#
**Last recorded test run:** 475 / 475 EditMode passing (batch-mode run, 2026-11-19, includes all uncommitted work and the EXP system).
**Rules for agents:** read [AGENTS.md](AGENTS.md) first. No auto-commit, no `git add .`, never stage `.claude/`, `TestResults.txt`, `project.yaml`, `Library/`, `Temp/`.

---

## 1. What the game is

FELNA is a dark-fantasy pixel-art RPG. The player builds a party of 4 heroes, prepares in the Geistwald City hub, and clears 8 dungeons ("layers") in a fixed order. Each dungeon is a branching node map (battle, elite, event, treasure, rest, shop, boss). Combat is turn-based with a speed-based initiative queue and QTE (timed input) on every attack and defense.

Authoritative design: [Assets/Docs/GDD_Terbaru.md](Assets/Docs/GDD_Terbaru.md) (Indonesian, v1.5). Supporting sheets are in `Assets/Docs/` (equipment catalogue, relics, Dungeon 1 roster, Dungeon 1 event pool, modifier list) plus the UI spec `FELNA — UI System.html`.

## 2. Current state in one paragraph

The **systems** are largely built and tested: character creation, races, classes, backgrounds, skills, talents, combat, QTE, damage, status effects, equipment, relics, modifiers, hub facilities, saving, and music. The **game loop is not**: only Dungeon 1 has real content. (EXP and levelling were wired up on 2026-11-19.) Dungeons 2–8 reuse Dungeon 1's enemies and boss. See [UNITY_IMPLEMENTATION_AUDIT.md](UNITY_IMPLEMENTATION_AUDIT.md) for the full status table and roadmap.

## 3. Design decisions (settled)

| Area | Decision |
| --- | --- |
| Party | Exactly 4 heroes. |
| Races | 7 playable: Human, Elf, Dwarf, Halfling, Dragonborn, Liichtian, Merfolk. |
| Classes | **8** playable: Fighter, Barbarian, Rogue, Ranger, Cleric, Wizard, Warlock, Druid. (Older docs said 9; that is wrong.) |
| Point buy | 27 points, scores 8–15 before racial bonus. |
| Level cap | 15. ASI points at levels 4, 8, 12, 14. Talent picks at 3, 6, 9, 12, 15. |
| Dungeons | 8, fixed order, no going back. Dungeon 1 has 3 stages; the others currently have 1. |
| Dungeon clear | Commits EXP/levels/SP/skills/equipment/consumables/materials/key items; Run Gold converts 1:1 to Hub Gold; all dungeon modifiers end. |
| Party wipe | Discards the current dungeon's uncommitted progress and Run Gold; keeps previously committed progress, Hub Gold and relics; returns to Main Menu. |
| Abandon | Same discard as a wipe, but returns to the Hub. |
| Relics | Permanent: capacity 5, shared by the party, no duplicates, kept on dungeon clear and on party wipe, replaceable in the Hub, sellable at the Magic Shop. |
| **Relics and New Game** | **Relics do NOT carry over into a New Game** (decided 2026-11-19). A New Game starts with zero relics. Code already matches this: `PersistenceCommitHandler.CreateNewGameCommittedSnapshot` sets `relics = []`, and no relic state lives outside a save. There is therefore no meta-progression between New Games. |
| Modifiers | Temporary; stack up to a per-effect cap; last until the party leaves the dungeon (kept across stages of the same dungeon). |
| Saves | 5 slots: 1 Auto Save + 4 manual. Save only in the Hub; load only from Main Menu or Hub. No mid-dungeon save/resume. |
| Starter gold | 600. |
| Damage types | Physical, Elemental, Almighty. No "Magical" category. Rounding: floor, once per hit. |
| Initiative | Dynamic Initiative Queue on Effective SPD (speed modifier clamped 0.50–1.50), no RNG, one turn per unit per round. |
| Enemy scaling | Linear tier-weighted scaling by dungeon number (GDD Bab 10 §7). |
| Control effects | Stun, Sleep, Mind Control, Madness, Silence do not stack and grant 1-turn immunity to that effect on expiry/cleanse. |
| Dragonborn lineages | Code follows GDD Bab 6: Crimson (Fire, +2 STR), Stone (Earth, +2 STR), Azure (Lightning, +2 CHA), Frost (Ice, +2 CHA); Exhausted is −20% damage. The other "v1.1" lineage set mentioned in GDD Bab 21.1 exists only in the document, not in code. |
| UI | 640×360 reference resolution, DotGothic16-style pixel font, "Starlit Reliquary" palette ([Palette.cs](Assets/Scripts/Render/Palette.cs)). Screens are built from the Figma export through [FigmaUi.cs](Assets/Scripts/Ui/FigmaUi.cs). |

## 4. Known problems to fix first

1. ~~No EXP or levelling in play.~~ **Fixed 2026-11-19.** Winning a battle gives EXP to standing heroes (Normal 35 / Elite 80 / Boss 175, adjusted by R031/R032); level N needs `100 + 25×(N−1)`; cap 15. Tuning lives in `RunConfig` ([Rewards.cs](Assets/Scripts/Run/Rewards.cs)), and the numbers are placeholders until the GDD sets them. The reward screen shows +EXP, EXP bars and LEVEL UP. EXP, SP and skill ranks are now saved and roll back with the commit boundary (before this, SP and ranks were not saved at all).
2. **Dragonborn lineage cannot be chosen.** The 4 lineages exist in `Database.DragonbornLineages`, but the race is hard-wired to `lineageId = "crimson"` ([Database.cs:337](Assets/Scripts/Data/Database.cs:337)) and the creation screen has no lineage picker. Every Dragonborn is Crimson (+2 STR, Fire). The lineage is also not stored per character. Separately, the Stone lineage uses `Element.Physical` in code while the GDD says Earth (`Element.Earth` exists).
3. **Dungeons 2–8 have no content.** [MapGenerator.cs:41-57](Assets/Scripts/Run/MapGenerator.cs:41) gives them a default one-stage plan whose boss is `d1_boss` (Zoroa). Encounters come only from [Dungeon1Roster.cs](Assets/Scripts/Data/Dungeon1Roster.cs) plus 3 legacy enemies; acts 2+ get only the shared legacy events (`dungeonAct == 0`).
4. **Dungeon names disagree** in three places: [MapGenerator.cs:14](Assets/Scripts/Run/MapGenerator.cs:14) ("Ashen Catacombs…"), [SaveSystem.cs:58](Assets/Scripts/Data/SaveSystem.cs:58) ("Abyssal Crypt (Layer 6)"), and GDD Bab 4 §6 (Ruined Kingdom, Forgotten Prison, Colosseum…).
5. **No ending.** Clearing Act 8 goes to `GameState.Victory` → `EndScreen`, but the GDD (Bab 2 §9.2) has not defined the ending or run summary, and the final boss (Helelios) does not exist.
6. **Font assets in the working tree** (`Assets/UI/Fonts/Felna*Font.asset`) lose about 5,200 lines in the current diff. Confirm that is an intended regeneration before committing.

## 5. Uncommitted work in progress (as of 2026-11-19)

25 modified files, nothing staged. The code changes:

- **QTE variety** ([Qte.cs](Assets/Scripts/Qte/Qte.cs), [QteConfig.cs](Assets/Scripts/Qte/QteConfig.cs), [QtePanel.cs](Assets/Scripts/Screens/QtePanel.cs), [Controls.cs](Assets/Scripts/Core/Controls.cs)). Offense: `ChargeQte` (hold and release, for skills with power ≥ 1.8), `MovingTargetQte` (ranged, drifting zone), `ComboQte` (AoE/multi-hit, several zones, power averaged). Defense: `WardQte` (type runes against enemy magic) and arrow-direction dodge cues with feints (35% on Elite/Boss single physical hits). `QteFactory.KindFor` picks the type from existing skill data.
- **Player Poise** ([Combatant.cs](Assets/Scripts/Combat/Combatant.cs), [Battle.cs](Assets/Scripts/Combat/Battle.cs)): the party side now has a Break gauge. Good defensive QTE or Guard halves Poise damage; recovers 20 per turn only if not hit since the last turn. Player StaggerRes is a % reduction; enemies keep flat BreakResistance.
- **Modifier lifetime** ([DungeonModifierManager.cs](Assets/Scripts/Run/DungeonModifierManager.cs), [ModifierHudHelper.cs](Assets/Scripts/Ui/ModifierHudHelper.cs), [CatalogueModifiers.cs](Assets/Scripts/Data/CatalogueModifiers.cs)): every modifier lasts until dungeon exit; a pick at the stack cap is labelled MAX and adds nothing.
- **Event blessings** ([Defs.cs](Assets/Scripts/Data/Defs.cs), [CharacterCreation.cs](Assets/Scripts/Data/CharacterCreation.cs), [RunState.cs](Assets/Scripts/Run/RunState.cs)): an event can grant a `battleBuff` applied at the start of every battle until dungeon exit; saved as `BattleBuffSaveData`.
- Tests updated: `BattleTests`, `DungeonModifierTests`, `QteTests`, `RunStateTests`, `GameFlowTests`.

## 6. Where things live

```text
Assets/
├── Docs/                     GDD_Terbaru.md, UI System html, design spreadsheets
├── Resources/                Heroes/ (idle, act, hit, ko frames), Enemies/ (idle + atk only, D1 + Zoroa),
│                             Icons/, Fx/, Sfx/, MusicLibrary.asset, Figma* UI assets
├── Scripts/
│   ├── Audio/                MusicLibrary, MusicManager, Sfx (procedural + baked)
│   ├── Combat/               Battle, Combatant, BattleAction, EnemyAI, EnemyBrains, Elements,
│   │                         Damage/ (DamageCalculator…), Status/ (StatusEffectController…)
│   ├── Core/                 Game (state machine, screen routing, camera), Controls (keyboard/mouse/gamepad)
│   ├── Data/                 Database (races, classes, skills, enemies), CatalogueDatabase (89 equipment),
│   │                         CatalogueRelics (31 relics), CatalogueModifiers, CharacterCreation, StatsSystem,
│   │                         InventorySystem, TalentSystem, BackgroundSystem, DifficultySystem, ShopSystem,
│   │                         SaveSystem, Dungeon1Roster, Dungeon1EventPool
│   ├── Qte/                  Qte (all QTE kinds + factory), QteConfig
│   ├── Render/               PixelFont, Palette, PixelArt, Ui, UnitArt
│   ├── Run/                  RunState, MapGenerator, Rewards, RelicManager, DungeonModifierManager
│   ├── Screens/              one class per GameState (Main Menu, Hub + HubServiceScreens, Map, Combat, Reward,
│   │                         Event, Rest, Shop, Inventory, Equipment, Skills, Talents, Save/Load, Settings, End…)
│   └── Ui/                   FigmaUi, UitkMenu, ModifierHudHelper
└── Tests/
    ├── Editor/               ~25 EditMode suites (largest: InventoryAndEquipment, Dungeon1Roster, CharacterStats)
    └── PlayMode/             GameFlowTests, UiButtonPlayTests, MusicPlayModeTests, UiCaptureTests
```

`GameState` values: MainMenu, Settings, CharacterCreation, CharacterStatus, OpeningSequence, Map, Combat, Reward, Rest, Event, GameOver, Victory, Inventory, Equipment, Skills, Shop, Hub, Talents, Save, Load, Party, HubShop, Armory, Blacksmith, Temple, MagicShop, DungeonClear. The hub's "Training Ground" opens `GameState.Skills`.

## 7. Content inventory

| Content | Count | Notes |
| --- | --- | --- |
| Races | 7 | Racial feats + utilities in [Database.cs](Assets/Scripts/Data/Database.cs). |
| Classes | 8 | Each has tier 1–5 skills (1 at tier 1, 2 per tier after; Fighter has 3 at tier 2). |
| Backgrounds | 10 | 5 ranks each, unlocked by level. |
| Equipment | 89 | 72 standard + 17 cursed (EQ073–EQ089). |
| Relics | 31 | R001–R032, R007 removed; R023–R032 are demerit relics. |
| Dungeon 1 encounters | 8 | `d1_e1`–`d1_e7` + `d1_boss` (Zoroa, 2 phases), built from 9 enemy types + Zoroa. Stage bosses: `d1_e6`, `d1_e7`, `d1_boss`. |
| Events | Dungeon 1 pool + legacy shared pool | No pools for dungeons 2–8. |
| Music | 8 tracks | main_menu, map, battle_1–3, hub, zoroa_p1, zoroa_p2. |

## 8. Running tests

Run inside the open Unity Editor: menu `Tools/Run All Tests` (uses `TestRunnerApi`), results land in `TestResults.txt` (do not commit it). EditMode tests use `MemorySaveStorage`; the game uses `FileSaveStorage` at `Application.persistentDataPath/Saves/`.

## 9. Suggested next steps

1. Tune EXP numbers once the GDD defines them; optionally show EXP on the Character Status and Party screens.
2. Add the Dragonborn lineage picker and store `lineageId` per character (and fix Stone → Earth).
3. Settle the official layer names and use one list everywhere.
4. Build Dungeon 2 (Lilia, The Forgotten Prison) end to end as the template for dungeons 3–8.
5. Final boss (Helelios), ending and run summary once the GDD defines them.
