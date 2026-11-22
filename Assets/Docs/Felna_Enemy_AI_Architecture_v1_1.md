# FELNA — ADVANCED ENEMY AI ARCHITECTURE

**Document ID:** FELNA-AI-ARCH-001
**Version:** 1.1 (amended)
**Status:** Proposed — Architecture Design
**Scope:** Enemy AI across Dungeon 1–8
**Combat Model:** Turn-Based Individual Character Turns + Reaction QTE
**Party Size:** 4 Characters
**Companion documents:** `Felna_Enemy_AI_System_Spec_v1_1.md`, `Enemy_AI_Implementation_Handoff.md`

---

## Change log

| Version | Date | Change |
|---|---|---|
| 1.0 | — | Original architecture |
| 1.1 | 2026-11-22 | **A.1** Approved Zoroa exceptions; shipped code wins (§12.5, §11.3, §11.4, §15). **A.2** Planning order is independent of initiative (§13.2). **A.3** One encounter memory shared by every enemy (§4.2). **A.4** Locked targeting is mandatory (§7.4). **A.5** Observable information is defined exactly (§3.3). **A.6** One-time log reads (§11.5). **A.7** Starting values (§18). **A.8** Behavior trees are scoped to a fixed pipeline plus data condition lists (§8.3). **A.9** Concrete two-scenario prediction (§6.6). **A.10** Concrete confidence model (§4.1). **A.11** Capabilities by intelligence tier (§12). **A.12** Synergy derived from existing mechanics only (§9.4). Amended passages are marked **(v1.1)** |

---

# 1. System Overview

## 1.1 Purpose

The Advanced Enemy AI System defines how enemies perceive the battlefield, formulate tactical objectives, coordinate with allies, predict near-future combat situations, and adapt to player behavior.

The system extends the original Hybrid AI architecture through:

1. Strategic Tactical Planning.
2. Predictive Combat Evaluation.
3. Flexible Team Coordination.
4. Encounter-Based Player Modeling.
5. Tactical Synergy Planning.
6. Advanced Resource Management.
7. Counterplay-Aware Decision Making.
8. Individual Enemy Personality and Behavior Trees.

The system must produce intelligent enemy behavior without introducing arbitrary difficulty or violating established combat mechanics.

## 1.2 Confirmed Design Decisions

| Parameter | Decision |
|---|---|
| AI architecture | Hybrid AI |
| Action selection | Rule-Based + Utility AI |
| Strategic planning | Tactical Strategy Planner |
| Predictive horizon | Two future turn opportunities |
| Team coordination | Flexible, decentralized |
| Adaptation | High |
| Learning scope | Current encounter only |
| Player modeling | Behavior recognition |
| Tactical memory | Encounter-local, **(v1.1)** one memory shared by every enemy in the encounter |
| Decision randomness | Controlled and limited. **(v1.1)** None on the roster AI path |
| Planning execution | Round-based |
| Planning order | **(v1.1)** Fixed role order, independent of initiative (§13.2) |
| Initiative | Existing Dynamic Initiative Queue |

## 1.3 Core Design Philosophy

The AI follows Felna's Relentless Tactical Survival philosophy.

Enemies are permitted to:
- Exploit mistakes.
- Recognize repeated strategies.
- Coordinate attacks.
- Pressure vulnerable characters.
- Protect endangered allies.
- Plan future opportunities.
- Punish inefficient resource management.

However, they must not:
- Predict player commands.
- Access hidden player information.
- Ignore established skill conditions.
- Manipulate QTE timing.
- Secretly change telegraphed actions.
- Gain additional actions through AI behavior.
- Increase their statistics through adaptation.

**The AI should become more intelligent, not mechanically stronger.**

---

# 2. Advanced Architecture

## 2.1 Architecture Overview

The system consists of six major layers.

| Layer | Component | Responsibility |
|---|---|---|
| 1 | Battlefield Perception | Understand current combat conditions |
| 2 | Tactical Intelligence | Recognize player behavior and maintain encounter memory |
| 3 | Strategic Planning | Determine the team's tactical objectives |
| 4 | Coordination | Share intentions and opportunities between enemies |
| 5 | Individual Decision Making | Select actions and targets using hybrid AI |
| 6 | Execution and Validation | Validate and execute actions through existing combat systems |

### Architecture Flow

```text
                  COMBAT STATE
                       |
                       v
              BATTLEFIELD PERCEPTION
                       |
                       v
              TACTICAL INTELLIGENCE
                /            \
               v              v
        PLAYER MODELING   TACTICAL MEMORY      (v1.1: both live on the encounter Blackboard)
               \              /
                \            /
                 v          v
              STRATEGIC PLANNER
                       |
                       v
              SHARED BLACKBOARD
                       |
             FLEXIBLE COORDINATION
                       |
                       v
              INDIVIDUAL ENEMY AI
                 /          \
                v            v
          RULE-BASED       UTILITY AI
                \            /
                 \          /
                       v
               PREDICTIVE EVALUATION
                       |
                       v
                COUNTERPLAY CHECK
                       |
                       v
                 ACTION SELECTION
                       |
                       v
                ACTION VALIDATION
                       |
                       v
                COMBAT EXECUTION
                       |
                       v
                MEMORY UPDATE
```

The architecture is modular. Each component can be implemented, tested, and balanced independently.

---

# 3. Battlefield Perception Layer

## 3.1 Purpose

Battlefield Perception provides the information required by the AI.

It converts the current combat state into a structured representation that other AI components can evaluate.

## 3.2 Information Collection

The perception system reads:

**Party Information**
- Current HP and Max HP.
- Current MP and Max MP, where observable.
- Active and defeated status.
- Down and control conditions.
- Defensive conditions.
- Active status effects.
- Observable character actions.
- Observable damage results.

