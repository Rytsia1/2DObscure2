# FELNA — ENEMY AI SYSTEM SPECIFICATION

**Document:** Enemy AI System
**Version:** 1.1 — Design Proposal (amended)
**Status:** Proposed specification, pending integration into the GDD
**Applies to:** All enemy encounters across the eight dungeons
**Primary reference:** GDD Terpadu and Felna Dungeon 1 Enemy Roster, Scaling, and Turn Order v1.9 FINAL
**Companion documents:** `Felna_Enemy_AI_Architecture_v1_1.md` (advanced architecture), `Enemy_AI_Implementation_Handoff.md` (staged implementation plan)

---

## Change log

| Version | Date | Change |
|---|---|---|
| 1.0 | — | Original proposal |
| 1.1 | 2026-11-22 | **A.1** Approved Zoroa exceptions (double turn, Harvest Call, instant Soulbind). Shipped code wins over the spec where they conflict. **A.2** Planning order is independent of initiative (§4.5). **A.3** Memory is one shared encounter-level memory. **A.4** Locked targeting is mandatory. **A.5** Observable information is defined exactly. **A.6** Adaptation is never secret: one-time log reads. **A.7** Starting values added (§17). Amended passages are marked **(v1.1)** |

---

## 1. System Overview

### 1.1 Purpose

The Enemy AI System governs how enemies:

- Select actions and skills.
- Select party targets.
- Coordinate with other enemies.
- Respond to changes in the battlefield.
- Prepare and communicate dangerous attacks.
- Execute their decisions within Felna's existing turn-order system.

The system must make enemies tactically intelligent without making their behavior arbitrary or unfair.

### 1.2 Design Decisions

| Area | Decision |
|---|---|
| Action selection | Hybrid AI |
| Enemy coordination | Shared Blackboard |
| Adaptation | High |
| Planning model | Round-based tactical planning |
| Planning order | **(v1.1)** Fixed role order, independent of initiative (§4.5) |
| Decision consistency | Deterministic scoring with controlled variation only where appropriate |
| Memory | **(v1.1)** One memory per encounter, shared by every enemy in that encounter (§5.3) |
| Information access | Observable combat state, defined in **(v1.1)** §5.6 |

### 1.3 Design Philosophy

The Enemy AI System follows **Relentless Tactical Survival**.

Enemies should punish predictable strategies, exploit opportunities, and coordinate their attacks. However, players must be able to understand why enemies act as they do and have meaningful opportunities to respond.

The system must follow these principles:

1. **Tactical Intelligence:** Enemies select actions based on battlefield conditions rather than blindly cycling through skills.
2. **Role Identity:** Different enemy types must behave differently.
3. **Team Coordination:** Enemies cooperate without behaving like a single shared character.
4. **Adaptive Behavior:** Enemies recognize repeated player behavior and adjust their priorities.
5. **Readable Threats:** Dangerous actions must remain understandable and telegraphed.
6. **Fair Counterplay:** Adaptation must never secretly invalidate a player's decisions. **(v1.1)** Every adaptation response is announced once in the combat log (§5.7).
7. **Mechanical Integrity:** AI cannot bypass established combat, QTE, Break, or action-economy rules.

---

## 2. AI Architecture

The system consists of four cooperating components.

| Component | Responsibility |
|---|---|
| Rule-Based AI | Enforces mandatory behavior and skill-specific conditions |
| Utility AI | Evaluates available actions and targets |
| Shared Blackboard | Provides battlefield information and coordination |
| Tactical Memory | Tracks previous events and informs adaptation. **(v1.1)** Part of the Blackboard, one per encounter |

### 2.1 Hybrid Decision Model

The AI uses two decision mechanisms.

**Rule-Based Decision Making**

Rules determine what the enemy must or must not do.

Examples:

- Use Mending Litany when its established healing condition is satisfied.
- Use Arcane Charge when its specific MP threshold and cooldown conditions are satisfied.
- Cancel Royal Ossuary Knight's charged attack if it becomes Broken before release.
- Use an MP-free Basic Attack when no eligible skill can be selected.
- Never select an unavailable skill or an illegal target.
- **(v1.1)** Respect a locked targeting rule (§3.5).

**Utility-Based Decision Making**

When multiple legal actions are available and no mandatory rule determines the choice, the AI evaluates their tactical value.

Examples:

- Whether attacking a vulnerable target is better than attacking a healthier one.
- Whether a status effect could disrupt an important party member.
- Whether an attack would create a useful opportunity for another enemy.
- Whether an action is becoming ineffective against a particular target.
- Whether the team should focus on one target or distribute pressure.

