# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Updated:** 2026-11-04 (Post-GDD Terbaru Realignment, Dungeon 1 Items & Layer 1 Relics Implementation)  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** **269 / 269 EditMode Tests Passing (100% Pass Rate, 0 Failures)**  
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

## 2. Recent Major Milestone: GDD Terbaru Audit & Alignment (Nov 2026)

All previous documentation was consolidated and superseded by the master design document:
- Master GDD: [`Assets/Docs/GDD_Terbaru.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Docs/GDD_Terbaru.md) (389KB authoritative GDD).
- Master Spreadsheets:
  - `Assets/Docs/Felna_Items_and_Equipment_Catalogue.xlsx`
  - `Assets/Docs/Felna_Layer1_Relic_List.xlsx`
  - `Assets/Docs/Felna_Dungeon_Modifier_List.xlsx`

### Scope Boundary Notice
Per explicit user instruction: **"Jangan implementasi list item, relic, ataupun modifier dulu"** (Do not implement item, relic, or modifier catalogues yet).
The scope of this milestone focused exclusively on auditing all core mechanics, combat formulas, QTE rules, status effects, and race/class systems against the new GDD, resolving discrepancies, and bringing the full EditMode test suite to 100% green.

A detailed discrepancy report is archived at:  
[`audit_report_gdd_terbaru.md`](file:///C:/Users/Dios/.gemini/antigravity/brain/054b8c94-0b06-4c35-b278-038b9c39ffe8/audit_report_gdd_terbaru.md)

---

## 3. Directory Structure & Key Files

```text
2DObscure/
├── AGENTS.md                                         # Strict agent security & git commit guidelines (NO AUTO-COMMIT)
├── handsoff.md                                       # This authoritative handoff document
├── Assets/
│   ├── Docs/
│   │   ├── GDD_Terbaru.md                            # Master Game Design Document (Nov 2026)
│   │   ├── Felna_Items_and_Equipment_Catalogue.xlsx  # Authoritative Item & Equipment Catalogue
│   │   ├── Felna_Layer1_Relic_List.xlsx              # Authoritative Relic Catalogue
│   │   └── Felna_Dungeon_Modifier_List.xlsx          # Authoritative Dungeon Modifier Catalogue
│   ├── Scripts/
│   │   ├── Audio/
│   │   │   ├── MusicLibrary.cs                       # Track registries & music metadata
│   │   │   ├── MusicManager.cs                       # AudioSource cross-fade controller
│   │   │   └── SoundEffects.cs                       # Procedural & clip sound effect manager
│   │   ├── Combat/
│   │   │   ├── Battle.cs                             # Combat state machine, turn loop, feat ticks & redirection
│   │   │   ├── BattleAction.cs                       # Action execution, QTE damage scaling & logging
│   │   │   ├── Combatant.cs                          # Combat wrapper around Unit (stats, feats, break, combo)
│   │   │   ├── Elements.cs                           # Element enum (Physical, Fire, Ice, Lightning, Arcane, etc.)
│   │   │   ├── Damage/
│   │   │   │   ├── DamageCalculator.cs               # Authoritative damage calculation pipeline
│   │   │   │   ├── DamageContext.cs                  # Damage context flags (isFinisher, attacker/target mods)
│   │   │   │   ├── DamageModifier.cs                 # DamageModifierSet, multiplicative stacking & scopes
│   │   │   │   ├── DamageResult.cs                   # Immutable damage outcome container
│   │   │   │   └── ElementType.cs                    # ElementType enum & helper mappings
│   │   │   └── Status/
│   │   │       ├── Status.cs                         # Status enum definitions & baseline stats
│   │   │       ├── StatusController.cs               # Per-combatant status manager & resistances
│   │   │       ├── StatusEffectInstance.cs           # Active status state & tick execution
│   │   │       └── StatusResistanceProfile.cs        # Crowd control & status resistances
│   │   ├── Core/
│   │   │   ├── Controls.cs                           # Unified Keyboard/Mouse input abstraction
│   │   │   ├── Game.cs                               # Global game director & scene/screen switcher
│   │   │   └── RunState.cs                           # Roguelike expedition run state & map tracking
│   │   ├── Data/
│   │   │   ├── CharacterCreation.cs                  # PointBuy, Session, Roster & NewGamePartyBuilder
│   │   │   ├── Database.cs                           # Static registries (Races, Lineages, Classes, Skills, Items)
│   │   │   ├── Defs.cs                               # Core data records (CharacterDef, SkillDef, ItemDef, etc.)
│   │   │   ├── DifficultySystem.cs                   # Adaptive & fixed difficulty scaling
│   │   │   ├── GameSave.cs                           # Persistence controller (JSON / PlayerPrefs)
│   │   │   ├── InventorySystem.cs                    # InventoryRuntime, EquipmentInstance, Affixes, Rarities
│   │   │   ├── ShopSystem.cs                         # Shop generation, purchasing, selling & restocking
│   │   │   └── StatsSystem.cs                        # AbilityScores, DerivedStats, EquipmentLoadout
│   │   ├── Qte/
│   │   │   ├── Qte.cs                                # TimingQte, DefenseQte, TypingQte, AutoQte & SecondChance
│   │   │   └── QteConfig.cs                          # Profiles, timing windows, multipliers (1.5x / 1.0x / 0.5x)
│   │   ├── Render/
│   │   │   ├── Palette.cs                            # Retro 16-color fantasy palette
│   │   │   ├── PixelArt.cs                           # Procedural procedural bitmap & box generation
│   │   │   ├── PixelFont.cs                          # 5x7 bitmap font rendering & word fitting
│   │   │   └── Ui.cs                                 # Sprite UI primitives (Label, Button, Panel, Menu)
│   │   └── Screens/
│   │       ├── CharacterCreationScreen.cs            # Step 1-7 New Game 4-Hero Party Creator
│   │       ├── CharacterStatusScreen.cs              # Full party inspect & formula tooltips
│   │       ├── CombatScreen.cs                       # Side-view combat view, timeline & QTE prompts
│   │       ├── EquipmentScreen.cs                    # 5 core slots, equipment picker & live delta comparison
│   │       ├── HubScreen.cs                          # Expedition hub / camp screen
│   │       ├── InventoryScreen.cs                    # 4 tabs (Equipment, Consumables, Materials, Key Items)
│   │       ├── MainMenuScreen.cs                     # Main menu & opening prologue screen
│   │       ├── MapScreen.cs                          # Node map navigation screen
│   │       ├── QtePanel.cs                           # Visual QTE track, gold zone & result labels (PERFECT/GOOD/POOR)
│   │       ├── RewardScreen.cs                       # Loot & victory reward distribution
│   │       ├── ShopScreen.cs                         # Shop merchant UI
│   │       └── SkillTreeScreen.cs                    # Class skill tree & active/passive loadout UI
│   └── Tests/
│       ├── Editor/
│       │   ├── BattleTests.cs                        # Combat turn & targeting tests
│       │   ├── CharacterStatsTests.cs                # Stats, racial feats, weapon compatibility, GDD tests (54 tests)
│       │   ├── DamageSystemTests.cs                  # Damage formulas, QTE multipliers, defense (29 tests)
│       │   ├── DifficultySystemTests.cs              # Difficulty curve & modifier tests
│       │   ├── InventoryAndEquipmentTests.cs         # 6 Rarities, affixes, upgrades, sell formula, persistence
│       │   ├── QteTests.cs                           # QTE precision, windows & assist mode
│       │   ├── RunStateTests.cs                      # Expedition persistence & node transitions
│       │   ├── ShopSystemTests.cs                    # Shop generation, purchasing, selling & restocking
│       │   ├── StatusEffectTests.cs                  # Buffs, DoTs, HoTs, CC & cleansing (31 tests)
│       │   ├── Step4Tests.cs                         # Status integration regression suite
│       │   └── TalentSystemTests.cs                  # Talent unlock & stat bonus tests
│       └── PlayMode/
│           ├── GameFlowTests.cs                      # End-to-end screen transitions & boot loop
│           └── MusicPlayModeTests.cs                 # Audio manager playback & fading
```

---

## 4. Subsystem Implementation Status Dashboard

The comprehensive codebase audit evaluates 24 core subsystems against [`GDD_Terbaru.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Docs/GDD_Terbaru.md):