**Enemy Information**
- Current HP and Max HP.
- Current MP and Max MP.
- Available skills.
- Skill cooldowns.
- Break Gauge and Broken state.
- Active status effects.
- Enemy role and personality.
- Current planned action.
- Previous actions.

**Combat Information**
- Current round.
- Initiative queue.
- Enemy spawn order.
- Planned enemy intents.
- Telegraph state.
- Encounter phase.
- Active tactical objectives.

## 3.3 Information Restrictions

The perception system must distinguish three information categories.

| Category | Description | AI Access |
|---|---|---|
| Observable | Information directly available through combat events | Allowed |
| Derived | Information calculated from observable events | Allowed |
| Hidden | Information unavailable to enemies | Prohibited |

**(v1.1) Exact definition:**
- **Observable:** everything shown on the combat HUD (HP, MP, statuses, Guard, Broken, Poise, turn order, telegraphs) and every event of this encounter (actions, damage numbers, elements used, effectiveness shown, QTE grades of resolved hits).
- **Derived:** values computed only from observable information.
- **Hidden:** inventory counts, skills a hero has not used yet, unresolved QTE grades, any affinity not yet shown in this encounter.

For example, the AI can recognize that a character repeatedly uses Fire attacks.

It cannot automatically know that the character intends to use Fire on their next turn.

The AI may evaluate known elemental effectiveness based on available encounter information, but it must not receive undiscovered information simply because the player possesses it.

---

# 4. Tactical Intelligence Layer

This layer contains two components:

1. Player Behavior Modeling.
2. Encounter Tactical Memory.

## 4.1 Player Behavior Modeling

The AI maintains a behavioral profile for each active party member.

The purpose is to recognize repeated patterns, not predict individual commands.

### Behavioral Categories

| Category | Recognition Criteria |
|---|---|
| Aggressive | Frequently uses offensive actions |
| Defensive | Frequently uses Guard or defensive abilities |
| Burst Damage | Concentrates damage into short periods |
| Elemental Specialist | Frequently uses elemental attacks |
| Support Dependent | Frequently relies on healing and support |
| Focus Fire | Repeatedly concentrates attacks on one enemy |
| Break Specialist | Frequently attempts to interrupt enemies through Break |

A character may possess multiple behavioral characteristics.

### Behavioral Confidence

Each observed behavior receives a confidence value.

The conceptual model is:

`Confidence = f(Observation Frequency, Recency, Consistency)`

The following conditions affect confidence:

- Repeated behavior increases confidence.
- Recent behavior receives greater influence.
- Contradictory behavior reduces confidence.
- Old behavior gradually loses influence.
- One isolated action should not establish a strong behavioral profile.

Numerical confidence thresholds remain TBD.

**(v1.1) Concrete model (starting values, §18):**
- **Weighted count:** `s = Σ w · decay^age − contradiction`. Each observation adds `w = 1` and loses weight with its age in rounds.
- **Contradicting pairs:** Aggressive ↔ Defensive, and Support Dependent ↔ Burst Damage. Each opposite observation subtracts the contradiction weight.
- **Confidence:** `confidence = s / (s + k)`, clamped to [0, 1].
- **Recognized:** a category is recognized at `confidence ≥ RecognizeAt`.
- **Examples:** with `k = 2`, one observation gives 0.33, which is never recognized. Three recent consistent observations give about 0.6, which is recognized.

### Example

A Mage repeatedly uses Fire attacks against enemies vulnerable to Fire.

The AI gradually recognizes the pattern.

It may respond by:
- Prioritizing an exposed Mage.
- Coordinating pressure against that character.
- Selecting other tactically useful targets.
- Reconsidering inefficient defensive positioning.

It must not gain automatic Fire immunity.

## 4.2 Encounter Tactical Memory

Tactical Memory records relevant combat events.

**(v1.1) Scope:** there is **one** tactical memory per encounter, together with all player behavior profiles. It lives on the encounter Blackboard and is **shared by every enemy in that encounter**. No enemy has a private memory.
- It persists through a boss phase change.
- A summoned enemy reads it from the moment it joins.

### Memory Categories

| Memory | Information |
|---|---|
| Player Action History | Previously observed player actions |
| Target History | Characters frequently attacked |
| Defensive History | Guarding and protection patterns |
| Damage History | Observed damage types and effectiveness |
| Break History | Successful enemy interruptions and player Break attempts |
| Enemy Action History | Previously selected enemy actions |
| Coordination History | Previous enemy target assignments |
| Tactical Outcome History | Successful and ineffective decisions |

### Memory Lifecycle

Memory follows this lifecycle:

```text
Encounter Begins
       |
       v
Initialize Tactical Memory            (v1.1: one instance, on the encounter Blackboard)
       |
       v
Observe Combat Events
       |
       v
Update Player Behavior Profiles
       |
       v
Evaluate Current Tactical State
       |
       v
Adjust Enemy Decision Priorities
       |
       v
Encounter Ends                         (v1.1: victory, defeat, or retreat)
       |
       v
Destroy Encounter Memory
```

**Learning scope is strictly encounter-local.**

Memory must not persist:
- Between different encounters.
- Between dungeon nodes.
- Between different dungeons.
- Between separate runs.

Enemies may have predefined personalities and behavior profiles, but those are configuration data, not learned player information.

---

# 5. Strategic Tactical Planner

## 5.1 Purpose

The Strategic Tactical Planner determines what the enemy team should attempt to accomplish.

It operates above individual action selection.

Instead of immediately selecting a skill, the planner evaluates the encounter and identifies useful team objectives.

## 5.2 Tactical Objectives

The following objectives are proposed.

| Objective | Description |
|---|---|
| Focus Assault | Concentrate pressure on an appropriate target |
| Defensive Formation | Preserve endangered enemy allies |
| Disruption | Pressure important party roles |
| Attrition | Gradually exhaust party resources |
| Opportunistic Attack | Exploit vulnerable targets |
| Area Pressure | Capitalize on multiple active party members |
| Recovery | Restore enemy HP or MP when appropriate |
| Break Pressure | Exploit opportunities involving Break |

