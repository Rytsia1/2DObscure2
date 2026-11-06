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
        public event Action<string> Logged;
        public event Action<Combatant> BossPhaseTransitioning;

        public int Round { get; private set; }
        public readonly int Dungeon; // drives enemy stat scaling (Bab 10 §7)
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

        public Battle(IEnumerable<Unit> party, IEnumerable<EnemyDef> enemies, List<string> items, Random rng, RunState run = null)
        {
            this.run = run;
            Party = party.Select((u, i) => new Combatant(u, Side.Party, i, u.name, u.name.Substring(0, Math.Min(3, u.name.Length)).ToUpperInvariant())).ToList();
            var defs = enemies.ToList();
            int dungeonNum = Dungeon = run != null ? (run.Map != null ? run.Map.act : run.Act) : 1;
            Enemies = defs.Select((d, i) =>
            {
                bool dup = defs.Count(x => x == d) > 1;
                char letter = (char)('A' + defs.Take(i).Count(x => x == d));
                return new Combatant(new Unit(d, dungeonNum), Side.Enemy, i, dup ? $"{d.shortName} {letter}" : d.shortName, dup ? d.tag + letter : d.tag, d);
            }).ToList();
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
        public bool HasActed(Combatant c) => acted.Contains(c);

        // QTE result -> offense multiplier. Skills may override Perfect (critical hits).
        public static float QtePower(BattleAction a, QteResult r) =>
            r == QteResult.Perfect && a.kind == ActionKind.Skill && a.skill.perfectPower > 0 ? a.skill.perfectPower : QteConfig.Offense(r);

        // Advances to the next combatant able to act, starting new rounds as needed.
        // Returns null when the battle is over, or when a full cycle passed with nobody able to act
        // (e.g. everyone stunned); calling again simply continues, since skips count stuns down.
        public Combatant NextTurn()
        {
            Active = null;
            int budget = (Party.Count + Enemies.Count) * 2 + 2;
            while (Outcome == Outcome.None && budget-- > 0)
            {
                if (queue.Count == 0) StartRound();
                var c = queue[0];
                queue.RemoveAt(0);
                if (!c.Alive || !acted.Add(c)) continue;
                c.guarding = false;
                StartTurn(c);
                if (!c.Alive) continue; // DoT finished it
                if (c.stunTurns > 0 || c.StatusController.HasStatus(StatusId.Stun))
                {
                    if (c.stunTurns > 0) c.stunTurns--;
                    c.StatusController.DecrementControlImmunities();
                    Say($"{c.name} is stunned and cannot act.");
                    continue;
                }
                if (c.StatusController.HasStatus(StatusId.Sleep))
                {
                    c.StatusController.DecrementControlImmunities();
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
            queue.AddRange(TurnOrder.Build(All));
            foreach (var e in Enemies) EnemyAI.Plan(this, e); // intents are telegraphed from the start of the round
        }

        // Start of the bearer's turn: cooldowns and status durations tick, DoTs bite, Break recovers.
        void StartTurn(Combatant c)
        {
            c.hasActedInBattle = true;
            c.tookDamageSinceLastTurn = false;
            run?.RelicManager?.OnTurnStart(c);
            c.ResetTurnBackgroundPerks();
            if (c.wayfarerSpeedBuffTurns > 0)
                c.wayfarerSpeedBuffTurns--;

            foreach (var id in c.cooldowns.Keys.ToList())
                if (--c.cooldowns[id] <= 0) c.cooldowns.Remove(id);

            // Process DoTs and status lifecycle via StatusController (GDD Section 8, 10, 11, 12)
            c.StatusController.ProcessTurnStart(this, c, Say);
            if (!c.Alive) TryAdvancePhase(c); // a DoT can also deplete a boss HP bar

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
            if (c.DraconicExhaustedTurns > 0 && --c.DraconicExhaustedTurns == 0)
            {
                c.DamageModifiers.RemoveBySource("draconic_exhausted");
            }
            if (c.LightboundTurns > 0 && --c.LightboundTurns == 0)
            {
                c.DamageModifiers.RemoveBySource("lightbound");
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

            // Break gauge recovers 20 at start of owner's turn (Bab 10 §5.3)
            if (c.HasBreakGauge && !c.IsBroken && c.CurrentBreakGauge < c.MaxBreakGauge)
            {
                c.CurrentBreakGauge = Math.Min(c.MaxBreakGauge, c.CurrentBreakGauge + 20);
            }
        }

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
                    if (a.attack != null && a.attack.self) return null;
                    if (a.attack != null && a.attack.allTargets) return Opponents(a.actor).Any(c => c.Alive) ? null : "Invalid target.";
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
            if (actor.IsEnemy) actor.Intent = null; // the round's plan is being carried out

            // GDD Section 11 & 12: Check Action Disruption (Shock / Despair)
            if (actor.StatusController.CheckActionDisruption(a, Rng, out var disruptionReason))
            {
                Say(disruptionReason);
                Active = null;
                return hits;
            }

            // GDD Section 12: Check Confusion / Madness redirection
            if (actor.StatusController.TryRedirectAction(a, this, Rng, out var redirectedAction))
            {
                if (redirectedAction.target != a.target && redirectedAction.target != null)
                    Say($"{actor.name}'s senses are scrambled! Targets {redirectedAction.target.name} instead.");
                a = redirectedAction;
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
                    }
                    if (a.charging)
                    {
                        actor.Charge = new EnemyIntent { move = move, target = a.target };
                        Say($"{actor.name} begins charging {move.name}{(move.allTargets ? "" : $" at {a.target.name}")}!");
                        break;
                    }
                    if (move != null)
                    {
                        actor.unit.mp = Math.Max(0, actor.unit.mp - move.mpCost); // only Skills cost MP; it never regenerates on its own
                        if (move.cooldown > 0) actor.cooldowns[Combatant.MoveKey(move)] = move.cooldown;
                        if (move.telegraph) actor.Charge = null;
                    }
                    if (move != null && move.self)
                    {
                        Say($"{actor.name} uses {move.name}.");
                        if (move.restoreMp > 0f) RestoreMp(actor, (int)Math.Floor(actor.Stats.maxMp * move.restoreMp));
                        foreach (var ap in move.apply ?? Array.Empty<StatusApply>()) Apply(actor, ap, actor, 1f);
                        break;
                    }
                    var targets = move != null && move.allTargets ? Opponents(actor).Where(c => c.Alive).ToList() : new List<Combatant> { a.target };
                    Say(move == null ? $"{actor.name} attacks {a.target.name}."
                        : move.allTargets ? $"{actor.name} unleashes {move.name}!"
                        : $"{actor.name} uses {move.name} on {a.target.name}.");
                    var element = move?.element ?? Element.Physical;
                    for (int i = 0; i < (move?.hits ?? 1); i++)
                    {
                        var reaction = defense != null && i < defense.Count ? defense[i] : (QteResult?)null;
                        float hitPower = (move?.PowerAt(i) ?? 1f) * power;
                        bool heavy = move != null && move.HeavyAt(i);
                        foreach (var t in targets.Where(c => c.Alive))
                        {
                            // Elemental moves scale from MATK vs MDEF, physical from ATK vs DEF (Bab 10 §7.7).
                            int raw = move != null && move.UsesMagic ? Rules.Magic(actor, t, hitPower, Rng) : Rules.Physical(actor, t, hitPower, Rng);
                            var hit = Deal(actor, t, raw, element, reaction, null, heavy);
                            hits.Add(hit);
                            if (move != null && move.drain > 0f && actor.Alive)
                            {
                                int drained = (int)Math.Floor(hit.amount * move.drain); // from final damage actually dealt
                                if (drained > 0) hits.Add(Heal(actor, drained));
                            }
                        }
                    }
                    // Statuses roll once per target per attack and are not blocked by a Perfect parry (Bab 11 §2.3, Bab 12 §4.3).
                    foreach (var t in targets.Where(c => c != null && c.Alive))
                        foreach (var ap in move?.apply ?? Array.Empty<StatusApply>()) Apply(t, ap, actor, 1f);
                    break;

                case ActionKind.Guard:
                    actor.guarding = true;
                    Say($"{actor.name} raises a guard.");
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
                                actor.unit.RestoreMp(refund);
                                Say($"{actor.name}'s Enlightened Mind restores {refund} MP.");
                            }
                        }
                    }
                    foreach (var t in Targets(actor, s.target, a.target))
                    {
                        if (s.appliesNullBreak && s.nullBreakTarget.IsValid)
                        {
                            if (t.NullBreak.TryApply(s.nullBreakTarget, s.skillLevel))
                                Say($"{t.name}'s {s.nullBreakTarget} Nullification is broken!");
                        }

                        if (s.HasDamageSystemComponents)
                        {
                            QteResult mappedQte = power > 1.05f
                                ? QteResult.Perfect
                                : (power < 0.95f ? QteResult.Poor : QteResult.Good);
                            actor.ApplyMartialArtistCombo(mappedQte, s.isFinisher);
                            if (actor.IsPlayer)
                            {
                                run?.RelicManager?.OnQteResolved(actor, true, mappedQte, this);
                            }
                            var dmgResult = Rules.CalculateSkillDamage(actor, t, s, mappedQte, Rng, s.skillLevel);
                            foreach (var hitRes in dmgResult.hits)
                            {
                                if (!t.Alive && hitRes.hitIndex > 0) break;
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
                                hit.critical = hitRes.anyComponentCriticalHit;

                                var primaryComp = hitRes.componentResults.Count > 0 ? hitRes.componentResults[0] : default;
                                Effectiveness eff = primaryComp.effectiveAffinity switch
                                {
                                    AffinityType.Weakness => Effectiveness.Weak,
                                    AffinityType.Resistant => Effectiveness.Resist,
                                    AffinityType.Nullification => Effectiveness.Immune,
                                    _ => Effectiveness.Neutral
                                };
                                hit.effect = eff;

                                if (t.Alive && t.HasBreakGauge && actor != null && actor.side != t.side)
                                    BreakHit(t, Rules.BreakDamage(actor, s, eff, mappedQte), actor, ref hit);

                                hits.Add(hit);
                            }
                        }
                        else
                        {
                            int rank = actor.unit != null ? Math.Max(1, actor.unit.skillLoadout.GetRank(s.id)) : 1;
                            float scaledPower = s.GetPower(rank);
                            for (int i = 0; i < s.hits && t.Alive; i++)
                            {
                                float skillPwr = scaledPower * power;
                                switch (s.kind)
                                {
                                    case SkillKind.Physical:
                                        int pDmg = s.isRanged ? Rules.Ranged(actor, t, skillPwr, Rng) : Rules.Physical(actor, t, skillPwr, Rng);
                                        hits.Add(Deal(actor, t, pDmg, s.element, null, s));
                                        break;
                                    case SkillKind.Magic:
                                        hits.Add(Deal(actor, t, Rules.Magic(actor, t, skillPwr, Rng), s.element, null, s));
                                        break;
                                    case SkillKind.Heal:
                                        hits.Add(Heal(t, Rules.Healing(actor, skillPwr, Rng), actor));
                                        break;
                                }
                            }
                        }
                        // Self-applied statuses link to the target (Interpose remembers whom it guards).
                        foreach (var ap in s.apply ?? Array.Empty<StatusApply>()) Apply(ap.onSelf ? actor : t, ap, ap.onSelf ? t : actor, power);
                    }
                    break;

                case ActionKind.Item:
                    var item = a.item;
                    Items.Remove(item.id);
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
                            int before = a.target.unit.mp;
                            a.target.unit.mp = Math.Min(a.target.Stats.maxMp, before + amt);
                            Say($"{a.target.name} recovers {a.target.unit.mp - before} MP.");
                        }
                        else if (item.mana > 0)
                        {
                            int before = a.target.unit.mp;
                            a.target.unit.mp = Math.Min(a.target.Stats.maxMp, before + item.mana);
                            Say($"{a.target.name} recovers {a.target.unit.mp - before} MP.");
                        }
                    }
                    break;
            }
            a.actor.StatusController.DecrementControlImmunities();
            Active = null;
            return hits;
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

            // Check Mental Effect immunity if boss (GDD Section 13)
            bool isBoss = t.enemy != null && t.enemy.tier == Tier.Boss;
            if (t.StatusController.ResistanceProfile.IsImmune(ap.id, isBoss))
            {
                Say($"{t.name} is immune to {StatusInfo.Name(ap.id)}!");
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

            bool applied = t.StatusController.TryApplyStatus(applyClone, source, t, out var inst, Rng, 1);
            if (applied)
            {
                t.AddStatus(ap.id, inst.remainingTurns, inst.magnitude, source);
                Say(StatusInfo.IsBuff(ap.id) ? $"{t.name} gains {StatusInfo.Name(ap.id)}." : $"{t.name} suffers {StatusInfo.Name(ap.id)}.");
            }
            else
            {
                if (t.StatusController.ResistanceProfile.GetEffectiveResistance() > 0f)
                {
                    Say($"{t.name} resists {StatusInfo.Name(ap.id)}!");
                    // Hermit Rank V: When the character successfully resists a status effect, restore MP equal to 3% of Max MP, at most once per turn.
                    if (t.unit?.background != null && !t.hermitResistedThisTurn)
                    {
                        var bgRank = t.unit.background.GetRank(t.unit.level);
                        if (bgRank != null && bgRank.statusResistMpRestorePercent > 0f)
                        {
                            t.hermitResistedThisTurn = true;
                            int mpRestore = Math.Max(1, (int)Math.Round(t.Stats.maxMp * bgRank.statusResistMpRestorePercent));
                            t.unit.RestoreMp(mpRestore);
                            Say($"{t.name}'s Tranquil Spirit restores {mpRestore} MP.");
                        }
                    }
                }
            }
        }

        // All damage goes through here: reaction, then element, then HP, then Break Gauge reduction.
        Hit Deal(Combatant attacker, Combatant t, int amount, Element element, QteResult? reaction, SkillDef skill, bool heavy = false)
        {
            // GDD Bab 11 & 12: Broken cannot achieve Perfect on Defensive QTE (caps at Good / 50% reduction)
            if (t.IsBroken && reaction == QteResult.Perfect)
            {
                reaction = QteResult.Good;
            }

            // GDD Bab 11 & 12: Stun, Sleep, Mind Control cause auto-Poor reaction
            if (t.stunTurns > 0 || t.StatusController.HasStatus(StatusId.Stun) || t.StatusController.HasStatus(StatusId.Sleep) || t.StatusController.HasStatus(StatusId.MindControl))
            {
                reaction = QteResult.Poor;
            }

            // Heavy attack check (Bab 12)
            bool isHeavy = heavy || skill != null && (skill.HasTag("Heavy") || (attacker?.enemy?.attacks != null && attacker.enemy.attacks.Any(a => a.name == skill.name && a.isHeavy)));

            if (reaction == QteResult.Perfect)
            {
                if (isHeavy)
                {
                    amount = Math.Max(1, (int)Math.Round(amount * 0.5f));
                    Say($"{t.name} parries the Heavy attack for 50% damage reduction!");
                }
                else
                {
                    Say($"{t.name} perfectly parries!");
                    return new Hit { target = t, amount = 0, blocked = true };
                }
            }
            var effect = t.Affinities.Against(element);
            if (effect == Effectiveness.Immune)
            {
                Say($"{t.name} is immune to {element}!");
                return new Hit { target = t, amount = 0, effect = effect };
            }
            amount = Math.Max(1, (int)Math.Round(amount * Affinities.Multiplier(effect)));
            string note = effect == Effectiveness.Weak ? " - weakness!" : effect == Effectiveness.Resist ? " - resisted" : "";
            if (reaction == QteResult.Good)
            {
                amount = Math.Max(1, (int)Math.Round(amount * QteConfig.GoodDefense));
                note += " (blocked)";
            }

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

            var hit = Damage(t, amount, note);
            hit.effect = effect;

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
                int rawBreak = Rules.BreakDamage(attacker, skill, effect);
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
                BreakHit(t, rawBreak, attacker, ref hit);
            }

            return hit;
        }

        // Break Gauge reduction; becoming Broken interrupts a charge in progress (Bab 12 §6.5).
        void BreakHit(Combatant t, int rawBreak, Combatant attacker, ref Hit hit)
        {
            bool charging = t.IsCharging;
            hit.brokeTarget = t.ApplyBreakDamage(rawBreak, attacker, out int reduced);
            hit.breakAmount = reduced;
            if (!hit.brokeTarget) return;
            Say($"{t.name}'s guard shatters! (BROKEN)");
            if (charging) Say($"{t.name}'s charge is interrupted!");
        }

        // A boss whose HP bar is depleted continues as its next phase with a fresh bar (same encounter, same combatant).
        bool TryAdvancePhase(Combatant t)
        {
            if (t.Alive || t.enemy?.nextPhase == null || !Database.Enemies.TryGetValue(t.enemy.nextPhase, out var next)) return false;
            string text = t.enemy.phase2Text;
            BossPhaseTransitioning?.Invoke(t);
            t.AdvanceToPhase(next, Dungeon);
            Say(text ?? $"{t.name} enters phase {t.phase}!");
            return true;
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
            if (!t.Alive && TryAdvancePhase(t)) return new Hit { target = t, amount = amount };
            if (!t.Alive)
            {
                acted.Add(t); // a unit defeated this round never acts again in it, even if revived (Bab 10 §8.6)
                Say(t.IsEnemy ? $"{t.name} is destroyed." : $"{t.name} falls!");
                // Relic System: Enemy Defeated (R013 Dirge-Eater's Fang, R022 Zoroa's Dull Crown)
                if (t.IsEnemy)
                {
                    run?.RelicManager?.OnEnemyDefeated(Active, t, this);
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

        // MP restoration skills (e.g. Arcane Charge); never exceeds Max MP.
        public int RestoreMp(Combatant t, int amount)
        {
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
            if (t?.unit?.background != null)
            {
                var bgRank = t.unit.background.GetRank(t.unit.level);
                if (bgRank != null && bgRank.healingReceivedBonus > 0f)
                {
                    amount = (int)Math.Round(amount * (1f + bgRank.healingReceivedBonus));
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