| System ID | Subsystem Name | GDD Reference | Status | Verified Test Count | Notes |
|:---|:---|:---|:---:|:---:|:---|
| **SYS-01** | Six Primary Ability Scores | Bab 5 §1 | **IMPLEMENTED** | 12 tests | STR, DEX, VIT, INT, WIS, CHA with mathematical floor modifier formula. |
| **SYS-02** | 27-Point Buy Character Creation | Bab 7 §1 | **IMPLEMENTED** | 8 tests | 8–15 range, progressive cost table ($8 \to 0, 15 \to 9$). |
| **SYS-03** | Save / Load & Persistence | Bab 19 | **IMPLEMENTED** | 5 tests | Full save/load of Roster, RunState, and Model C Persistent Equipment. |
| **SYS-04** | 7 Playable Races, Lineages & Feats | Bab 6 | **IMPLEMENTED** | 14 tests | Strict +2 points total; 4 Dragonborn Lineages; 7 active/passive feats. |
| **SYS-05** | 9 Playable Classes & Identities | Bab 8 | **IMPLEMENTED** | 9 tests | Fighter, Barbarian, Martial Artist, Rogue, Ranger, Cleric, Wizard, Warlock, Druid. |
| **SYS-06** | Strict Weapon Compatibility | Bab 8 §4 | **IMPLEMENTED** | 9 tests | Validates 1H, 2H, dual-wielding, and weapon category rules per class. |
| **SYS-07** | 9 Class Skill Trees (225 Skills) | Bab 8 §5 | **PARTIAL** | 6 tests | Progression & loadout mechanics fully functional; full 225 skill catalog pending. |
| **SYS-08** | Progression & Level 15 Cap | Bab 5 §3 | **IMPLEMENTED** | 4 tests | Level cap 15 clamped; ASI points awarded at levels 4, 8, 12, 14. |
| **SYS-09** | 15 Derived Stats & Recalculation | Bab 5 §2 | **IMPLEMENTED** | 14 tests | Authoritative recalculation pipeline in `Unit.Derived`. |
| **SYS-10** | Consolidated Damage System | Bab 11 | **IMPLEMENTED** | 29 tests | Physical, Elemental, Almighty. Finishers cannot crit; multi-hit & mitigation. |
| **SYS-11** | Status Effect System | Bab 11 §8-15 | **IMPLEMENTED** | 31 tests | Buffs, Debuffs, DoTs, HoTs, CC, Mental, Pact. Status rolls independent of QTE. |
| **SYS-12** | QTE Combat Execution System | Bab 12 | **IMPLEMENTED** | 15 tests | Perfect (1.5x / 0%), Good (1.0x / 50%), Poor (0.5x / 100%). Heavy parry 50%. |
| **SYS-13** | Break & Stagger Mechanics | Bab 10 | **IMPLEMENTED** | 8 tests | 100 base (200 boss); QTE scales break damage; 20 pt turn-start recovery; broken cap. |
| **SYS-14** | Turn-Order Timeline & Initiative | Bab 10 §2 | **IMPLEMENTED** | 5 tests | Initiative rolls, dynamic timeline, speed buffs/debuffs. |
| **SYS-15** | Roguelike Node Map Generation | Bab 14 §2 | **IMPLEMENTED** | 8 tests | Seeded DAG generation, node branching, floor traversal. |
| **SYS-16** | Combat Encounter Flow | Bab 10 | **IMPLEMENTED** | 10 tests | Combatant initialization, action turns, victory/defeat. |
| **SYS-17** | Character Status UI | Bab 18 §2 | **IMPLEMENTED** | 4 tests | Stat inspection, formula tooltips, party hero switching. |
| **SYS-18** | Character Creation UI (New Game) | Bab 7 & 18 | **IMPLEMENTED** | 11 tests | 7-step party creation, templates, randomizer, review mode. |
| **SYS-19** | Audio & Sound Effects System | Bab 17 | **IMPLEMENTED** | 6 tests | Procedural synth SFX, audio clips, background music fading. |
| **SYS-20** | Settings & Accessibility | Bab 12 §6 | **IMPLEMENTED** | 4 tests | Volume sliders, fullscreen toggle, QTE Auto-Success assist. |
| **SYS-21** | Shop System | Bab 15 | **IMPLEMENTED** | 8 tests | Shop node, dynamic inventory, buying, selling, restocking. |
| **SYS-22** | Rest Site / Camp System | Bab 14 §4 | **IMPLEMENTED** | 4 tests | Camp heal, stamina recovery, camp save triggers. |
| **SYS-23** | 8 Sequential Dungeons Progression | Bab 14 §1 | **PARTIAL** | 4 tests | Floor generation and transitions functional; full 8-dungeon encounter tables pending. |
| **SYS-24** | Inventory & Equipment System | Bab 13 | **IMPLEMENTED** | 11 tests | Model C: 6 Rarities, Affixes, Upgrades (+0 to +5), 100 slot cap, 5 core slots. |