### 2.2 Decision Priority

The following priority order is mandatory:

1. Validate enemy state and action eligibility.
2. Apply mandatory encounter and phase rules.
3. Apply explicit skill-specific AI rules.
4. Generate remaining legal actions.
5. Evaluate actions through Utility AI.
6. Evaluate targets.
7. Apply enemy personality and tactical memory.
8. Apply shared coordination adjustments.
9. Select the highest-scoring legal decision.
10. Publish the planned action and its intent when required.

Mandatory rules always override utility scores.

Utility AI must never override an explicit skill condition, cooldown, MP requirement, immunity, or encounter restriction.

---

## 3. Utility AI

### 3.1 Action Evaluation

Every legal action receives a tactical score.

The proposed evaluation model is:

`ActionScore = BaseUtility + TargetUtility + RoleBonus + SynergyBonus + AdaptationBonus - ResourcePenalty - RepetitionPenalty`

**(v1.1)** In implementation: `BaseUtility = aiPriority × 10`. All other terms are summed and clamped to ±`UtilityClamp`. The authored priority list stays the backbone, and today's behaviour is the score when every modifier is zero.

Each factor is evaluated according to the current battle state.

| Factor | Evaluation |
|---|---|
| Base Utility | General usefulness of the action |
| Target Utility | Value of the selected target |
| Role Bonus | Compatibility with the enemy's intended role |
| Synergy Bonus | Contribution to the enemy team's current strategy |
| Adaptation Bonus | Relevance to previously observed player behavior |
| Resource Penalty | Cost of MP or other limited resources |
| Repetition Penalty | Reduced preference for repeatedly ineffective actions |

**Important:** The formula defines the evaluation structure, not finalized numerical weights. Individual weights and normalization values require playtesting. **(v1.1)** §17 gives *starting values*, each with a test, a pass/fail metric and a tuning direction. They are not final values.

### 3.2 Action Utility Factors

The AI should evaluate:

- Expected damage effectiveness.
- Expected healing effectiveness.
- Status application opportunities.
- Enemy survival and resource conditions.
- Target resistance or weakness, if already observed or otherwise legitimately known.
- Break-related opportunities.
- Whether an action can interrupt or punish an important player strategy.
- Whether an area attack has sufficient active targets.
- Whether the skill's cooldown and MP requirements are satisfied.

The AI must not invent a benefit that the skill does not actually provide.

### 3.3 Target Evaluation

Target selection is evaluated separately from action selection.

This allows the AI to choose an appropriate skill first and then identify the most useful legal target for that skill.

Proposed target factors:

| Factor | Purpose |
|---|---|
| Current HP | Identify vulnerable targets |
| Defensive capability | Identify targets susceptible to the chosen attack |
| Elemental affinity | Exploit known weaknesses or avoid known resistance |
| Status condition | Identify opportunities for additional pressure |
| Break condition | Exploit or respond to Break-related opportunities |
| Threat level | Consider the target's demonstrated impact on the battle |
| Recent targeting | Prevent unnecessary duplication of attacks |
| Team coordination | Support the team's current target strategy |

Target scores are subject to explicit enemy-specific targeting rules (see §3.5).

### 3.4 Deterministic Selection

The AI selects the highest-scoring legal action and target.

If multiple candidates have equal scores, resolve the tie using a deterministic rule, such as:

1. Higher enemy-role priority.
2. Better resource efficiency.
3. Lower repetition count.
4. Stable action or target identifier order.

Random action selection must not become the primary source of enemy difficulty. **(v1.1)** The roster AI path makes no random calls at all.

### 3.5 Locked Targeting Rules (v1.1)

A locked targeting rule is **mandatory**. The locked rules are:
- the Dungeon 1 Elite Design Lock (Elites target the lowest current HP);
- Gravebound Archer's lowest-DEF rule;
- Reliquary Confessor's lowest-HP-percentage heal target.

Utility may only:
- skip a candidate that is already **reserved as lethal** by an earlier planner's committed damage, then choose the next candidate by the **same rule**;
- break exact ties.

---

## 4. Shared Enemy Blackboard

### 4.1 Purpose

The Shared Blackboard is the central information system used by enemies participating in the same battle.

It allows individual enemies to coordinate without requiring a dedicated commander.

Each enemy retains its own personality and utility evaluation, but can use information published by other enemies.

### 4.2 Shared Information

The Blackboard contains the following information:

