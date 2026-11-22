# Handoff: Stage 3, weakness coverage (Arcane removal, skill retags, Dungeon 1 table)

## Rules for the implementing agent
- Follow `AGENTS.md`: no auto-commit or push, never `git add .` / `-A`, and leave the changes uncommitted for the user to review.
- **Parent handoff:** `Assets/Docs/Analysis_System_Handoff.md`, Stage 3. This document replaces that row's coverage rule and table instructions. **Design source:** `Assets/Docs/Felna_Analysis_System_Spec_v1.md` Part F. If this handoff and the spec disagree on the coverage rule or the Dungeon 1 table, this handoff wins, and you update the spec to match (see 3c).
- Every number is a **posture-B starting value**. Keep its label, its pass test and its tuning direction as a comment next to the constant.
- **One sub-stage at a time:** 3a, then 3b, then 3c. Report each gate's result to the user before you start the next sub-stage.
- Paths are relative to `Assets/Scripts/` unless they start with `Tests/`. Line numbers are approximate (as of 2026-11-22), so search for the symbol.

## Why this exists
The original Stage 3 coverage rule was: "every weakness must be dealable by ≥ 2 playable classes' early skills". It can't pass with the current skill database. Damaging skills at T1–2 in `Database.Skills` today:

| Element | Classes at T1–2 |
|---|---|
| Physical | fighter, barbarian, martial_artist, rogue, ranger |
| Fire / Ice / Lightning | wizard only |
| Holy | cleric only (the druid's Holy skills are all heals) |
| Dark | warlock only |
| Arcane | wizard only |
| Earth / Wind / Water | **none at any tier** |

Two more problems show up in that data:
- The druid's damage skills are `kind = Magic` but `element = Physical`, even though they read as nature/earth.
- `Element.Arcane` is its own element, but the UI already labels it "AL"/"FIXED", as though it were Almighty.

Changing only the table would leave Physical as the only weakness that passes. Every party would then exploit every enemy, and ANALYSIS would have nothing to discover.

## Decisions (made by the user, don't reopen)
1. **The druid deals Earth damage**, and gets Holy damage at T3 or later.
2. **Remove `Element.Arcane`.** Retag its skills to real elements (Fire / Ice / Lightning / Earth / Wind / Water). **Almighty is reserved for later in the game.** Don't convert anything to `DamageType.Almighty`.
3. **The coverage rule becomes a per-enemy check** (3c). Most elements still have only 1 class at T1–2.

## Sub-stages
| # | Stage | Gate |
|---|---|---|
| 3a | **Remove `Element.Arcane`.** No behaviour change except removing the element | Full EditMode suite passes (after the test updates listed below). A save written before the change loads. A stored element int of `4` resolves to `Physical` |
| 3b | **Skill retags** in `Data/Database.cs` | `BattleTests`, `QteTests` and `Step4Tests` pass (they reference `arcane_burst` by id). The −2 MP Arcane Affinity still applies to the retagged wizard skills. Record the `BalanceSimulation` delta |
| 3c | **Coverage rule, Dungeon 1 table and coverage test** | Coverage test and tier-limit validator pass. `BalanceSimulation` encounter deaths stay within ±15% of the pre-3b baseline |

## 3a: Remove `Element.Arcane`
- `Combat/Elements.cs`: remove `Arcane` and **pin explicit values** on the remaining members:
  `Physical = 0, Fire = 1, Ice = 2, Lightning = 3, Holy = 5, Dark = 6, Earth = 7, Wind = 8, Water = 9, Shadow = Dark`
  - **Why the values must be pinned:** `Data/ModifierInstance.cs` (around L42) serializes `(int)element`, and `EnemyCodex` dictionaries are keyed on `Element`. Renumbering would silently corrupt existing saves.
  - Where a stored element int is read back, add one guard: `Enum.IsDefined(typeof(Element), v) ? (Element)v : Element.Physical`.
  - Remove the `Element.Arcane => "ARC"` case from the short-name switch.
- `Combat/AffinityResolver.cs`: remove `Element.Arcane` from `AllElements`, and the `GetRawArcaneAffinity()` branch in `GetRaw`.
- `Combat/Damage/EnemyAffinity.cs`: remove `arcaneAffinity` and its getter, setter, clone field and the `Element.Arcane` case in the legacy bridge.
- UI: remove the `Element.Arcane` cases in `Render/Palette.cs` (three switches), `Screens/CombatScreen.cs` (the VFX/SFX switch, around L1723) and `Screens/CharacterStatusScreen.cs` (drop the `(Element.Arcane, 0f, "FIXED")` row, around L445). Make sure Wind and Water get a sensible colour, code and sound. Reuse `Sound.Arcane` for them if no element-specific sound exists.
- **Keep the `"Arcane"` skill tag.** `Combatant.GetEffectiveMpCost` uses `skill.HasTag("Arcane")` for the wizard's −2 MP Arcane Affinity. It has to keep working with no change.
- Tests:
  - `Tests/Editor/AnalysisSystemTests.cs` (around L97, L113): change `Element.Arcane` to `Element.Lightning` in the reflect fixture and the test skill.
  - `Tests/Editor/SaveSystemTests.cs` (around L586): delete the `Palette.ElementCode(Element.Arcane) == "AL"` assertion.
  - `Tests/Editor/CharacterStatsTests.cs`: update every Arcane reference.

## 3b: Skill retags (`Data/Database.cs`)
**Keep every skill `id`.** Saves, `SkillUnlockNode` entries, class skill lists and tests all reference them. Change only `element`, plus `name` and the QTE `word` where listed.

| Skill id | Tier / target | Old element | New element | New name / word (suggested) |
|---|---|---|---|---|
| `arcane_bolt` | wizard T1, single | Arcane | **Water** | Tidal Bolt / `UNDA` |
| `arcane_burst` | wizard T2, AoE | Arcane | **Wind** | Gale Burst / `VENTUS` |
| `arcane_overload` | wizard T4, single | Arcane | **Water** | Maelstrom / `ABYSSUS` |
| `grand_arcanum` | wizard T5, AoE | Arcane | **Wind** | Tempest / `TEMPESTAS` |
| `arcane_missile` | no `classId`, no live references | Arcane | **Lightning** | unchanged |
| `thorn_shot`, `entangling_roots`, `vine_lash` | druid T1–2 | Physical | **Earth** | unchanged |
| `overgrowth`, `thorn_prison`, `wrath_of_the_forest`, `primal_cataclysm`, `natures_dominion` | druid T3–5 | Physical | **Earth** | unchanged |
| `natures_wrath` | druid T3, AoE | Physical | **Holy** (the "Holy at T3+" slot) | Radiant Wrath; rewrite the description to match |

Wizard branch 2 becomes a Wind/Water "Tempest" branch. If a new QTE word breaks a `TypingQte` length or difficulty rule, keep the old word and report it.

Legacy affinity entries that reference Arcane:
- `resist = new[] { Element.Arcane }` (3 entries, around L1522, L1936, L5015): change to `Element.Water`.
- `weak = new[] { Element.Arcane }` (around L5495): change to `Element.Lightning`.

Coverage at T1–2 after 3b:

| Element | Classes |
|---|---|
| Physical | 5 Physical classes |
| Earth | druid |
| Holy | cleric |
| Fire / Ice / Lightning / Wind / Water | wizard |
| Dark | warlock |

## 3c: Coverage rule, Dungeon 1 table, coverage test

### Rule (posture B)
**Definitions:**
- "Early" means `tier <= 2`.
- "Deals element E" means a skill with `classId` set to a playable class, `kind` Physical or Magic, `power > 0`, and `element == E`. Skills with no element set count as Physical.

**Checks:**
1. **No dead weakness:** every weakness element of every Dungeon 1 enemy is dealt by **≥ 1** class.
2. **No locked-out party:** for each enemy, and for each Zoroa phase separately, the classes that can hit **at least one** of its weaknesses must number **≥ 2**.

| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| Per-enemy class count (check 2) | ≥ 2 | A party without the cleric, or without the wizard, exploits a known weakness in its first action ≥ 50% of the time in the 2nd fight against a family (the existing learning metric, cut by party makeup) | Require ≥ 3 |
| "Early" tier cut-off | ≤ 2 | Same as above | Relax to ≤ 3 |

Update the Part F **Rules** bullet in `Felna_Analysis_System_Spec_v1.md` to this rule, and update the Part F table to match the one below.

### Dungeon 1 table (`Data/Dungeon1Roster.cs`)
Class key: C = cleric, W = wizard, D = warlock, Dr = druid, P = Physical classes. **Only these rows change** from spec Part F:

| Enemy | Old weak | New weak | Classes | Other change |
|---|---|---|---|---|
| Cinder Wisp | Water, Ice | **Water, Earth** | W + Dr | — |
| Soulbound Hound | Fire, Lightning | **Fire, Holy** | W + C | — |
| Possessed Royal Guard | Holy | **Holy, Lightning** | C + W | — |
| Soul Reaper (miniboss) | Holy | **Holy, Fire** | C + W | — |
| Reliquary Confessor | Dark | **Dark, Physical** | D + P | — |
| Graveglass Arcanist | Physical, Earth | (same) | P + Dr | Reflect Arcane → **Reflect Lightning** |
| Tombstone Colossus | Lightning, Water | **Lightning, Dark** | W + D | Resist Physical, Earth unchanged |
| Zoroa, Phase 1 | Holy | **Holy, Fire** | C + W | Drain Dark (concealed) unchanged |
| Zoroa, Phase 2 | Holy, Lightning | (same) | C + W | Reflect Arcane → **Reflect Fire** (still concealed) |

Design intent for Zoroa: Phase 1 teaches the party to use Fire. After "AFFINITIES SHIFTED", Phase 2 reflects Fire, so the party has to re-read the boss. Fire **must** be auto-revealed as changed, not as unchanged, by the phase-shift rule.

These rows stay as written in spec Part F and pass the new rule: Hollow Squire, Gravebound Archer (Holy, Wind), Crypt Leech, Forgotten Militia (Holy, Earth), Royal Ossuary Knight, Crownless Duelist, Mourning Bell Wraith. The tier limits still hold: Normal-tier enemies have ≤ 2 weaknesses, ≤ 1 resistance and no Null/Drain/Reflect; elites and minibosses have ≤ 1 of Null/Drain/Reflect. Enhanced forms keep copying their base affinities.

### Coverage test (`Tests/Editor/AnalysisSystemTests.cs`)
- Build `Dictionary<Element, HashSet<string>> classesByElement` from `Database.Skills` with the filter in the Rule section.
- For every Dungeon 1 roster enemy (base forms; Enhanced copies them) and for each Zoroa phase, assert checks 1 and 2.
- The failure message names the enemy, its weaknesses, and the classes that can reach each weakness.

## Out of scope
- Converting anything to Almighty.
- New skills for Holy, Fire, Ice, Lightning, Wind or Water on other classes.
- Rebalancing class power.

## Report back
- The gate result for each sub-stage (fill in the table below).
- Any QTE word or name you kept unchanged, and why.
- **The v1.9 xlsx needs updating** to the new table and element list. Say so; don't edit the xlsx.
- Party-balance note for the user: Holy is still a weakness on 11 of 16 rows and is still cleric-only at T1–2.

## Results (fill in)
| Stage | Date | Sample | Key metrics | Gate |
|---|---|---|---|---|
| 3a | 2026-11-22 | Full EditMode suite (633) | 630 passed, 0 failed, 3 skipped; `Element.Arcane` removed, enum values pinned (`Holy=5..Water=9`), pre-3a save & int `4` → `Physical` verified | PASS |
| 3b | | | | |
| 3c | | | | |
