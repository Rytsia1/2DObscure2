# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Generated:** 2026-10-30  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** 122 / 122 EditMode Tests Passing (100% Pass Rate)

---

## 1. Executive Summary & Project Core

`2DObscure` is a dark-fantasy, pixel-art, party-based, turn-based roguelike RPG built in Unity.
The core loop consists of assembling a 4-hero party, traversing 8 sequential dungeons of increasing peril, engaging in timeline-driven turn-based combat with interactive QTE strike mechanics, managing equipment and status effects, and building characters up to level 15 before confronting the final Abyss encounter.

### Established High-Level Design Constraints
- **Party Size:** Exactly 4 playable characters.
- **Level Cap:** Maximum character level is 15.
- **Dungeon Flow:** 8 sequential dungeons (no backward selection, run failure resets all progress and gold, returning to Main Menu).
- **Save Slots:** 4 distinct save slots; saving permitted only at Camps or Node completion.
- **Races & Classes:** 7 Playable Races with signature Racial Feats; 9 Playable Classes with signature Starter Passives and Strict Weapon Compatibility.
- **Combat & Damage:** Consolidated Damage System v1.0 (Physical, Elemental, Almighty — no "Magical" category; Nature is Druid-themed Elemental damage).
- **Point Buy:** 27-point budget, 6 Primary Ability Scores (STR, DEX, VIT, INT, WIS, CHA) ranging 8–15 prior to racial modifiers.
- **UI Architecture:** Custom pure sprite-based pixel UI rendered top-left at $320 \times 180$ resolution ($\text{PPU} = 1$), driven by [`PixelFont`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs), [`Ui.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs), and dedicated screen controllers.

---

## 2. Directory Structure & Key Files

```text
2DObscure/
├── Assets/
│   ├── Docs/
│   │   ├── Game_Design_Document_Draft_v0.1.md         # Baseline Game Design Document
│   │   ├── GDD_Visual_Direction_v0.2.md               # Visual, UI & Resolution Specifications
│   │   ├── Consolidated_Damage_Status_Effect_GDD_v1.0.md # Authoritative Damage & Status Rules
│   │   └── UNITY_IMPLEMENTATION_AUDIT.md             # Complete 23-System Technical Audit
│   ├── Scripts/
│   │   ├── Audio/
│   │   │   ├── MusicLibrary.cs                       # Track registries & music metadata
│   │   │   ├── MusicManager.cs                       # AudioSource cross-fade controller
│   │   │   └── SoundEffects.cs                       # Procedural & clip sound effect manager
│   │   ├── Combat/
│   │   │   ├── Battle.cs                             # Combat state machine & turn loop
│   │   │   ├── BattleAction.cs                       # Action execution, QTE integration & logging
│   │   │   ├── Combatant.cs                          # Combat wrapper around Unit (stats, statuses)
│   │   │   ├── DamageCalculator.cs                   # Authoritative damage calculation pipeline
│   │   │   ├── DamageResult.cs                       # Immutable damage outcome container
│   │   │   ├── QteSystem.cs                          # Timing windows, evaluation & input
│   │   │   ├── Status.cs                             # Status enum definitions & baseline stats
│   │   │   └── Status/
│   │   │       ├── StatusController.cs               # Per-combatant status manager
│   │   │       ├── StatusEffectInstance.cs           # Active status state & tick execution
│   │   │       └── StatusResistanceProfile.cs        # Crowd control & status resistances
│   │   ├── Core/
│   │   │   ├── Controls.cs                           # Unified Keyboard/Mouse input abstraction
│   │   │   ├── Game.cs                               # Global game director & scene/screen switcher
│   │   │   └── RunState.cs                           # Roguelike expedition run state & map tracking
│   │   ├── Data/
│   │   │   ├── CharacterCreation.cs                  # PointBuy, Session, Roster & NewGamePartyBuilder
│   │   │   ├── Database.cs                           # Static registries (Races, Classes, Skills, Items)
│   │   │   ├── Defs.cs                               # Core data records (CharacterDef, SkillDef, ItemDef)
│   │   │   ├── GameSave.cs                           # Persistence controller (JSON / PlayerPrefs)
│   │   │   └── StatsSystem.cs                        # AbilityScores, DerivedStats, EquipmentLoadout
│   │   ├── Render/
│   │   │   ├── Palette.cs                            # Retro 16-color fantasy palette
│   │   │   ├── PixelArt.cs                           # Procedural procedural bitmap & box generation
│   │   │   ├── PixelFont.cs                          # 5x7 bitmap font rendering & word fitting
│   │   │   └── Ui.cs                                 # Sprite UI primitives (Label, Button, Panel, Menu)
│   │   └── Screens/
│   │       ├── CharacterCreationScreen.cs            # Step 1-7 New Game 4-Hero Party Creator
│   │       ├── CharacterStatusScreen.cs              # Full party inspect & formula tooltips
│   │       ├── CombatScreen.cs                       # Side-view combat view, timeline & QTE prompts
│   │       ├── MainMenuScreen.cs                     # Main menu & opening prologue screen
│   │       └── MapScreen.cs                          # Node map navigation screen
│   └── Tests/
│       ├── Editor/
│       │   ├── BattleTests.cs                        # Combat turn & targeting tests
│       │   ├── CharacterStatsTests.cs                # Stats, racial feats, weapon compatibility (47 tests)
│       │   ├── DamageSystemTests.cs                  # Damage formulas, QTE multipliers, defense (29 tests)
│       │   ├── QteTests.cs                           # QTE precision, windows & assist mode
│       │   ├── RunStateTests.cs                      # Expedition persistence & node transitions
│       │   ├── StatusEffectTests.cs                  # Buffs, DoTs, HoTs, CC & cleansing (31 tests)
│       │   └── Step4Tests.cs                         # Legacy status integration regression suite
│       └── PlayMode/
│           ├── GameFlowTests.cs                      # End-to-end screen transitions & boot loop
│           └── MusicPlayModeTests.cs                 # Audio manager playback & fading
├── UNITY_IMPLEMENTATION_AUDIT.md                     # Root copy of the comprehensive 23-system audit
└── handoff.md                                        # This handoff briefing document
```

---

## 3. System Implementation Dashboard

The comprehensive codebase audit completed on 2026-10-30 evaluated 23 project subsystems against the approved GDD specifications.

| System ID | Subsystem Name | Status | Verified Test Count | Notes |
|:---|:---|:---:|:---:|:---|
| **SYS-01** | Six Primary Ability Scores | **IMPLEMENTED** | 12 tests | STR, DEX, VIT, INT, WIS, CHA with floor formula. |
| **SYS-02** | 27-Point Buy Character Creation | **IMPLEMENTED** | 8 tests | 8–15 range, progressive cost table ($8 \to 0, 15 \to 9$). |
| **SYS-03** | Save / Load & Persistence | **PARTIAL** | 4 tests | Functional save/load, but uses single key; missing 4 slots. |
| **SYS-04** | 7 Playable Races & Racial Feats | **IMPLEMENTED** | 7 tests | Human, Elf, Dwarf, Orc, Halfling, Dragonborn, Tiefling. |
| **SYS-05** | 9 Playable Classes & Identities | **IMPLEMENTED** | 9 tests | Knight, Barbarian, Rogue, Ranger, Mage, Cleric, Druid, Monk, Warlock. |
| **SYS-06** | Strict Weapon Compatibility | **IMPLEMENTED** | 9 tests | Validates 1H, 2H, dual-wielding, and weapon category rules. |
| **SYS-07** | 9 Class Skill Trees (225 Skills) | **PARTIAL** | 6 tests | Mechanics exist, but only 23 skills authored in Database. |
| **SYS-08** | Progression & Level 15 Cap | **PARTIAL** | 2 tests | `Unit.LevelUp()` exists, but lacks Level 15 clamp & combat EXP. |
| **SYS-09** | 15 Derived Stats & Recalculation | **IMPLEMENTED** | 14 tests | Authoritative recalculation pipeline in `Unit.Derived`. |
| **SYS-10** | Consolidated Damage System v1.0 | **IMPLEMENTED** | 29 tests | Physical, Elemental, Almighty, QTE mults, Affinities, Def. |
| **SYS-11** | Status Effect System v1.0 | **IMPLEMENTED** | 31 tests | Buffs, Debuffs, DoTs, HoTs, CC, Mental, Pact, Dynamic DoT. |
| **SYS-12** | QTE Combat Execution System | **IMPLEMENTED** | 12 tests | Miss, Bad, Good, Perfect, Critical with accessibility toggle. |
| **SYS-13** | Break & Stagger Mechanics | **IMPLEMENTED** | 6 tests | Physical/Magic break meters, Staggered state, recovery. |
| **SYS-14** | Turn-Order Timeline & Initiative | **IMPLEMENTED** | 5 tests | Initiative rolls, dynamic timeline, speed buffs/debuffs. |
| **SYS-15** | Roguelike Node Map Generation | **IMPLEMENTED** | 8 tests | Seeded DAG generation, node branching, floor traversal. |
| **SYS-16** | Combat Encounter Flow | **IMPLEMENTED** | 10 tests | Combatant initialization, action turns, victory/defeat. |
| **SYS-17** | Character Status UI | **IMPLEMENTED** | 4 tests | Stat inspection, formula tooltips, party hero switching. |
| **SYS-18** | Character Creation UI (New Game) | **IMPLEMENTED** | 11 tests | 7-step party creation, templates, randomizer, review mode. |
| **SYS-19** | Audio & Sound Effects System | **IMPLEMENTED** | 6 tests | Procedural synth SFX, audio clips, background music fading. |
| **SYS-20** | Settings & Accessibility | **IMPLEMENTED** | 4 tests | Volume sliders, fullscreen toggle, QTE Auto-Success assist. |
| **SYS-21** | Shop System v1.0 | **NOT IMPLEMENTED**| 0 tests | **Missing entirely**: no shop node, inventory, or UI. |
| **SYS-22** | Rest Site System | **PARTIAL** | 2 tests | Basic heal/stamina node exists; missing camp save triggers. |
| **SYS-23** | 8 Sequential Dungeons | **PARTIAL** | 2 tests | Only Act 1 (Floors 1–8) generated; no Act 2–8 progression. |

---

## 4. Key Architectural Patterns & Conventions

### 4.1 Data vs. Runtime Separation
- **`CharacterDef`**: Immutable persistent blueprint of a hero (Base ability scores, race, class, starter passive, equipped items, level, exp).
- **`Unit`**: Mutable runtime representation of an entity, owning authoritative stat recalculation (`unit.Recalculate()`) and primary scores.
- **`Combatant`**: Battle-scoped wrapper encapsulating `Unit`, managing temporary battle buffs, shields, break meters, and `StatusController`.
- **`RunState`**: Active expedition context tracking the node map, current floor, dungeon seed, temporary party gold, and party roster.

### 4.2 Stats Recalculation Flow
Primary Ability Scores strictly follow:
$$\text{FinalScore} = \text{Base} + \text{Racial} + \text{Permanent} + \text{Equipment} + \text{Temporary}$$
$$\text{AbilityModifier} = \lfloor(\text{FinalScore} - 10) / 2\rfloor \quad (\text{using mathematical floor})$$

Derived Stats are recalculated through `DerivedStats.Calculate(unit)`:
- $\text{MaxHP} = \text{BaseHP} + (\text{VIT\_Mod} \times 10) + \text{EqHP}$
- $\text{MaxMP} = \text{BaseMP} + (\text{INT\_Mod} \times 5) + \text{EqMP}$
- $\text{PhysicalPower} = \text{BaseAtk} + (\text{STR\_Mod} \times 2) + \text{WeaponPower}$
- $\text{PhysicalDefense} = \text{BaseDef} + (\text{VIT\_Mod} \times 2) + \text{ArmorDef}$
- $\text{MagicDefense} = \text{BaseMDef} + (\text{WIS\_Mod} \times 2) + \text{ArmorMDef}$
- $\text{Initiative} = \text{BaseIni} + \text{DEX\_Mod}$

### 4.3 Combat Damage Pipeline
Calculated strictly in [`DamageCalculator.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Combat/DamageCalculator.cs):
1. **Base Damage:** $\text{SkillPower} \times (1 + \text{ScalingFactor} \times \text{PrimaryMod})$.
2. **QTE Multiplier:** Miss ($0.0\times$), Bad ($0.5\times$), Good ($1.0\times$), Perfect ($1.25\times$), Critical ($1.5\times$).
3. **Critical Hit Roll:** Rolled if QTE is Good or higher; grants $+50\%$ damage and bypasses $20\%$ defense.
4. **Affinity Multipliers:** Weak ($1.5\times$), Normal ($1.0\times$), Resist ($0.5\times$), Immune ($0.0\times$), Absorb ($-1.0\times$).
5. **Defense Mitigation:** $\text{Damage} = \text{RawDamage} \times \frac{100}{100 + \text{EffectiveDefense}}$.

