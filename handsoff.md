# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Updated:** 2026-11-05 (Felna Save File System & UI Implementation, 5 Save Slots, Hub-Only Saving, Dungeon Commitment Boundary, 292 Passing Tests)  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** **292 / 292 EditMode Tests Passing (100% Pass Rate, 0 Failures)**  
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
- **UI Architecture:** Custom pure sprite-based pixel UI rendered top-left at $640 \times 360$ reference resolution ($\text{PPU} = 1$), driven by [`PixelFont.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs), [`Ui.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs), and dedicated screen controllers.

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
   - `ConfirmationModal`: Reusable modal dialog for Overwrite, Delete, Load, and Abandon operations.
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

---

## 3. Directory Structure & Key Files

```text
2DObscure/
├── AGENTS.md                                                                # Strict agent security & git commit guidelines (NO AUTO-COMMIT)
├── handoff.md                                                               # Project handoff documentation
├── handsoff.md                                                              # Legacy mirror of handoff documentation
├── Assets/
│   ├── Docs/
│   │   ├── GDD_Terbaru.md                                                   # Master Game Design Document (v1.5, Nov 2026)
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
│   │       └── BattleScreen.cs                                              # Combat UI & timeline
│   └── Tests/
│       └── Editor/
│           ├── SaveSystemTests.cs                                           # 15 Acceptance criteria tests for Save/Load System & UI
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
Total Tests: 292
Passed: 292
Failed: 0
Inconclusive: 0
Skipped: 0
Duration: 2.68s
======================================================
All 292 EditMode tests in 2DObscure passed with 0 failures!
```

---

## 5. Agent Security & Git Commit Guidelines (`AGENTS.md`)

All AI agents operating on this repository **MUST STRICTLY COMPLY** with [`AGENTS.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/AGENTS.md):
1. **NO AUTO-COMMIT:** Never run `git commit` or `git push` autonomously. Wait for explicit user instruction (e.g., *"tolong commit"*, *"commit perubahan ini"*).
2. **WORKING TREE PRESERVATION:** All modified and newly generated code must remain in the working tree (unstaged/uncommitted) so the user can review before committing.
3. **NEVER STAGE SENSITIVE OR TEMPORARY FILES:** Never stage `.gemini/`, `.claude/`, `TestResults.txt`, `TestResults.xml`, `Temp/`, `Library/`, or environment files.
4. **NO WILDCARD STAGING:** When committing upon user request, stage files selectively one-by-one (`git add <file>`), never `git add .` or `git add -A`.