## 5.3 Strategy Selection

The planner evaluates:

- Current party condition.
- Current enemy condition.
- Enemy team composition.
- Enemy skill availability.
- Current target assignments.
- Player behavior profiles.
- Encounter phase.
- Projected combat opportunities.

It selects one or more tactical objectives when useful.

A tactical objective is a recommendation to the enemy team, not a mandatory command.

## 5.4 Objective Priority

Each objective receives a priority score.

Conceptual model:

`ObjectiveScore = TacticalValue + EncounterContext + RoleCompatibility + AdaptationValue`

The planner may change its preferred objective as the encounter develops.

For example:

- At the beginning of combat, Focus Assault may be valuable.
- When an enemy ally becomes critically injured, Recovery may become more valuable.
- When the party demonstrates strong defensive behavior, Disruption may become more valuable.

Strategic objectives do not create new combat abilities.

## 5.5 Objective Application (v1.1)

Objectives act on **action tags**: SingleDamage, AoE, Heal, MpRestore, Status, BreakPower and Protect.
- An active objective adds a bounded bonus (≤ `ObjectiveBonusCap`) to the utility of actions carrying its tags.
- That bonus sits inside the utility clamp, so an objective can never override a mandatory rule.
- **Hysteresis:** the current objective is kept unless a challenger beats it by a margin. This stops objectives flapping from round to round.

---

# 6. Predictive Combat Evaluation

## 6.1 Planning Horizon

**Decision: Two-turn predictive planning.**

The AI evaluates its current decision and projects the consequences across up to two subsequent initiative opportunities.

The planning horizon is:

`H = 2 Future Turn Opportunities`

The current action is evaluated first, followed by the projected state over the next two relevant turn opportunities.

The horizon is not permission to predict player commands.

## 6.2 Planning Information

The predictive system can use:

- Current initiative queue.
- Enemy actions already planned.
- Published enemy intents.
- Known skill effects.
- Current HP and MP.
- Known cooldowns.
- Deterministic combat effects.
- Existing target assignments.

It must not assume that a player will:
- Guard.
- Heal.
- Attack.
- Interrupt.
- Use a specific skill.
- Preserve or change a particular target.

When the forecast reaches an unknown player decision, the AI must stop making deterministic assumptions about that decision.

It may evaluate multiple possible outcomes, but must not treat an unobserved player action as certain.

## 6.3 Projected Battlefield State

The system creates a temporary projected state.

`ProjectedState = CurrentState + KnownCommittedEffects`

Examples of known effects:
- A committed enemy action.
- A published telegraphed attack.
- A deterministic charge release.
- A known healing action.
- A known cooldown transition.

The system uses the projection to identify opportunities and inefficiencies.

## 6.4 Predictive Use Cases

**Overkill Prevention**

If an enemy has already committed sufficient damage to a target, another enemy may consider a different target.

**Support Protection**

If a healer is likely to be defeated before its next opportunity, other enemies may evaluate defensive or offensive actions that preserve its survival.

**Resource Planning**

An enemy may conserve MP when a more valuable action is expected to become available later.

**Target Opportunity**

An enemy may choose a target based on the projected battlefield rather than only current HP.

**Charge Planning**

Royal Ossuary Knight may evaluate whether starting its charge is tactically appropriate, considering the possibility of player interruption.

Its established charge mechanics remain unchanged.

## 6.5 Predictive Uncertainty

Predictions must carry a confidence level.

| Prediction | Confidence |
|---|---|
| Already committed deterministic enemy effect | High |
| Existing telegraphed action | High |
| Expected result of an enemy attack | Estimated |
| Player response | Unknown |
| Uncommitted player action | Unknown |

Low-confidence predictions should have less influence on action selection.

The system must never make an important decision solely because it assumes the player will behave in a particular way.

## 6.6 Concrete Projection Model (v1.1)

1. **Horizon.** Project over *this enemy's* next 2 turn opportunities.
2. **Committed effects** are the only deterministic inputs:
   - published intents;
   - charge releases;
   - scheduled DoT and status ticks;
   - cooldown expiries;
   - expected damage (the mean, without crits).
3. **Party actions are unknown.** Each candidate is scored in two scenarios:
   - **Party passive:** no party actions.
   - **Party pressure:** each living hero repeats its *observed* per-turn average output against its *observed* most-frequent target.
4. **FutureValue** = `PassiveShare × passive + PressureShare × pressure × confidence`. Here *confidence* is the confidence of the observed averages (§4.1). Before a hero has acted, its pressure contribution is zero.
5. **Cost.** The prediction is analytic (no search tree). It is enabled for Elite tier and above (§12).

---

# 7. Flexible Enemy Coordination

## 7.1 Coordination Philosophy

Enemy coordination is decentralized and flexible.

There is no permanent commander, fixed formation, or mandatory assignment of targets.

Each enemy:
- Maintains its own personality.
- Evaluates its own available actions.
- Receives shared battlefield information.
- Considers its allies' planned actions.
- Selects its own action.

The Shared Blackboard supports cooperation without removing individual decision-making.

## 7.2 Shared Blackboard

The Blackboard stores:

| Data | Purpose |
|---|---|
| Current Party State | Shared understanding of party condition |
| Current Enemy State | Shared understanding of enemy condition |
| Planned Actions | Records enemy intentions |
| Target Reservations | Identifies targets already assigned. **(v1.1)** Includes lethal reservations |
| Expected Outcomes | Tracks projected action results |
| Tactical Objectives | Stores current team priorities |
| Recent Events | Records important combat events |
| Enemy Opportunities | Identifies potential synergies |
| Encounter Phase | Tracks boss or encounter state |
| **(v1.1)** Encounter Memory + Player Profiles | The single shared memory of this encounter (§4.2) |