### 4.4 Status Effect Lifecycle
Managed in [`StatusController.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Combat/Status/StatusController.cs):
- **Application:** Checked against `StatusResistanceProfile` (status resistance, immunity).
- **Turn Start:** Processes DoTs (`Poison`, `Burn`, `Bleed`, `Shock`, `DynamicDoT`) and HoTs (`Regen`).
- **Turn End:** Decrements remaining turn durations; expires statuses whose duration reaches 0.
- **Cleansing:** `Cleanse()` removes all Debuffs, Mental, and Crowd Control effects while preserving Buffs and Pacts.

### 4.5 UI & Font Rendering Standards
- **Resolution:** Virtual top-left pixel space $320 \times 180$ (`Ui.Width`, `Ui.Height`), with `PixelPerfectCamera` snapping.
- **Font:** Custom $5 \times 7$ baked glyph font in [`PixelFont.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs).
- **String Fitting:** All labels with limited bounding boxes MUST use `PixelFont.Fit(text, maxWidth)` to prevent horizontal overflow.
- **Button Focus:** Buttons feature dynamic borders (`Palette.Gold` when focused, `Palette.Shade` when idle). Horizontal button bars use `showCursor: false` in [`Ui.Button`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs#L42) to avoid overlapping adjacent buttons.

---

## 5. Work Completed in Recent Sessions

1. **Test Suite Stabilization:**
   - Fixed nullable DoT scaling in [`Status.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Combat/Status.cs).
   - Fixed un-buffing on status list manipulation in [`Combatant.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Combat/Combatant.cs).
   - Corrected base stat assertions and racial modifier handling in [`StatusEffectTests.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Tests/Editor/StatusEffectTests.cs).
   - Achieved 100% pass rate across all 122 EditMode tests.
2. **Comprehensive Technical Audit:**
   - Conducted an exhaustive review of all 23 subsystems against approved GDDs.
   - Authored and placed [`UNITY_IMPLEMENTATION_AUDIT.md`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/UNITY_IMPLEMENTATION_AUDIT.md) in the project root.
3. **Character Creation UI Refactor:**
   - Fixed class label left-truncation (`ASS < FIGHTER...`) by relocating class gameplay identity, racial feat, and starter passive into dedicated display rows in the right panel and enforcing 150px string clamping in `Selector()`.
   - Fixed cursor overlap (`[NEW] > [ADD HERO]`) by introducing `bool showCursor = true` in `Ui.Button` and disabling external cursor rendering on horizontal button bars.
   - Fixed bottom tooltip text clipping using `PixelFont.Fit(..., 308)`.
   - Fixed roster pre-population so a New Game cleanly starts with `PARTY (0/4): CREATE 4 HEROES TO BEGIN`.

---

## 6. Priority Roadmap for the Next Session

### Phase 1: Core Loop & Foundation Integrity (Immediate Next Steps)
- **Task 1: Multi-Slot Save Architecture (SYS-03)**
  - Expand [`GameSave.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Data/GameSave.cs) from a single `"obscure.save"` key to 4 independent slots (`"obscure.save.slot.0"` through `"obscure.save.slot.3"`).
  - Create a Save Slot Selection / Management UI in Main Menu and Pause/Settings.
  - Enforce saving only at Camp nodes and between dungeon floor transitions.
- **Task 2: 8 Sequential Dungeons Progression (SYS-23)**
  - Update [`RunState.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Core/RunState.cs) to generate and advance through Acts 1 through 8 sequentially.
  - Implement dungeon completion transitions (carry over remaining gold, reset temporary buffs, advance to the next dungeon theme).
  - Enforce permadeath: dungeon defeat wipes run state and temporary gold, returning the player to the Main Menu.

### Phase 2: Missing Core Gameplay Systems
- **Task 3: Shop System v1.0 (SYS-21)**
  - Introduce `NodeType.Shop` into `NodeDef` and `MapGenerator`.
  - Create `ShopInventory` supporting consumables, weapons, armor, and accessories.
  - Implement purchasing (deducting temporary run gold), selling (returning $50\%$ gold value), and restock mechanics.
  - Create `ShopScreen.cs` conforming to the $320 \times 180$ pixel art UI standard.
- **Task 4: Combat EXP & Level 15 Progression (SYS-08)**
  - Award EXP to living party members upon winning a battle encounter.
  - Clamp character level progression strictly to Level 15.
  - Award Skill Points (SP) upon leveling up and create a skill upgrade allocation screen.

### Phase 3: Content Expansion
- **Task 5: Author Full Class Skill Trees (SYS-07)**
  - Expand [`Database.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Data/Database.cs) from the current 23 skills to the full 225 player skills (25 skills $\times$ 9 classes across Tiers 1–5).

---

## 7. How to Run Tests & Verify Code

Because Unity Editor runs in the background, EditMode tests are executed via `unity-mcp` using the `Unity_RunCommand` tool:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor
{
    internal class CommandScript : IRunCommand
    {
        public void Execute(ExecutionResult result)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter { testMode = TestMode.EditMode };
            api.Execute(new ExecutionSettings(filter) { runSynchronously = true });
            result.Log("Triggered synchronous EditMode test run");
        }
    }
}
```

Test results are logged to the Unity Console and saved to:  
`C:/Users/Dios/AppData/LocalLow/DefaultCompany/2DObscure/TestResults.xml`

---

## 8. Open Design Questions Requiring User Confirmation

Before beginning implementation on Phase 1 and 2, confirm the following rules with the designer:
1. **Inter-Dungeon Recovery:** Does clearing a dungeon grant a full party HP/MP heal and purge all debuffs, or is HP/MP preserved between dungeons?
2. **Shop Economy:** What are the exact base price tables for items and the formula for rerolling shop stock?
3. **EXP Curve:** What is the exact formula for EXP required per level from Level 1 to 15 (e.g., linear vs. polynomial) and the base EXP awarded per enemy type?
4. **Target Resolution:** Should new screens remain strictly in $320 \times 180$ ($\text{PPU} = 1$), or should the project begin migrating toward the $640 \times 360$ ($\text{PPU} = 32$) standard described in Visual Direction GDD v0.2?