| Information | Description |
|---|---|
| Party State | Active, defeated, Down, HP, status, and relevant combat conditions |
| Enemy State | Active enemies, HP, MP, status, and Break conditions |
| Planned Intents | Actions and targets already planned by other enemies |
| Target Assignments | Current enemy target distribution, **(v1.1)** including lethal reservations |
| Recent Actions | Actions performed by enemies and their observed results |
| Threat Records | Important party actions and demonstrated tactical impact |
| Enemy Conditions | Charging, Broken, controlled, or otherwise unable to act |
| Encounter State | Current encounter and boss phase |
| Coordination State | Current team focus and opportunities for cooperation |
| **(v1.1)** Encounter Memory | The single tactical memory of this encounter (§5.3) |

The Blackboard is battle-local and is reset when the encounter ends.

### 4.3 Coordination Rules

Enemies may coordinate through the following mechanisms.

**Focus Fire**

Enemies can prioritize the same target when concentrating damage is tactically useful.

However, they should not automatically attack the same character if doing so creates unnecessary overkill or wastes useful opportunities.

**(v1.1)** At most `FocusCapNormal` Normal-tier enemies may plan single-target damage on the same hero in one round. Elites and bosses are exempt.

**Target Distribution**

Enemies may distribute attacks when:

- Their roles benefit from attacking different targets.
- A target is already likely to be defeated.
- Another party member presents a more valuable opportunity.
- The team can apply different forms of pressure more effectively.

**Support Coordination**

Support enemies can respond to the condition of other enemies.

For example, a healer should consider the health of its allies before spending its turn on an offensive action.

**Threat Coordination**

Enemies can recognize a party member who repeatedly creates significant tactical advantages.

Examples include:

- A character repeatedly defeating enemies quickly.
- A character frequently disrupting enemy actions.
- A character repeatedly exploiting elemental weaknesses.
- A character consistently protecting vulnerable allies.

These observations may influence future targeting decisions.

### 4.4 Coordination Restrictions

The Shared Blackboard must not:

- Grant enemies additional actions.
- Allow enemies to bypass their own cooldowns.
- Allow enemies to ignore MP costs.
- Grant enemies information about future player actions.
- Change an already telegraphed action secretly.
- Allow an enemy to use another enemy's skills.
- Override encounter-specific rules.

### 4.5 Planning Order (v1.1)

Intents are planned and published in a fixed **planning order that is independent of initiative**:

1. Support roles.
2. Controller roles.
3. All other roles.

Ties are broken by spawn/formation slot.

- Speed buffs, Slow, and any other initiative change never alter *what* enemies decide. They only alter *when* enemies act.
- An enemy that plans earlier publishes and reserves first, and later planners read those reservations.
- A mid-round re-plan, such as Zoroa's second turn (§8.5), reads the Blackboard as it is at that moment.

---

## 5. High Adaptation System

### 5.1 Adaptation Objective

High Adaptation means enemies actively respond to the party's demonstrated behavior during the current battle.

The objective is to prevent the player from relying on one repeated strategy without making the AI unpredictable.

Adaptation should improve tactical decisions, not increase enemy statistics.

### 5.2 Observable Player Behavior

The AI may learn from the following events:

**Targeting Behavior**
- Repeatedly attacking one enemy.
- Prioritizing support enemies.
- Ignoring particular enemy roles.

**Defensive Behavior**
- Frequently using Guard.
- Repeatedly protecting one party member.
- Consistently responding well to particular attack patterns.

**Offensive Behavior**
- Frequently using a specific damage type.
- Repeatedly exploiting a known weakness.
- Relying heavily on multi-hit attacks.
- Repeatedly using high-impact skills.

**Resource Behavior**
- Frequently healing at critical HP.
- Conserving MP for specific situations.
- Repeatedly using particular consumables or defensive resources, when those actions are observable to the AI.

**Combat Results**
- Successfully interrupting enemy charge attacks.
- Repeatedly inflicting Break.
- Repeatedly defeating enemies before they can execute their plans.

### 5.3 Adaptation Memory

**(v1.1) Scope:** there is **one tactical memory per encounter**, held on the Blackboard and **shared by every enemy in that encounter**. Enemies have no private memories.
- It persists through a boss phase change.
- A summoned enemy reads it from the moment it joins.
- It is destroyed when the encounter ends: victory, defeat, or retreat.

| Memory Category | Recorded Information |
|---|---|
| Player Pattern | Repeatedly observed player actions |
| Target Pattern | Frequently attacked or protected characters |
| Defensive Pattern | Guarding and defensive behavior |
| Damage Pattern | Observed damage types and effectiveness |
| Disruption Pattern | Break and interruption results |
| Enemy Performance | Successful and ineffective enemy actions |
| Coordination History | Recent enemy target assignments and actions |

Memory should use recency weighting.