---

## 5. Detailed Gameplay Mechanics Implemented

### 5.1 QTE System Alignment (Bab 12)
- **Terminology:** `QteResult.Miss` is officially renamed to `QteResult.Poor` (`Miss` preserved as alias). UI panels display `"POOR"` on failure.
- **Offensive Multipliers:**
  - `Perfect`: **1.5x** damage & break power.
  - `Good`: **1.0x** damage & break power.
  - `Poor`: **0.5x** damage & break power.
  - Pure heal, buff, and support skills are excluded from Offensive QTE.
- **Defensive Multipliers:**
  - `Perfect`: **0.0x** (100% damage negated).
  - `Heavy Attack Perfect`: **0.5x** (50% damage reduction; heavy attacks cannot be completely negated).
  - `Good`: **0.5x** (50% damage negated).
  - `Poor`: **1.0x** (full damage received).
- **Player-Only Scaling:** Ability scores (STR, DEX, VIT, etc.) no longer modify QTE timing windows or damage multipliers. Timing window bonuses come solely from equipment, talents, or passives and are capped at $+25\%$.

### 5.2 Race System & Racial Feats (Bab 6)
- **Attribute Budget:** Exactly $+2$ attribute points total across all 7 playable races:
  - **Human:** $+1$ STR, $+1$ VIT (Adaptive Potential).
  - **Elf:** $+2$ DEX (Elven Grace).
  - **Dwarf:** $+2$ VIT (Dwarven Resilience).
  - **Halfling:** $+2$ DEX (Nimble Folk).
  - **Dragonborn:** $+2$ STR (Crimson/Stone) or $+2$ CHA (Azure/Frost).
  - **Liichtian:** $+2$ INT (Celestial Glow).
  - **Merfolk:** $+2$ WIS (Aquatic Adaptation).
