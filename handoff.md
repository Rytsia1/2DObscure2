# PROJECT HANDOFF & AGENT ONBOARDING CONTEXT
**Project:** 2DObscure — 2D Turn-Based Roguelike RPG in Unity  
**Updated:** 2026-10-30  
**Target Engine:** Unity 6000.3.16f1 (URP 2D, Pixel Perfect Camera)  
**Primary Language:** C# (.NET Standard 2.1 / C# 9.0+)  
**Test Suite Status:** 133 / 133 EditMode Tests Passing (100% Pass Rate)

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
- **Inventory & Equipment System:** GDD v1.0 Model C (Persistent Equipment Retention upon Defeat, 100-slot equipment capacity, consumable stacks, 6 rarity tiers, randomized affixes, upgrading, and live stat comparison).
- **UI Architecture:** Custom pure sprite-based pixel UI rendered top-left at $320 \times 180$ resolution ($\text{PPU} = 1$), driven by [`PixelFont`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/PixelFont.cs), [`Ui.cs`](file:///f:/Folder%20dio/Dicoding/2DObscure/2DObscure/Assets/Scripts/Render/Ui.cs), and dedicated screen controllers.

---

## 2. Directory Structure & Key Files

```text
2DObscure/
├── Assets/
│   ├── Docs/
│   │   ├── Equipment and Inventory.md                 # Authoritative Equipment & Inventory GDD v1.0
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
│   │   │   ├── Database.cs                           # Static registries (Races, Classes, Skills, Items, Affixes, Effects)
│   │   │   ├── Defs.cs                               # Core data records (CharacterDef, SkillDef, ItemDef, EquipmentDef)
│   │   │   ├── GameSave.cs                           # Persistence controller (JSON / PlayerPrefs)
│   │   │   ├── InventorySystem.cs                    # AffixDef, SpecialEffectDef, EquipmentInstance, InventoryRuntime, LootGen
│   │   │   └── StatsSystem.cs                        # AbilityScores, DerivedStats, EquipmentLoadout, EquipmentRarity
│   │   ├── Render/
│   │   │   ├── Palette.cs                            # Retro 16-color fantasy palette
│   │   │   ├── PixelArt.cs                           # Procedural bitmap & box generation
│   │   │   ├── PixelFont.cs                          # 5x7 bitmap font rendering & word fitting
│   │   │   └── Ui.cs                                 # Sprite UI primitives (Label, Button, Panel, Menu)
│   │   └── Screens/
│   │       ├── CharacterCreationScreen.cs            # Step 1-7 New Game 4-Hero Party Creator
│   │       ├── CharacterStatusScreen.cs              # Full party inspect & formula tooltips
│   │       ├── CombatScreen.cs                       # Side-view combat view, timeline & QTE prompts
│   │       ├── EquipmentScreen.cs                    # 5-Slot Equipment screen with live delta preview
│   │       ├── InventoryScreen.cs                    # 4-Tab Inventory browser, pagination, sorting & actions
│   │       ├── MainMenuScreen.cs                     # Main menu & opening prologue screen
│   │       └── MapScreen.cs                          # Node map navigation screen
│   └── Tests/
│       ├── Editor/
│       │   ├── BattleTests.cs                        # Combat turn & targeting tests
│       │   ├── CharacterStatsTests.cs                # Stats, racial feats, weapon compatibility (47 tests)
│       │   ├── DamageSystemTests.cs                  # Damage formulas, QTE multipliers, defense (29 tests)
│       │   ├── InventoryAndEquipmentTests.cs         # Inventory & Equipment System validation (11 tests)
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

The comprehensive codebase audit completed on 2026-10-30 evaluated project subsystems against the approved GDD specifications.

| System ID | Subsystem Name | Status | Verified Test Count | Notes |
|:---|:---|:---:|:---:|:---|
| **SYS-01** | Six Primary Ability Scores | **IMPLEMENTED** | 12 tests | STR, DEX, VIT, INT, WIS, CHA with floor formula. |
| **SYS-02** | 27-Point Buy Character Creation | **IMPLEMENTED** | 8 tests | 8–15 range, progressive cost table ($8 \to 0, 15 \to 9$). |
| **SYS-03** | Save / Load & Persistence | **IMPLEMENTED** | 5 tests | Full save/load of Roster, RunState, and Model C Persistent Equipment. |
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
| **SYS-21** | Shop System v1.0 | **NOT IMPLEMENTED**| 0 tests | Missing shop node, inventory, or UI. |
| **SYS-22** | Rest Site System | **PARTIAL** | 2 tests | Basic heal/stamina node exists; missing camp save triggers. |
| **SYS-23** | 8 Sequential Dungeons | **PARTIAL** | 2 tests | Only Act 1 (Floors 1–8) generated; no Act 2–8 progression. |
| **SYS-24** | **Inventory & Equipment System** | **IMPLEMENTED** | 11 tests | **GDD v1.0 (Model C)**: 6 Rarities, Affixes, Upgrades, Selling, 100 Cap, 5 Slots, Delta UI. |

---

## 4. Key Architectural Patterns & Conventions

### 4.1 Data vs. Runtime Separation
- **`EquipmentDef`**: Immutable ScriptableObject/code blueprint for equipment items (ID, name, base stats, slot, weapon type, restrictions, rarity, base value).
- **`EquipmentInstance`**: Unique runtime mutable instance (`instanceId` GUID, level, rarity, upgrade level, list of `GeneratedAffix`, `specialEffectId`, `boundCharacterId`).
- **`InventoryRuntime`**: Manages the player's 100-slot equipment storage, stacked consumables (up to 99), tracked crafting materials, and quest key items.
- **`CharacterDef`**: Immutable persistent blueprint of a hero (Base ability scores, race, class, starter passive, equipped items, level, exp).
- **`Unit`**: Mutable runtime entity owning authoritative stat recalculation (`unit.Recalculate()`), primary ability scores, and `equippedInstances` dictionary.
- **`Combatant`**: Battle-scoped wrapper encapsulating `Unit`, managing temporary battle buffs, shields, break meters, and `StatusController`.
- **`RunState`**: Active expedition context tracking the node map, current floor, dungeon seed, temporary party gold, party roster, and `InventoryRuntime`.

### 4.2 Stats Recalculation Flow
Primary Ability Scores strictly follow:
$$\text{FinalScore} = \text{Base} + \text{Racial} + \text{Permanent} + \text{Equipment} + \text{Temporary}$$
$$\text{AbilityModifier} = \lfloor(\text{FinalScore} - 10) / 2\rfloor \quad (\text{using mathematical floor})$$

Derived Stats are recalculated through `DerivedStats.Calculate(unit)`:
- $\text{MaxHP} = \text{BaseHP} + (\text{VIT\_Mod} \times 10) + \text{EqHP}$
- $\text{MaxMP} = \text{BaseMP} + (\text{INT\_Mod} \times 5) + \text{EqMP}$
- $\text{PhysicalPower} = \text{BaseAtk} + (\text{STR\_Mod} \times 2) + \text{EffectiveWeaponPower}$
- $\text{PhysicalDefense} = \text{BaseDef} + (\text{VIT\_Mod} \times 2) + \text{EffectivePhysicalDefense}$
- $\text{MagicDefense} = \text{BaseMDef} + (\text{WIS\_Mod} \times 2) + \text{EffectiveMagicDefense}$
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

### 4.6 Inventory & Equipment Architecture (SYS-24)
- **5 Core Slots:**
  1. `Weapon` $\to$ `EquipmentSlot.MainHand`
  2. `Head` $\to$ `EquipmentSlot.HeadArmor`
  3. `Body` $\to$ `EquipmentSlot.BodyArmor`
  4. `Accessory 1` $\to$ `EquipmentSlot.Accessory1`
  5. `Accessory 2` $\to$ `EquipmentSlot.Accessory2`
- **6 Rarity Tiers & Affix Rules:**
  - Common: 0 affixes (White `#E0E0E0`, $1.0\times$ sell multiplier)
  - Uncommon: 1 affix (Green `#4BE368`, $1.5\times$ sell multiplier)
  - Rare: 2 affixes (Blue `#38B6FF`, $2.2\times$ sell multiplier)
  - Epic: 3 affixes (Purple `#A35CFF`, $3.2\times$ sell multiplier)
  - Legendary: 3 affixes + 1 Legendary Special Effect (Orange `#FF8C00`, $5.0\times$ sell multiplier)
  - Mythic: 4 affixes + 1 Mythic Special Effect (Red `#FF3B30`, $8.0\times$ sell multiplier)
- **Upgrades:** Upgrades (+0 to +5) scale base stats only ($+5\%$ base weapon power, defense per rank) without mutating affixes or special effects.
- **Selling Formula:** $\text{SellValue} = \text{Round}(\text{BaseValue} \times \text{RarityMultiplier} \times (1 + (\text{Level} - 1) \times 0.1) \times (1 + \text{UpgradeLevel} \times 0.2))$.
- **Model C Persistence:** When the party is defeated in combat (`RunStatus.Defeat`), gold is wiped, but all inventory equipment, consumables, materials, key items, and equipped hero gear are permanently preserved in the save file.

---

## 5. Work Completed in Recent Sessions

1. **Inventory & Equipment System Implementation (SYS-24 / GDD v1.0, Model C):**
   - **Data Foundation:** Implemented `AffixDef`, `AffixCategory`, `AffixModifierType`, `GeneratedAffix`, `SpecialEffectDef`, and `EquipmentInstance` in `InventorySystem.cs`. Registered 21 affixes across all categories and 7 Legendary/Mythic special effects in `Database.cs`. Added `EquipmentRarities` helper with fixed affix counts, UI colors, and sell multipliers.
   - **Inventory Runtime:** Implemented `InventoryRuntime` with 100-slot equipment capacity, consumable stacks (up to 99), crafting materials, and key items.
   - **Equipment Management:** Added `equippedInstances` to `Unit`, implementing `TryEquip` and `Unequip` with full validation against class proficiencies, slot types, and level requirements. Synchronized with legacy `Unit.equipped` via `ToEffectiveDef()` for 100% backward compatibility.
   - **Equipment Generation:** Implemented `EquipmentLootGenerator` supporting dungeon tier scaling, enemy tier rarity weights, unique affix rolls, and special effects. Added `RewardKind.Equipment` to reward distribution.
   - **Model C Persistence:** Created serializable data transfer objects (`InventorySaveData`, `EquipmentInstanceSaveData`, etc.) and integrated with `GameSave.cs`. Ensured inventory and equipment are preserved upon run defeat while resetting temporary gold.
   - **UI Screens:** Created `InventoryScreen.cs` (4 tabs, pagination, sorting, filtering, item details, Equip, Upgrade, Sell with confirmation for Legendary/Mythic, Discard, Use) and `EquipmentScreen.cs` (5 core slots, candidate inventory picker, live stat comparison deltas, Equip/Unequip). Wired into `Game.cs` with hotkeys (`I` for Inventory, `E` for Equipment) and navigation shortcuts on Map and Status screens.
   - **Testing & Verification:** Authored `InventoryAndEquipmentTests.cs` (11 comprehensive test cases). All 133 / 133 EditMode tests passing with 100% pass rate.
2. **Character Creation UI Refactor & EditMode Test Fix:**
   - Fixed `CharacterCreationScreen` so EditMode tests always construct a clean builder without stale state.
   - Fixed class label left-truncation, cursor overlaps, and tooltip clipping.

---

## 6. Priority Roadmap for Next Steps

### Phase 1: Core Loop & Progression
- **Task 1: Multi-Slot Save Architecture (SYS-03)**
  - Expand `GameSave.cs` to 4 independent slots (`"obscure.save.slot.0"` through `"obscure.save.slot.3"`).
  - Create Save Slot Selection UI in Main Menu and Settings.
- **Task 2: 8 Sequential Dungeons Progression (SYS-23)**
  - Update `RunState.cs` to advance through Acts 1 through 8 sequentially.
  - Implement dungeon transitions and theme progression.
- **Task 3: Combat EXP & Level 15 Progression (SYS-08)**
  - Award EXP upon winning encounters, clamp level progression to 15, and award Skill Points.

### Phase 2: Missing Core Gameplay Systems
- **Task 4: Shop System v1.0 (SYS-21)**
  - Introduce `NodeType.Shop` into `NodeDef` and `MapGenerator`.
  - Create `ShopInventory` and `ShopScreen.cs` integrating with `InventoryRuntime` and equipment purchasing/selling.
- **Task 5: Author Full Class Skill Trees (SYS-07)**
  - Expand `Database.cs` to the full 225 player skills across 9 classes.

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
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