## 7.3 Intent Reservation

When an enemy selects an action, it publishes its planned action.

Example:

```text
Enemy A:
    Action: Soul Snarl
    Target: Party Mage

Enemy B:
    Action: Grave Arrow
    Target: Party Mage
```

Enemy B evaluates Enemy A's planned action before finalizing its own decision.

Possible outcomes:

- Continue focusing the Mage.
- Choose another target.
- Select a different legal action.
- Preserve the original plan because focus fire is valuable.

The system does not force target distribution.

**(v1.1)** Two exceptions:
- **Lethal reservation:** a target whose HP is already covered by earlier committed damage is reserved, and later planners are penalised for choosing it.
- **Focus cap:** at most `FocusCapNormal` Normal-tier enemies may plan single-target damage on the same hero per round. Elites and bosses are exempt.

## 7.4 Flexible Coordination Rules

Coordination is implemented through soft tactical preferences.

A tactical objective can increase or decrease an action's utility, but it cannot override:
- Mandatory skill rules.
- Explicit target rules.
- Legal action restrictions.
- Cooldowns.
- MP requirements.
- Telegraph commitments.

### Important Existing Rule

The Dungeon 1 Elite Design Lock specifies that Elite enemies target the party member with the lowest current HP.

That rule remains authoritative.

The flexible coordination system must not override it unless a future version of the encounter specification explicitly changes it.

Reliquary Confessor also retains its specific healing condition and lowest-HP-percentage ally selection.

**(v1.1) Locked targeting is mandatory.** The locked rules are the Elite lowest-HP lock, Gravebound Archer's lowest-DEF rule, and the Confessor's lowest-HP% heal target. Utility may only:
- skip a candidate that is already **reserved as lethal**, then choose the next one by the **same rule**;
- break exact ties.

---

# 8. Individual Enemy Intelligence

## 8.1 Hybrid Decision System

Each enemy uses three complementary decision mechanisms.

| Mechanism | Responsibility |
|---|---|
| Rule-Based AI | Mandatory behavior and skill restrictions |
| Behavior Trees | Conditional sequences and complex workflows |
| Utility AI | Tactical action and target selection |

The mechanisms operate together.

Behavior Trees organize the decision process.

Rule-Based AI guarantees correctness.

Utility AI determines which legal choice is most useful.

## 8.2 Rule-Based AI

Rules have the highest priority.

Examples:
- Use Mending Litany when its finalized conditions are satisfied.
- Use Arcane Charge according to its finalized MP threshold and cooldown.
- Cancel Royal Ossuary Knight's charge when Broken before release.
- Use MP-free Basic Attack when no eligible skill can be selected.
- Respect boss phase and signature attack restrictions.

Existing AI use rules are authoritative.

## 8.3 Behavior Trees

Behavior Trees handle complex enemy-specific workflows.

Example:

```text
Enemy Behavior
│
├── Validate State
│   ├── Defeated?
│   ├── Down?
│   └── Controlled?
│
├── Check Mandatory Rules
│
├── Check Special Action State
│   ├── Charging?
│   ├── Healing Condition?
│   └── Phase-specific Rule?
│
└── Tactical Decision
    ├── Evaluate Available Actions
    ├── Evaluate Targets
    ├── Read Tactical Objectives
    ├── Read Encounter Memory
    └── Select Best Legal Decision
```

**(v1.1) Scope:** the tree above is implemented as a **fixed code pipeline** shared by every enemy. There is no behavior-tree runtime or editor.
- Enemy-specific workflows are small **data-declared condition lists** on the enemy's AI profile. They are used only by minibosses and the boss, for example "start Graveward Charge only if own Break gauge ≥ `ChargeShieldGate`".
- **Rationale:** the roster has 15 enemies with fewer than 6 moves each. A BT runtime would cost more than the behavior it expresses, and the pipeline keeps every decision traceable (§17, AI trace).

## 8.4 Utility AI

Utility AI evaluates available legal actions.

Proposed formula:

`ActionScore = ImmediateValue + FutureValue + RoleBonus + SynergyBonus + AdaptationBonus - ResourceCost - RepetitionPenalty - RiskPenalty`

**(v1.1)** In implementation:
- `score = aiPriority × 10 + clamp(Immediate + Future + Role + Synergy + Objective + Adaptation − Resource − Repetition − Risk, −UtilityClamp, +UtilityClamp)`.
- The authored priorities stay the backbone. With every modifier at zero, decisions match the current shipped behavior exactly.

### Evaluation Factors

| Factor | Purpose |
|---|---|
| Immediate Value | Immediate damage, healing, or tactical benefit |
| Future Value | Potential benefit across the predictive horizon |
| Role Bonus | Compatibility with enemy personality |
| Synergy Bonus | Contribution to allied actions |
| Adaptation Bonus | Response to observed player behavior |
| Resource Cost | Opportunity cost of spending MP |
| Repetition Penalty | Avoid repeated ineffective decisions |
| Risk Penalty | Accounts for tactical disadvantages |

All factors require normalization before final implementation.

Numerical weights remain TBD. **(v1.1)** Starting values are in §18.

## 8.5 Deterministic Selection

The AI selects the highest-scoring legal decision.

If multiple decisions have equal scores, use deterministic tie-breaking.

Suggested order:

1. Explicit mandatory priority.
2. Higher tactical utility.
3. Better resource efficiency.
4. Lower repetition.
5. Stable action identifier.

Random selection must not be used to create artificial difficulty.

---

# 9. Tactical Synergy Chains

## 9.1 Purpose

Tactical Synergy Chains allow enemies to recognize opportunities created by their allies.

A chain consists of:

1. Setup.
2. Opportunity.
3. Follow-up.

