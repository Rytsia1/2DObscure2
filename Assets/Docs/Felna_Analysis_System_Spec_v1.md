# FELNA — ANALYSIS SYSTEM (Battle Inspection, Codex, Drain/Reflect, Per-Status Resistance)

**Document ID:** FELNA-ANALYSIS-001
**Version:** 1.0
**Status:** Proposed — System Specification
**Implementation plan:** `Analysis_System_Handoff.md`
**Related:** `Felna_Enemy_AI_System_Spec_v1_1.md`, `Felna_Enhanced_Common_Variant_Spec_v1_1.md`, `Felna_On_Defeat_Transformation_Spec_v1_1.md`

---

## 0. Purpose

ANALYSIS is a free battle command for inspecting enemies and allies. It costs **no turn, no MP and no item**.

**Enemies are learned.** Their elemental affinities and status resistances start as `???` and are revealed through combat. Each discovery is recorded **permanently** in a per-save-slot Codex.

**Allies are known.** Everything about them is always visible.

This spec also adds the two affinity states the command needs:
- **Drain:** the hit heals the target.
- **Reflect:** the hit bounces back to the attacker.

It also adds **per-status resistance**.

### Design decisions

| Area | Decision |
|---|---|
| Cost | Free: no turn, MP or item |
| Enemy affinities | Discovered progressively; `???` until known; NORMAL is recorded too |
| Discovery memory | Permanent per enemy type, per save slot, across runs |
| Bosses | Each phase is a separate entry; can have *concealed* affinities |
| Allies | Every stat, affinity and resistance always visible |
| Status effects | Active effects always visible on both sides |
| Drain / Reflect | Real combat mechanics (Part E) |
| Status resistance | Per-status values added on top of the universal value (Part F) |

### Code facts this spec relies on
- **Two parallel affinity models exist today:**
  - `Affinities` / `Effectiveness { Neutral, Weak, Resist, Immune }` (`Combat/Elements.cs`, used by `Battle.Deal`);
  - `EnemyAffinityProfile` / `AffinityType { Normal, Weakness, Resistant, Nullification }` (`Combat/Damage/EnemyAffinity.cs`, used by `DamageCalculator` and Null Break).
- Neither model has Drain or Reflect.
- **No Dungeon 1 roster enemy has any affinity set yet**, so Part F authors a table.
- **Status resistance is one universal value per unit** (`StatusResistanceProfile`: additive sources, 75% cap, multiplicative reductions). Bosses also have 100% mental immunity.
- **Elements:** Physical, Fire, Ice, Lightning, Arcane, Holy, Dark (= Shadow), Earth, Wind, Water.
- **Attributes:** STR, DEX, VIT, INT, WIS, CHA. There is no AGI attribute; **AGI maps to DEX**.
- **Enemies have no level.** Inspection shows the tier and the dungeon instead of inventing one.

---

## Part A: The command

- **Where:** ANALYSIS is a fifth option in the hero's command menu, after GUARD. It is always enabled.
- **Cost:** opening it costs nothing. Closing it returns to the same hero's command menu with the turn untouched.
- **When:** whenever the player is choosing a command or a target. **Not** during enemy turns or QTEs. It never pauses a QTE.
- **Layout:**
  - Two tabs: **ENEMIES** and **ALLIES**.
  - Left/right cycles between units.
  - Up/down scrolls the sections.
- **Target-picker tags:** when choosing the target of any damaging attack or skill, each enemy shows its **known** effectiveness for that move's element: WEAK, RESIST, NULL, DRAIN, REFLECT, NORMAL or `???`. Players get the benefit of the Codex without having to open the panel.

---

## Part B: Enemy inspection

| Section | Contents |
|---|---|
| Identity | Name; family/type (Undead, Spirit, Beast…); role; tier badge (Normal / Enhanced / Elite / Miniboss / Boss); dungeon; current phase (bosses) |
| Elemental affinity | One row per element (10): `???` until discovered, then WEAK / NORMAL / RESIST / NULL / DRAIN / REFLECT |
| Status resistance | One row per status: `???` until discovered, then a tier (VULNERABLE / NORMAL / RESISTANT / IMMUNE). Enemies never show raw % |
| Active effects | Every buff, debuff and ailment with its remaining duration, plus Broken, Charging, Null Break and Enhanced. **Always visible** |
| Combat readout | HP bar (as on the HUD), Break shield value / max, and the telegraphed intent, if any |

