# FELNA — QTE VARIATION SYSTEM

**Document ID:** FELNA-QTE-VARIATION-001
**Version:** 1.0
**Status:** Proposed — System Specification (additive to GDD Bab 12)
**Implementation plan:** `QTE_Variation_Handoff.md`
**Related:** `Felna_Enemy_AI_System_Spec_v1_1.md`, `Felna_Enhanced_Common_Variant_Spec_v1_1.md`

---

## 0. Why this exists

The QTE system already has wide surface variety:
- **Offense:** a sweep bar with a random zone, Charge (hold then release), MovingTarget, Combo multi-zone, and Typing incantations.
- **Defense:** tap, dodge arrows, feints, stutter cues, interval jitter, the Heavy brace, Ward (typed runes against magic), and Struggle (mash against drain).

The gap is **variation within each type that the player can learn**:
- **Almost all variation is random.** Arrows, feints, stutters and zone positions are rolled for each attack. So defending is a reaction test that never becomes knowledge, and a Hollow Squire feels the same to defend against as a Gravebound Archer. That contradicts the AI and Variant specs' goal that *the player learns enemy behaviour through observation*.
- **There's a single action key.** A press can only mean *when*, never *what*.
- **Basic attacks are all identical.** The most frequent QTE is always the same sweep bar, whatever weapon the hero holds.

This system adds **variations** of the established QTE types. It does not change any QTE that already exists.

---

## Part A: Principles

0. **Additive only.** Every established QTE stays exactly as it is:
   - on offense: the sweep bar, Charge, MovingTarget, Combo and Typing;
   - on defense: the tap on the QTE key, random dodge arrows, feints, stutter, jitter, Brace, Ward and Struggle;
   - all keys, windows and prompts.

   Each of these becomes the **base variant** of its type, and this spec only **adds** new variants beside them. An attack with no authored variation data plays exactly as it does today.
1. **Variation must be learnable.** An authored move's QTE is the same every time unless a *visible* remix says otherwise. Mastery means recognising the enemy.
2. **Windows never change.** Under GDD Bab 12 §8.2, timing windows come only from:
   - the tier profile;
   - the assist setting;
   - equipment and talent bonuses (clamped to ±25%);
   - the 66 ms Perfect floor.

   Variations change the **shape** (which verb, which direction, which rhythm). Distortions change only the **presentation**.
3. **Every new input has a meaning.** The QTE key means *act*; the new Guard key means *defend*. Each verb maps to fiction: tap, dodge, brace, guard, slip, flurry, roll.
4. **Complexity is budgeted by tier**, so Normal fights stay readable (§D.3).
5. **Signals are redundant.** Every new verb has its own **shape, colour and label**, so it never relies on colour alone.

---

## Part B: Inputs

| Key | Default (keyboard / pad) | Meaning |
|---|---|---|
| **QTE key** (unchanged) | Space / South (A) | Every established QTE, exactly as today |
| **Guard** *(new; used only by new variants)* | Left Shift / East (B) | The Guard cue, Slip, Flurry, and the key half of a Roll |
| Arrows | ←↑→↓ / D-pad or left stick | Dodge (unchanged), and the arrow half of a Roll |
| Letters | Keyboard | Typing (incantation and Ward), unchanged |

- Every key can be remapped.
- **Accessibility, Single-key mode:** Guard-key variants also accept the QTE key. Each verb still asks for its own action; only the key stops mattering.

---

## Part C: Verbs

### C.1 Defense

| Verb | Input | Glyph | Status |
|---|---|---|---|
| **Tap** | Tap the QTE key at impact | Today's cue | **Established, unchanged** |
| **Dodge** | Tap the shown arrow at impact | Arrow | **Established, unchanged.** An authored motif may *fix* the arrow. Unauthored moves stay random |
| **Brace** | Hold through impact | Cracked half-shield | **Established, unchanged** (Heavy hits) |
| **Guard cue** *(new, a Tap variant)* | Tap **Guard** at impact | Blue shield | Same windows as Tap. This is the button-command variation |
| **Slip** *(new, a Brace variant)* | Hold Guard from when the cue appears, then **release** at impact | Shield with an open notch | For lunges and sweeps; the reverse of Brace |
| **Flurry** *(new, a Tap variant)* | Two presses: one at impact, one at impact + `FlurryGap` | Cue with two pips | A double strike tighter than the 200 ms minimum interval. Each press is judged; the cue's grade is the worse of the two |
| **Roll** *(new, a Dodge variant)* | Arrow and Guard together (no more than `ChordTolerance` apart) at impact | Arrow inside a shield | Elite and above, signature hits only |