## 9.2 Example

Soulbound Hound and Soul Reaper:

**Setup:** Soulbound Hound uses Soul Snarl.

**Opportunity:** The result is evaluated and recorded in the Blackboard.

**Follow-up:** Soul Reaper evaluates its available attacks and may exploit the resulting tactical opportunity.

The follow-up is not guaranteed.

## 9.3 Synergy Evaluation

The AI evaluates:

- Whether the setup is likely to be effective.
- Whether the follow-up is available.
- Whether the setup consumes a valuable turn.
- Whether the target remains a useful target.
- Whether another tactical objective has higher value.

Synergy must use existing skill effects.

It must not introduce:
- Additional hits.
- Free attacks.
- Guaranteed status effects.
- Unapproved damage bonuses.
- New control effects.

## 9.4 Derived Synergy Table (v1.1)

A chain may exist **only where an existing rule already makes the follow-up better**. Examples:
- the target is Broken (it takes increased damage);
- the target carries a stat-down status;
- Fear or Silence has removed options from that hero.

The chain table is **derived from `StatusEffectDef` data and the Break rules**, not hand-authored.

A setup only adds utility for allies' follow-ups. It never changes damage, hits or status chances, and a test diffs the combat rule outputs to prove this.

---

# 10. Resource and Cooldown Planning

## 10.1 Purpose

The AI must evaluate the long-term value of its resources.

This is particularly important for Elites and Bosses.

## 10.2 Resource Evaluation

The system evaluates:

- Current MP.
- Maximum MP.
- Skill MP costs.
- Cooldowns.
- Available Basic Attacks.
- Potential future skill opportunities.
- Current tactical urgency.

## 10.3 Resource Decision

The AI distinguishes:

| Value | Description |
|---|---|
| Immediate Value | Benefit of using the skill now |
| Future Value | Benefit of retaining resources |
| Opportunity Cost | Value of alternative actions lost |
| Emergency Value | Importance of responding to a critical condition |

The Resource Planner operates within finalized skill-specific conditions.

It does not replace established MP thresholds or introduce automatic MP regeneration.

**(v1.1)** Reserve rule (a starting value): keep enough MP for a signature or heal that comes off cooldown within the horizon (H = 2), unless Emergency Value is triggered.

---

# 11. Counterplay-Aware AI

## 11.1 Purpose

The AI evaluates whether its decisions preserve the intended player response opportunities.

This is not a system that deliberately makes enemies weaker.

It ensures that tactical difficulty comes from meaningful decisions rather than hidden rule manipulation.

## 11.2 Counterplay Evaluation

The system considers:

- Whether an action requires a telegraph.
- Whether the telegraph is correctly displayed.
- Whether the player has an opportunity to respond.
- Whether the action can be interrupted.
- Whether Defensive QTE is available.
- Whether the attack's target is still valid.
- Whether the action follows the established Heavy restrictions.

## 11.3 Telegraph Integrity

The following existing rules remain authoritative:

- Heavy attacks are telegraphed.
- Boss signature attacks are telegraphed.
- Charge releases are telegraphed.
- Status-only actions are telegraphed, **(v1.1) except Zoroa's Soulbind, which is instant (§12.5)**.
- Ordinary attacks without Heavy are not automatically telegraphed.
- Telegraphs display action type, target portrait, and hit count.
- Telegraphs do not change QTE timing or pattern.
- **(v1.1)** Zoroa's Harvest Call is shown on the intent line ("HARVEST CALL: SUMMONS").

If a telegraphed target is Down when the attack is released, the GDD's deterministic retargeting rule applies: the attack is redirected to the next active character in party order.

The AI must not secretly substitute a different skill or arbitrary target. **(v1.1)** A published intent is executed as published while it is still legal, and it is never re-scored.

## 11.4 Mechanical Restrictions

Counterplay evaluation must never:

- Change QTE timing windows.
- Modify status chance.
- Modify damage values.
- Modify enemy statistics.
- Modify hit count.
- Remove an established telegraph.
- Change the initiative queue during an active round.
- Grant enemy actions beyond the unit's configured `turnsPerRound` **(v1.1)**.

## 11.5 Readable Adaptation (v1.1)

Whenever an adaptation response changes a decision, the combat log shows a one-line "read" the **first** time it triggers in the encounter. Example: "The Hollow sense Mira's guard. They turn elsewhere."

The enemies' focus target is **not** shown on the HUD (GDD §8.9 keeps non-telegraphed intent hidden).

---

# 12. Encounter Intelligence Profiles

The architecture is shared, but different enemy tiers can use different levels of tactical complexity.

| Enemy Category | Intelligence Profile | Intended Behavior |
|---|---|---|
| Normal | Basic Tactical AI | Understand role and select useful actions |
| Elite | Advanced Tactical AI | Use coordination, memory, and predictive evaluation |
| Miniboss | Specialized Tactical AI | Strong individual personality and deliberate planning |
| Boss | Advanced Encounter AI | Full tactical evaluation and phase-specific planning |

This is a proposed AI complexity progression.

It does not change the existing enemy tier, QTE tier, statistics, or skill restrictions.

**(v1.1) Capability matrix:**

| Capability | Normal | Elite | Miniboss | Boss |
|---|---|---|---|---|
| Utility + role | ✓ | ✓ | ✓ | ✓ |
| Read shared memory and Blackboard | ✓ | ✓ | ✓ | ✓ |
| Publish reservations | ✓ | ✓ | ✓ | ✓ |
| Synergy follow-up | ✓ | ✓ | ✓ | ✓ |
| Synergy setup / strategic objectives | — | ✓ | ✓ | ✓ |
| Prediction H = 2 | — | ✓ | ✓ | ✓ |
| Profile condition list (§8.3) | — | — | ✓ | ✓ |
| Memory decay / recognition | 0.7 / 0.5 | 0.7 / 0.5 | 0.7 / 0.5 | 0.8 / 0.45 |

