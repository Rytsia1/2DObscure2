# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Updated:** 2026-11-05 (Character Creation to Hub Flow, 600 Starter Gold, 394 Passing Tests)  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** **394 / 394 EditMode Tests Passing (100% Pass Rate, 0 Failures)**  
**Working Tree State:** All changes uncommitted in working tree per `AGENTS.md` guidelines.

---

## 1. Executive Summary & Project Core

`2DObscure` (Felna) is a dark-fantasy, pixel-art, party-based, turn-based roguelike RPG built in Unity.
The core loop consists of assembling a 4-hero party, traversing 8 sequential dungeons of increasing peril, engaging in timeline-driven turn-based combat with interactive QTE strike mechanics, managing equipment and status effects, and building characters up to level 15 before confronting the final Abyss encounter.

### Established High-Level Design Constraints
- **Party Size:** Exactly 4 playable characters.
- **Level Cap:** Maximum character level is 15 (ASI points awarded at levels 4, 8, 12, and 14).
- **Dungeon Flow:** 8 sequential dungeons (no backward selection, run failure resets all progress and gold, returning to Main Menu).
- **Save Architecture:** 5 slots total (1 dedicated Auto Save slot + 4 independent Manual Save slots). Hub-Only Saving policy. Saving or resuming during active dungeons is strictly prohibited.
- **Persistence Boundary:** Dungeon Clear commits acquired progression (EXP, Level, SP, skills, equipment, consumables, materials, Key Items, and 1:1 Run Gold $\to$ Hub Gold). Party wipe and voluntary abandonment discard uncommitted current-dungeon progress while preserving previously committed campaign progression, Hub Gold, and owned Relics.
- **Races & Classes:** 7 Playable Races with signature Racial Feats and strict +2 attribute point budgets; 9 Playable Classes with signature Starter Passives and Strict Weapon Compatibility.
- **Combat & Damage:** Consolidated Damage System v1.0 (Physical, Elemental, Almighty — no "Magical" category; Nature is Druid-themed Elemental damage).
- **Point Buy:** 27-point budget, 6 Primary Ability Scores (STR, DEX, VIT, INT, WIS, CHA) ranging 8–15 prior to racial modifiers.
- **UI Architecture:** Custom procedural sprite-based pixel UI rendered top-left at $640 \times 360$ reference resolution ($\text{PPU} = 1$), driven by [`PixelFont.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs), [`Ui.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs), [`Palette.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/Palette.cs), [`PixelArt.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/PixelArt.cs), and dedicated screen controllers.

---

## 2. Recent Major Milestones

### Milestone A: Save File System & UI (`Felna_Save_File_System_and_UI_Specification.md`) — Nov 2026
1. **5 Save Slots Architecture:**
   - 1 Dedicated Auto Save slot (`SaveSlotId.AutoSave`).
   - 4 Independent Manual Save slots (`SaveSlotId.Slot1` through `SaveSlotId.Slot4`).
   - Auto Save never overwrites manual slots, and manual saving cannot target the Auto Save slot.
2. **Hub-Only Saving & Restricted Loading:**
   - Manual Save & Auto Save available *only* in the Sanctuary Hub (`SaveManager.CanSaveCurrentState()`).
   - Loading permitted *only* from the Main Menu and the Hub (`SaveManager.CanLoadCurrentState()`).
   - Mid-dungeon saving, loading, or resuming is strictly prohibited.
3. **Dungeon Commitment Boundary & Rollback (`PersistenceCommitHandler`):**
   - **Dungeon Clear:** Commits EXP, levels, SP, skill unlocks, equipment, consumables, materials, Key Items, and transfers Run Gold $\to$ Hub Gold at 1:1.
   - **Party Wipe:** Discards uncommitted current-dungeon progress and Run Gold; preserves previously committed campaign progression, Hub Gold, and owned Relics; returns to Main Menu.
   - **Voluntary Abandonment:** Discards uncommitted current-dungeon progress and Run Gold; preserves committed campaign progression; returns to Hub.
4. **Save & Load UI Screens:**
   - `SaveScreen.cs`: 2-column card layout displaying 5 slots, metadata, and Save / Overwrite / Delete actions.
   - `LoadScreen.cs`: Slot selection, metadata inspection, disabled empty slots, and Load action returning to Hub.
   - `ConfirmationModal`: Reusable modal dialog for Overwrite, Delete, Load, and Abandon operations with `ButtonStyle.Danger`.
   - `MainMenuScreen.cs`: Added `LOAD GAME` and rewired `CONTINUE` to load the latest valid save into the Hub.
   - `HubScreen.cs`: Added `SAVE`, `LOAD`, and `TITLE` utility buttons with Auto Save lifecycle triggers (dungeon clear, abandonment, pre-embark, and hub transactions).
   - `MapScreen.cs`: Added `RETURN TO HUB` (Abandon Expedition) button with confirmation modal.