Recent behavior should generally influence decisions more strongly than older behavior.

A single player action should not immediately create a permanent tactical conclusion.

**(v1.1)** Recency weighting: every stored value decays once per round (`value = value × decay + event`). A behavior is acted on only when its confidence reaches the recognition threshold (§17), which one action alone can never reach.

### 5.4 Adaptation Responses

The AI can respond in the following ways:

| Player Behavior | Possible Enemy Response |
|---|---|
| Repeatedly attacks one enemy | Other enemies may protect or pressure the attacker |
| Repeatedly guards | Prefer actions that provide useful pressure without relying on defeating Guard through unsupported mechanics |
| Repeatedly exploits an elemental weakness | Reconsider exposed enemy positions and target selection |
| Repeatedly targets support enemies | Other enemies may prioritize disrupting the attackers |
| Repeatedly interrupts charges | Reconsider when to initiate charge-based attacks, within the skill's existing rules. **(v1.1)** Prefer to start a charge when the enemy's own Break gauge is at least `ChargeShieldGate`, so the player learns to chip the shield first |
| Frequently heals a specific character | Increase tactical interest in pressuring that character |
| Repeatedly focuses one party member | Other enemies may distribute pressure to avoid inefficient overcommitment |

These are examples of adaptive behavior, not automatic hard counters.

The AI should not become immune to a strategy simply because the player uses it frequently.

### 5.5 Adaptation Limits

High Adaptation must follow these restrictions:

1. Memory is reset when the battle ends.
2. Enemies cannot predict future player commands.
3. Enemies cannot know hidden player information that has not been exposed to them.
4. Enemies cannot modify their own stats through adaptation.
5. Enemies cannot modify QTE timing or status probabilities through adaptation.
6. Enemies cannot change skill behavior through adaptation.
7. Enemies cannot cancel a telegraphed action simply because the player responds correctly.
8. Adaptation must influence decision priorities rather than create automatic immunity or guaranteed success.

High Adaptation is a tactical intelligence feature, not a difficulty multiplier.

### 5.6 Observable Information (v1.1)

| Category | Contents | AI access |
|---|---|---|
| Observable | Everything shown on the combat HUD (HP, MP, statuses, Guard, Broken, Poise, turn order, telegraphs) and every event of this encounter (actions, damage numbers, elements used, effectiveness shown, QTE grades of resolved hits) | Allowed |
| Derived | Values computed only from observable information | Allowed |
| Hidden | Inventory counts, skills a hero has not used yet, unresolved QTE grades, any affinity not yet shown in this encounter | Prohibited |

### 5.7 Adaptation Is Never Secret (v1.1)

Whenever an adaptation response changes a decision, the combat log shows a one-line "read" the **first** time it triggers in that encounter. Example: "The Hollow sense Mira's guard. They turn elsewhere."

The enemies' focus target is **not** shown on the HUD, because GDD §8.9 keeps non-telegraphed intent hidden.

---

## 6. Enemy Personality and Role System

### 6.1 Purpose

Every enemy should have an identifiable combat personality.

The role defines which tactical opportunities receive additional utility.

Role preferences do not override explicit skill rules.

### 6.2 Proposed Enemy Personalities

The following assignments are design proposals based on the current enemy descriptions and skill kits.

| Enemy | Proposed Personality | Tactical Identity |
|---|---|---|
| Hollow Squire | Vanguard | Consistent frontline pressure |
| Gravebound Archer | Opportunistic Ranged Attacker | Looks for vulnerable defensive targets |
| Cinder Wisp | Elemental Attacker | Applies Fire pressure and Burn |
| Soulbound Hound | Disruptor | Uses Fear and fast physical pressure |
| Possessed Royal Guard | Bruiser | Applies consistent physical damage |
| Crypt Leech | Attrition Survivor | Sustains itself through successful damage |
| Forgotten Militia | Combo Attacker | Uses multi-hit pressure |
| Soul Reaper | Tactical Executioner | Combines physical and Shadow pressure |
| Royal Ossuary Knight | Defensive Punisher | Uses its charge attack to threaten Heavy damage |
| Reliquary Confessor | Support / Healer | Maintains enemy survival |
| Graveglass Arcanist | Arcane Damage Dealer | Applies Shadow damage and manages MP |
| Crownless Duelist | Skirmisher | Applies multi-hit physical pressure |
| Tombstone Colossus | Break Pressure Bruiser | Applies Break Power through Gravefall |
| Mourning Bell Wraith | Area Controller | Punishes the party through Shadow AoE |
| Zoroa | Adaptive Controller | Uses status pressure, signature attacks, phase-specific abilities, and **(v1.1)** Harvest Call |