## 12.1 Normal

Normal enemies primarily use:
- Basic Utility AI.
- Individual role behavior.
- Limited tactical memory.
- Simple target evaluation.

They should remain relatively easy to understand.

## 12.2 Elite

Elite enemies use:
- Full Utility AI.
- Shared Blackboard.
- Player Behavior Modeling.
- Two-turn predictive evaluation.
- Flexible tactical coordination.

Their existing targeting and skill rules remain authoritative.

## 12.3 Miniboss

Minibosses receive specialized decision profiles.

Soul Reaper should emphasize execution pressure and tactical attack opportunities.

Royal Ossuary Knight should emphasize charge timing, survival, and Break-related tactical decisions.

## 12.4 Boss

Boss AI uses the complete architecture.

Zoroa evaluates:
- Current phase.
- Party condition.
- Recent player behavior.
- Available actions.
- MP and cooldowns.
- Signature attack opportunities.
- Tactical outcomes.

Phase 2 retains its existing skill identity and gains exactly the established additional attack, Soulstorm.

Boss intelligence does not authorize new attacks or additional Heavy actions.

## 12.5 Approved Zoroa Exceptions (v1.1)

These are shipped and approved. Each one overrides any conflicting statement elsewhere in this document.

1. **Two turns per round** (`turnsPerRound = 2`, both phases).
   - Turn B acts at Speed × `SecondTurnSpeedFactor`.
   - Round-scoped upkeep ticks once per round.
   - Stun, Sleep and Broken cost the whole round.
   - A charge releases on the next turn, whichever it is.
   - One Heavy per round covers both turns.
   - Turn B is re-planned after turn A from the Blackboard as it is at that moment.
   - **Every turn obeys every AI rule.**
2. **Harvest Call** (both phases). It fills the field back up to 2 living summons.
   - Each summon is 80% a Dungeon 1 Normal enemy and 20% from the v1.9 random Elite pool (never a stage boss), at 70% HP.
   - The skill has a 5-round cooldown (initial 2) and is instant.
   - Summons give no rewards, persist through the phase change, and are removed when the final bar breaks.
   - Summons are battle participants, not new skills.
3. **Soulbind is instant** (not telegraphed).

---

# 13. Round-Based AI Planning Pipeline

## 13.1 Planning Trigger

Enemy planning occurs during round preparation.

It remains separate from initiative execution.

## 13.2 Planning Sequence

**(v1.1) Planning order.** Intents are planned and published in a fixed order that is **independent of initiative**:

1. Support roles.
2. Controller roles.
3. All other roles.

Ties are broken by spawn/formation slot.

- Speed buffs, Slow and initiative changes never alter *what* enemies decide, only *when* they act.
- Earlier planners publish and reserve first.
- A mid-round re-plan (Zoroa turn B) reads the Blackboard as it is at that moment.

```text
BEGIN ROUND PREPARATION

    Build deterministic initiative queue

    Read current combat state

    Update Battlefield Perception

    Update Encounter Tactical Memory          (v1.1: one shared instance; apply per-round decay)

    Update Player Behavior Profiles

    Evaluate Strategic Tactical Objectives    (v1.1: with hysteresis)

    Initialize Shared Blackboard

    FOR each active enemy in PLANNING ORDER   (v1.1: Support -> Controller -> others, slot ties; NOT initiative)

        Validate enemy state

        Read personality configuration

        Read mandatory skill rules

        Read shared tactical objectives

        Read allied planned actions

        Generate legal actions

        Apply mandatory rules

        IF mandatory action exists:

            Select mandatory action

        ELSE:

            Evaluate legal actions

            Evaluate legal targets

            Apply role preferences

            Apply adaptation modifiers

            Apply coordination preferences

            Evaluate projected outcomes

            Evaluate resource opportunity costs

            Evaluate counterplay restrictions

            Select highest-scoring legal decision

        END IF

        Publish planned action

        Publish required telegraph

        Update Blackboard reservations

    END FOR

END ROUND PREPARATION
```

## 13.3 Execution Sequence

```text
BEGIN ENEMY TURN

    Validate active state

    Execute established start-of-turn sequence

    IF unable to act:

        Skip turn

    ELSE:

        Validate planned action

        Validate target

        Validate MP and cooldown

        Validate special action conditions

        IF action is legal:

            Execute planned action            (v1.1: never re-scored)

        ELSE:

            Evaluate legal fallback

            IF fallback exists:

                Execute fallback

            ELSE:

                Skip turn

            END IF

        END IF

    END IF

    Resolve combat results

    Update Blackboard

    Update Tactical Memory

    IF unit has another turn this round (v1.1, Zoroa):

        Re-plan next turn from the current Blackboard and publish it

END ENEMY TURN
```

The initiative system continues to control execution order.

---

# 14. Data Architecture

The following structures are conceptual implementation schemas. They are not engine-specific final code.

## 14.1 Enemy AI Configuration

```json
{
  "enemyId": "soul_reaper",
  "aiProfile": {
    "decisionModel": "hybrid",
    "personality": "executioner",
    "intelligenceTier": "miniboss",
    "adaptationLevel": "high",
    "planningHorizon": 2,
    "coordination": "flexible"
  },
  "utilityWeights": {},
  "actionRules": [],
  "targetRules": [],
  "behaviorTree": "executioner_behavior",
  "conditionList": []
}
```

**(v1.1)** `behaviorTree` names the shared pipeline variant. `conditionList` holds the miniboss/boss workflow conditions (§8.3).

## 14.2 Player Behavior Profile

```json
{
  "characterId": "party_character_02",
  "battleId": "encounter_001",
  "behaviorConfidence": {
    "aggressive": 0.0,
    "defensive": 0.0,
    "burstDamage": 0.0,
    "elementalSpecialist": 0.0,
    "supportDependent": 0.0,
    "focusFire": 0.0,
    "breakSpecialist": 0.0
  },
  "recentObservations": []
}
```