**No hidden numbers leak.** Enemies never show ATK, DEF or any other raw stat.

---

## Part C: Ally inspection (always fully visible)

**Resources**
- HP / Max HP.
- MP / Max MP.
- Shield, and the source of each shield.
- Poise gauge.

**Attributes**
- STR, DEX (the "AGI"), VIT, INT, WIS, CHA.

**Derived stats**
- ATK (physical power), MATK (magic power).
- DEF, MDEF, SPD.
- Crit chance and crit damage.
- Block Power, Stagger Resistance, QTE timing bonus.

**Elemental affinities**
- Every element, exact values. Allies never show `???`.

**Status resistance**
- The universal %, and the final % for each status. Exact values.

**Active statuses, buffs and debuffs**
- Each one with its remaining duration.

**Defensive effects**
- Guard, Phased, Iron Resolve, Ocean's Embrace, Lightbound, Determination, Draconic Overdrive and Exhausted, each with remaining turns.
- Shields.
- Interpose (Guardian).

---

## Part D: Discovery and the Codex

### What an entry is
- Each entry is keyed by **enemy definition id**.
- The base form, its Enhanced form and **each boss phase** are separate entries.
- Summons share the entry of their definition.

### Scope
- Permanent within a **save slot**, carried across runs. Each save slot has its own Codex; they never share.
- Written at the end of every battle, **victory or defeat**, so knowledge from a lost run is kept.

### Affinity reveal
- An affinity is revealed when a damaging hit of element E **resolves** against any enemy of that entry. The source doesn't matter: hero attack, counter, or elemental DoT.
- NORMAL is recorded too, so tested-and-normal shows as NORMAL, not `???`.

### Status tier reveal
- A status tier is revealed when an **application attempt** of that status resolves against the entry.
- It counts whether the attempt succeeds or fails, including when immunity blocks it.

### Other reveals
- **Elf Keen Senses:** reveals one *weakness* per dungeon (GDD racial utility).
- **Future "Insight" items or skills:** the hook is reserved only.

### Bosses
- **Concealed affinities:**
  - A boss affinity may be flagged *concealed*.
  - The first time it triggers, the hit shows **"?!"** ("something reacted") and the row stays `???`.
  - It is revealed on the second trigger in this save slot.
- **Phase changes:**
  - On a phase change, the panel shows **"AFFINITIES SHIFTED"**.
  - The new phase starts from its own Codex entry.
  - Any element whose affinity is identical in both phases is auto-revealed from the earlier phase's knowledge.

### Feedback on a reveal (at least 2 channels)
1. A floating word on the hit: **WEAK!**, **RESIST**, **NULL**, **DRAINED!**, **REFLECTED!**
2. A combat log line.
3. A one-time toast: "NEW: Hollow Squire — WEAK to Holy".
4. A codex chime (an existing sound).

### AI parity
Enemies learn hero affinities only once they have been shown in the encounter (AI Spec v1.1 §5.6). The player always sees allies in full. This asymmetry is intended: players know their own party.

---

## Part E: Drain and Reflect (new mechanics)

| | Drain | Reflect |
|---|---|---|
| Target | Takes 0 damage and **heals** by what the hit would have dealt at NORMAL, capped at its missing HP. Never revives | Takes 0 damage |
| Attacker | — | Takes the NORMAL-equivalent damage of the hit, then the **attacker's own** affinity for that element is applied. **No Defensive QTE** (it's the attacker's own strike returning). Capped at `ReflectCapPct` of the attacker's Max HP per hit |
| Break / Poise | No Break damage | No Break damage to the target; no Poise damage to the attacker |
| Status riders | Not applied | Not applied |
| Loops | — | Reflected damage can never be reflected again |
| Null Break | Has no effect. The GDD rule stands: Null Break converts Null only | Has no effect |
| Area attacks | Resolved per target | Resolved per target; each reflection hits the attacker separately |
| Defeats | — | A kill by reflected damage is a normal defeat |