### 6.3 Role Behavior

Role bonuses should influence the utility score rather than force every enemy into a fixed action sequence.

For example:

- An Executioner may favor an already vulnerable target.
- A Disruptor may favor a character whose actions have significant tactical impact.
- A Support enemy may favor healing an endangered ally.
- An Area Controller may prefer an AoE attack when enough targets are active.

---

## 7. Enemy Intent and Telegraph System

### 7.1 Round Preparation

Enemy AI planning occurs during round preparation.

The system must:

1. Read the current battlefield.
2. Read tactical memory.
3. Read the Shared Blackboard.
4. Generate legal actions.
5. Evaluate actions and targets.
6. Select a planned action.
7. Publish its intent.
8. Display required telegraphs.

This process is separate from initiative execution. **(v1.1)** Enemies plan in the planning order of §4.5, not in initiative order.

### 7.2 Intent Categories

The following categories are proposed for internal AI classification.

| Category | Description |
|---|---|
| Normal Intent | Standard attack or action |
| Tactical Intent | Action selected because of battlefield conditions |
| Special Intent | Dangerous or signature action requiring a telegraph |

The categories do not change the skill's existing mechanics or QTE behavior.

### 7.3 Telegraph Lock

Once an enemy publishes a telegraphed action:

- The action must remain the planned action.
- Its selected target must remain consistent.
- The AI must not secretly replace it because the player reacts.
- The player must be allowed to respond to the announced threat.

If the action becomes impossible to execute because of an invalid target, defeat, control effect, or another legal restriction, the system must revalidate the action.

The enemy may use a legal fallback only when the original action is no longer executable and the fallback is consistent with the established combat rules.

If no legal action exists, the unit is skipped. No replacement turn is granted.

**(v1.1)** A published intent is executed as published while it is still legal, and it is **never re-scored**, telegraphed or not.

### 7.4 Existing Telegraph Rules

The following existing rules remain unchanged:

- Dangerous actions are telegraphed from the beginning of the round.
- Royal Ossuary Knight's Graveward Charge is telegraphed one turn before release.
- Zoroa's Final Reaping is telegraphed one turn prior. **(v1.1)** Soulbind is no longer telegraphed (§8.5).
- Zoroa Phase 2's Soulstorm is telegraphed one turn prior.
- Mourning Bell Wraith's Funeral Chorus is telegraphed one turn prior.
- A charge attack is cancelled if the enemy becomes Broken before release.
- **(v1.1)** Zoroa's Harvest Call is shown on the intent line ("HARVEST CALL: SUMMONS").

---

## 8. Boss AI

### 8.1 General Boss Behavior

Boss AI uses the same architecture as normal enemies but has additional encounter-specific rules.

Boss AI may evaluate:

- Current phase.
- Party condition.
- Recent player behavior.
- Previous boss actions.
- Available skills and MP.
- Signature attack availability.
- Current tactical opportunities.

Boss behavior must preserve the established boss attack count, signature attack restrictions, Heavy restrictions, and phase rules.

### 8.2 Zoroa — Phase 1

Zoroa's first phase uses:

- Soul Rend.
- Soul Lance.
- Soulbind.
- Final Reaping.
- **(v1.1)** Harvest Call (summon, §8.5).

The proposed AI personality is an Adaptive Controller.

Its utility evaluation may consider:

- Whether direct physical pressure is appropriate.
- Whether Shadow damage is effective.
- Whether Silence offers a useful tactical opportunity.
- Whether Final Reaping is appropriate to prepare.

Final Reaping remains the single signature attack and retains its established four-hit Accelerando pattern with Heavy on the final hit.

### 8.3 Zoroa — Phase 2

Phase 2 retains the Phase 1 skill identity and adds Soulstorm.

The AI may adapt its action and target priorities based on the current party condition.

However:

- Phase 2 must add exactly one new attack.
- Soulstorm remains the new AoE attack.
- Final Reaping remains the signature attack.
- No additional Heavy attack may be introduced.
- Zoroa remains immune to Mental Effects.
- Its immunity does not prevent it from applying eligible status effects to party members.

### 8.4 Boss Adaptation

Bosses should have stronger tactical memory and more sophisticated utility evaluation than ordinary enemies.

However, boss adaptation must remain bounded by their existing skill kits.

A boss must not:

- Create new skills dynamically.
- Change its damage values during battle.
- Gain turns beyond its configured `turnsPerRound` **(v1.1)**.
- Remove the player's opportunity to respond to a telegraph.
- Ignore established phase transitions.
- Break the Heavy attack restrictions.