5. **Storage & Validation:**
   - `FileSaveStorage`: Atomic file writing (`.tmp` $\to$ target) in `Application.persistentDataPath + "/Saves/"`.
   - `MemorySaveStorage`: Fast in-memory storage for isolated EditMode unit tests.
   - `SaveValidator`: Validates JSON schema, version compatibility, 4-character party, and database foreign keys.

### Milestone B: GDD Terbaru v1.5, Items, & Relic Revisions
1. **89 Equipment Items:** 72 Standard Equipment items + 17 Cursed Equipment items (`EQ073`–`EQ089`) with paired Boons and Curses.
2. **31 Layer 1 Relics:** Relics `R001`–`R032` (R007 Mourner's Coin removed), Demerit Relics `R023`–`R032`, permanent player progression.
3. **Linear Tier-Weighted Enemy Scaling (Bab 10 §7):** Effective stats scale dynamically by dungeon tier and growth rate.
4. **Dynamic Initiative Queue (Bab 10 §8):** Clamped Speed modifier $0.50$–$1.50$ affecting turn order.
5. **Control Effect Non-Stacking & Immunity (Bab 11 §8.1):** 1-turn immunity on expiration/cleansing.
6. **Talent Respec via Scroll of Emptiness (Bab 9 §5):** Consumable to reset and refund allocated talent slots.

### Milestone C: FELNA — UI System & DotGothic16 Font Integration — Nov 2026
1. **DotGothic16 Procedural Pixel Font Engine (`PixelFont.cs`):**
   - Added `FontKind.DotGothic` (alias `FontKind.DotGothic16`).
   - Authentic 10px glyph height (`DotGothicGlyphHeight = 10, DotGothicLineHeight = 13`), scaling cleanly across 720p/1080p target displays.
   - 103 high-legibility pixel glyphs: Uppercase A–Z, Lowercase a–z, Numerals 0–9, full punctuation, and game symbols (`▲`, `▼`, `●`, `◆`, `★`, `✓`, `×`, `▸`, `·`).
   - Backwards compatible with existing `FontKind.Interface` and `FontKind.Serif`.
2. **Starlit Reliquary Design Tokens (`Palette.cs`):**
   - **Inks:** `Ink0` (#070A14), `Ink1` (#0C1122), `Ink2` (#121A31), `Ink3` (#1A2443), `Ink4` (#25315A), `Ink5` (#3A4878).
   - **Mists:** `Mist1` (#EEF0F8), `Mist2` (#C3C9DE), `Mist3` (#8F98B8).
   - **Golds:** `Gold1` (#FFE6A3), `Gold2` (#F2C25B), `Gold3` (#C9932E), `Gold4` (#7A5717).
   - **Embers:** `Ember1` (#FFC196), `Ember2` (#EF8A3C), `Ember3` (#8A4317).
   - **Vitals:** `Hp` (#52D18C), `HpLow` (#FF6A55), `Mp` (#5AA2FF), `Xp` (#C79BFF), `Shield` (#9FE3FF).
   - **Semantics:** `Ok` (#52D18C), `Err` (#FF6A55), `Warn` (#FFB84D).
   - **Elements & Rarities:** Canonical color tokens and helper methods `Palette.ForHp(cur, max)`, `Palette.ForElement(el)`, `Palette.ElementCode(el)`, `Palette.ForRarity(r)`, `Palette.RarityBadge(r)`, and `Palette.ForClass(classId)`.
3. **Procedural UI Components (`PixelArt.cs` & `Ui.cs`):**
   - `PixelArt.NotchedBox`: 2px notched corners for enemy and threat cards (`.ec`).
   - `PixelArt.DoubleBorderBox`: Concentric double borders for Legendary and Mythic items.
   - `PixelArt.DangerBox` & `ButtonStyle.Danger`: High-contrast warning style for destructive actions (Delete Save, Abandon Expedition).
   - `PixelArt.Coin`: Solid gold disc for Hub Gold vs dashed ember diamond for Run Gold.
   - `Ui.EnemyCard`, `Ui.DoubleBorderCard`, `Ui.StatusBadge`, and `Ui.CurrencyTag`.
4. **Comprehensive Screen Upgrades:**
   - Updated headers, labels, buttons, badges, and readouts across all 16 screen controllers:
     `MainMenuScreen.cs`, `HubScreen.cs`, `CombatScreen.cs`, `InventoryScreen.cs`, `CharacterStatusScreen.cs`, `EquipmentScreen.cs`, `ShopScreen.cs`, `RewardScreen.cs`, `SaveScreens.cs`, `EventScreen.cs`, `RestScreen.cs`, `SettingsScreen.cs`, `EndScreen.cs`, `CharacterCreationScreen.cs`, `SkillTreeScreen.cs`, and `TalentScreen.cs`.
5. **UI Test Suite (`FelnaUiSystemTests` in `SaveSystemTests.cs`):**
   - 7 dedicated automated tests covering glyph rendering, font metrics, palette color tokens, element/rarity mappings, UI components, and screen instantiation.

### Milestone D: Character Creation to Hub Flow & 600 Starter Gold — Nov 2026
1. **Direct Hub Entry Flow:**
   - Upon confirming the 4-hero party in `CharacterCreationScreen.cs`, players transition directly into the Sanctuary Hub (`GameState.Hub`) via `Game.EnterHubFromCreation()`.
2. **600 Starter Gold Economy:**
   - Players begin their campaign with **600 Starter Gold** (`PersistenceCommitHandler.StarterGold = 600`, `GameSave.StarterGold = 600`).
   - `Run.Gold = 600`, `Run.HubGold = 600`, and committed snapshot `hubGold = 600`.
   - Dedicated AutoSave slot is automatically written upon Hub arrival with the 4 confirmed heroes and 600 Hub Gold.
3. **Non-Skipping Dungeon Embarkation:**
   - Embark button (`EMBARK >>` in `HubScreen.cs`) inspects `ActiveCommittedSnapshot.nextDungeonAct` (Act 1 for brand new games), embarking directly to Dungeon 1 (Act 1) with starter gold intact.
4. **Test Suite Expansion:**
   - Added `NewGame_PartyConfirmation_Grants600StarterGold_AndInitializesSnapshot` and `CharacterCreation_EnterHubFromCreation_TransitionsToHubWith600Gold`, raising EditMode test count to 394 tests (100% pass rate).

---

## 3. Directory Structure & Key Files

```text
2DObscure/
├── AGENTS.md                                                                # Strict agent security & git commit guidelines (NO AUTO-COMMIT)
├── handoff.md                                                               # Project handoff documentation
├── Assets/
│   ├── Docs/
│   │   ├── GDD_Terbaru.md                                                   # Master Game Design Document (v1.5, Nov 2026)
│   │   ├── FELNA — UI System.html                                           # Complete UI System specification (44 pages)
│   │   ├── Felna_Save_File_System_and_UI_Specification.md                   # Save System & UI Specification
│   │   ├── Felna_Items_and_Equipment_Catalogue_v2_Cursed_Equipment.xlsx     # 89 Items & Cursed Equipment Catalogue
│   │   ├── Felna_Layer1_Relic_Framework_31_Relics_No_Mourners_Coin.xlsx     # 31 Layer 1 Relics with Demerits & Magic Shop
│   │   └── Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_7(1).xlsx     # Authoritative Enemy Scaling & Initiative Roster
│   ├── Scripts/
│   │   ├── Audio/
│   │   │   ├── MusicLibrary.cs                                              # Track registries & music metadata
│   │   │   └── MusicPlayer.cs                                               # Audio playback singleton
│   │   ├── Combat/
│   │   │   ├── Battle.cs                                                    # Combat core: turns, actions, scaling, rewards, defeat
│   │   │   ├── Combatant.cs                                                 # Combatant runtime wrapper, derived stats, initiative/Speed
│   │   │   ├── Damage/                                                      # Damage System v1.0 (Calculators, Contexts, Results)
│   │   │   ├── QTE/                                                         # QTE System (Timing, Mash, Typing, Windows, Curves)
│   │   │   └── Status/                                                      # Status Effect Controller & Resistance Profiles
│   │   ├── Core/
│   │   │   └── Game.cs                                                      # Game boot, state machine, transitions, Save/Load routing
│   │   ├── Data/
│   │   │   ├── SaveSystem.cs                                                # SaveManager, SaveSlotRepository, PersistenceCommitHandler, Validator
│   │   │   ├── Defs.cs                                                      # Definitions: Unit, Skill, Enemy, Race, Class, RelicDef
│   │   │   ├── Database.cs                                                  # Core static registries & starter content
│   │   │   ├── CatalogueDefs.cs                                             # Item & Equipment schema definitions
│   │   │   ├── CatalogueDatabase.cs                                         # 89 Equipment (72 Standard + 17 Cursed), Consumables, Materials
│   │   │   ├── CatalogueRelics.cs                                           # 31 Layer 1 Relics database
│   │   │   ├── RelicDefs.cs                                                 # RelicCategory, RelicTriggerType, RelicActivationLimit
│   │   │   ├── StatsSystem.cs                                               # Stats, EquipmentTrait, AbilityScores, DerivedStats
│   │   │   ├── InventorySystem.cs                                           # InventoryRuntime, EquipmentInstance, ScrollOfEmptiness
│   │   │   ├── TalentSystem.cs                                              # TalentProgression, Respec, Milestones
│   │   │   └── DifficultySystem.cs                                          # Difficulty presets & modifiers
│   │   ├── Render/
│   │   │   ├── PixelFont.cs                                                 # Multi-font pixel engine: DotGothic16, Interface, Serif
│   │   │   ├── Palette.cs                                                   # Starlit Reliquary design tokens, inks, mists, golds, embers, vitals
│   │   │   ├── PixelArt.cs                                                  # Procedural shapes, NotchedBox, DoubleBorderBox, DangerBox, Coin
│   │   │   └── Ui.cs                                                        # UI primitives, Buttons, Badges, Cards, CurrencyTags, Headers
│   │   ├── Run/
│   │   │   ├── RelicManager.cs                                              # 31 Relics manager, Demerits, Cursed equipment hooks, replace relic
│   │   │   ├── RelicInstance.cs                                             # Runtime relic state & counter serialization
│   │   │   └── RunState.cs                                                  # Run loop, node map, enemy stat scaling, party persistence, AbandonExpedition
│   │   └── Screens/
│   │       ├── MainMenuScreen.cs                                            # Main Menu: New Game, Continue, Load Game, Settings, Exit
│   │       ├── HubScreen.cs                                                 # Sanctuary Hub: 5 Facilities, Embark, Save, Load, Title
│   │       ├── SaveScreens.cs                                               # SaveScreen, LoadScreen, ConfirmationModal
│   │       ├── MapScreen.cs                                                 # Node exploration UI & Return to Hub (Abandon)
│   │       ├── InventoryScreen.cs                                           # Inventory UI
│   │       ├── CombatScreen.cs                                              # Combat UI & timeline
│   │       ├── CharacterStatusScreen.cs                                     # Hero status & attribute sheet
│   │       ├── EquipmentScreen.cs                                           # Equipment management UI
│   │       ├── ShopScreen.cs                                                # Wandering caravan & facility shop UI
│   │       ├── RewardScreen.cs                                              # Victory spoils & card selection
│   │       ├── RestScreen.cs                                                # Campfire sanctuary UI
│   │       ├── EventScreen.cs                                               # Narrative choices UI
│   │       ├── SettingsScreen.cs                                            # Audio, video, and gameplay preferences
│   │       ├── EndScreen.cs                                                 # Victory/Defeat summary
│   │       ├── CharacterCreationScreen.cs                                   # Point-buy & roster generation UI
│   │       ├── SkillTreeScreen.cs                                           # Skill acquisition UI
│   │       └── TalentScreen.cs                                              # Hero talent selection & respec UI
│   └── Tests/
│       └── Editor/
│           ├── SaveSystemTests.cs                                           # 15 Save criteria tests + 9 FelnaUiSystemTests (Hub flow & Starter Gold)
│           ├── Layer1RelicTests.cs                                          # 31 Relics, capacity, permanent persistence, replace tests
│           ├── CursedEquipmentAndSystemRevisionTests.cs                     # 17 Cursed EQ, scaling, initiative, respec, control effects
│           ├── BattleTests.cs                                               # Turn order, combat actions
│           ├── DamageSystemTests.cs                                         # Damage formulas & element tests
│           ├── InventoryAndEquipmentTests.cs                                # 89 Items, affix rolls, upgrade levels
│           ├── StatusEffectTests.cs                                         # DoTs, cleanses, mental effects
│           └── TalentSystemTests.cs                                         # Milestone slots, flexible talent trees
```

---

## 4. Test Suite Execution & Verification

The test suite is verified directly inside the running Unity Editor using `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` via menu item `Tools/Run All Tests`.

**Current Verification Output:**
```text
======================================================
TEST RUN RESULT: PASSED (100% Pass Rate)
Total Tests: 394
Passed: 394
Failed: 0
Inconclusive: 0
Skipped: 0
======================================================
All 394 EditMode tests in 2DObscure passed with 0 failures!
```

---

## 5. Agent Security & Git Commit Guidelines (`AGENTS.md`)

All AI agents operating on this repository **MUST STRICTLY COMPLY** with [`AGENTS.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/AGENTS.md):
1. **NO AUTO-COMMIT:** Never run `git commit` or `git push` autonomously. Wait for explicit user instruction (e.g., *"tolong commit"*, *"commit perubahan ini"*).
2. **WORKING TREE PRESERVATION:** All modified and newly generated code must remain in the working tree (unstaged/uncommitted) so the user can review before committing.
3. **NEVER STAGE SENSITIVE OR TEMPORARY FILES:** Never stage `.gemini/`, `.claude/`, `TestResults.txt`, `TestResults.xml`, `Temp/`, `Library/`, or environment files.
4. **NO WILDCARD STAGING:** When committing upon user request, stage files selectively one-by-one (`git add <file>`), never `git add .` or `git add -A`.
