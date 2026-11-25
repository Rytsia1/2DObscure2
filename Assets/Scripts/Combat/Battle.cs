using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum Outcome { None, Victory, Defeat }

    // Turn-based battle simulation. No Unity, no UI: callers ask NextTurn(), then Perform() an action.
    public class Battle
    {
        public readonly List<Combatant> Party, Enemies;
        public readonly List<string> Items; // shared with the run, consumed in place
        public readonly Random Rng;
        public readonly List<string> Log = new List<string>();
        public readonly AiTraceLog AiTrace = new AiTraceLog();
        public readonly EncounterBlackboard Blackboard = new EncounterBlackboard();
        public event Action<string> Logged;
        public event Action<Combatant> BossPhaseTransitioning;
        public event Action<Combatant> BossAffinitiesShifted;
        public event Action<Combatant> EnemyTransformed;
        public event Action<Combatant, StatusId, bool> StatusApplied;
        public event Action<Combatant, StatusId, bool> StatusResisted; // bool: immune (true) or resisted by roll (false)
        public event Action<Combatant, CodexRevealEvent> CodexRevealed;

        readonly EnemyCodex localCodex = new EnemyCodex();
        public EnemyCodex Codex => run?.Codex ?? localCodex;
        public bool BossJustShiftedAffinities { get; private set; }

        public int Round { get; private set; }
        public readonly int Dungeon; // drives enemy stat scaling (Bab 10 §7)
        public readonly int Stage;   // drives Stage 2-3 enemy escalation & hidden affixes (1 in Prologue / Stage 1)
        public RunState run;
        public bool partyHasAttackedInBattle;
        public bool IsInspiringLeaderActive(Side side)
        {
            if (Round > TalentSystem.InspiringLeaderDuration) return false;
            var allies = side == Side.Party ? Party : Enemies;
            return allies.Any(a => a.Alive && a.unit != null && a.unit.HasTalent("inspiring_leader"));
        }
        public Combatant Active { get; private set; }

        readonly List<Combatant> queue = new List<Combatant>();
        readonly HashSet<Combatant> acted = new HashSet<Combatant>();
        Combatant startingTurn;

        public Battle(IEnumerable<Unit> party, IEnumerable<EnemyDef> enemies, List<string> items, Random rng, RunState run = null)
        {
            this.run = run;
            var defs = enemies.ToList();
            int dungeonNum = Dungeon = run != null ? (run.Map != null ? run.Map.act : run.Act) : 1;
            int stageNum = Stage = (run != null && !run.IsPrologue) ? Math.Max(1, run.Stage) : 1;
            int affixSeed = run != null ? run.DungeonSeed : 1;
            Enemies = defs.Select((d, i) =>
            {
                bool dup = defs.Count(x => x == d) > 1;
                char letter = (char)('A' + defs.Take(i).Count(x => x == d));
                var c = new Combatant(new Unit(d, dungeonNum, stageNum), Side.Enemy, i, dup ? $"{d.shortName} {letter}" : d.shortName, dup ? d.tag + letter : d.tag, d);
                var affixes = EnemyAffixSystem.RollAffixes(d, stageNum, i, affixSeed, isSummon: false);
                if (affixes.Count > 0)
                {
                    c.EnemyAffixes.AddRange(affixes);
                    EnemyAffixSystem.ApplyInitialModifiers(c, resetCurrentBreakGauge: true);
                }
                return c;
            }).ToList();
            var usedTags = new HashSet<string>(Enemies.Select(e => e.tag));
            Party = party.Select((u, i) => new Combatant(u, Side.Party, i, u.name, UniqueTag(u.name, usedTags)) { run = run }).ToList();
            Items = items;
            Rng = rng;

            run?.RelicManager?.OnBattleStart(this);

            // Background Perk Rank V: Wayfarer (10% chance to gain 10% Speed for 2 turns at start of battle)
            foreach (var p in Party)
            {
                if (p.unit?.background != null)
                {
                    var rank = p.unit.background.GetRank(p.unit.level);
                    if (rank != null && rank.battleStartSpeedBuffChance > 0f)
                    {
                        if (Rng.NextDouble() < rank.battleStartSpeedBuffChance)
                            p.wayfarerSpeedBuffTurns = rank.battleStartSpeedBuffDuration;
                    }
                }
            }

            // Elf Keen Senses (Spec Part D): once per dungeon, reveal one unknown enemy weakness at battle start.
            if (run != null && !run.IsPrologue && run.HasRace("elf") && run.ElfWeaknessRevealUsesRemaining > 0)
            {
                for (int i = 0; i < Enemies.Count; i++)
                {
                    var en = Enemies[i];
                    if (en?.enemy == null) continue;
                    if (Codex.TryRevealWeaknessWithKeenSenses(en.enemy, out var ev))
                    {
                        run.TryConsumeElfWeaknessReveal();
                        Say($"Keen Senses: {en.enemy.name} is WEAK to {ev.element}!");
                        CodexRevealed?.Invoke(en, ev);
                        break;
                    }
                }
            }
        }

        // Records an elemental hit in the Codex (hero attack, skill, counter, or elemental DoT; Spec Part D).
        public CodexRevealEvent RecordCodexElement(Combatant target, Element element)
        {
            if (target == null || !target.IsEnemy || target.enemy == null) return default;
            var raw = AffinityResolver.GetRaw(target, element);
            var ev = Codex.RecordElementHit(target.enemy, element, raw);
            if (ev.concealedPing)
            {
                Say($"{target.name} reacts strangely to {element}! (?!)");
                CodexRevealed?.Invoke(target, ev);
            }
            else if (ev.newlyRevealed)
            {
                Say($"NEW: {target.enemy.name} — {EnemyCodex.FormatEffectivenessTag(ev.effectiveness)} to {element}");
                CodexRevealed?.Invoke(target, ev);
            }
            return ev;
        }

        // Records a status application attempt in the Codex (succeeded, resisted, or immune; Spec Part D).
        public CodexRevealEvent RecordCodexStatus(Combatant target, StatusId status)
        {
            if (target == null || !target.IsEnemy || target.enemy == null) return default;
            bool isBoss = target.enemy.tier == Tier.Boss;
            var tier = target.StatusController.ResistanceProfile.GetTier(status, isBoss);
            var ev = Codex.RecordStatusAttempt(target.enemy, status, tier);
            if (ev.newlyRevealed)
            {
                Say($"NEW: {target.enemy.name} — {EnemyCodex.FormatStatusTier(ev.statusTier)} to {StatusInfo.Name(status)}");
                CodexRevealed?.Invoke(target, ev);
            }
            return ev;
        }

        // Turn-bar tag: first 3 letters, else first 2 + a later letter (CLERIC -> CLR), else first 2 + digit. Adds the result to `used`.
        public static string UniqueTag(string name, HashSet<string> used)
        {
            var n = (name ?? "").ToUpperInvariant();
            var tag = n.Substring(0, Math.Min(3, n.Length));
            for (int j = 3; used.Contains(tag) && j < n.Length; j++)
                if (char.IsLetter(n[j])) tag = n.Substring(0, 2) + n[j];
            for (int k = 2; used.Contains(tag); k++) tag = n.Substring(0, Math.Min(2, n.Length)) + k;
            used.Add(tag);
            return tag;
        }

        public IEnumerable<Combatant> All => Party.Concat(Enemies);
        public Tier Tier => Enemies.Max(e => e.enemy.tier); // encounter difficulty, drives player QTEs
        public IEnumerable<Combatant> Opponents(Combatant c) => c.IsEnemy ? Party : Enemies;
        public IEnumerable<Combatant> Allies(Combatant c) => c.IsEnemy ? Enemies : Party;

        public Outcome Outcome =>
            Enemies.All(e => !e.Alive) ? Outcome.Victory
            : Party.All(p => !p.Alive) ? Outcome.Defeat
            : Outcome.None;

        // Living combatants still to act this round, in order (excludes Active).
        public IEnumerable<Combatant> Upcoming => queue.Where(c => c.Alive);
        public List<Combatant> NextRoundOrder => TurnOrder.Build(All);
        public bool HasActed(Combatant c) => c.turnsTakenThisRound > 0 || acted.Contains(c);

        // QTE result -> offense multiplier. Skills may override Perfect (critical hits).
        public static float QtePower(BattleAction a, QteResult r) =>
            r == QteResult.Perfect && a.kind == ActionKind.Skill && a.skill.perfectPower > 0 ? a.skill.perfectPower : QteConfig.Offense(r);

        // Advances to the next combatant able to act, starting new rounds as needed.
        // Returns null when the battle is over, or when a full cycle passed with nobody able to act
        // (e.g. everyone stunned); calling again simply continues, since skips count stuns down.
        public Combatant NextTurn()
        {
            Active = null;
            int budget = (Party.Count + Enemies.Count) * 4 + 4;
            while (Outcome == Outcome.None && budget-- > 0)
            {
                if (queue.Count == 0) StartRound();
                var c = queue[0];
                queue.RemoveAt(0);
                if (!c.Alive || c.turnsTakenThisRound >= c.TurnsPerRound) continue;
                bool isFirstTurn = c.turnsTakenThisRound == 0;
                c.turnsTakenThisRound++;
                acted.Add(c);
                c.guarding = false;
                if (isFirstTurn)
                {
                    startingTurn = c;
                    try { StartTurn(c); }
                    finally { startingTurn = null; }
                }
                if (!c.Alive) continue; // DoT finished it
                if (c.stunTurns > 0 || c.StatusController.HasStatus(StatusId.Stun))
                {
                    if (isFirstTurn)
                    {
                        if (c.stunTurns > 0) c.stunTurns--;
                        c.StatusController.DecrementControlImmunities();
                    }
                    c.turnsTakenThisRound = c.TurnsPerRound; // Stun, Sleep and Broken cost the whole round
                    Say($"{c.name} is stunned and cannot act.");
                    continue;
                }
                if (c.StatusController.HasStatus(StatusId.Sleep))
                {
                    if (isFirstTurn) c.StatusController.DecrementControlImmunities();
                    c.turnsTakenThisRound = c.TurnsPerRound; // Stun, Sleep and Broken cost the whole round
                    Say($"{c.name} is asleep and cannot act.");
                    continue;
                }
                if (c.IsEnemy && !EnemyAI.HasLegalAction(this, c))
                {
                    Say($"{c.name} has no usable action."); // skipped, no replacement turn (Bab 10 §8.10)
                    continue;
                }
                return Active = c;
            }
            return null;
        }

        void StartRound()
        {
            Round++;
            acted.Clear();
            foreach (var c in All)
            {
                c.turnsTakenThisRound = 0;
                c.UsedHeavyThisRound = false;
                c.cooldownsJustExpiredThisTurn.Clear();
            }
            queue.AddRange(TurnOrder.Build(All));
            Blackboard.BeginRound(this);
            // Fixed planning order (Spec §4.5, Arch §13.2): Support -> Controller -> other roles, slot ties (independent of initiative).
            foreach (var e in EncounterBlackboard.PlanningOrder(Enemies)) EnemyAI.Plan(this, e); // intents are telegraphed from the start of the round
        }

        // Start of the bearer's turn: cooldowns and status durations tick, DoTs bite, Break recovers.
        void StartTurn(Combatant c)
        {
            c.hasActedInBattle = true;
            bool wasHitSinceLastTurn = c.tookDamageSinceLastTurn;
            c.tookDamageSinceLastTurn = false;
            run?.RelicManager?.OnTurnStart(c);
            EnemyAffixSystem.OnTurnStart(c);
            c.ResetTurnBackgroundPerks();
            if (c.wayfarerSpeedBuffTurns > 0)
                c.wayfarerSpeedBuffTurns--;

            c.cooldownsJustExpiredThisTurn.Clear();
            foreach (var id in c.cooldowns.Keys.ToList())
                if (--c.cooldowns[id] <= 0)
                {
                    c.cooldowns.Remove(id);
                    c.cooldownsJustExpiredThisTurn.Add(id);
                }
            if (c.CounterTurns > 0 && --c.CounterTurns == 0) Say($"{c.name}'s counter stance ends.");

            // Process DoTs and status lifecycle via StatusController (GDD Section 8, 10, 11, 12)
            bool wasEnhancedBeforeDoT = c.enemy != null && c.enemy.isEnhanced;
            int phaseBeforeDoT = c.phase;
            c.StatusController.ProcessTurnStart(this, c, Say);
            if (!c.Alive) TryContinueAfterLethal(c); // fallback if ProcessTurnStart did not already handle it
            // If the combatant just transformed or changed phase from a turn-start DoT, its fresh state is already initialized.
            if ((c.enemy != null && c.enemy.isEnhanced && !wasEnhancedBeforeDoT) || c.phase != phaseBeforeDoT)
                return;

            // Synchronize legacy statuses list with StatusController active instances
            for (int i = c.statuses.Count - 1; i >= 0; i--)
            {
                var s = c.statuses[i];
                var inst = c.StatusController.GetFirst(s.id);
                if (inst != null)
                {
                    s.turns = inst.remainingTurns;
                }
                else
                {
                    // If not in StatusController or expired
                    if (--s.turns <= 0)
                    {
                        c.statuses.RemoveAt(i);
                        if (s.id == StatusId.Broken && c.HasBreakGauge)
                            c.CurrentBreakGauge = c.MaxBreakGauge;
                        if (s.id == StatusId.AttackUp && c.OverdriveExhaustPending)
                            c.TriggerDraconicExhaustion();
                        Say($"{c.name}'s {StatusInfo.Name(s.id)} wears off.");
                    }
                }
            }

            if (c.NullBreak != null && c.NullBreak.HasActiveNullBreak)
            {
                c.NullBreak.TickTurn();
                if (!c.NullBreak.HasActiveNullBreak)
                    Say($"{c.name}'s Null Break wears off.");
            }

            // Racial Feats turn updates (Bab 6)
            if (c.IsPhased) c.IsPhased = false; // Phased ends at start of Elf's next turn
            if (c.FeyStepCooldown > 0) c.FeyStepCooldown--;
            if (c.IronResolveActive)
            {
                c.IronResolveActive = false;
                c.DamageModifiers.RemoveBySource("iron_resolve");
            }
            if (c.IronResolveCooldown > 0) c.IronResolveCooldown--;
            if (c.OceansEmbraceCooldown > 0) c.OceansEmbraceCooldown--;
            if (c.OceansEmbraceTurns > 0 && --c.OceansEmbraceTurns == 0)
            {
                c.EndOceansEmbrace(c.OceansEmbraceTarget);
                c.OceansEmbraceTarget = null;
            }
            if (c.DraconicOverdriveTurns > 0 && --c.DraconicOverdriveTurns == 0)
            {
                c.TriggerDraconicExhaustion();
            }
            else if (c.DraconicExhaustedTurns > 0 && --c.DraconicExhaustedTurns == 0)
            {
                c.DamageModifiers.RemoveBySource("draconic_exhausted");
            }
            if (c.LightboundTurns > 0 && --c.LightboundTurns == 0)
            {
                c.DamageModifiers.RemoveBySource("lightbound");
                if (c.LightboundResistBonus > 0f)
                {
                    c.StatusController.ResistanceProfile.buffResistance = Math.Max(0f, c.StatusController.ResistanceProfile.buffResistance - c.LightboundResistBonus);
                    c.LightboundResistBonus = 0f;
                }
            }
            if (c.DeterminationTurns > 0 && --c.DeterminationTurns == 0)
            {
                c.DamageModifiers.RemoveBySource("determination");
            }

            // Martial Artist Combo Flow: reset if a full turn passes without attacking (Bab 8 §5.1)
            if (c.unit?.classDef != null && c.unit.classDef.id == "martial_artist")
            {
                if (!c.AttackedThisTurn && c.CurrentCombo > 0)
                {
                    c.CurrentCombo = 0;
                    Say($"{c.name}'s Combo Flow resets.");
                }
                c.AttackedThisTurn = false;
            }

            // Break gauge recovers 20 at start of owner's turn (Bab 10 §5.3); player Poise only if not hit since last turn
            if (c.HasBreakGauge && !c.IsBroken && c.CurrentBreakGauge < c.MaxBreakGauge && !(c.IsPlayer && wasHitSinceLastTurn))
            {
                int recover = c.IsPlayer ? Combatant.PlayerPoiseRecovery : Combatant.EnemyShieldRecovery;
                c.CurrentBreakGauge = Math.Min(c.MaxBreakGauge, c.CurrentBreakGauge + recover);
            }
        }

        public BattleAction BuildMindControlledAction(Combatant actor) =>
            StatusEffectController.BuildMindControlledAction(actor, this, Rng);

        // Null when the action is legal right now, otherwise a reason for the player.
        public string Validate(BattleAction a)
        {
            if (Outcome != Outcome.None) return "The battle is over.";
            if (a?.actor == null || a.actor != Active) return "It is not that combatant's turn.";
            if (!a.actor.StatusController.CanPerformAction(a, out var cantReason))
                return cantReason;
            switch (a.kind)
            {
                case ActionKind.Attack:
                    if (a.attack != null && a.actor.unit.mp < a.attack.mpCost) return "Not enough MP.";
                    if (a.attack != null && (a.attack.self || a.attack.Summons)) return null;
                    if (a.attack != null && a.attack.SupportsAlly) return IsAlly(a.actor, a.target) ? null : "Invalid target.";
                    if (a.attack != null && a.attack.allTargets) return Opponents(a.actor).Any(c => c.Alive) ? null : "Invalid target.";
                    if (a.actor.IsMindControlled && a.target != null && a.target.Alive && a.target.side == a.actor.side) return null;
                    return IsFoe(a.actor, a.target) ? null : "Invalid target.";
                case ActionKind.Guard:
                    return null;
                case ActionKind.Skill:
                    if (a.skill == null || !a.actor.unit.HasActiveSkill(a.skill.id)) return "Unknown skill.";
                    if (a.actor.Cooldown(a.skill.id) > 0) return "On cooldown.";
                    if (a.actor.unit.mp < a.actor.GetEffectiveMpCost(a.skill, run)) return "Not enough MP.";
                    if (a.skill.requiresShield && a.actor.unit != null && !a.actor.unit.HasEquippedShield()) return "Requires an equipped shield.";
                    if (a.skill.requiredWeaponType != WeaponType.None && a.actor.unit != null && !a.actor.unit.HasEquippedWeaponType(a.skill.requiredWeaponType))
                        return $"Requires a {a.skill.requiredWeaponType}.";
                    return a.skill.target switch
                    {
                        TargetType.Enemy => IsFoe(a.actor, a.target) ? null : "Invalid target.",
                        TargetType.Ally => IsAlly(a.actor, a.target) ? null : "Invalid target.",
                        TargetType.Self => a.target == null || a.target == a.actor ? null : "Invalid target.",
                        _ => null,
                    };
                case ActionKind.Item:
                    if (a.item == null || !Items.Contains(a.item.id)) return "No such item.";
                    if (a.item.isRevive)
                        return (a.target != null && a.target.side == a.actor.side && !a.target.Alive) ? null : "Target is not a fallen ally.";
                    return IsAlly(a.actor, a.target) ? null : "Invalid target.";
                case ActionKind.RacialFeat:
                    if (!a.actor.CanUseRacialFeat(out var featReason)) return featReason;
                    if (string.Equals(a.actor.unit?.race?.id, "merfolk", StringComparison.OrdinalIgnoreCase))
                        return IsAlly(a.actor, a.target) ? null : "Invalid target.";
                    return null;
            }
            return "Unknown action.";
        }

        static bool IsFoe(Combatant a, Combatant t) => t != null && t.Alive && t.side != a.side;
        static bool IsAlly(Combatant a, Combatant t) => t != null && t.Alive && t.side == a.side;

        // Interpose: a Guardian takes single-target hits aimed at the ally it protects.
        // Fey Step (Elf): Phased targets redirect single-target attacks to the next active party member (Bab 6 & 12).
        public Combatant Intercept(Combatant target)
        {
            if (target == null) return null;
            if (target.IsPhased)
            {
                var redirect = Allies(target).FirstOrDefault(c => c != target && c.Alive && !c.IsPhased);
                if (redirect != null)
                {
                    Say($"{target.name} is Phased! Attack redirects to {redirect.name}.");
                    target = redirect;
                }
            }
            var guard = Allies(target).FirstOrDefault(c => c != target && c.Alive && c.Get(StatusId.Guardian)?.source == target);
            if (guard == null) return target;
            Say($"{guard.name} interposes for {target.name}!");
            return guard;
        }

        // Resolves a validated action and ends the actor's turn. Throws without side effects if illegal.
        // power: offense multiplier from the actor's QTE (damage, healing; >1 also guarantees statuses).
        // defense: the defender's reaction per hit of an attack; null = no reaction (full damage).
        public List<Hit> Perform(BattleAction a, float power = 1f, IReadOnlyList<QteResult> defense = null)
        {
            var error = Validate(a);
            if (error != null) throw new InvalidOperationException(error);

            var actor = a.actor;
            var hits = new List<Hit>();
            bool statusAppliedDuringAction = false;
            void TrackStatus(Combatant _t, StatusId _id, bool _isBuff) => statusAppliedDuringAction = true;
            StatusApplied += TrackStatus;
            try
            {
            if (actor.IsEnemy) actor.Intent = null; // the round's plan is being carried out

            // GDD Section 11 & 12: Check Action Disruption (Shock / Despair)
            if (actor.StatusController.CheckActionDisruption(a, Rng, out var disruptionReason))
            {
                Say(disruptionReason);
                Active = null;
                return hits;
            }

            // GDD Section 12: Check Mind Control / Madness / Confusion redirection
            if (actor.StatusController.TryRedirectAction(a, this, Rng, out var redirectedAction))
            {
                if (actor.IsMindControlled && redirectedAction.target != null)
                    Say($"{actor.name} is mind controlled! Attacks {redirectedAction.target.name}!");
                else if (redirectedAction.target != a.target && redirectedAction.target != null)
                    Say($"{actor.name}'s senses are scrambled! Targets {redirectedAction.target.name} instead.");
                a = redirectedAction;
            }
            else if (actor.IsMindControlled && a.kind == ActionKind.Attack && a.target != null && a.target.side == actor.side)
            {
                Say($"{actor.name} is mind controlled!");
            }

            switch (a.kind)
            {
                case ActionKind.Attack:
                    var move = a.attack;
                    QteResult attackQte = power > 1.05f ? QteResult.Perfect : (power < 0.95f ? QteResult.Poor : QteResult.Good);
                    actor.ApplyMartialArtistCombo(attackQte, false);
                    if (actor.IsPlayer)
                    {
                        run?.RelicManager?.OnQteResolved(actor, true, attackQte, this);
                        if (attackQte == QteResult.Perfect && actor.unit.mp < actor.Stats.maxMp) RestoreMp(actor, QteConfig.PerfectMpRefund);
                    }
                    if (a.charging)
                    {
                        if (move != null && move.HasHeavy) actor.UsedHeavyThisRound = true;
                        actor.Charge = new EnemyIntent { move = move, target = a.target, commitment = IntentCommitment.Committed };
                        Say($"{actor.name} begins charging {move.name}{(move.allTargets ? "" : $" at {a.target.name}")}!");
                        break;
                    }
                    if (move != null)
                    {
                        if (move.HasHeavy) actor.UsedHeavyThisRound = true;
                        actor.unit.mp = Math.Max(0, actor.unit.mp - move.mpCost); // only Skills cost MP; it never regenerates on its own
                        if (move.cooldown > 0) actor.cooldowns[Combatant.MoveKey(move)] = move.cooldown;
                        if (move.telegraph) actor.Charge = null;
                        actor.JustReleasedCharge = move.telegraph; // ponytail: bool = gap of 1; counter if ChargeGapTurns > 1
                    }
                    if (move != null && move.Summons)
                    {
                        Say($"{actor.name} uses {move.name}!");
                        int livingSummons = Enemies.Count(e => e.Alive && e.IsSummon);
                        for (int sIdx = livingSummons; sIdx < move.summonCount; sIdx++)
                            Summon(actor, Dungeon1Roster.RollHarvestCallSummon(Rng));
                        break;
                    }
                    if (move != null && move.self)
                    {
                        Say($"{actor.name} uses {move.name}.");
                        if (move.restoreMp > 0f) RestoreMp(actor, (int)Math.Floor(actor.Stats.maxMp * move.restoreMp));
                        foreach (var ap in move.apply ?? Array.Empty<StatusApply>()) Apply(actor, ap, actor, 1f);
                        break;
                    }
                    if (move != null && move.SupportsAlly)
                    {
                        Say($"{actor.name} uses {move.name} on {a.target.name}.");
                        if (move.Heals) hits.Add(Heal(a.target, (int)Math.Floor(a.target.Stats.maxHp * move.healAlly)));
                        foreach (var ap in move.buffAlly ?? Array.Empty<StatusApply>()) Apply(a.target, ap, actor, 1f);
                        break;
                    }
                    var targets = move != null && move.allTargets ? Opponents(actor).Where(c => c.Alive).ToList() : new List<Combatant> { a.target };
                    Say(move == null ? $"{actor.name} attacks {a.target.name}."
                        : move.allTargets ? $"{actor.name} unleashes {move.name}!"
                        : $"{actor.name} uses {move.name} on {a.target.name}.");
                    var element = move?.element ?? Element.Physical;
                    var transformedInAttack = new HashSet<Combatant>();
                    var blockedRidersInAttack = new HashSet<Combatant>();
                    for (int i = 0; i < (move?.hits ?? 1); i++)
                    {
                        var reaction = defense != null && i < defense.Count ? defense[i] : (QteResult?)null;
                        float hitPower = (move?.PowerAt(i) ?? 1f) * power;
                        bool heavy = move != null && move.HeavyAt(i);
                        foreach (var t in targets.Where(c => c.Alive && !transformedInAttack.Contains(c)))
                        {
                            // Elemental moves scale from MATK vs MDEF, physical from ATK vs DEF (Bab 10 §7.7).
                            float physPen = (actor.IsPlayer && run?.ModifierManager != null) ? run.ModifierManager.GetPhysicalPenetrationBonus(actor) : 0f;
                            int raw = move != null && move.UsesMagic ? Rules.Magic(actor, t, hitPower, Rng) : Rules.Physical(actor, t, hitPower, Rng, physPen);
                            var hit = Deal(actor, t, raw, element, reaction, null, heavy, attackQte);
                            if (hit.transformed) transformedInAttack.Add(t);
                            if (hit.drained || hit.reflected) blockedRidersInAttack.Add(t);
                            if (!hit.transformed && !hit.drained && !hit.reflected && i == (move?.hits ?? 1) - 1 && move != null && move.breakPower > 0 && t.Alive && t.HasBreakGauge && !t.IsBroken)
                            {
                                int hitBreak = hit.breakAmount;
                                int extraBreak = move.breakPower;
                                if (t.IsPlayer && (reaction == QteResult.Good || reaction == QteResult.Perfect || t.guarding))
                                    extraBreak = Math.Max(1, (int)Math.Round(extraBreak * Combatant.PlayerPoiseMitigatedMult));
                                BreakHit(t, extraBreak, actor, ref hit);
                                hit.breakAmount += hitBreak;
                            }
                            hits.Add(hit);
                            if (move != null && move.drain > 0f && actor.Alive && !hit.drained && !hit.reflected)
                            {
                                int drained = (int)Math.Floor(hit.amount * move.drain); // from final damage actually dealt
                                if (drained > 0) hits.Add(Heal(actor, drained));
                            }
                        }
                    }
                    // Statuses roll once per target per attack and are not blocked by a Perfect parry (Bab 11 §2.3, Bab 12 §4.3).
                    // Spec Part E: Drain and Reflect suppress status riders on the draining/reflecting target.
                    foreach (var t in targets.Where(c => c != null && c.Alive && !transformedInAttack.Contains(c) && !blockedRidersInAttack.Contains(c)))
                        foreach (var ap in move?.apply ?? Array.Empty<StatusApply>()) Apply(t, ap, actor, 1f);
                    TriggerCounters(actor, targets, hits);
                    break;

                case ActionKind.Guard:
                    actor.guarding = true;
                    Say($"{actor.name} raises a guard.");
                    Blackboard.Memory.ObserveGuard(actor, Round);
                    break;

                case ActionKind.Skill:
                    var s = a.skill;
                    actor.unit.mp -= actor.GetEffectiveMpCost(s, run);
                    if (s.cooldown > 0) actor.cooldowns[s.id] = s.cooldown;
                    Say($"{actor.name} uses {s.name}.{(power > QteConfig.PerfectPower ? " Critical!" : "")}");

                    // Sage Rank V: After using an Active Skill, 10% chance to restore MP equal to 5% of Max MP.
                    if (actor.unit?.background != null)
                    {
                        var bgRank = actor.unit.background.GetRank(actor.unit.level);
                        if (bgRank != null && bgRank.skillMpRefundChance > 0f)
                        {
                            if (Rng.NextDouble() < bgRank.skillMpRefundChance)
                            {
                                int refund = Math.Max(1, (int)Math.Round(actor.Stats.maxMp * bgRank.skillMpRefundPercent));
                                if (actor.StatusController != null && !actor.StatusController.CanRestoreMp())
                                    Say($"{actor.name}'s Despair blocks Enlightened Mind MP recovery!");
                                else
                                {
                                    actor.unit.RestoreMp(refund);
                                    Say($"{actor.name}'s Enlightened Mind restores {refund} MP.");
                                }
                            }
                        }
                    }
                    QteResult mappedQte = power > 1.05f
                        ? QteResult.Perfect
                        : (power < 0.95f ? QteResult.Poor : QteResult.Good);
                    bool skillDealsDirectDamage = s.HasDamageSystemComponents
                        || s.kind == SkillKind.Physical
                        || (s.kind == SkillKind.Magic && s.power > 0f);
                    if (skillDealsDirectDamage)
                    {
                        actor.ApplyMartialArtistCombo(mappedQte, s.isFinisher);
                        if (actor.IsPlayer)
                        {
                            run?.RelicManager?.OnQteResolved(actor, true, mappedQte, this);
                            if (mappedQte == QteResult.Perfect && actor.unit.mp < actor.Stats.maxMp) RestoreMp(actor, QteConfig.PerfectMpRefund);
                        }
                    }

                    var transformedInSkill = new HashSet<Combatant>();
                    var blockedRidersInSkill = new HashSet<Combatant>();
                    foreach (var t in Targets(actor, s.target, a.target))
                    {
                        if (s.appliesNullBreak && s.nullBreakTarget.IsValid)
                        {
                            if (t.NullBreak.TryApply(s.nullBreakTarget, s.skillLevel))
                                Say($"{t.name}'s {s.nullBreakTarget} Nullification is broken!");
                        }

                        if (s.HasDamageSystemComponents)
                        {
                            var dmgResult = Rules.CalculateSkillDamage(actor, t, s, mappedQte, Rng, s.skillLevel, run);
                            foreach (var hitRes in dmgResult.hits)
                            {
                                if ((!t.Alive || transformedInSkill.Contains(t)) && hitRes.hitIndex > 0) break;

                                var primaryComp = hitRes.componentResults.Count > 0 ? hitRes.componentResults[0] : default;
                                Element compElem = primaryComp.damageType == DamageType.Physical
                                    ? Element.Physical
                                    : (primaryComp.elementType.IsValidElement() ? primaryComp.elementType.ToLegacyElement() : s.element);
                                Effectiveness eff = AffinityResolver.ToEffectiveness(primaryComp.effectiveAffinity);
                                var compReveal = RecordCodexElement(t, compElem);

                                if (hitRes.anyComponentDrained && hitRes.finalHitDamage == 0)
                                {
                                    var dHit = ResolveDrain(t, hitRes.finalDrainedAmount, compElem);
                                    dHit.concealedPing = compReveal.concealedPing;
                                    blockedRidersInSkill.Add(t);
                                    hits.Add(dHit);
                                    continue;
                                }
                                if (hitRes.anyComponentReflected && hitRes.finalHitDamage == 0)
                                {
                                    var rHit = ResolveReflect(actor, t, hitRes.finalReflectedAmount, compElem);
                                    rHit.concealedPing = compReveal.concealedPing;
                                    blockedRidersInSkill.Add(t);
                                    hits.Add(rHit);
                                    continue;
                                }

                                string note = hitRes.allComponentsNullified
                                    ? " (nullified)"
                                    : (hitRes.anyComponentCriticalHit ? " (CRITICAL!)" : "");

                                int skillDmg = hitRes.finalHitDamage;
                                if (actor.IsPlayer && run?.RelicManager != null && t != null)
                                {
                                    float relicDmgMult = 1f;
                                    int relicBreak = 0;
                                    run.RelicManager.ModifyDamageDealt(actor, t, ref relicDmgMult, ref relicBreak, !partyHasAttackedInBattle, this, isSkill: true, skill: s);
                                    partyHasAttackedInBattle = true;
                                    if (Math.Abs(relicDmgMult - 1f) > 0.001f)
                                    {
                                        skillDmg = Math.Max(1, (int)Math.Round(skillDmg * relicDmgMult));
                                    }
                                }

                                var hit = Damage(t, skillDmg, note);
                                if (actor != null && t != null && actor.side != t.side && hit.amount > 0)
                                {
                                    t.LastAttacker = actor;
                                    t.LastDamagedRound = Round;
                                }
                                hit.critical = hitRes.anyComponentCriticalHit;
                                hit.effect = eff;
                                hit.element = compElem;
                                hit.concealedPing = compReveal.concealedPing;

                                if (hitRes.anyComponentDrained && hitRes.finalDrainedAmount > 0 && t.Alive)
                                    hits.Add(ResolveDrain(t, hitRes.finalDrainedAmount, compElem));
                                if (hitRes.anyComponentReflected && hitRes.finalReflectedAmount > 0)
                                    hits.Add(ResolveReflect(actor, t, hitRes.finalReflectedAmount, compElem));

                                if (hit.transformed)
                                {
                                    transformedInSkill.Add(t);
                                    hits.Add(hit);
                                    break; // Transformation Spec §6.1: remaining hits of the killing action stop on the transformed enemy
                                }

                                if (t.Alive && t.HasBreakGauge && actor != null && actor.side != t.side)
                                    BreakHit(t, Rules.BreakDamage(actor, s, eff, mappedQte), actor, ref hit);

                                hits.Add(hit);
                            }
                        }
                        else
                        {
                            int rank = actor.unit != null ? Math.Max(1, actor.unit.skillLoadout.GetRank(s.id)) : 1;
                            float scaledPower = s.GetPower(rank);
                            for (int i = 0; i < s.hits && t.Alive && !transformedInSkill.Contains(t); i++)
                            {
                                float skillPwr = scaledPower * power;
                                Hit hit = default;
                                switch (s.kind)
                                {
                                    case SkillKind.Physical:
                                        float physPen = (actor.IsPlayer && run?.ModifierManager != null) ? run.ModifierManager.GetPhysicalPenetrationBonus(actor) : 0f;
                                        int pDmg = s.isRanged ? Rules.Ranged(actor, t, skillPwr, Rng, physPen) : Rules.Physical(actor, t, skillPwr, Rng, physPen);
                                        hit = Deal(actor, t, pDmg, s.element, null, s, offenseQte: mappedQte);
                                        hits.Add(hit);
                                        break;
                                    case SkillKind.Magic:
                                        hit = Deal(actor, t, Rules.Magic(actor, t, skillPwr, Rng), s.element, null, s, offenseQte: mappedQte);
                                        hits.Add(hit);
                                        break;
                                    case SkillKind.Heal:
                                        hit = Heal(t, Rules.Healing(actor, skillPwr, Rng), actor);
                                        hits.Add(hit);
                                        break;
                                }
                                if (hit.transformed)
                                    transformedInSkill.Add(t);
                                if (hit.drained || hit.reflected)
                                    blockedRidersInSkill.Add(t);
                            }
                        }
                        // Self-applied statuses link to the target (Interpose remembers whom it guards).
                        // Spec Part E: Drain and Reflect suppress status riders on the draining/reflecting target.
                        foreach (var ap in s.apply ?? Array.Empty<StatusApply>())
                        {
                            if (!ap.onSelf && (transformedInSkill.Contains(t) || blockedRidersInSkill.Contains(t))) continue;
                            var scaledAp = s.kind == SkillKind.Support ? SupportScaled(ap, actor, s) : DebuffScaled(ap, actor, s);
                            Apply(ap.onSelf ? actor : t, scaledAp, ap.onSelf ? t : actor, power);
                        }
                    }
                    if (s.counterMultiplier > 0f)
                    {
                        actor.CounterTurns = Math.Max(1, s.counterTurns);
                        actor.CounterMultiplier = s.counterMultiplier;
                        actor.CounterComboGain = s.counterComboGain;
                        Say($"{actor.name} readies a counter.");
                    }
                    break;

                case ActionKind.Item:
                    var item = a.item;
                    Items.Remove(item.id);
                    run?.Inventory?.RemoveConsumable(item.id, 1); // keep the bag in step with the battle list
                    Say($"{actor.name} uses {item.name} on {a.target.name}.");
                    float consumableHealMult = 1f;
                    run?.RelicManager?.ModifyConsumableHeal(ref consumableHealMult);

                    if (item.isRevive && !a.target.Alive)
                    {
                        int maxHp = a.target.Stats.maxHp;
                        float pct = item.reviveHpPercent > 0 ? item.reviveHpPercent : 0.30f;
                        int restoredHp = Math.Max(1, (int)Math.Round(maxHp * pct * consumableHealMult));
                        a.target.unit.hp = Math.Min(maxHp, restoredHp);
                        hits.Add(new Hit { target = a.target, amount = -a.target.unit.hp, heal = true });
                        Say($"{a.target.name} rises from death with {a.target.unit.hp} HP!");
                    }
                    else if (a.target.Alive)
                    {
                        if (item.healPercent > 0)
                        {
                            int baseHeal = Math.Max(1, (int)Math.Round(a.target.Stats.maxHp * item.healPercent));
                            hits.Add(Heal(a.target, (int)Math.Round(baseHeal * consumableHealMult), actor));
                        }
                        else if (item.heal > 0)
                        {
                            hits.Add(Heal(a.target, (int)Math.Round(item.heal * consumableHealMult), actor));
                        }

                        if (item.manaPercent > 0)
                        {
                            int amt = Math.Max(1, (int)Math.Round(a.target.Stats.maxMp * item.manaPercent));
                            RestoreMp(a.target, amt);
                        }
                        else if (item.mana > 0)
                        {
                            RestoreMp(a.target, item.mana);
                        }
                    }
                    break;

                case ActionKind.RacialFeat:
                    switch (actor.unit?.race?.id)
                    {
                        case "elf":
                            actor.ActivateFeyStep();
                            Say($"{actor.name} invokes Fey Step!");
                            break;
                        case "dwarf":
                            actor.ActivateIronResolve();
                            Say($"{actor.name} invokes Iron Resolve!");
                            break;
                        case "dragonborn":
                            actor.ActivateDraconicOverdrive();
                            Say($"{actor.name} unleashes Draconic Overdrive!");
                            break;
                        case "liichtian":
                            actor.ActivateLightbound(Allies(actor));
                            Say($"{actor.name} invokes Lightbound!");
                            break;
                        case "merfolk":
                            var ally = a.target ?? actor;
                            actor.ActivateOceansEmbrace(ally);
                            Say($"{actor.name} invokes Ocean's Embrace on {ally.name}!");
                            break;
                    }
                    break;
            }
            if (a.actor.IsEnemy && a.kind == ActionKind.Attack && a.attack != null && !a.charging
                && !a.attack.allTargets && !a.attack.self && !a.attack.SupportsAlly && !a.attack.Summons
                && a.target != null && a.target.IsPlayer)
            {
                a.actor.LastTargetedHero = a.target;
            }
            if (a.kind != ActionKind.Guard)
                Blackboard.Memory.ObserveAction(this, a, hits, statusAppliedDuringAction, Round);
            if (a.actor.IsEnemy)
            {
                a.actor.cooldownsJustExpiredThisTurn.Clear();
                Blackboard.MarkExecuted(this, a.actor);
            }
            if (a.actor.turnsTakenThisRound == 1)
                a.actor.StatusController.DecrementControlImmunities();
            Active = null;
            if (a.actor.IsEnemy && a.actor.Alive && a.actor.turnsTakenThisRound < a.actor.TurnsPerRound)
                EnemyAI.Plan(this, a.actor); // Plan turn B's intent at the end of turn A and show it right away
            return hits;
            }
            finally
            {
                StatusApplied -= TrackStatus;
            }
        }

        // Spawns a summoned enemy (Harvest Call) at 70% normal HP. Summons join at the next round (never act in the round they appear).
        public void Summon(Combatant summoner, EnemyDef def)
        {
            if (def == null) return;
            var u = new Unit(def, Dungeon, Stage);
            int scaledHp = Math.Max(1, (int)Math.Round(u.stats.maxHp * Dungeon1Roster.SummonHpMult));
            u.stats = new Stats(scaledHp, u.stats.maxMp, u.stats.atk, u.stats.def, u.stats.spd, u.stats.mag);
            u.hp = scaledHp;
            u.RecalculateStats();

            int priorSame = Enemies.Count(e => e.enemy == def);
            bool dup = priorSame > 0;
            if (priorSame == 1)
            {
                var first = Enemies.First(e => e.enemy == def);
                if (first.name == def.shortName)
                {
                    int idx = Enemies.IndexOf(first);
                    var renamed = new Combatant(first.unit, Side.Enemy, first.slot, $"{def.shortName} A", def.tag + "A", def)
                    {
                        IsSummon = first.IsSummon,
                        turnsTakenThisRound = first.turnsTakenThisRound,
                        MaxBreakGauge = first.MaxBreakGauge,
                        CurrentBreakGauge = first.CurrentBreakGauge,
                        Intent = first.Intent,
                        Charge = first.Charge,
                        JustReleasedCharge = first.JustReleasedCharge,
                        UsedHeavyThisRound = first.UsedHeavyThisRound,
                        guarding = first.guarding,
                        stunTurns = first.stunTurns,
                        LastAttacker = first.LastAttacker,
                        LastTargetedHero = first.LastTargetedHero,
                        LastDamagedRound = first.LastDamagedRound,
                    };
                    if (first.EnemyAffixes.Count > 0)
                    {
                        renamed.EnemyAffixes.AddRange(first.EnemyAffixes);
                        EnemyAffixSystem.ApplyInitialModifiers(renamed, resetCurrentBreakGauge: false);
                    }
                    foreach (var st in first.statuses) renamed.statuses.Add(st);
                    foreach (var cd in first.cooldowns) renamed.cooldowns[cd.Key] = cd.Value;
                    Enemies[idx] = renamed;
                    for (int q = 0; q < queue.Count; q++) if (queue[q] == first) queue[q] = renamed;
                    Blackboard.ReplaceEnemy(first, renamed);
                }
            }
            char letter = (char)('A' + priorSame);
            string name = dup ? $"{def.shortName} {letter}" : def.shortName;
            string tag = dup ? def.tag + letter : def.tag;
            int nextSlot = Enemies.Count > 0 ? Enemies.Max(e => e.slot) + 1 : 0;
            var summoned = new Combatant(u, Side.Enemy, nextSlot, name, tag, def)
            {
                IsSummon = true,
                turnsTakenThisRound = Math.Max(1, def.turnsPerRound), // never acts in the round it is summoned
            };
            acted.Add(summoned);
            Enemies.Add(summoned);
            Say($"{(summoner != null ? summoner.name + " summons " : "")}{summoned.name}!");
        }

        // Buff Effectiveness (FELNA-SYS-ATTR-002 v2.0): Final Buff Magnitude = Base * (1 + clamp(0.04*CHA + 0.015*WIS, -0.20, +0.40)).
        // Applies to positive buffs that support attribute scaling (supportsChaScaling or CHA-scaled Support skill).
        // Duration, status chance, cooldown, target count, and Healing Power are untouched.
        public static StatusApply SupportScaled(StatusApply ap, Combatant caster, SkillDef skill = null)
        {
            if (ap == null || caster?.unit == null) return ap;
            bool isEligiblePositiveBuff = ap.id == StatusId.AttackUp || ap.id == StatusId.DefenseUp || ap.id == StatusId.MagicDefenseUp;
            if (!isEligiblePositiveBuff) return ap;
            bool explicitScale = ap.supportsChaScaling || (skill == null || skill.scalingAbility == Ability.CHA);
            if (!explicitScale) return ap;
            float mult = QteConfig.SupportPotencyMultiplier(caster);
            if (Math.Abs(mult - 1f) < 1e-5f) return ap;
            return new StatusApply
            {
                id = ap.id,
                turns = ap.turns,
                magnitude = ap.magnitude * mult,
                chance = ap.chance,
                onSelf = ap.onSelf,
                supportsChaScaling = ap.supportsChaScaling
            };
        }

        // Debuff Effectiveness (FELNA-SYS-ATTR-002 v2.0): Final Debuff Magnitude = Base * (1 + clamp(0.035*CHA + 0.015*INT, -0.20, +0.40)).
        // Applies to non-control stat-weakening debuffs when explicitly enabled or invoked directly.
        // Never alters hard Control Effect durations (Stun, Sleep, Mind Control, Madness, Silence) or Boss Mental Immunity.
        public static StatusApply DebuffScaled(StatusApply ap, Combatant caster, SkillDef skill = null)
        {
            if (ap == null || caster?.unit == null) return ap;
            bool isEligibleDebuff = ap.id == StatusId.AttackDown || ap.id == StatusId.DefenseDown
                || ap.id == StatusId.MagicDefenseDown || ap.id == StatusId.Slow
                || ap.id == StatusId.Fear || ap.id == StatusId.ResistanceDown;
            if (!isEligibleDebuff) return ap;
            bool explicitScale = ap.supportsChaScaling || (skill == null || skill.scalingAbility == Ability.CHA);
            if (!explicitScale) return ap;
            float mult = QteConfig.DebuffPotencyMultiplier(caster);
            if (Math.Abs(mult - 1f) < 1e-5f) return ap;
            return new StatusApply
            {
                id = ap.id,
                turns = ap.turns,
                magnitude = ap.magnitude * mult,
                chance = ap.chance,
                onSelf = ap.onSelf,
                supportsChaScaling = ap.supportsChaScaling
            };
        }

        // Draconic Overdrive raises the counter multiplier 1.0x -> 1.25x (GDD Bab 6 §5.5, Bab 12 §8.5).
        public const float OverdriveCounterMult = 1.25f;

        // Bab 12 §6.5: a ready counter retaliates once per enemy action, after the action resolves, if the user made a
        // Defensive Perfect on any hit (Heavy included). No Offensive QTE; damage from QteConfig.CalculateCounterDamage,
        // Break at 1.0x through Deal. Martial Artist counters add combo but do not count as attacking (Bab 8 §5.1).
        void TriggerCounters(Combatant enemy, IEnumerable<Combatant> defenders, List<Hit> hits)
        {
            foreach (var d in defenders.Where(c => c != null).Distinct().ToList())
            {
                bool perfect = d.perfectDefenseThisAction;
                d.perfectDefenseThisAction = false;
                if (!perfect || d.CounterTurns <= 0 || !d.Alive || !enemy.Alive || !d.IsPlayer) continue;
                float mult = d.CounterMultiplier * (d.DraconicOverdriveTurns > 0 ? OverdriveCounterMult : 1f);
                int dmg = QteConfig.CalculateCounterDamage(d, enemy, mult, Rng);
                Say($"{d.name} counters {enemy.name}!");
                hits.Add(Deal(d, enemy, dmg, Element.Physical, null, null));
                if (d.CounterComboGain > 0 && d.unit?.classDef?.id == "martial_artist")
                    d.CurrentCombo = Math.Min(Combatant.MaxComboNormal, d.CurrentCombo + d.CounterComboGain);
            }
        }

        IEnumerable<Combatant> Targets(Combatant actor, TargetType type, Combatant chosen) => type switch
        {
            TargetType.AllEnemies => Opponents(actor).Where(c => c.Alive).ToList(),
            TargetType.AllAllies => Allies(actor).Where(c => c.Alive).ToList(),
            TargetType.Self => new[] { actor },
            _ => new[] { chosen },
        };

        public void Apply(Combatant t, StatusApply ap, Combatant source, float power)
        {
            if (!t.Alive) return;

            if (StatusInfo.IsBuff(ap.id) && !t.StatusController.CanGainBuffs())
            {
                Say($"{t.name}'s Despair prevents gaining {StatusInfo.Name(ap.id)}!");
                StatusResisted?.Invoke(t, ap.id, true);
                return;
            }

            // Check immunity (GDD Section 13 & Spec Part F)
            bool isBoss = t.enemy != null && t.enemy.tier == Tier.Boss;
            if (t.StatusController.ResistanceProfile.IsImmune(ap.id, isBoss))
            {
                Say($"{t.name} is immune to {StatusInfo.Name(ap.id)}!");
                StatusResisted?.Invoke(t, ap.id, true);
                if (!StatusInfo.IsBuff(ap.id)) RecordCodexStatus(t, ap.id);
                return;
            }

            // Power from QTE: power > GoodPower guarantees application unless resisted/immune
            // GDD Bab 11 §8: Status roll is independent of QTE grade
            float effChance = ap.chance;
            if (source?.unit != null)
            {
                if (ap.id == StatusId.Stun && source.unit.HasTalent("crusher"))
                    effChance += TalentSystem.CrusherStunBonus;
                if (ap.id == StatusId.Bleed && source.unit.HasTalent("slasher"))
                    effChance += TalentSystem.SlasherBleedBonus;
            }
            var applyClone = new StatusApply
            {
                id = ap.id,
                turns = ap.turns,
                magnitude = ap.magnitude,
                chance = effChance,
                onSelf = ap.onSelf,
                dotDamageType = ap.dotDamageType,
                dotElementType = ap.dotElementType,
                dotScalingStat = ap.dotScalingStat,
                dotSkillPower = ap.dotSkillPower,
                dotScaling = ap.dotScaling
            };

            var roll = run?.Tutorial != null && run.Tutorial.ForcesStatus(source, ap) ? new SureRandom() : Rng; // Prologue: first Dread Pulse lands
            bool applied = t.StatusController.TryApplyStatus(applyClone, source, t, out var inst, roll, 1);
            if (!StatusInfo.IsBuff(ap.id)) RecordCodexStatus(t, ap.id);
            if (applied)
            {
                t.AddStatus(ap.id, inst.remainingTurns, inst.magnitude, source);
                if (ap.id == StatusId.MindControl && t.IsEnemy)
                {
                    t.Intent = null;
                    t.Charge = null;
                    Blackboard.ClearEnemy(this, t);
                }
                Say(StatusInfo.IsBuff(ap.id) ? $"{t.name} gains {StatusInfo.Name(ap.id)}." : $"{t.name} suffers {StatusInfo.Name(ap.id)}.");
                StatusApplied?.Invoke(t, ap.id, StatusInfo.IsBuff(ap.id));
            }
            else
            {
                if (t.StatusController.ResistanceProfile.GetEffectiveResistance(ap.id) > 0f)
                {
                    Say($"{t.name} resists {StatusInfo.Name(ap.id)}!");
                    StatusResisted?.Invoke(t, ap.id, false);
                    // Hermit Rank V: When the character successfully resists a status effect, restore MP equal to 3% of Max MP, at most once per turn.
                    if (t.unit?.background != null && !t.hermitResistedThisTurn)
                    {
                        var bgRank = t.unit.background.GetRank(t.unit.level);
                        if (bgRank != null && bgRank.statusResistMpRestorePercent > 0f)
                        {
                            t.hermitResistedThisTurn = true;
                            int mpRestore = Math.Max(1, (int)Math.Round(t.Stats.maxMp * bgRank.statusResistMpRestorePercent));
                            if (t.StatusController != null && !t.StatusController.CanRestoreMp())
                                Say($"{t.name}'s Despair blocks Tranquil Spirit MP recovery!");
                            else
                            {
                                t.unit.RestoreMp(mpRestore);
                                Say($"{t.name}'s Tranquil Spirit restores {mpRestore} MP.");
                            }
                        }
                    }
                }
            }
        }

        // All damage goes through here: reaction, then element, then HP, then Break Gauge reduction.
        // offenseQte: the attacker's Offensive QTE grade; it scales Break Power 1.5 / 1.0 / 0.5 (Bab 11 §2.5).
        Hit Deal(Combatant attacker, Combatant t, int amount, Element element, QteResult? reaction, SkillDef skill, bool heavy = false,
            QteResult offenseQte = QteResult.Good)
        {
            // GDD Bab 11 & 12: Broken cannot achieve Perfect on Defensive QTE (caps at Good / 50% reduction)
            if (t.IsBroken && reaction == QteResult.Perfect)
            {
                reaction = QteResult.Good;
            }

            // GDD Bab 11 & 12: Stun, Sleep, Mind Control cause auto-Poor reaction
            if (t.CannotReact)
            {
                reaction = QteResult.Poor;
            }

            if (reaction.HasValue && t.IsPlayer)
            {
                run?.RelicManager?.OnQteResolved(t, false, reaction.Value, this);
            }
            if (reaction == QteResult.Perfect) t.perfectDefenseThisAction = true; // after Broken/Stun downgrades

            // Heavy attack check (Bab 12)
            bool isHeavy = heavy || skill != null && (skill.HasTag("Heavy") || (attacker?.enemy?.attacks != null && attacker.enemy.attacks.Any(a => a.name == skill.name && a.isHeavy)));

            // Full parry needs a choice: Guarding this round. A plain Perfect only deflects by Block Power (below).
            if (reaction == QteResult.Perfect && !isHeavy && t.guarding)
            {
                Say($"{t.name} perfectly parries!");
                return new Hit { target = t, amount = 0, blocked = true };
            }
            var effect = AffinityResolver.Get(t, element);
            var codexEv = RecordCodexElement(t, element);
            if (effect == Effectiveness.Immune)
            {
                Say($"{t.name} is immune to {element}!");
                return new Hit { target = t, amount = 0, effect = effect, element = element, concealedPing = codexEv.concealedPing };
            }
            bool isDrainOrReflect = effect == Effectiveness.Drain || effect == Effectiveness.Reflect;
            if (!isDrainOrReflect)
                amount = Math.Max(1, (int)Math.Round(amount * AffinityResolver.DamageMultiplier(effect)));
            else
                amount = Math.Max(1, amount);
            string note = effect == Effectiveness.Weak ? " - weakness!" : effect == Effectiveness.Resist ? " - resisted" : "";

            // Elf Fey Step: first attack next turn gets +20% damage (Bab 6)
            if (attacker != null && attacker.FeyStepBonusPending)
            {
                attacker.FeyStepBonusPending = false;
                amount = Math.Max(1, (int)Math.Round(amount * 1.20f));
                Say($"{attacker.name}'s Fey Step empowers the strike (+20% damage)!");
            }

            // Martial Artist: +2% physical damage per combo point (Bab 8 §5.1)
            if (attacker != null && attacker.unit?.classDef?.id == "martial_artist" && attacker.CurrentCombo > 0)
            {
                amount = Math.Max(1, (int)Math.Round(amount * (1f + attacker.CurrentCombo * 0.02f)));
            }

            // Background Perk: Charlatan (Bonus damage vs debuffed targets & unacted targets)
            if (attacker?.unit?.background != null && t != null)
            {
                var bgRank = attacker.unit.background.GetRank(attacker.unit.level);
                if (bgRank != null)
                {
                    float chBonus = 0f;
                    if (t.HasDebuff && bgRank.damageVsDebuffedBonus > 0f)
                    {
                        chBonus += bgRank.damageVsDebuffedBonus;
                    }
                    if (bgRank.firstAttackUnactedBonus > 0f && !t.hasActedInBattle && attacker.charlatanFirstAttackedTargets.Add(t))
                    {
                        chBonus += bgRank.firstAttackUnactedBonus;
                    }
                    if (chBonus > 0f)
                    {
                        amount = Math.Max(1, (int)Math.Round(amount * (1f + chBonus)));
                    }
                }
            }

            // Background Perk: Soldier (War Veteran retaliation bonus on next physical damage instance)
            if (attacker != null && (element == Element.Physical || (skill != null && skill.kind == SkillKind.Physical)))
            {
                if (attacker.soldierRetaliationReady && attacker.unit?.background != null)
                {
                    var bgRank = attacker.unit.background.GetRank(attacker.unit.level);
                    if (bgRank != null && bgRank.onDamagedNextPhysicalDamageBonus > 0f)
                    {
                        amount = Math.Max(1, (int)Math.Round(amount * (1f + bgRank.onDamagedNextPhysicalDamageBonus)));
                        attacker.soldierRetaliationReady = false;
                        Say($"{attacker.name}'s War Veteran retaliation empowers the strike!");
                    }
                }
            }

            // Background Perk: Guard (Physical Damage Taken reduction)
            if (t?.unit?.background != null && (element == Element.Physical || (skill != null && skill.kind == SkillKind.Physical)))
            {
                var bgRank = t.unit.background.GetRank(t.unit.level);
                if (bgRank != null && bgRank.physicalDamageTakenReduction > 0f)
                {
                    float red = bgRank.physicalDamageTakenReduction;
                    if (bgRank.lowHpPhysicalDamageTakenReduction > 0f && t.unit.hp < t.Stats.maxHp * 0.30f)
                    {
                        red += bgRank.lowHpPhysicalDamageTakenReduction;
                    }
                    amount = Math.Max(1, (int)Math.Round(amount * (1f - red)));
                }
            }

            // Relic System: ModifyDamageDealt (R001, R006, R012, R017, R021, R022)
            if (attacker != null && attacker.IsPlayer && run?.RelicManager != null && t != null)
            {
                float relicDmgMult = 1f;
                int relicBreak = 0;
                run.RelicManager.ModifyDamageDealt(attacker, t, ref relicDmgMult, ref relicBreak, !partyHasAttackedInBattle, this);
                partyHasAttackedInBattle = true;
                if (Math.Abs(relicDmgMult - 1f) > 0.001f)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * relicDmgMult));
                }
            }

            // Dungeon Modifier System: ModifyDamageDealt (MOD-001, MOD-002, MOD-016, MOD-017, MOD-022, MOD-024)
            if (attacker != null && attacker.IsPlayer && run?.ModifierManager != null && t != null)
            {
                float modDmgMult = 1f;
                run.ModifierManager.ModifyDamageDealt(attacker, t, element, ref modDmgMult, this);
                if (Math.Abs(modDmgMult - 1f) > 0.001f)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * modDmgMult));
                }
            }

            // Talent: Desperate Grit (+15% All Damage Dealt when HP <= 35% Max HP)
            if (attacker?.unit != null && attacker.unit.HasTalent("desperate_grit") &&
                attacker.unit.hp <= (int)Math.Floor(attacker.Stats.maxHp * TalentSystem.DesperateGritHpThreshold))
            {
                amount = Math.Max(1, (int)Math.Round(amount * (1f + TalentSystem.DesperateGritDamageBonus)));
            }

            // Dungeon Modifier System: ModifyDamageReceived BEFORE Defensive QTE (Bab 12 §5.1 & Bab 15 §5)
            if (t != null && t.IsPlayer && run?.ModifierManager != null)
            {
                float modRecvMult = 1f;
                run.ModifierManager.ModifyDamageReceived(t, element, ref modRecvMult, this);
                if (Math.Abs(modRecvMult - 1f) > 0.001f)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * modRecvMult));
                }
            }

            // Talent: Desperate Grit (-15% All Damage Taken when HP <= 35% Max HP)
            if (t?.unit != null && t.unit.HasTalent("desperate_grit") &&
                t.unit.hp <= (int)Math.Floor(t.Stats.maxHp * TalentSystem.DesperateGritHpThreshold))
            {
                amount = Math.Max(1, (int)Math.Round(amount * (1f - TalentSystem.DesperateGritDamageReduction)));
            }

            // Hidden Stage 2-3 Enemy Affixes (Relentless on enemy attacks; Fortified on legacy Deal path).
            if (attacker != null && attacker.IsEnemy)
                EnemyAffixSystem.ModifyDamageDealt(attacker, ref amount);
            if (t != null && t.IsEnemy)
                EnemyAffixSystem.ModifyDamageReceivedLegacy(t, ref amount);

            // Curse: +25% All Damage Taken BEFORE Defensive QTE (GDD Bab 11 & Bab 12 §5.1)
            if (t?.StatusController != null)
            {
                float statusDmgTakenMult = t.StatusController.GetDamageTakenMultiplier();
                if (Math.Abs(statusDmgTakenMult - 1f) > 0.001f)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * statusDmgTakenMult));
                }
            }

            // Spec Part E: Drain and Reflect resolve on NORMAL-equivalent damage with no Break, no status rider, and no QTE on reflection.
            if (effect == Effectiveness.Drain)
            {
                var dHit = ResolveDrain(t, amount, element);
                dHit.concealedPing = codexEv.concealedPing;
                return dHit;
            }
            if (effect == Effectiveness.Reflect)
            {
                var rHit = ResolveReflect(attacker, t, amount, element);
                rHit.concealedPing = codexEv.concealedPing;
                return rHit;
            }

            // Defensive QTE mitigation applied after damage modifiers (Bab 12 §5.1), scaled by the target's Block Power (§3.5).
            if (reaction == QteResult.Perfect || reaction == QteResult.Good)
            {
                float taken = QteConfig.Defense(reaction.Value, isHeavy, t);
                amount = Math.Max(1, (int)Math.Round(amount * taken));
                if (reaction == QteResult.Perfect && isHeavy) Say($"{t.name} braces against the Heavy attack ({(1f - taken) * 100f:0}% damage reduction)!");
                else note += reaction == QteResult.Perfect ? " (deflected)" : " (blocked)";
            }

            var hit = Damage(t, amount, note);
            if (attacker != null && t != null && attacker.side != t.side && hit.amount > 0)
            {
                t.LastAttacker = attacker;
                t.LastDamagedRound = Round;
            }
            hit.effect = effect;
            hit.element = element;
            hit.concealedPing = codexEv.concealedPing;
            if (hit.transformed) return hit; // Overkill and killing-blow Break are discarded on transformation (Transformation Spec §6.1)

            // Background Perk: Soldier (record retaliation when damaged by enemy attack)
            if (t != null && t.Alive && attacker != null && attacker.side != t.side && amount > 0)
            {
                if (t.unit?.background != null)
                {
                    var bgRank = t.unit.background.GetRank(t.unit.level);
                    if (bgRank != null && bgRank.onDamagedNextPhysicalDamageBonus > 0f && !t.soldierDamagedThisTurn)
                    {
                        t.soldierDamagedThisTurn = true;
                        t.soldierRetaliationReady = true;
                    }
                }
            }

            // Background Perk: Scribe (track attacks on same enemy, granting crit after 3 attacks)
            if (attacker != null && t != null && attacker.unit?.background != null)
            {
                var bgRank = attacker.unit.background.GetRank(attacker.unit.level);
                if (bgRank != null && bgRank.scribeTargetAttackThreshold > 0)
                {
                    if (!attacker.scribeAttackCounts.ContainsKey(t)) attacker.scribeAttackCounts[t] = 0;
                    if (attacker.scribeAttackCounts[t] >= bgRank.scribeTargetAttackThreshold)
                        attacker.scribeAttackCounts[t] = 0;
                    else
                        attacker.scribeAttackCounts[t]++;
                }
            }

            // Break Gauge reduction on enemy targets when struck by an attack or offensive skill.
            if (t.Alive && t.HasBreakGauge && attacker != null && attacker.side != t.side)
            {
                int rawBreak = Rules.BreakDamage(attacker, skill, effect, offenseQte);
                if (attacker.IsPlayer && run?.RelicManager != null && run.RelicManager.HasRelic("R010"))
                {
                    var r010 = run.RelicManager.GetInstance("R010");
                    if (r010 != null && !r010.battleTriggered)
                    {
                        r010.battleTriggered = true;
                        rawBreak += Math.Max(1, (int)Math.Round(rawBreak * 0.15f));
                        Say("[Relic: Executioner's Bell] First strike of battle gains +15% Break Power!");
                    }
                }
                if (t.IsPlayer)
                {
                    float poiseMult = 1f;
                    if (reaction == QteResult.Good || reaction == QteResult.Perfect || t.guarding) poiseMult *= Combatant.PlayerPoiseMitigatedMult;
                    if (isHeavy) poiseMult *= Combatant.PlayerPoiseHeavyMult;
                    rawBreak = Math.Max(1, (int)Math.Round(rawBreak * poiseMult));
                }
                BreakHit(t, rawBreak, attacker, ref hit);
            }

            return hit;
        }

        // Spec Part E — Drain: target takes 0 damage and heals by NORMAL-equivalent damage capped at missing HP. Never revives.
        Hit ResolveDrain(Combatant target, int normalDamage, Element element)
        {
            if (target == null || !target.Alive)
                return new Hit { target = target, amount = 0, effect = Effectiveness.Drain, element = element, drained = true };
            float drainHealMult = target.StatusController != null ? target.StatusController.GetHealingReceivedMultiplier() : 1f;
            int scaled = Math.Max(0, (int)Math.Round(normalDamage * AffinityResolver.DrainHealPct * drainHealMult));
            int missing = Math.Max(0, target.Stats.maxHp - target.unit.hp);
            int healAmt = Math.Min(missing, scaled);
            target.unit.hp += healAmt;
            Say($"{target.name} drains {element} and heals {healAmt} HP! (DRAINED!)");
            return new Hit { target = target, amount = healAmt, heal = true, effect = Effectiveness.Drain, element = element, drained = true };
        }

        // Spec Part E — Reflect: target takes 0 damage; attacker takes NORMAL-equivalent damage with attacker's own affinity,
        // capped at ReflectCapPct of attacker Max HP, with no Defensive QTE, no Break/Poise, no status rider, and no re-reflection.
        Hit ResolveReflect(Combatant attacker, Combatant reflector, int normalDamage, Element element)
        {
            Say($"{reflector.name} reflects {element} back at {attacker?.name ?? "attacker"}! (REFLECTED!)");
            if (attacker == null || !attacker.Alive)
                return new Hit { target = reflector, amount = 0, effect = Effectiveness.Reflect, element = element, reflected = true };

            var selfEff = AffinityResolver.Get(attacker, element);
            if (selfEff == Effectiveness.Immune)
            {
                Say($"{attacker.name} is immune to the reflected {element}!");
                return new Hit { target = reflector, amount = 0, effect = Effectiveness.Reflect, element = element, reflected = true, reflectedTarget = attacker, reflectedAmount = 0 };
            }
            if (selfEff == Effectiveness.Drain)
            {
                int missing = Math.Max(0, attacker.Stats.maxHp - attacker.unit.hp);
                float drainHealMult = attacker.StatusController != null ? attacker.StatusController.GetHealingReceivedMultiplier() : 1f;
                int healAmt = Math.Min(missing, Math.Max(0, (int)Math.Round(normalDamage * AffinityResolver.DrainHealPct * drainHealMult)));
                attacker.unit.hp += healAmt;
                Say($"{attacker.name} drains the reflected {element} for {healAmt} HP!");
                return new Hit { target = reflector, amount = 0, effect = Effectiveness.Reflect, element = element, reflected = true, reflectedTarget = attacker, reflectedAmount = 0 };
            }
            float selfMult = selfEff == Effectiveness.Reflect ? 1f : AffinityResolver.DamageMultiplier(selfEff);
            float curseMult = attacker.StatusController != null ? attacker.StatusController.GetDamageTakenMultiplier() : 1f;
            int scaled = Math.Max(1, (int)Math.Round(normalDamage * selfMult * curseMult));
            int cap = Math.Max(1, (int)Math.Floor(attacker.Stats.maxHp * AffinityResolver.ReflectCapPct));
            int reflectedDmg = Math.Min(cap, scaled);
            var refHit = Damage(attacker, reflectedDmg, " (REFLECTED!)");
            return new Hit
            {
                target = reflector,
                amount = 0,
                effect = Effectiveness.Reflect,
                element = element,
                reflected = true,
                reflectedTarget = attacker,
                reflectedAmount = reflectedDmg,
                killed = refHit.killed || !attacker.Alive,
            };
        }

        // Break Gauge reduction; becoming Broken interrupts a charge in progress (Bab 12 §6.5).
        void BreakHit(Combatant t, int rawBreak, Combatant attacker, ref Hit hit)
        {
            bool charging = t.IsCharging;
            hit.brokeTarget = t.ApplyBreakDamage(rawBreak, attacker, out int reduced);
            hit.breakAmount = reduced;
            if (!hit.brokeTarget) return;
            Say(t.IsPlayer ? $"{t.name} is staggered! (BROKEN)" : $"{t.name}'s guard shatters! (BROKEN)");
            if (charging) Say($"{t.name}'s charge is interrupted!");
            if (t.IsEnemy) Blackboard.ClearEnemy(this, t);
            Blackboard.Memory.ObserveBreak(attacker, t, charging, Round);
        }

        // Exclusions for On-Defeat Transformation (Transformation Spec §3.1 v1.1):
        // Never eligible: summoned enemies (Harvest Call), Tutorial Prologue enemies, or any enemy in a Boss-node encounter.
        public bool IsExcludedFromTransform(Combatant t)
        {
            if (t == null || !t.IsEnemy || t.enemy == null) return true;
            if (t.IsSummon) return true;
            if (run != null && run.IsPrologue) return true;
            if (PrologueRoster.Enemies.Any(pe => pe.id == t.enemy.id)) return true;
            if (Tier == Tier.Boss) return true;
            if (run?.Current != null && run.Current.type == NodeType.Boss) return true;
            return false;
        }

        // A boss whose HP bar is depleted continues as its next phase with a fresh bar; an eligible common enemy gets one
        // independent roll to transform into its Enhanced Common Variant at full HP (Transformation Spec §4, §6, §7, §8).
        // forced = true (e.g. summon collapse on boss final defeat) never rolls transformation.
        public bool TryContinueAfterLethal(Combatant t, bool forced = false)
        {
            if (t == null || t.Alive) return false;
            if (!forced && t.enemy?.nextPhase != null && Database.Enemies.TryGetValue(t.enemy.nextPhase, out var next))
            {
                var oldPhaseDef = t.enemy;
                string text = t.enemy.phase2Text;
                BossPhaseTransitioning?.Invoke(t);
                t.AdvanceToPhase(next, Dungeon, Stage);
                Codex.CarryOverPhaseKnowledge(oldPhaseDef, next);
                BossJustShiftedAffinities = true;
                Say(text ?? $"{t.name} enters phase {t.phase}!");
                Say($"{t.name}: AFFINITIES SHIFTED!");
                BossAffinitiesShifted?.Invoke(t);
                return true;
            }
            if (!forced && t.IsEnemy && t.enemy != null && !string.IsNullOrEmpty(t.enemy.enhancedVariant)
                && !t.transformRolled && !IsExcludedFromTransform(t)
                && Database.Enemies.TryGetValue(t.enemy.enhancedVariant, out var variant))
            {
                t.transformRolled = true;
                if (Rng.NextDouble() < t.enemy.transformChance)
                {
                    string baseName = t.enemy.name;
                    t.AdvanceToPhase(variant, Dungeon, Stage);
                    t.ResetForTransform();
                    Blackboard.ClearEnemy(this, t);
                    Say($"The {baseName} refuses to fall. It rises, Enhanced!");
                    EnemyTransformed?.Invoke(t);
                    // Transformation Spec §8.1: if turn is still pending this round, re-plan immediately with Enhanced kit and publish intent.
                    if (queue.Contains(t) || startingTurn == t)
                        EnemyAI.Plan(this, t);
                    return true;
                }
            }
            return false;
        }

        public Hit Damage(Combatant t, int amount, string note = "")
        {
            if (t != null && t.side == Side.Party && IsInspiringLeaderActive(t.side))
            {
                amount = Math.Max(1, (int)Math.Round(amount * (1f - TalentSystem.InspiringLeaderReduction)));
            }

            // Relic System: ModifyDamageReceived (R003 Rustbound Medal, R011 Hollow Knight's Buckle)
            if (t != null && t.side == Side.Party && run?.RelicManager != null)
            {
                float relicRecvMult = 1f;
                run.RelicManager.ModifyDamageReceived(t, ref relicRecvMult, true, this);
                if (Math.Abs(relicRecvMult - 1f) > 0.001f)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * relicRecvMult));
                }
            }

            // Shield absorption (Bab 11 §6 & Bab 6 §4.1)
            if (t != null && t.unit.shield > 0 && amount > 0)
            {
                int absorbed = Math.Min(t.unit.shield, amount);
                t.unit.shield -= absorbed;
                amount -= absorbed;
                if (t.unit.shield <= 0 && t.OceansEmbraceShieldActive)
                {
                    t.EndOceansEmbrace(t);
                    Say($"{t.name}'s Ocean's Embrace shield breaks and restores 10% HP!");
                }
            }

            int hpBefore = t != null ? t.unit.hp : 0;

            // Relic System: Lethal damage protection (R009 Soul-Tether Ring)
            if (t != null && t.side == Side.Party && amount >= t.unit.hp && amount > 0)
            {
                if (run?.RelicManager != null && run.RelicManager.CheckLethalDamageProtection(t, amount, this))
                {
                    amount = Math.Max(0, hpBefore - 1);
                }
            }

            t.unit.hp = Math.Max(0, t.unit.hp - amount);
            if (amount > 0 && t != null) t.tookDamageSinceLastTurn = true;

            // Relic System: Boss HP threshold (R020 Sovereign's Last Breath)
            if (t != null && t.IsEnemy && t.enemy != null && t.enemy.tier == Tier.Boss)
            {
                run?.RelicManager?.OnBossHpChanged(t, this);
            }

            // Human Determination trigger (Bab 6 §5.1)
            if (amount > 0 && t.CheckDeterminationTrigger(hpBefore, t.unit.hp))
            {
                Say($"{t.name}'s Determination ignites! (Gains Shield and +15% Damage)");
            }

            // GDD Section 12: Sleep ends when target receives direct damage or DoT
            if (amount > 0)
            {
                t.StatusController.OnDamageReceived(amount, false, Say);
                t.statuses.RemoveAll(s => s.id == StatusId.Sleep);
            }

            Say($"{t.name} takes {amount} damage{(t.guarding ? " (guarded)" : "")}{note}.");
            if (!t.Alive && TryContinueAfterLethal(t)) return new Hit { target = t, amount = amount, transformed = true };
            if (!t.Alive)
            {
                acted.Add(t); // a unit defeated this round never acts again in it, even if revived (Bab 10 §8.6)
                t.turnsTakenThisRound = t.TurnsPerRound;
                if (t.IsEnemy)
                {
                    t.Intent = null;
                    t.Charge = null;
                    Blackboard.ClearPendingForEnemy(this, t);
                }
                Say(t.IsEnemy ? $"{t.name} is destroyed." : $"{t.name} falls!");
                // Relic System: Enemy Defeated (R013 Dirge-Eater's Fang, R022 Zoroa's Dull Crown) — skipped for summons
                if (t.IsEnemy && !t.IsSummon)
                {
                    run?.RelicManager?.OnEnemyDefeated(Active, t, this);
                }
                // Zoroa's final bar breaking ends the battle: kill every living summon (no rewards/relic triggers) so Outcome.Victory triggers.
                if (t.IsEnemy && t.enemy != null && t.enemy.tier == Tier.Boss && t.enemy.nextPhase == null)
                {
                    foreach (var s in Enemies.Where(e => e.Alive && e.IsSummon).ToList())
                    {
                        s.unit.hp = 0;
                        s.turnsTakenThisRound = s.TurnsPerRound;
                        acted.Add(s);
                        Say($"{s.name} collapses with its master!");
                    }
                }
            }
            else CheckPhase(t);
            return new Hit { target = t, amount = amount, killed = !t.Alive };
        }

        // Boss phase 2 begins the first time HP drops to the threshold.
        void CheckPhase(Combatant t)
        {
            var d = t.enemy;
            if (d == null || d.phase2At <= 0 || t.phase != 1 || t.unit.hp > t.Stats.maxHp * d.phase2At) return;
            t.phase = 2;
            if (d.bossBreakRecoverOnPhase2 && t.IsBroken) t.RecoverBreakGauge();
            Say(d.phase2Text ?? $"{t.name} changes its stance!");
            if (d.phase2Buff != null) Apply(t, d.phase2Buff, t, 1f);
        }

        // MP restoration skills (e.g. Arcane Charge); never exceeds Max MP. Blocked while Despair is active.
        public int RestoreMp(Combatant t, int amount)
        {
            if (t == null || amount <= 0) return 0;
            if (t.StatusController != null && !t.StatusController.CanRestoreMp())
            {
                Say($"{t.name}'s Despair blocks MP recovery!");
                return 0;
            }
            int gained = Math.Max(0, Math.Min(amount, t.Stats.maxMp - t.unit.mp));
            t.unit.mp += gained;
            Say($"{t.name} restores {gained} MP.");
            return gained;
        }

        public Hit Heal(Combatant t, int amount, Combatant healer = null)
        {
            if (healer?.unit != null && healer.unit.HasTalent("healer"))
            {
                amount = (int)Math.Round(amount * (1f + TalentSystem.HealerBonus));
            }
            if (t?.unit != null && t.unit.HasTalent("hardy_constitution"))
            {
                amount = (int)Math.Round(amount * (1f + TalentSystem.HardyConstitutionHealReceivedBonus));
            }
            if (t?.unit?.background != null)
            {
                var bgRank = t.unit.background.GetRank(t.unit.level);
                if (bgRank != null && bgRank.healingReceivedBonus > 0f)
                {
                    amount = (int)Math.Round(amount * (1f + bgRank.healingReceivedBonus));
                }
            }
            if (t != null && t.IsPlayer && run?.ModifierManager != null && amount > 0)
            {
                float modHealMult = run.ModifierManager.GetHealingReceivedMultiplier(t);
                if (Math.Abs(modHealMult - 1f) > 0.001f)
                {
                    amount = modHealMult <= 0f ? 0 : Math.Max(1, (int)Math.Round(amount * modHealMult));
                }
            }
            if (t?.StatusController != null && amount > 0)
            {
                float statusHealMult = t.StatusController.GetHealingReceivedMultiplier();
                if (Math.Abs(statusHealMult - 1f) > 0.001f)
                {
                    amount = statusHealMult <= 0f ? 0 : Math.Max(1, (int)Math.Round(amount * statusHealMult));
                }
            }
            int before = t.unit.hp;
            t.unit.Heal(amount);
            Say($"{t.name} recovers {t.unit.hp - before} HP.");
            return new Hit { target = t, amount = t.unit.hp - before, heal = true };
        }

        // Post-battle recovery for Farmer (HP) and Noble (MP) Background Perks (Rank V).
        public void ApplyPostBattleRecovery()
        {
            if (Outcome != Outcome.Victory) return;
            foreach (var p in Party)
            {
                if (p.Alive && p.unit?.background != null)
                {
                    var rank = p.unit.background.GetRank(p.unit.level);
                    if (rank != null)
                    {
                        if (rank.postBattleHpRestorePercent > 0f)
                            p.unit.Heal(Math.Max(1, (int)Math.Round(p.Stats.maxHp * rank.postBattleHpRestorePercent)));
                        if (rank.postBattleMpRestorePercent > 0f)
                            p.unit.RestoreMp(Math.Max(1, (int)Math.Round(p.Stats.maxMp * rank.postBattleMpRestorePercent)));
                    }
                }
            }
        }

        public void Say(string line)
        {
            Log.Add(line);
            Logged?.Invoke(line);
        }
    }
}