### 8.5 Approved Zoroa Exceptions (v1.1)

These are shipped and approved. Each one overrides any conflicting statement elsewhere in this document.

1. **Two turns per round.** Zoroa (both phases) has `turnsPerRound = 2`.
   - Turn A acts at its Speed, and turn B at Speed × `SecondTurnSpeedFactor`.
   - Round-scoped upkeep (cooldowns, statuses, DoT, Break recovery) ticks once per round, on turn A.
   - Stun, Sleep and Broken cost the whole round.
   - A charge releases on Zoroa's next turn, whichever it is.
   - The one-Heavy-per-round rule covers both of its turns.
   - **Every turn obeys every AI rule in this document.**
2. **Harvest Call.** In both phases, Zoroa can fill the field back up to 2 living summons.
   - Each summon is 80% a Dungeon 1 Normal enemy and 20% an enemy from the v1.9 random Elite pool (never a stage boss).
   - Summons have 70% HP. The skill has a 5-round cooldown (initial 2) and is instant.
   - Summons join at the next round and give no EXP, gold or relic triggers. They persist through the phase change and are removed when Zoroa's final bar breaks.
   - Summons are battle participants, not new skills.
3. **Soulbind is instant.** It is no longer telegraphed (it deals no damage and has no QTE).

---

## 9. Combat Execution and Initiative Integration

### 9.1 Separation of Responsibilities

Enemy AI is responsible for deciding what an enemy intends to do.

The Initiative System is responsible for deciding when that enemy acts.

These systems must remain independent. **(v1.1)** Planning order (§4.5) is therefore independent of initiative order.

### 9.2 Execution Flow

When an enemy reaches its position in the initiative queue:

1. Validate that the enemy is active.
2. Apply the established start-of-turn sequence.
3. Check whether the enemy is Down, Broken, or controlled as applicable.
4. Validate the planned action.
5. Validate MP, cooldown, target, and action conditions.
6. Execute the planned action.
7. Resolve damage, healing, status effects, and QTE interactions.
8. Update the Shared Blackboard.
9. Update Tactical Memory.
10. Complete the enemy's turn.

### 9.3 Unavailable Actions

An enemy must not act when it is:

- Defeated.
- Down or otherwise unable to act.
- Controlled under the established combat rules.
- Unable to perform any legal action.

No replacement turn is granted.

### 9.4 MP Management

Enemies must obey their skill costs.

If an enemy cannot afford its planned skill, it must select a legal alternative.

Every enemy that has an MP-free Basic Attack must retain that fallback.

Insufficient MP must never cause an enemy to skip its turn if a legal Basic Attack is available.

No automatic MP regeneration is introduced by this AI specification.

---

## 10. Difficulty and Balance Restrictions

The AI System must preserve the following existing restrictions.

| System | Existing Restriction |
|---|---|
| Normal Enemy Patterns | Normal enemies use Steady patterns only |
| Elite Enemy Patterns | Maximum two patterns |
| Boss Skill Count | Three to five attacks total |
| Boss Signature | Exactly one signature attack |
| Boss Signature Pattern | Four to six hits; Accelerando or Syncopated; Heavy on the final hit |
| Enemy Action Economy | One normal turn and one main action per eligible unit per round, **(v1.1) except units with `turnsPerRound` > 1 (Zoroa, §8.5)** |
| Heavy Attacks | Maximum one Heavy-marked attack per round |
| Heavy and Stun | No Stun and Heavy on the same target in the same round |
| Enemy Scaling | Only the established enemy attributes scale automatically |
| QTE | Timing windows remain independent of enemy AI adaptation |
| Status Chance | Independent of QTE grade |
| Break Gauge | Does not automatically scale with dungeon progression |

The AI must never use adaptation as a justification for violating these restrictions.

---

## 11. AI Planning Algorithm

### 11.1 Round Preparation

```text
BEGIN ROUND PREPARATION

    Rebuild initiative queue

    Read current battle state

    Update shared battlefield information

    Update tactical memory from previous round      // (v1.1) one shared encounter memory; decay applied here

    FOR each active enemy in PLANNING ORDER (§4.5: Support -> Controller -> others, slot ties; NOT initiative):

        Validate enemy eligibility

        Read enemy personality

        Read current battlefield state

        Read shared Blackboard

        Read tactical memory

        Generate legal actions

        Apply mandatory skill rules

        IF mandatory action exists:
            Select mandatory action

        ELSE:
            Evaluate each legal action

            Evaluate legal targets

            Apply role modifiers

            Apply adaptation modifiers

            Apply coordination modifiers

            Select highest-scoring legal action and target

        Publish planned action to Blackboard

        Publish required telegraph

    END FOR

END ROUND PREPARATION
```