These stay as they are: **Feint** (a fake cue that recoils, so don't press), **Stutter** (a cue whose speed breaks), **Ward** (typing against magic) and **Struggle** (mashing against drain).

**Authored feints and stutters.** An authored motif states *which* hit may feint or stutter. In **Normal-tier** motifs a feint always has a **tell**: the fake cue flickers once when it appears. Unauthored moves keep today's random feint and stutter chances.

### C.2 Offense: new variants of the established types

The established sweep bar, Charge, MovingTarget, Combo and Typing stay the base variants. New variants are added **alongside** them, and all use the same windows:

- **Sweep:**
  - **Ease-in:** the indicator accelerates.
  - **Ease-out:** the indicator decelerates.
  - **Double-pass:** the indicator returns once, and either pass may be hit.
- **Charge, Thrust:** a short hold with a narrow release zone.
- **Combo, Echo zones:** the second zone mirrors the first zone's position.
- **Typing, Cadence:** letters are typed on a metronome beat, graded by the same Perfect/Good rules.

### C.3 Weapon Strike Signatures

For Basic Attack and single-hit physical skills, the weapon family in the main hand picks the default variant. It sets the **shape** only, never the windows.

| Weapon family | Strike Signature |
|---|---|
| Sword, Axe, Mace, none | Today's sweep, **unchanged** |
| Dagger, RitualBlade | Ease-in sweep |
| DualDaggers, Fist, Gauntlet | Double-pass sweep |
| Greatsword, Greataxe, Warhammer, Scythe | Ease-out sweep (heavy wind-up feel) |
| Spear, Polearm, Rapier | Thrust |
| Bow, Crossbow | Today's MovingTarget, **unchanged** |
| Wand, Staff, Rod, Scepter, Tome | Today's Typing, **unchanged** |

Skill-specific kinds still take priority: Typing for magic, Combo for multi-hit and AoE skills, Charge for skills with power ≥ 1.8. Each family has exactly **one** signature, so the player learns *their own* weapon.

---

## Part D: Motifs, rhythm and remix

### D.1 Motif
A motif is authored per enemy move (physical hits):
- for each hit, a variant (a verb) and a direction;
- the hit that may feint, if any;
- the hit that may stutter, if any, and which kind;
- an optional set of remixes.

Rhythm uses the existing per-move `intervals`.

### D.2 Rhythm vocabulary and tier floors
| Rhythm | Shape | Allowed for |
|---|---|---|
| Steady | Equal 350 ms gaps | All tiers |
| Accelerando | Shrinking gaps | Enhanced (≥ 310 ms), Elite (≥ 310 ms), Boss (≥ 280 ms) |
| Ritardando *(new)* | Growing gaps; a late hit catches an impatient player | Elite, Boss |
| Syncopated *(new)* | Long, short, long (for example 420 / 280 / 420) | Boss signature only (GDD) |

### D.3 Complexity budget per attack (posture B, starting values)
| Tier | Max distinct verbs | Allowed verbs | Max hits |
|---|---|---|---|
| Normal | 2 | Tap, Dodge, Brace (established), plus Guard cue and Slip | 2 |
| Enhanced | 2 | Normal's verbs + Flurry | 3 |
| Elite / Miniboss | 3 | Enhanced's verbs + Roll | 4 |
| Boss | 4 | All | 4 (signature 4–6 per GDD) |

### D.4 Remix (Elite, Boss and Enhanced only; always visible)
A remix is a deterministic transform of the authored motif:
- **Mirror:** arrows flip left↔right and up↔down.
- **Upgrade:** one Tap becomes a Guard cue or a Flurry, or one Dodge becomes a Roll, where the tier's budget allows.
- **Tempo:** Steady becomes Accelerando, at the tier's floor.

**A remix is always shown in the telegraph**, as a "REMIX" mark plus the transformed row of glyphs. It is never secret.

Remix chance (posture B): Elite 25%, Boss 35%. **Enhanced forms always use their remix**, as their alternative pattern (Variant Spec v1.1 §6).

### D.5 Fallback
A move with no authored motif uses today's random generator. Variation is added move by move, and nothing that exists today changes.

---

## Part E: Status distortions (presentation only)

A distortion applies when the **hero who is reacting** (defense) or **acting** (offense) carries the status.

**Hard rules:**
- **Windows, cue timing and required inputs never change.** A test asserts identical judgment with and without a distortion.
- The last `ClearZone` before an impact is **always undistorted**.
- Only **one** distortion shows at a time: the highest in the priority order below.
- An accessibility toggle, **"Reduce QTE distortions"**, turns them all off.

| Status (highest priority first) | Distortion |
|---|---|
| Fear | Cues shake (no more than `ShakePx`); the impact line stays still |
| Confusion | Glyphs lose their colour coding; shape and label remain |
| Despair | The bar desaturates and the QTE audio tick is muffled, leaving only the visual cue |
| Shock | A brief flicker of the bar (60 ms or less, never inside `ClearZone`) |
| Slow | Cues leave afterimage trails |
| Freeze | A frost vignette creeps over the bar's outer 20% |
| Burn, Poison, Bleed | A light pulse at the screen edge |

---

## Starting values (posture B, not final)

| Knob | Value | Pass test | If it fails |
|---|---|---|---|
| `FlurryGap` | 150 ms | Good+ rate on Flurry cues is within 15 pp of Tap cues at the same tier | 180 ms |
| `ChordTolerance` | 80 ms | Roll Good+ rate ≥ 60% | 110 ms |
| `ClearZone` | 150 ms before impact | Good+ rate on distorted cues is within 5 pp of undistorted ones | 220 ms |
| `ShakePx` | 4 px | Same as `ClearZone` | 2 px |
| Remix chance | Elite 25%, Boss 35% | ≥ 70% of players call remixes "noticeable but fair" | ±10 pp |
| **Mastery metric** | — | The Perfect rate on the same motif rises **≥ 10 pp from the 1st to the 3rd exposure** | Simplify motifs that don't improve |
| Normal-tier budget | 2 verbs / 2 hits | Defense Good+ rate in Normal fights drops no more than 5 pp versus today | Cut to 1 verb |

The mastery metric is the proof that this system works: variation that players can't learn is just noise.