Values are illustrative. Final confidence calculations remain TBD. **(v1.1)** The starting model is in §4.1. Profiles live inside the encounter Blackboard.

## 14.3 Tactical Memory

```json
{
  "battleId": "encounter_001",
  "round": 1,
  "observedEvents": [],
  "playerPatterns": [],
  "enemyActionHistory": [],
  "coordinationHistory": [],
  "tacticalOutcomes": [],
  "triggeredReads": []
}
```

**(v1.1)** There is exactly one instance per encounter, owned by the Blackboard and shared by all enemies. `triggeredReads` holds the adaptation reads already shown in the log.

## 14.4 Shared Blackboard

```json
{
  "battleId": "encounter_001",
  "currentRound": 1,
  "tacticalObjectives": [],
  "partyState": [],
  "enemyState": [],
  "plannedActions": [],
  "targetReservations": [],
  "lethalReservations": [],
  "expectedOutcomes": [],
  "activeOpportunities": [],
  "memory": {},
  "playerProfiles": []
}
```

## 14.5 Predictive State

```json
{
  "planningHorizon": 2,
  "currentState": {},
  "projectedStates": [],
  "knownCommittedEffects": [],
  "uncertainEvents": [],
  "predictionConfidence": 0.0
}
```

## 14.6 Enemy Intent

```json
{
  "enemyId": "soul_reaper",
  "actionId": "accelerating_rend",
  "targetIds": ["party_character_02"],
  "planningRound": 1,
  "turnIndex": 0,
  "intentType": "normal",
  "telegraphRequired": false,
  "reservationStatus": "published",
  "executionStatus": "pending"
}
```

---

# 15. Determinism and Mechanical Integrity

The advanced architecture must preserve the following established rules.

| System | Restriction |
|---|---|
| Initiative | Deterministic Effective SPD ordering |
| Tie-breaking | Party before enemies; party slot; enemy spawn order |
| Action Economy | One normal turn and one main action per eligible unit per round, **(v1.1) except units with `turnsPerRound` > 1 (Zoroa, §12.5)** |
| Queue Refresh | Battle start and beginning of each round |
| Mid-round Speed | Applies to next round |
| Enemy Intent | Selected during round preparation, **(v1.1)** in planning order (§13.2), not initiative order |
| Normal Patterns | Steady only |
| Elite Patterns | Maximum two patterns |
| Boss Attacks | Three to five attacks total; exactly one signature |
| Heavy | Maximum one Heavy-marked attack per round |
| Stun + Heavy | Cannot affect the same target in the same round |
| Boss Control | Zoroa remains immune to Mental Effects |
| Status Chance | Independent of QTE |
| QTE Timing | Independent of AI adaptation |
| Scaling | Existing stat scaling only |
| MP | Existing skill costs and fallback rules |
| Telegraph | Existing action, target, hit count, and retargeting rules |

No AI component can override these restrictions.

---

# 16. Testing and Validation

The system must be evaluated using automated combat simulations and manual playtesting.

## 16.1 Predictive Planning Tests

- Enemy avoids unnecessary overkill.
- Enemy recognizes committed allied attacks.
- Enemy evaluates future resource requirements.
- Enemy identifies useful opportunities across two future turn opportunities.
- Enemy does not assume future player commands.
- Enemy does not rely on uncertain predictions as guaranteed outcomes.

## 16.2 Player Modeling Tests

- Repeated actions increase behavioral confidence.
- Single actions do not create excessive confidence.
- Contradictory actions reduce confidence.
- Old observations lose influence.
- Memory resets after the encounter.
- Different encounters do not inherit learned player profiles.
- **(v1.1)** Memory is shared: a summon reacts to behavior observed before it joined. Memory survives Zoroa's phase change.

## 16.3 Flexible Coordination Tests

- Enemies can focus fire.
- Enemies can distribute attacks.
- Enemies can recognize allied support opportunities.
- Enemies can exploit useful setup actions.
- Enemies can abandon an unproductive tactical objective.
- Individual enemy personalities remain recognizable.
- Explicit target rules remain intact.
- **(v1.1)** Planning order and decisions are identical with and without a Speed buff on any enemy.

## 16.4 Counterplay Tests

- Telegraphs appear at the correct time.
- Telegraphs display the correct target.
- Downed telegraphed targets follow deterministic retargeting.
- Charge cancellation works correctly.
- Defensive QTE behavior remains unchanged.
- No hidden action substitution occurs.
- **(v1.1)** Every adaptation response logs its read exactly once per encounter.

## 16.5 Performance Tests

Measure:

| Metric | Purpose |
|---|---|
| Decision Time | Cost of AI evaluation |
| Prediction Cost | Cost of two-turn planning |
| Blackboard Update Cost | Coordination overhead |
| Memory Update Cost | Player modeling overhead |
| Wasted Action Rate | Ineffective enemy decisions |
| Overkill Rate | Unnecessary damage concentration |
| Coordination Efficiency | Value of shared planning |
| Adaptation Effectiveness | Quality of response to player behavior |
| Player Counterplay Rate | Availability of meaningful responses |
| Encounter Completion Rate | Overall difficulty |

The AI must remain efficient enough for ordinary combat encounters, including encounters with several enemies.

---

# 17. Implementation Roadmap

The architecture should be implemented in the following stages.

| Stage | System | Deliverable |
|---|---|---|
| 1 | Hybrid AI Foundation | Rule-Based and Utility AI |
| 2 | Shared Blackboard | Flexible coordination and intent reservation |
| 3 | Player Modeling | Behavioral recognition and confidence |
| 4 | Predictive Planning | Two-turn lookahead |
| 5 | Strategic Planner | Tactical objective selection |
| 6 | Tactical Synergy | Setup and follow-up evaluation |
| 7 | Behavior Trees | Complex enemy workflows |
| 8 | Resource Planner | MP and cooldown evaluation |
| 9 | Counterplay Evaluation | Telegraph and fairness validation |
| 10 | Boss Integration | Advanced encounter and phase planning |

