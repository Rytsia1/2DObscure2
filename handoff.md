# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Updated:** 2026-11-05 (GDD Terbaru v1.5 Realignment, 89 Items & Cursed Equipment, 31 Layer 1 Relics with Demerits & Permanent Progression, Dynamic Initiative & Enemy Scaling)  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** **277 / 277 EditMode Tests Passing (100% Pass Rate, 0 Failures)**  
**Working Tree State:** All changes uncommitted in working tree per `AGENTS.md` guidelines.

---

## 1. Executive Summary & Project Core

`2DObscure` is a dark-fantasy, pixel-art, party-based, turn-based roguelike RPG built in Unity.
The core loop consists of assembling a 4-hero party, traversing 8 sequential dungeons of increasing peril, engaging in timeline-driven turn-based combat with interactive QTE strike mechanics, managing equipment and status effects, and building characters up to level 15 before confronting the final Abyss encounter.

### Established High-Level Design Constraints
- **Party Size:** Exactly 4 playable characters.
- **Level Cap:** Maximum character level is 15 (ASI points awarded at levels 4, 8, 12, and 14).
- **Dungeon Flow:** 8 sequential dungeons (no backward selection, run failure resets all progress and gold, returning to Main Menu).
- **Save Architecture:** 4 distinct save slots; saving permitted only at Camps or Node completion. Model C persistence preserves inventory equipment and equipped hero gear across runs.
- **Races & Classes:** 7 Playable Races with signature Racial Feats and strict +2 attribute point budgets; 9 Playable Classes with signature Starter Passives and Strict Weapon Compatibility.
- **Combat & Damage:** Consolidated Damage System v1.0 (Physical, Elemental, Almighty — no "Magical" category; Nature is Druid-themed Elemental damage).
- **Point Buy:** 27-point budget, 6 Primary Ability Scores (STR, DEX, VIT, INT, WIS, CHA) ranging 8–15 prior to racial modifiers.
- **UI Architecture:** Custom pure sprite-based pixel UI rendered top-left at $320 \times 180$ resolution ($\text{PPU} = 1$), driven by [`PixelFont.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs), [`Ui.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs), and dedicated screen controllers.

---

## 2. Recent Major Milestone: GDD Terbaru v1.5, Items, & Relic Revisions (Nov 2026)

All previous documentation was consolidated and updated to match the latest authoritative design documents:
- Master GDD: [`Assets/Docs/GDD_Terbaru.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Docs/GDD_Terbaru.md) (453KB authoritative GDD).
- Master Spreadsheets:
  - `Assets/Docs/Felna_Items_and_Equipment_Catalogue_v2_Cursed_Equipment.xlsx` (89 Equipment pieces including 17 Cursed Equipment, 20 Consumables, 12 Materials, 10 Key Items).
  - `Assets/Docs/Felna_Layer1_Relic_Framework_31_Relics_No_Mourners_Coin.xlsx` (31 Layer 1 Relics with Demerit mechanics & Magic Shop pricing; R007 Mourner's Coin removed).
  - `Assets/Docs/Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_7(1).xlsx` (Linear tier-weighted enemy stat growth & dynamic turn order).

### Key Systems Implemented & Revised:
1. **Item & Equipment Catalogue Expansion (89 Equipment Items):**
   - 72 Standard Equipment items + 17 Cursed Equipment items (`EQ073`–`EQ089`).
   - Cursed Equipment system: Paired `Boon` and `Curse` active only while equipped (`EquipmentTrait.Cursed`, `IsCursed`, combat recoil, MP cost surcharges, healing received penalties, and amplified damage).
   - 20 Consumables, 12 Materials, 10 Key Items.
2. **Relic System Overhaul (31 Layer 1 Relics):**
   - Relics `R001`–`R032` (skipping `R007` Mourner's Coin, which was deleted).
   - Rebalanced numeric values for `R001`–`R022` (e.g. Ashen Signet +5%, Soulglass Shard 2% Max MP, Coffin Nail +7%, Zoroa's Dull Crown 3 kills -> +3%).
   - Added Demerit Relics `R023`–`R032` with paired upside and downside mechanics (e.g., Crimson Testament +18% dmg / +12% dmg taken, Arcanum Unbound +25% skill dmg / +20% MP cost, The Empty Chalice -30% MP cost / -25% healing received).
   - **Permanent Player Progression (Bab 14 §2):** Relics persist across dungeon transitions (`AdvanceToNextDungeon`) and across run wipes (`LoseCombat`).
   - **Relic Replacement Mechanism:** When at capacity (5/5), player can replace an existing relic (`ReplaceRelic`).
3. **Linear Tier-Weighted Enemy Stat Scaling (Bab 10 §7):**
   - Normal, Elite, and Boss growth rates across Dungeons 1–8:
     $$\text{EffectiveStat} = \lfloor \text{BaseStat} \times (1 + \text{GrowthRate} \times (\text{Dungeon} - 1)) \rfloor$$
4. **Dynamic Initiative Queue & Effective Speed Formula (Bab 10 §8):**
   $$\text{SpeedModifier} = \text{clamp}(1 + \text{TotalSpeedBonus} - \text{TotalSpeedPenalty}, 0.50, 1.50)$$
   $$\text{EffectiveSPD} = \max(1, \lfloor \text{BaseSPD} \times \text{SpeedModifier} \rfloor)$$
5. **Control Effect Non-Stacking & Control Immunity (Bab 11 §8.1):**
   - Control Effects (`Stun`, `Sleep`, `Mind Control`, `Madness`, `Silence`) cannot stack or refresh duration while already active.
   - Grants 1-turn Control Immunity to that specific status upon expiration or cleansing.
6. **Talent Respec via Scroll of Emptiness (Bab 9 §5):**
   - Rare consumable usable outside combat to reset and refund allocated talent slots.

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
│   │   ├── Data/
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
│   │   │   └── RunState.cs                                                  # Run loop, node map, enemy stat scaling, party persistence
│   │   └── Screens/
│   │       ├── InventoryScreen.cs                                           # Inventory UI
│   │       ├── MapScreen.cs                                                 # Node exploration UI
│   │       └── BattleScreen.cs                                              # Combat UI & timeline
│   └── Tests/
│       └── Editor/
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

The test suite is verified directly inside the running Unity Editor using `UnityEditor.TestTools.TestRunner.Api.TestRunnerApi` (or via batchmode EditMode test runner).

**Current Verification Output:**
```text
======================================================
TEST RUN RESULT: PASSED (100% Pass Rate)
Total Tests: 277
Passed: 277
Failed: 0
Inconclusive: 0
Skipped: 0
Duration: 2.41s
======================================================
All 277 EditMode tests in 2DObscure passed with 0 failures!
```

---

## 5. Agent Security & Git Commit Guidelines (`AGENTS.md`)

All AI agents operating on this repository **MUST STRICTLY COMPLY** with [`AGENTS.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/AGENTS.md):
1. **NO AUTO-COMMIT:** Never run `git commit` or `git push` autonomously. Wait for explicit user instruction (e.g., *"tolong commit"*, *"commit perubahan ini"*).
2. **WORKING TREE PRESERVATION:** All modified and newly generated code must remain in the working tree (unstaged/uncommitted) so the user can review before committing.
3. **NEVER STAGE SENSITIVE OR TEMPORARY FILES:** Never stage `.gemini/`, `.claude/`, `TestResults.txt`, `TestResults.xml`, `Temp/`, `Library/`, or environment files.
4. **NO WILDCARD STAGING:** When committing upon user request, stage files selectively one-by-one (`git add <file>`), never `git add .` or `git add -A`.