### 11.2 Enemy Turn Execution

```text
BEGIN ENEMY TURN

    Validate enemy state

    Execute established start-of-turn sequence

    IF enemy cannot act:
        Skip turn
        END

    Validate planned action

    IF planned action is still legal:
        Execute planned action                      // (v1.1) never re-scored

    ELSE:
        Find legal fallback

        IF fallback exists:
            Execute fallback
        ELSE:
            Skip turn
        END IF

    END IF

    Resolve combat results

    Update shared Blackboard

    Update tactical memory

    IF unit has another turn this round (v1.1, Zoroa):
        Re-plan next turn's intent from the current Blackboard and publish it

END ENEMY TURN
```

---

## 12. AI Data Structure Proposal

The following is a conceptual implementation structure, not a finalized engine-specific schema.

### Enemy AI Configuration

```text
EnemyAIConfig
{
    enemyId
    enemyRole
    personalityProfile

    decisionModel
    adaptationLevel

    actionRules[]
    utilityWeights

    targetPriorityRules[]
    memoryConfiguration

    telegraphRules
}
```

### Encounter Tactical Memory (v1.1: one per encounter, held by the Blackboard)

```text
EncounterTacticalMemory
{
    encounterId

    observedPlayerActions[]
    observedTargetPatterns[]
    observedDefensivePatterns[]
    observedDamageResults[]

    successfulEnemyActions[]
    ineffectiveEnemyActions[]

    recentTargetAssignments[]
    recentActionHistory[]

    triggeredReads[]          // (v1.1) responses already announced in the log
}
```

### Shared Blackboard

```text
EnemyBlackboard
{
    encounterId

    partyState[]
    enemyState[]

    plannedEnemyActions[]
    currentTargetAssignments[]
    lethalReservations[]      // (v1.1)

    activeThreats[]
    enemyConditions[]

    recentCombatEvents[]
    encounterPhase

    memory                    // (v1.1) EncounterTacticalMemory
}
```

### Planned Enemy Action

```text
EnemyIntent
{
    enemyId
    actionId
    targetIds[]

    intentCategory

    isTelegraphed
    telegraphTiming

    planningRound
    turnIndex                 // (v1.1) 0 or 1 for units with two turns
    executionStatus
}
```

---

## 13. Implementation Roadmap

Implementation should be divided into five stages.

| Stage | Feature | Objective |
|---|---|---|
| 1 | Hybrid Action Selection | Implement mandatory rules and Utility AI |
| 2 | Shared Blackboard | Allow enemies to coordinate targets and intentions |
| 3 | Enemy Personalities | Establish unique tactical identities |
| 4 | High Adaptation | Implement tactical memory and behavior recognition |
| 5 | Boss AI | Apply advanced adaptation to boss phases and signature attacks |

Each stage should be tested before proceeding to the next.

**(v1.1)** The executable plan merges this roadmap with the Architecture roadmap into stages 0–10, each with a hard gate: see `Enemy_AI_Implementation_Handoff.md`.

---

## 14. Testing and Validation

The AI must be tested through automated combat simulations and manual playtesting.

### 14.1 Action Selection Tests

- Enemy selects a legal skill.
- Enemy does not select a skill with insufficient MP.
- Enemy uses its Basic Attack fallback.
- Mandatory skill conditions override utility scoring.
- Enemy does not waste actions on invalid targets.

### 14.2 Target Selection Tests

- Enemy identifies vulnerable targets.
- Enemy respects locked Elite targeting rules.
- Enemy avoids unnecessary overkill when a better target exists.
- Enemy can recognize observed elemental resistance.
- Enemy can coordinate target distribution.
- **(v1.1)** A locked rule switches target only when the first candidate is reserved as lethal, and then uses the same rule.

### 14.3 Coordination Tests

- Multiple enemies can focus a target.
- Enemies do not unnecessarily duplicate attacks.
- Support enemies recognize endangered allies.
- Enemies can coordinate against high-impact party members.
- Enemy decisions remain understandable and consistent.
- **(v1.1)** Planning order and decisions are identical with and without a Speed buff on any enemy.

### 14.4 Adaptation Tests

- Enemy recognizes repeated player behavior.
- Enemy responds to frequent Guard usage.
- Enemy responds to repeated elemental attacks.
- Enemy responds to repeated target focus.
- Enemy does not immediately overreact to a single action.
- Enemy does not gain hidden immunity or statistical advantages.
- Tactical memory resets after battle.
- **(v1.1)** Memory is shared: a summon reacts to behavior observed before it joined. Memory survives Zoroa's phase change.
- **(v1.1)** Each adaptation response logs its read exactly once per encounter.

