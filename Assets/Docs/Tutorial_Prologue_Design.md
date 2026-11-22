# Tutorial Prologue — Stage Design (v1.0 draft)

| Item | Value |
| --- | --- |
| Builds on | `Tutorial_20Stage.xlsx` (roster, stats, skills = FINAL, unchanged here) |
| Cross-checked against | `GDD_Terbaru.md` v1.5 (Bab 3, 10, 11, 12 incl. revisions 21 Nov 2026) and current code (`Qte.cs`, `Battle.cs`, `Combatant.cs`, `Rewards.cs`) |
| Date | 21 November 2026 |
| Status | ACCEPTED by owner, 21 Nov 2026 (all §11 decisions as proposed). New numbers remain [B] starting values until playtested. §10 story beats remain a placeholder until the narrative owner replaces them. |

**Number labels** (GDD Numbers Policy): **[sheet]** = owner's FINAL value from the workbook · **[GDD]** = already in GDD/code · **[B]** = new starting value, with test + direction to move in §9. No new number in this doc is source-backed or measured yet.

---

## 0. What this document changes (summary)

The workbook defines *who* you fight. It does not yet define *how the player learns*. Cross-checking it with the GDD and code found these gaps; each is fixed in the sections below.

| # | Problem found | Why it matters | Fix (section) |
| --- | --- | --- | --- |
| 1 | A tutorial party wipe would end the run (Bab 3 §3.2) and delete the 4 characters the player just built. | Worst possible first 10 minutes. | Tutorial-only retry rule (§4.1) |
| 2 | Every failed run returns to New Game → Character Creation → Prologue. | Roguelike players will replay the Prologue dozens of times. | Skippable, with identical rewards (§4.2) |
| 3 | Code's reward path gives 150 EXP per battle and 750 per boss [GDD]. Run through it, the Prologue gives 1050 EXP → party enters Dungeon 1 at **Level 7**, skipping the Tier 2/3 unlock pacing. | Breaks D1 balance. | Fixed Prologue rewards (§4.3) |
| 4 | Defensive QTE now has 6+ input variants (Arrow, Ward, Brace, Arrow chain, Feint, Stutter — Bab 12 §6.6) and Feint/Stutter roll randomly on every tier, including Normal. The workbook never mentions them. | A random feint on the player's very first defensive QTE punishes them for something never shown. | Scripted variant ramp (§4.4) |
| 5 | Dread Pulse applies Fear at **20%** [sheet]. | 4 out of 5 players never see the status lesson. | First cast is scripted; both outcomes teach (§3 Node 2) |
| 6 | Node 2 is meant to teach "elemental damage", but tutorial enemies have no affinities, and only Cleric/Wizard/Warlock have Tier 1 elemental skills. A Fighter/Rogue/Ranger/Barbarian party never sees an affinity. | The lesson fails for many parties. | Affinities that every party can show, plus Ember Oil (§3 Node 2) |
| 7 | Guard is the only way to full-parry (Bab 12 §3.5, revision 21 Nov) and the right answer to a telegraphed Heavy hit. The workbook never teaches it. | The key defensive decision is missing. | Guard lesson in Node 3 (§3) |
| 8 | Interrupting Threshold Collapse needs ~200 Break in one window. A level-1 party deals roughly 10–25 Break per action (`physicalBreakPower = 10 + 2×STR mod` × skill × QTE grade). From a full gauge, interrupting is impossible. | The "cancel the boss attack" lesson can never happen. | Signature timing rule (§3 Node 3) |
| 9 | Inconsistencies inside the workbook (§8). | Implementer confusion. | Listed for cleanup |

---

## 1. Player goal and context