- **Dragonborn Lineages:** Crimson (Fire), Stone (Physical), Azure (Lightning), Frost (Ice).
- **Combat Logic for All 7 Feats:**
  - **Human (*Determination*):** Triggered automatically when alive and HP drops to $\le 30\%$ $\to$ grants $+20\%$ Max HP shield and $+15\%$ All Damage Bonus for 2 turns (1/battle).
  - **Elf (*Fey Step*):** Active; grants `IsPhased` until next turn (single-target enemy attacks automatically redirect to another active ally) and $+20\%$ damage on the first attack next turn (CD 4).
  - **Dwarf (*Iron Resolve*):** Active; grants $-30\%$ All Damage Taken and $-50\%$ incoming Break Power until next turn (CD 3).
  - **Halfling (*Second Chance*):** 2 retries per battle for failed QTEs (max 1 retry per action/turn; does not guarantee automatic success).
  - **Dragonborn (*Draconic Overdrive*):** Boosts All Damage $+35\%$, Lineage Element damage $+25\%$, Break Power $+50\%$, and counter damage $+25\%$ for 3 turns; followed by *Exhausted* ($-20\%$ damage, $-30\%$ break) for 2 turns (1/battle).
  - **Liichtian (*Lightbound*):** Heals $10\%$ Max HP to all active party members, reduces damage taken by $-10\%$, and grants $+20\%$ status resistance for 2 turns (1/battle).
  - **Merfolk (*Ocean's Embrace*):** Targets 1 ally to grant a $25\%$ Max HP shield and $+10\%$ defensive QTE window for 3 turns; heals $10\%$ Max HP if the shield breaks or expires (CD 4).

### 5.3 Break Gauge & Stun Mechanics (Bab 10)
- **Gauge Baselines:** 100 points for Normal and Elite enemies, 200 points for Bosses.
- **QTE Scaling:** Break damage dealt is scaled by the offensive QTE multiplier ($1.5\times / 1.0\times / 0.5\times$).
- **Turn Recovery:** Non-broken entities recover 20 Break gauge points at the start of their turn if damaged.
- **Broken State:** Broken entities cannot achieve `Perfect` on defensive QTEs (defensive reaction capped at `Good`).
- **Incapacitated Reactions:** Targets suffering from Stun, Sleep, or Mind Control are forced to `Poor` defense. Direct damage wakes up sleeping targets.

### 5.4 Class Mechanics & Finishers (Bab 8 & 11)
- **Finishers:** Skills tagged with `isFinisher = true` consume all accumulated Martial Artist combo points and can **never** critically hit (`ResolveCriticalHitForHit`).
- **Martial Artist Combo Flow:**
  - Good hit: $+1$ combo point.
  - Perfect hit: $+2$ combo points.
  - Poor hit: resets combo to 0.
  - Passing a turn without attacking resets combo to 0.
  - Grants $+2\%$ physical damage per active combo point.

---

## 6. How to Run Tests & Verify Code

Tests are executed using the official Unity Editor executable in batchmode:

```powershell
Start-Process "F:\Game Engine\Unity Engine\Editor\Unity.exe" `
  -ArgumentList "-batchmode -projectPath . -runTests -testPlatform EditMode -testResults Temp/TestResults.xml -logFile Temp/unity.log" `
  -Wait
```

### Parsing Test Results
```powershell
python -c "
import xml.etree.ElementTree as ET
import os

path = r'C:/Users/Dios/AppData/LocalLow/DefaultCompany/2DObscure/TestResults.xml'
if not os.path.exists(path): path = 'Temp/TestResults.xml'

tree = ET.parse(path)
root = tree.getroot()
print(f'Total: {root.attrib.get(\"total\")}, Passed: {root.attrib.get(\"passed\")}, Failed: {root.attrib.get(\"failed\")}')
"
```

**Current Verification Output:**
```text
Total: 269, Passed: 269, Failed: 0, Inconclusive: 0, Skipped: 0
=======================================
ALL 269 TESTS PASSED WITH 0 FAILURES!
=======================================
```

---

## 7. System Implementation Status & Roadmap

### Completed Systems & Content:
- **GDD Alignment:** Complete audit & mechanical realignment of combat, QTEs, damage formula, races/classes, abilities, break gauges, status effects.
- **Items & Equipment Catalogue (Dungeon 1):** Fully implemented 72 equipment pieces, 20 consumables, 12 materials, 10 key items in `CatalogueDefs.cs` and `CatalogueDatabase.cs`.
- **Relic System (Layer 1):** Fully implemented modular Relic System (`RelicManager.cs`, `RelicDefs.cs`, `RelicInstance.cs`, `CatalogueRelics.cs`) supporting all 22 approved Layer 1 relics (`R001`–`R022`) with the 5 Confirmed Relic Rules:
  1. *Relic Capacity:* Max 5 relics per party; 6th relic acquisition rejected with clear feedback.
  2. *Relic Ownership:* Party-wide ownership.
  3. *Duplicate Restrictions:* Strict duplicate rejection without stacking.
  4. *Dungeon Persistence:* Temporary dungeon-specific items; wiped on dungeon clear (`AdvanceToNextDungeon`), next dungeon starts empty.
  5. *Run Failure Reset:* Cleared on party wipe / run loss (`LoseCombat`, `ResetToNewGame`).

### Next Steps & Pending Milestones:
- **Phase 3: Dungeon Modifiers Catalog Import**
  - Parse `Assets/Docs/Felna_Dungeon_Modifier_List.xlsx`.
  - Implement dungeon modifier data structures and integrate with [`RunState.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Run/RunState.cs) and encounter generators.
- **Phase 4: Dungeon 1 Enemy Roster Integration**
  - Parse `Assets/Docs/Felna_Dungeon_1_Enemy_Roster.xlsx` and verify enemy skill sets, stats, and loot tables against Dungeon 1 items and relics.
- **Phase 5: Class Skill Library Expansion**
  - Author the remaining class skills from the GDD into [`Database.cs`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/Assets/Scripts/Data/Database.cs) to complete the 225-skill library across the 9 playable classes.

---

## 8. Agent Security & Git Commit Guidelines (`AGENTS.md`)

All AI agents operating on this repository **MUST STRICTLY COMPLY** with [`AGENTS.md`](file:///f:/Folder%20dio/Portofolio/2DObscure/2DObscure/AGENTS.md):
1. **NO AUTO-COMMIT:** Never run `git commit` or `git push` autonomously. Wait for explicit user instruction (e.g., *"tolong commit"*).
2. **WORKING TREE PRESERVATION:** All modified and newly generated code must remain in the working tree (unstaged/uncommitted) so the user can review before committing.
3. **NEVER STAGE SENSITIVE OR TEMPORARY FILES:** Never stage `.gemini/`, `.claude/`, `TestResults.txt`, `TestResults.xml`, `Temp/`, `Library/`, or environment files.
4. **NO WILDCARD STAGING:** When committing upon user request, stage files selectively one-by-one (`git add <file>`), never `git add .` or `git add -A`.
