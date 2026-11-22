# Handoff: Character Creation audit — findings and fixes

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Design source:** `Assets/Docs/GDD_Terbaru.md`, race chapter §5 (Racial Ability Bonus, Draconic Heritage lineages). Where the GDD and this handoff disagree, the GDD wins, except for the user decisions marked below.
- Report test results, every existing test you changed, and anything you could not verify.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`.

## Context
The user asked: "Character creation: is something wrong?" I read the flow:
- the session and step logic, `CharacterCreationSession` / `NewGamePartyBuilder` in `Data/CharacterCreation.cs`;
- the UI, `Screens/CharacterCreationScreen.cs`, with pages Race → Class → Abilities → Background → Passive → Gear → Review;
- the race, lineage, background and class data in `Data/Database.cs`;
- Point Buy (`Data/StatsSystem.cs`: 27 points, 8–15 before racial bonuses, the standard cost table);
- the GDD race chapter (`GDD_Terbaru.md` §5).

## Findings (by severity)

### F1. Racial Ability Bonus *choices* from the GDD are not implemented (high, a GDD violation)
Every race's bonus is hard-coded in `Database.Races`:

| Race | GDD | Code today |
|---|---|---|
| Human (Adaptive Potential) | **+1 to two different abilities, player's choice** | Fixed +1 STR, +1 VIT |
| Elf (Elven Grace) | **+2 DEX *or* +2 INT** | Fixed +2 DEX |
| Liichtian (Celestial Wisdom) | **+2 WIS *or* +2 CHA** | Fixed +2 WIS |
| Merfolk (Aquatic Affinity) | **+2 WIS *or* +2 DEX** | Fixed +2 WIS |
| Dragonborn (Draconic Heritage) | **Bonus set by the lineage chosen at creation** | Locked to Crimson (+2 STR, Fire). `Database.DragonbornLineages` defines Crimson, Stone, Azure and Frost, but nothing in the UI lets you pick one |
| Dwarf, Halfling | Fixed | Fixed (correct) |

**Why it matters:**
- Build identity breaks. An Elf Wizard can't take INT. A Liichtian or Merfolk can't follow their GDD builds. A Human is forced into STR/VIT, which is wrong for casters.
- Dragonborn's lineage also decides **Draconic Overdrive's element bonus** (`Combatant.GetDragonbornLineageElement`). So Azure (Lightning) and Frost (Ice) Dragonborn, and Stone, can't exist.

**Trap to avoid:** `lineageId` lives on the **shared static `RaceDef`**. A picker that writes `Race.lineageId` would change *every* Dragonborn. The choice must live per character: in the session, and `CharacterDef.lineageId`, which already exists and is copied from `Race.lineageId` at ~L1153. The unit's racial modifiers and `GetDragonbornLineageElement` must read the per-character value.

### F2. Duplicate hero names (medium; heroes stay nameless by design)
- **User decision:** heroes are *nameless adventurers*, so there is no naming field. `CharacterName` stays the class name.
- **But duplicates collide:** two Fighters are both "Fighter" in the combat log, the AI read lines and the ANALYSIS ally tab.
- **Fix:** a second hero of the same class becomes "Fighter II", a third "Fighter III". Apply it on confirm, and re-apply when the party is edited (removing hero 1 renames "Fighter II" back to "Fighter").

### F3. The step model doesn't match the pages (medium, data integrity)
- `CreationStep` has no Background step. The **Background page is mapped to `CreationStep.ReviewStats`** (`CharacterCreationScreen` ~L1805).
- "Review Stats" as described in the session comments never appears as its own step.
- The header shows `STEP n/7` from the enum, so the step number and name are misleading. Any validation keyed on `ReviewStats` actually runs on the Background page.

### F4. The Passive and Gear pages have no decisions on them (medium, flow bloat; the user's decisions)
- **Passive:** the starter passive is fixed per class. **Remove the page.** Show each passive *inside the option that grants it*:
  - the class starter passive on the Class card and details;
  - the racial feat and utility on the Race card;
  - the background perk on the Background card.
- **Gear:** **remove the page.** Starting gear is the class's default loadout, applied automatically (today's `ApplyClassSelection(resetEquipmentAndLoadout: true)` path). It can be inspected on the new Character Sheet (F7).
- **New flow:** Race → Class → Abilities → Background → **Character Sheet** (confirm), so 5 steps.

### F7. There's no full sheet before confirming (the user's request)
- **Add a Character Sheet tab** as the last step before "Add to Party". It's read-only and shows everything about this hero:
  - identity: race (plus racial bonus choice and lineage), class, background;
  - final ability scores with their breakdown (base, racial, background);
  - every derived stat: HP, MP, ATK, MATK, DEF, MDEF, SPD, crit, Block, Stagger Res, status resistance, QTE bonus, Break power;
  - elemental affinities;
  - **equipped gear**, each slot with its stats and effects;
  - active skills and their ranks;
  - the starter passive, racial feat and utility, and the background perk.
- Use the existing `Session.PreviewDerivedStats` / `FinalScores` sources, so the sheet always matches what the hero gets in battle.
- Its buttons: **Confirm (Add to Party)** and **Back** to any earlier step.

### F8. Choosing a preset should go straight to the party (the user's request)
- Selecting a preset hero (template) **adds it to the party immediately** (after `Validate`) and opens the **party menu** (the 4-slot review), so the player sees the current party right away.
- From there, the player can edit that member (which opens the normal flow at the Character Sheet) or remove them.

### F5. There's no party-level view (medium, an ambitious gap)
- You build 4 heroes one at a time, and nothing tells you what the **party** covers.
- **Add a Party Overview panel** on the party Review screen. It's information only and never blocks anything.
- **User decision: the panel only flags gaps.** It is a short list of soft warnings, not a stats summary, and **it says nothing about elements**: no element list and no element gaps.
- **Gaps it can flag** (each one is a fact about the party):
  - "No healer": no hero has a healing skill or healing feat.
  - "No tank": no hero has the tank role.
  - "No Break pressure": no hero has a Break-focused skill or above-average Break power.
  - "No status cleanse": no hero can remove ailments.
- **When there are no gaps,** the panel shows "No gaps found" and nothing else.
- **The warnings never mention enemies** or their weaknesses, so the ANALYSIS Codex is still how players discover affinities.

### F6. Changing race after allocating points has no feedback (low)
- Pre-racial scores are capped at 15. When a new racial choice is applied, the final scores change silently.
- **Fix:** on a race change, briefly highlight the final-score deltas on the Abilities column, and keep the point-buy allocation.

## Fix plan (for the handoff)
1. **Racial choices (F1):**
   - Add a data field to `RaceDef`: `RacialBonusOption[] bonusOptions` plus a `pickCount`.
     - Human: any 2 different abilities at +1.
     - Elf: {DEX, INT} at +2.
     - Liichtian: {WIS, CHA} at +2.
     - Merfolk: {WIS, DEX} at +2.
     - Dwarf and Halfling: a fixed bonus, no options.
   - Session:
     - `SelectRacialBonus(...)` and `SelectLineage(lineageId)`, stored **on the session and the `CharacterDef`**, never on `RaceDef`.
     - The default pick is the first option, so old saves and templates keep working.
     - `RacialModifiers` / `FinalScores` read the chosen bonus.
   - The Dragonborn bonus comes from `DragonbornLineages[chosen].abilityModifiers`.
   - **Save:** extend `SavedCharacterData` / `CharacterDef` with `racialBonusChoice`. `lineageId` already exists.
     - Migration: missing values fall back to today's defaults (Human STR+VIT, Elf DEX, Liichtian WIS, Merfolk WIS, Dragonborn Crimson), so existing saves load unchanged.
   - **Respec:** the GDD allows changing the racial choice through respec. Hook it into the existing `Respec` flow.
   - **UI (user decision: the choice happens during race selection, on the Race page):**
     - After the player selects a race, the Race page's detail panel shows a **"RACIAL ABILITY BONUS"** chooser right below the race description. The player can't leave the Race page until the choice is complete; the Next button stays disabled and says why.
     - **Human:** six ability chips; pick exactly 2 different ones, each +1.
     - **Elf / Liichtian / Merfolk:** two chips (for example DEX / INT); pick 1, for +2.
     - **Dragonborn:** a **lineage picker** (Crimson, Stone, Azure, Frost). Each card shows its element, the Draconic Overdrive element bonus, and its ability bonus.
     - **Dwarf / Halfling:** the fixed bonus is shown as a locked chip, with no choice.
     - Each chip shows the resulting final score preview (for example "INT 15 → 17").
     - Switching to another race resets its choice to that race's default, highlighted, so the player notices.
   - **Templates and randomize:** templates may specify the choice, and it's validated in `ValidateTemplate`. `GenerateRandom…` picks the option that matches the class guide's primary ability.
2. **Duplicate suffix (F2):**
   - In `NewGamePartyBuilder`, after every confirm, edit or remove, recompute the names. The Nth hero (by party order) of a class gets "<Class> II", "<Class> III", and so on; the first keeps the plain class name.
   - The combat tag uniqueness (`Battle.UniqueTag`) stays as it is.
3. **Steps (F3, F4, F7):**
   - Pages become **Race → Class → Abilities → Background → Character Sheet**.
   - Remove the Passive and Gear pages. Starting gear is applied automatically from the class default.
   - Show passives inside the Race, Class and Background option cards and detail panels.
   - Add the `CreationStep.SelectBackground` and `CreationStep.CharacterSheet` steps, appended so stored values stay stable. Mark `ReviewStarterPassive` / `ConfigureEquipment` obsolete, and make sure nothing navigates to them.
   - Header: `STEP n/5`.
   - Add a page→step mapping test.
4. **Character Sheet (F7):** build it from existing session sources (`FinalScores`, the derived-stat preview, `Equipped`, `Loadout`, race, class and background defs). It's read-only; Confirm adds the hero to the party.
5. **Presets (F8):** in the preset handler (`CharacterCreationScreen` ~L714, `TryApplyTemplate`): on success, call `Builder.TryConfirmCurrentCharacter`, then go to the party review. On a validation failure, show the reason and stay on the page.
6. **Party Overview (F5):** a read-only gap list on the 4-hero review, computed from `PendingParty` alone:
   - **No healer:** no healing skill (`SkillKind.Heal`) and no healing feat (Lightbound, Ocean's Embrace).
   - **No tank:** no class with the tank role in `ClassDef`.
   - **No Break pressure:** no skill with a high break multiplier, and no hero above the party-average Break power.
   - **No status cleanse:** no skill that removes ailments.

   It never reads elements, enemy affinity data or enemy names. With no gaps, it shows "No gaps found".
7. **Race-change feedback (F6):** highlight the final-score deltas when the race or racial choice changes, and keep the allocation.

## Tests
- **Racial choices:**
  - each race's bonus matches the GDD for every option;
  - Human can't pick the same ability twice;
  - Dragonborn lineage sets both the bonus **and** `GetDragonbornLineageElement`;
  - two Dragonborns with different lineages in one party stay independent (shared-`RaceDef` regression test);
  - a save round-trip keeps the choice;
  - an old save without the fields loads with today's defaults.
- **Templates:** validation rejects an invalid choice; randomize produces a valid one.
- **Duplicate suffix:** Fighter, Fighter II, Fighter III; removing the first renames the rest; the combat log uses the suffixed names.
- **Steps:**
  - page→step mapping test;
  - the Background page is `SelectBackground`;
  - the header count is 5;
  - starting gear is auto-equipped and matches the class default;
  - no page navigates to the removed Passive or Gear steps.
- **Character Sheet:** every value equals what the hero has at battle start (build a `Unit` from the confirmed `CharacterDef` and compare).
- **Presets:** choosing a preset adds the hero and opens the party review; an invalid preset doesn't add it and shows the reason.
- **Party Overview:**
  - a party with no healer flags "No healer";
  - a balanced party shows "No gaps found";
  - no element text ever appears in the panel.
- All existing character-creation tests pass, or are updated only where they asserted the old hard-coded bonuses. Report each one changed.

## Playtest
1. **New player:** creates an Elf Wizard with +2 INT and an Azure Dragonborn without help.
2. **Readability:** with two Fighters, the player can tell them apart in the log ("Fighter" and "Fighter II").
3. **Sheet:** before confirming, the player can answer "what weapon does this hero use, and what's their DEF?" from the Character Sheet alone.
4. **Presets:** picking a preset lands the player in the party view with that hero already in a slot.
5. **Party Overview:** a player who sees a gap such as "No healer" changes one hero or consciously accepts the gap. The panel is information, never a block.
6. **Race selection:** the player makes the racial bonus choice (or the Dragonborn lineage) on the Race page without help, and can say which ability it raised.