- **When:** directly after Character Creation, before the first Hub visit. **[sheet]**
- **Player state:** 4 fresh Level 1 characters with **any** of 9 classes. The design cannot assume a healer, a caster, or a shield user.
- **Player goal:** "Get through the portal and survive the first thing that guards it."
- **Designer goal:** by the end of Node 3 the player has *done*, not just read: Basic Attack, Skill, Item, Guard, Offensive QTE, each Defensive QTE variant, reading turn order, reading intent icons, choosing a target, reading affinity popups, managing HP/MP across nodes, and breaking an enemy.
- **Time budget [B]:** 15–20 minutes total, with Node 3 taking 6–9 minutes.

### Teaching rules (apply to every node)

1. **Do, then name it.** A mechanic gets one short prompt (≤ 1 sentence) at the moment it first matters. There are no up-front text walls.
2. **One new input per moment.** A new QTE variant never shows up alongside another new variant or a random feint/stutter.
3. **First time = slower.** The first instance of each QTE variant uses the existing Assist speed ×1.5 (T_lead 1000 → 1500 ms; Bab 12 §10.4) **[GDD value, reused]**. The second instance runs at normal speed.
4. **Prompts are re-readable.** Every prompt is added to a "Field Notes" log (pause menu) so nothing is lost if dismissed.
5. **Failure teaches.** Every Poor result on a new variant shows *why* (for example "Too early", "Wrong direction", "Released before impact").

---

## 2. Flow

```
Character Creation ─► Prologue intro (skip offer if eligible)
   ─► Map screen (3 linear nodes, teaches map reading)
   ─► Node 1: First Combat ─► Reward screen (EXP)
   ─► Node 2: Mixed Encounter ─► Reward screen (EXP → Level 2, items)
   ─► Node 3: Aetherion ─► Prologue clear (Gold) ─► "Hardcore from here" card
   ─► Hub (Geistwald) ─► Dungeon 1
```

- HP and MP **carry over** between nodes, as in a real run. This is the resource lesson.
- The map screen shows all 3 nodes. Future-node icons stay visible so the player learns what a combat and a boss node look like.

---

## 3. Node-by-node design

### Node 1 — First Combat: 2× Riftling [sheet]

**Lessons:** turn order bar, Basic Attack, Offensive QTE, target selection, Defensive QTE (Arrow), Skill + MP.

| Beat | Trigger | What happens / prompt |
| --- | --- | --- |
| 1 | Battle start | Turn-order bar highlights. Prompt: *"Units act from left to right. Faster units go first."* |
| 2 | First party turn | Only **Attack** is enabled. Pick a Riftling. |
| 3 | Offensive QTE #1 (×1.5 slow) | Pause before the bar starts: *"Press [Space] when the cursor reaches the bracket."* After the result: show the damage multiplier and "+1 MP" on Perfect. |
| 4 | Second party turn | **Skill** unlocks. Prompt points at the MP cost. |
| 5 | First Riftling Rift Claw → **Arrow** QTE (×1.5 slow) | *"Press the arrow shown, on the beat."* |
| 6 | Remaining turns | Full menu unlocked except Item. No more prompts. |

**Overrides:** Feint 0%, Stutter 0% for this node **[B]**. Riftling AI focuses one target per turn, so a 2-Riftling round puts at most 28 base damage on one character [sheet 2 × 14].

**Win condition:** both Riftlings dead. Expected 3–4 rounds **[B]**.

### Node 2 — Mixed Encounter: Shard Wisp + Veil Stalker [sheet]

**Lessons:** Ward QTE (magic), Feint, affinities, intent icons, status effects, target priority, Item.

**Proposed affinities (new data, [B]):**

| Enemy | Weak | Resistant | Reason |
| --- | --- | --- | --- |
| Shard Wisp | Physical ("crystal body shatters") | Lightning | Every party has Physical, so every player sees a **WEAK** popup. A Wizard/Warlock sees Normal, which is also information. |
| Veil Stalker | Holy, Fire | Shadow | Clerics see WEAK. Warlocks see RESIST. Martial parties use Ember Oil (below). |

**Ember Oil:** the party starts the Prologue with 1× Ember Oil (CON014, existing item). This guarantees a Fire source for any composition and teaches the **Item** command.