### 14.5 Telegraph Tests

- Dangerous actions are telegraphed correctly.
- Telegraphs are visible during the correct round.
- The AI does not secretly replace a telegraphed action.
- Royal Ossuary Knight's charge is cancelled when Broken.
- Zoroa's signature attack remains correctly telegraphed.
- Invalid targets are handled without granting additional turns.

### 14.6 Balance Metrics

Track the following during testing:

| Metric | Purpose |
|---|---|
| Wasted Action Rate | Measures ineffective enemy turns |
| Target Distribution | Measures focus-fire behavior |
| Repeated Action Rate | Measures excessive action repetition |
| Adaptation Response Rate | Measures how often memory influences decisions |
| Telegraph Response Rate | Measures player opportunity to counter dangerous actions |
| MP Efficiency | Measures enemy resource usage |
| Enemy Survival | Measures team coordination effectiveness |
| Player Down Frequency | Measures tactical pressure |
| Encounter Completion Rate | Measures overall encounter difficulty |

The AI should not be judged solely by whether it wins or loses against the player.

The goal is to create challenging, readable, and tactically meaningful combat.

---

## 15. Final System Definition

Felna uses a **Hybrid Utility-Based Enemy AI with Shared Blackboard Coordination and High Tactical Adaptation**.

The Rule-Based layer guarantees mechanical correctness.

The Utility AI layer selects tactically valuable actions and targets.

The Shared Blackboard layer allows enemies to cooperate.

The Tactical Memory layer allows enemies to respond to player behavior during the battle.

The entire system operates within Felna's existing initiative, action economy, skill, QTE, Break, telegraph, and difficulty restrictions.

**Final design objective:**

Enemies should not simply attack the player. They should observe the battlefield, understand their roles, coordinate with their allies, recognize repeated behavior, and exploit tactical opportunities—while always giving the player a fair opportunity to understand and respond to their threats.

---

## 16. Source References

1. `GDD_Terbaru.md`: GDD Terpadu v1.5. See especially Chapter 3 (Difficulty System), Chapter 10 (Combat Structure, Turn Order, and Enemy Scaling), Chapter 11 (Damage and Status Effects), Chapter 12 (QTE System), and Chapter 21 (Open Issues).
2. `Felna_Dungeon_1_Enemy_Roster_Scaling_Turn_Order_v1_9_FINAL.xlsx`: Enemy Roster, Skill Database, Initiative & Turn Order, Zoroa Boss Phases, GDD Compliance & Balance, and Elite Design Lock.
3. `Felna_Game_Story_and_World_Lore.docx`: World and Layer references, for encounter and boss identity context.
4. **(v1.1)** `D1_Difficulty_Pass_Handoff.md`, `Charge_Pacing_Handoff.md`, and `Zoroa_Double_Turn_And_Summon_Handoff.md`: the shipped changes behind §8.5.

**Status note:** This document introduces the Enemy AI architecture and proposed general enemy behaviors. It does not retroactively convert unfinalized targeting notes or proposed behavior into existing confirmed GDD rules. Numerical utility weights, detailed personality tuning, and final adaptation thresholds remain subject to implementation and playtesting.

---

## 17. Starting Values (v1.1)

These are **starting values, pending simulation**. They are not final. Each has a test, a pass/fail metric and a tuning direction. The full table, including prediction and strategy values, is in `Enemy_AI_Implementation_Handoff.md`.

| Knob | Starting value | Pass test | If it fails |
|---|---|---|---|
| `UtilityClamp` | ±15 (1.5 priority levels) | Wasted Action Rate ≤ 5%; each role's move mix stays recognisable (200-seed sim) | Erratic: ±10. Rigid: ±20 |
| Memory decay per round | Normal/Elite 0.7, Boss 0.8 | A behavior's influence fades about 3 rounds after it stops (boss about 5) | Adjust decay |
| Recognition (`k` / threshold) | `confidence = s/(s+2)`, recognized at ≥ 0.5 (boss 0.45) | A single action never recognizes; 3 recent consistent actions do | Adjust `k` |
| `FocusCapNormal` | 2 | Normal-encounter party deaths ≤ pre-AI baseline + 15% | 1 |
| `ChargeShieldGate` | 70% of own Break gauge | Telegraph Response Rate 20–50% | ±10 |
| Role weights | Support heal +8, Executioner low-HP +6, Disruptor threat +6, Controller AoE +2 per extra target, Break bruiser BreakPower +6 | Role identity ≥ 60% of turns in simulation | Retune per role |