### Single resolver
- Both affinity models get Drain and Reflect added:
  - `Effectiveness.Drain` and `Effectiveness.Reflect`;
  - `drain[]` and `reflect[]` arrays on `Affinities`;
  - `AffinityType.Drain = 4` and `AffinityType.Reflect = 5`.
- One **`AffinityResolver`** then answers "what is this unit's affinity to element E". Damage, Null Break, ANALYSIS and the AI all read it, so the display can never disagree with the rules.

### Heroes
No player source of Drain or Reflect exists yet. The rules are symmetric, so they apply automatically once future gear grants them.

---

## Part F: Per-status resistance

### Model
- Each status gets its own resistance on top of the existing universal value. `perStatus` ranges from −0.5 to +0.75.
- Effective resistance to status S:

  `effective(S) = clamp(universal + perStatus[S], 0, 0.75) × Π(1 − reductions)`

- **Immunity** stays a separate flag. Bosses keep their mental immunity.

### Enemy display tiers (posture B)

| Tier | Per-status value |
|---|---|
| VULNERABLE | ≤ −10% |
| NORMAL | between −10% and +15% |
| RESISTANT | above +15% |
| IMMUNE | immunity flag set |

### Allies
- Per-status values come from race, equipment and buffs.
- Lightbound stays a universal +20%.

### Dungeon 1 affinity and status table (starting values, not final; update the v1.9 xlsx to match)

**Rules:**
- Normal-tier enemies: at most 2 weaknesses and 1 resistance, and **never** Null, Drain or Reflect.
- Elites and minibosses: at most 1 of Null, Drain or Reflect.
- Zoroa: phase-dependent.
- **Every weakness must be dealable by at least 2 playable classes' early skills.** The implementer checks this against the skill database and adjusts the table if it fails.

| Enemy | Weak | Resist | Special | Status |
|---|---|---|---|---|
| Hollow Squire (Undead) | Holy, Fire | Dark | — | Fear resistant, Bleed vulnerable |
| Gravebound Archer (Undead) | Holy, Wind | Dark | — | Poison resistant |
| Cinder Wisp (Spirit) | Water, Ice | Fire | — | Burn **immune**, Freeze vulnerable |
| Soulbound Hound (Beast) | Fire, Lightning | — | — | Fear resistant |
| Possessed Royal Guard (Undead) | Holy | Physical | — | Stun resistant |
| Crypt Leech (Aberration) | Fire, Holy | Water | — | Poison immune |
| Forgotten Militia (Undead) | Holy, Earth | Dark | — | Bleed vulnerable |
| Soul Reaper (Miniboss) | Holy | — | **Drain Dark** | Fear immune |
| Royal Ossuary Knight (Miniboss) | Lightning, Holy | Physical | — | Stun resistant |
| Reliquary Confessor | Dark | — | **Drain Holy** | Silence resistant |
| Graveglass Arcanist | Physical, Earth | — | **Reflect Arcane** | Silence vulnerable |
| Crownless Duelist | Lightning, Holy | — | — | Bleed resistant |
| Tombstone Colossus | Lightning, Water | Physical, Earth | — | Stun resistant, Fear immune |
| Mourning Bell Wraith | Holy, Physical | Dark | — | Fear immune |
| Zoroa — Phase 1 | Holy | Physical | **Drain Dark** (concealed) | Mental immune (existing rule) |
| Zoroa — Phase 2 | Holy, Lightning | Dark | **Reflect Arcane** (concealed) | Mental immune |

Enhanced forms copy their base form's affinities but are separate Codex entries.

---

## Starting values (posture B, not final)

| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `ReflectCapPct` | 35% of attacker Max HP per hit | No hero at full HP dies to a single reflected hit (simulation) | 25% |
| Drain heal | NORMAL-equivalent damage, capped at missing HP | A drain-capable fight lasts ≤ 2 extra rounds when the party ignores the hint | Cap at 50% of NORMAL damage |
| Concealed reveal threshold | 2 triggers | Players describe the reveal as "earned, not random" | 1 trigger |
| Status tier cut-offs | −10% / +15% | Players predict application success from the tier with ≥ 70% accuracy | Move the cut-offs |
| **Learning metric** | — | In the 2nd fight against the same enemy family, the party's first action exploits a known weakness ≥ 50% of the time | Make the reveal feedback louder |

The learning metric proves the system: discovery that doesn't change decisions is decoration.