| Beat | Trigger | What happens / prompt |
| --- | --- | --- |
| 1 | Battle start | Veil Stalker shows a **status intent icon** (Dread Pulse) with the target portrait. Prompt: *"Icons show dangerous moves before they happen."* |
| 2 | Player inspects the enemies | Affinity row is revealed on first hit (WEAK / RESIST popups). |
| 3 | Shard Bolt → **Ward** QTE (×1.5 slow) | *"Magic is warded: type the runes shown."* |
| 4 | Dread Pulse resolves | **Scripted: first cast = 100% Fear** (overrides the sheet's 20% for this one cast) **[B]**. Prompt explains the status icon, duration, and that Fear lowers damage dealt. |
| 4-alt | Player kills Veil Stalker before it acts | Prompt: *"You stopped it before it acted. Reading intent pays."* The Fear explanation moves to Field Notes. **Both outcomes are wins.** |
| 5 | First Veil Strike after the first Arrow success | **Scripted Feint** (one hit) at ×1.5 speed: *"Some cues fake you out. Wait for the real one."* |
| 6 | Party turn with an Ember Oil holder | Item command highlighted. |

**Target-priority lesson:** Shard Wisp has low HP (70) and high MATK (16) [sheet]. Veil Stalker is tougher (95) but threatens a debuff. There is no right answer prompt. Both "kill the glass cannon" and "stop the debuffer" are valid, and the post-battle summary names what the player did.

**Overrides:** Feint only the scripted one, Stutter 0% **[B]**.

**Reward:** EXP that brings the party to Level 2 (§4.3), plus 2× Minor Health Potion and 1× Minor Mana Tonic. This shows a level-up and stocks the party for the boss.

### Node 3 — Aetherion, The Threshold Guardian [sheet]

**Lessons:** Break gauge, charge telegraph, Guard, Brace (Heavy), Arrow chain, Shared QTE, phase change, resource management.

**Variant map (from current `QteFactory.Defense` routing):**

| Attack [sheet] | Defensive input |
| --- | --- |
| Dimensional Strike | Arrow |
| Aether Bolt | Ward |
| Rift Pulse (Phase 2, all party) | Shared Ward |
| Threshold Collapse hits 1–3 | Arrow chain |
| Threshold Collapse hit 4 (Heavy) | **Brace** (press and hold through impact) |

**Signature timing rule (fix for problem 8) [B]:**

- Aetherion does **not** charge Threshold Collapse before round 3.
- From round 3 on, it charges when its turn comes **and** either its Break gauge has ≤ 40% left (80/200) **or** round ≥ 5.
- Result: a party that pressured the boss gets a *real* interrupt chance. A party that didn't still sees the signature and learns Guard + Brace. Both are teaching outcomes.
- ASSUMPTION: a 4-person level-2 party removes ~50 net Break per round (4 actions × ~18 Break − 20 regen). IMPACT: decides whether interrupt is reachable. IF WRONG: interrupt never or always happens. VALIDATE: `BalanceSimulation` run with 4 class mixes; target interrupt rate in §9.

| Beat | Trigger | What happens / prompt |
| --- | --- | --- |
| 1 | Battle start | Boss Break gauge highlighted: *"Hits wear this down. At zero, the target is Broken: it loses its next action and takes more damage."* |
| 2 | Charge turn | Charge icon with countdown and target portrait. Prompt (non-blocking): *"Threshold Collapse lands next turn on [Name]. Break it to cancel the attack, or have [Name] Guard."* Show the remaining gauge numerically. |
| 3 | Targeted character's turn | **Guard** highlighted: *"Guard halves damage. Guard + Perfect fully parries normal hits."* |
| 4a | Boss becomes Broken while charging | Big "INTERRUPTED" feedback (VFX + SFX + text). Prompt: *"Broken enemies drop their charged attack."* |
| 4b | Threshold Collapse releases (first time) | Arrow chain at ×1.5 speed, **no Feint/Stutter**. Hit 4 shows the Heavy border and the **Brace** prompt: *"Heavy: hold the button through the impact. Heavy hits can't be fully parried."* |
| 5 | HP ≤ 300 | Phase transition animation + one line of text [sheet]. Rift Pulse prompt: *"One ward protects everyone it hits."* |
| 6 | Any character < 35% HP, first time | Item prompt points at Minor Health Potion. |

**Feint/Stutter ramp [B]:** Phase 1 uses Normal-tier rates (Feint 15%, Stutter 20%). Phase 2 uses full Boss rates (40% / 50%) [GDD]. The first Threshold Collapse has none.

**Reward:** none from the boss. No Zoroa's Soul Seal [sheet]. Prologue clear reward in §4.3.

---

## 4. Tutorial system rules

### 4.1 Wipe handling (tutorial-only exception)

- A party wipe during the Prologue restarts **the current node**, with HP/MP and inventory exactly as they were on node entry. Unlimited retries.
- Fiction: *"The Threshold rejects you."* Aetherion pushes the party back out.
- After 2 wipes on the same node, offer QTE Assist settings (existing Relaxed/Auto). This is an offer only, never forced.
- On Prologue clear, show a full-screen card before the Hub: *"From here on, if the whole party falls, the expedition ends."* This turns the exception into a lesson about the hardcore rule.

### 4.2 Skip

- On New Game, if the profile has cleared the Prologue once, ask: **Play Prologue / Skip**. Default = Play.
- Skip grants **the identical** end state (Level 2, the same items, the same Gold). Skipping is never punished and replaying is never required.
- The "Prologue cleared" flag is the only cross-run data this needs. GDD Bab 2 §9.3 says meta progression is undecided, so this flag must be approved as a settings/profile flag, not a gameplay unlock.

### 4.3 Fixed rewards (fix for problem 3) [B]

| When | EXP per character | Items | Gold |
| --- | --- | --- | --- |
| Node 1 | 40 | — | 0 |
| Node 2 | 60 → total 100 = **Level 2** (`ExpToNext(1) = 100` [GDD]) | 2× Minor Health Potion, 1× Minor Mana Tonic | 0 |
| Node 3 / Prologue clear | 0 | — | 40 (≈ 2 Minor Health Potions at 18 each [GDD]) |
| Prologue start | — | 1× Ember Oil | — |

The Prologue must bypass `RunConfig.ExpFor` / `BattleGold` and use this table.

### 4.4 Determinism overrides

The Prologue runs with a **fixed RNG seed** per node, plus the scripted events above (Fear #1, Feint in Node 2, the signature timing rule). Hit Chance and Critical rolls stay random. They are not lessons, and fixing them would make the first real dungeon feel different.

---

## 5. Teaching matrix

| Mechanic | Introduced | Practised | First real test |
| --- | --- | --- | --- |
| Turn order / Basic Attack / Offensive QTE | N1 | N2, N3 | D1 |
| Skill + MP, Perfect +1 MP | N1 | N2, N3 | D1 |
| Arrow | N1 | N2, N3 | D1 |
| Ward | N2 | N3 (Aether Bolt) | D1 casters |
| Feint | N2 (scripted) | N3 P1 → P2 | D1 |
| Stutter | N3 Phase 1 (no prompt; the cue is readable) | N3 P2 | D1 |
| Intent icons | N2 | N3 | D1 |
| Status effects | N2 | — | D1 |
| Affinities | N2 | N3 (none set; Aetherion is Normal to all) | D1 |
| Item | N2 | N3 | D1 |
| Break / interrupt | N3 | — | D1 elites |
| Guard / full parry | N3 | — | D1 |
| Brace / Arrow chain / Shared | N3 | — | D1 boss |
| HP/MP carry-over | N1 → N3 | — | Whole run |
| Struggle (drain) | **Not taught.** No tutorial enemy drains. | — | First D1 drain enemy shows a first-time prompt |

---

## 6. 5-Component evaluation

| Component | Rating | Notes |
| --- | --- | --- |
| **Clarity** | Strong after fixes | Every dangerous action is telegraphed. Each new input is slow and solo the first time. Failure reasons are named. Weak point: two gauges (player Poise and enemy Break) on one HUD. Show the player gauge only above 50% fill (Bab 12 §3.1 mitigation). |
| **Motivation** | Medium | The characters are the player's own creation (high attachment), and node carry-over adds stakes. The retry rule lowers stakes on purpose. The end-card restores them. |
| **Response** | Strong | Turn-based, so no input latency risk outside QTE. QTE grace period and edge-trigger already exist [GDD]. Verify calibration offset is reachable from the Prologue pause menu. |
| **Satisfaction** | Needs assets | Interrupt, Perfect, WEAK and phase change each need at least 2 channels (VFX + SFX). "INTERRUPTED" is the Prologue's peak moment and needs its own SFX. |
| **Fit** | Good | Rift-born enemies at the boundary fit "The World's Between". Hardcore identity is stated explicitly at the end. |

---

## 7. Risks and abuse cases

| Risk | Mitigation |
| --- | --- |
| A Wizard-heavy party (low HP) wipes in N1 to focused Riftling claws. | AI focus is per Riftling, not shared. Retry rule. Test case P3 below. |
| A no-healer party runs dry before N3. | Node 2 potions. Assist offer after 2 wipes. |
| A player exploits retries to farm EXP. | EXP is fixed per node and granted once, on clear. |
| Skip is used to grab extra items. | Skip grants the same package once per New Game. |
| Interrupt never happens (Break too low) or always happens (too high). | Signature timing rule + telemetry target §9. |
| The Fear script makes Dread Pulse look stronger than it is in real play. | Only the first cast is scripted. Field Notes state the real chance. |
| Prompts annoy returning players who don't skip. | All prompts are off if the profile has a Prologue clear (still in Field Notes). |

---

## 8. Workbook cleanup (inconsistencies found)

1. **Rules & Open Items** says Aetherion's Break Gauge numbers are "belum ditentukan" and Threshold Collapse timing is "belum ditentukan". **Boss & Balance** and **Skill Design** give FINAL values (200; 350→298→280 ms). Update those two rows to FINAL.
2. **Dread Pulse** is "Telegraph: Yes — 1 turn prior". Per Bab 12 §6.4, status-only actions are telegraphed **from the start of the round**. Only charge attacks (the signature) are 1 turn prior. Change to "Yes — start of round".
3. **Node 2 "Elemental damage"** has no affinity data. Add an affinity column to Enemy Roster (proposal in §3 Node 2).
4. **Progression row (TBD)** → fill from §4.3. **Story row (TBD)** → see §10.
5. Add a **Defensive QTE variant** column to Skill Design (table in §3 Node 3), so the QTE routing is visible in data.

---

## 9. Playtest scenarios and tuning targets

Every target below is a **[B] starting value**: if it fails, move in the stated direction.

| # | Test | Pass | If it fails |
| --- | --- | --- | --- |
| P1 | New player, no explanation from the observer: complete the Prologue. | ≥ 7/8 testers clear N3 with ≤ 1 retry. | More retries → lower Aetherion ATK/MATK 10% or raise Node 2 potions to 3. |
| P2 | After the Prologue, ask "what does the icon above the enemy mean?" and "what does Guard do against a Heavy hit?" | ≥ 80% correct on both (mirrors the Bab 12 §6.4 test). | Make the prompt blocking for the first charge. |
| P3 | Run 4 fixed party mixes (4 martial / 4 caster / standard / no healer) in `BalanceSimulation`. | Every mix clears N1–N2 without a wipe in ≥ 90% of sims. | Lower Riftling ATK 14 → 12 for the failing mix. |
| P4 | Interrupt rate on the first Threshold Collapse (telemetry or sim). | 30–60%. | Below 30% → lower the ≤ 40% gauge trigger to ≤ 50%. Above 60% → raise it to ≤ 30%. |
| P5 | Duration (telemetry). | Median 15–20 min. Node 3 at most 9 min. | Too long → shorten prompts first, then boss HP 600 → 500. |
| P6 | Abuse: spam the QTE key through every node. | Poor ≥ 90% (Bab 12 §1.2 mash test). Still winnable thanks to retries. | — |
| P7 | Returning player skips. | Arrives at the Hub in < 30 s with an identical state. | — |
| P8 | Wipe at N3, retry. | HP/MP/items equal N3 entry values exactly. | Bug. |

---

## 10. Story beats (PROPOSAL — not canon)

GDD Bab 4 §8 leaves the protagonist and the opening event undefined. These beats only use canon facts and are meant to be replaced by the narrative owner.

1. **Open:** full moon over Geistwald. The portal opens. Text card on the Royal Expedition Regulation (each family sends one; 70/30 split). Your 4 conscripts step through.
2. **Node 1:** the Threshold, an unstable space just past the portal. Riftlings are "residue of the rift".
3. **Node 2:** the space distorts deeper. Wisps and Stalkers suggest something is *keeping* this edge.
4. **Node 3:** Aetherion holds the boundary between Felna and The World's Between [sheet lore]. It does not hate the party. It tests them.
5. **Close:** the core stabilises and the path to Layer 1 (the Ruined Kingdom) opens. The party returns to Geistwald to register and prepare, which is the Hub introduction.

---

## 10b. Implementation status (21 Nov 2026)

| Built | Where |
| --- | --- |
| Roster, affinities, encounters `tut_n1` / `tut_n2` / `tut_boss` | `Scripts/Data/PrologueRoster.cs` (kept out of `Database.Encounters`; looked up by `Database.Encounter(id)`) |
| 3-node map, fixed rewards, fixed node RNG, QTE ramp, scripted Fear/feint | `Scripts/Run/Prologue.cs` |
| Begin / Skip / End, node-entry snapshot, wipe → retry node | `RunState` (`BeginPrologue`, `SkipPrologue`, `EndPrologue`, `RetryPrologueNode`) |
| Signature gate (≤ 40% Break left or round ≥ 5) | `EnemyAttack.aiBreakLeftBelow` / `aiBreakGateRound`, checked in `PriorityBrain.Select` |
| Flow: Creation → intro (Play / Skip) → Prologue → clear card → Hub | `Game.EnterPrologueFromCreation`, `OpeningSequenceScreen`, `PrologueClearScreen`, `GameSettings.prologueCleared` |
| Combat prompts | `Scripts/Screens/PrologueCoach.cs` (partial of `CombatView`) |
| Tests | `Tests/Editor/PrologueTests.cs` |

Not built (cut for now, add when playtests ask for them):
- **Ember Oil.** The catalogue item has no combat effect in code yet. Node 2 still shows a WEAK popup to every party through the Shard Wisp's Physical weakness.
- **Locking the action menu to Attack on the first turn.** A blocking prompt explains the menu instead.
- **Field Notes log.** Prompts are copied into the combat log instead.
- **"Why it was Poor" text.** This is a QTE panel change, not a Prologue one.
- **Node 2 items** use the in-combat consumables (`draught` 40 HP, `ether` 20 MP), because the catalogue's Minor Health Potion / Mana Tonic are not usable in combat.

## 11. Owner decisions (accepted 21 Nov 2026)

1. **Tutorial-only retry rule** (§4.1): ACCEPTED. A wipe in the Prologue restarts the current node. The hardcore rule applies from Dungeon 1 on.
2. **Profile flag for skip** (§4.2): ACCEPTED as a settings/profile flag. It is not a meta-progression unlock.
3. **Flow**: ACCEPTED. Prologue → Hub → Dungeon 1.
4. **New affinity data** for Shard Wisp / Veil Stalker (§3 Node 2): ACCEPTED.
5. **Prologue ends at Level 2**: ACCEPTED.