Each stage should be tested before the next is introduced.

A replayable combat simulator should be developed alongside the AI system so that problematic decisions can be reproduced and inspected.

**(v1.1)** The executable plan adds **Stage 0** (baseline metrics, a per-decision AI trace, and the deterministic replay simulator) and gives every stage a hard exit gate. See `Enemy_AI_Implementation_Handoff.md`.

---

# 18. Open Design Parameters

The architecture is defined, but several implementation parameters must remain open until testing.

| Parameter | Status |
|---|---|
| Utility weights | TBD |
| Player behavior confidence thresholds | TBD |
| Behavioral decay rate | TBD |
| Repetition penalty | TBD |
| Synergy bonus | TBD |
| Predictive confidence calculation | TBD |
| Resource opportunity-cost weights | TBD |
| Blackboard reservation expiration | TBD |
| AI decision time budget | TBD |
| Enemy intelligence profiles by tier | Proposed; needs balancing |

These parameters must not be assigned arbitrary final values before simulation and playtesting.

**(v1.1) Starting values.** These are *not* final. Each has a test, a pass/fail metric and a tuning direction, so none of them is arbitrary, and the "TBD (final)" status above stays true until the simulation confirms them.

| Parameter | Starting value | Pass test | If it fails |
|---|---|---|---|
| Utility clamp | ±15 (1.5 priority levels) | Wasted Action Rate ≤ 5%; each role's move mix stays recognisable | Erratic: ±10. Rigid: ±20 |
| Confidence `k` / `RecognizeAt` | 2 / 0.5 (boss 0.45) | A single action never recognizes; 3 recent consistent actions do | Adjust `k` |
| Behavioral decay rate | 0.7 per round (boss 0.8) | Influence fades about 3 rounds after a behavior stops (boss about 5) | Adjust |
| Contradiction weight | 1.0 per opposite observation | 2 rounds of attacking after guarding drops Defensive below 0.5 | 1.5 |
| Repetition penalty | −3 per consecutive ineffective use of the same move×target | Repeated Action Rate is below the pre-AI baseline | −5 |
| Synergy bonus | +4 per real follow-up mechanic (inside the clamp) | Follow-ups observed; no change in the rule outputs | Adjust |
| Prediction mix | passive 0.5 + pressure 0.5 × confidence | Supports heal before a projected lethal round in ≥ 50% of scripted cases | Raise the pressure share |
| Resource reserve | Keep MP for a signature/heal that comes ready within H = 2 | No unaffordable-signature turns in the Zoroa simulation | Reserve earlier |
| Reservation expiration | End of round (re-built each planning pass) | No stale reservations across rounds | — |
| Objective bonus cap / hysteresis | +6 / challenger must win by 3 | Objectives change in ≥ 30% of fights and never in > 50% of rounds | Increase the hysteresis margin |
| `FocusCapNormal` | 2 | Normal-encounter deaths ≤ baseline + 15% | 1 |
| `ChargeShieldGate` | 70% of own Break gauge | Telegraph Response Rate 20–50% | ±10 |
| AI decision time budget | ≤ 2 ms per enemy plan, ≤ 10 ms per round (editor) | Profiler with 5 enemies | Disable prediction for Normal tier first |

---

# 19. Final System Definition

Felna uses an **Advanced Hybrid Tactical Enemy AI with Two-Turn Predictive Planning, Encounter-Based Learning, and Flexible Decentralized Coordination**.

The system combines:

- Rule-Based AI for mechanical correctness.
- Behavior Trees for complex conditional workflows. **(v1.1)** These are a fixed pipeline plus data condition lists.
- Utility AI for individual tactical decisions.
- A Strategic Planner for team objectives.
- A Shared Blackboard for flexible coordination.
- Player Behavior Modeling for adaptive responses.
- Tactical Memory for encounter-local learning. **(v1.1)** One memory per encounter, shared by every enemy.
- Predictive Evaluation for two-turn planning.
- Synergy Planning for coordinated opportunities.
- Resource Planning for MP and cooldown management.
- Counterplay Evaluation for tactical fairness.

The architecture is designed to make enemies more capable of understanding and exploiting combat situations without changing the underlying combat rules.

**Final Design Goal**

An enemy in Felna should not simply know which attack deals the most damage.

It should understand its role, recognize opportunities, consider what its allies are planning, learn from the party's behavior, evaluate likely future situations, and choose actions that contribute to its team's survival and victory.

The player should feel that the enemies are intelligent, coordinated, and dangerous—not that the game is secretly changing its rules to defeat them.

---

# 20. Source References

**Established project sources:**

1. `GDD_Terbaru.md`
   - Chapter 3 — Difficulty System.
   - Chapter 10 — Combat Structure, Turn Order, and Enemy Scaling.
   - Chapter 11 — Damage and Status Effects.
   - Chapter 12 — QTE System.
   - Chapter 21 — Conflicts, Assumptions, and Open Issues.

2. `Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx`
   - Enemy Roster.
   - Skill Database.
   - Initiative & Turn Order.
   - Encounter Plan.
   - Elite Design Lock.
   - GDD Compliance & Balance.

3. **(v1.1)** `D1_Difficulty_Pass_Handoff.md`, `Charge_Pacing_Handoff.md`, and `Zoroa_Double_Turn_And_Summon_Handoff.md`: the shipped changes behind §12.5.

**Document status:** This architecture is a proposed extension of the existing GDD. It does not automatically finalize its open numerical parameters or modify any previously locked enemy mechanics.
