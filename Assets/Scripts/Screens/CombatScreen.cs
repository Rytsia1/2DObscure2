using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    public class CombatScreen : GameScreen
    {
        protected override void Build()
        {
            var enc = Run.CurrentEncounter;
            Backdrop.Create(Root, false, 6, Palette.Void, Palette.Night, Palette.Dusk, Palette.Shade);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 40, Palette.Dusk, 5), 0, 62, Ui.Back + 5);
            Ui.Image(Root, PixelArt.Gradient(Ui.Width, 80, Palette.Dusk, Palette.Night, Palette.Void), 0, 82, Ui.Back + 6);

            var battle = new Battle(Run.Party, enc.enemies.Select(id => Database.Enemies[id]), Run.Items, Run.Rng);
            foreach (var buff in Run.TakeBattleBuffs()) // event blessings last one battle
                foreach (var p in battle.Party.Where(p => p.Alive)) p.AddStatus(buff.id, buff.turns, buff.magnitude, p);
            Root.gameObject.AddComponent<CombatView>().Init(battle, enc);
        }
    }

    // Renders a Battle and feeds it player choices and enemy AI decisions. All rules live in Battle.
    public class CombatView : MonoBehaviour
    {
        const float EnemyThink = 0.5f, AfterAction = 0.6f, LogWidth = 148;
        const int LogX = 162, NameWidth = 52; // party panel 0..160, log/command panel 162..320
        const int LogLines = 6;
        static readonly Vector2Int[] PartySlots = { new Vector2Int(80, 34), new Vector2Int(80, 72), new Vector2Int(52, 50), new Vector2Int(52, 88) };

        public Battle Battle { get; private set; }

        class Hud
        {
            public Vector2Int pos, size;
            public SpriteRenderer sprite, hpBar, hpBack, mpBar, brkBar, brkBack;
            public Label name, hp, mp, status;
            public int phase = 1;
        }

        readonly Dictionary<Combatant, Hud> huds = new Dictionary<Combatant, Hud>();
        readonly List<Label> logLabels = new List<Label>();
        readonly List<string> logLines = new List<string>();
        Transform turnBar, phase;
        Label roundLabel, hint;
        SpriteRenderer marker;
        string turnText = "";
        BattleAction chosen;
        Qte activeQte;
        readonly System.Random qteRng = new System.Random(); // separate from battle RNG so QTEs don't shift damage rolls

        static Vector2Int[] EnemySlots(int count) => count switch
        {
            1 => new[] { new Vector2Int(230, 34) },
            2 => new[] { new Vector2Int(212, 32), new Vector2Int(256, 50) },
            _ => new[] { new Vector2Int(200, 30), new Vector2Int(240, 50), new Vector2Int(278, 30) },
        };

        public void Init(Battle battle, EncounterDef encounter)
        {
            Battle = battle;
            var t = transform;

            Ui.Panel(t, 0, 0, Ui.Width, 14);
            roundLabel = Ui.Label(t, "", 5, 4, Palette.Parchment);
            turnBar = Child("TurnBar");
            // Hint strip under the turn bar: telegraphs, tooltips and target info never cover the battlefield.
            Ui.Panel(t, 0, 13, Ui.Width, 12, Palette.Void, Palette.Shade);
            hint = Ui.Label(t, "", 160, 16, Palette.Bone, Align.Center, order: Ui.Widget + 2);

            for (int i = 0; i < battle.Party.Count; i++) AddCombatant(battle.Party[i], PartySlots[i]);
            var slots = EnemySlots(battle.Enemies.Count);
            for (int i = 0; i < battle.Enemies.Count; i++) AddCombatant(battle.Enemies[i], slots[i]);
            marker = Ui.Image(t, PixelArt.FromRows(Art.Marker), 0, 0, Ui.Widget + 5);

            Ui.Panel(t, 0, 112, LogX - 2, 68);
            Ui.Panel(t, LogX, 112, Ui.Width - LogX, 68);
            for (int i = 0; i < LogLines; i++) logLabels.Add(Ui.Label(t, "", LogX + 5, 117 + i * 10, Palette.Parchment));

            battle.Logged += AddLog;
            AddLog(encounter.tier == Tier.Boss ? "The Warden's eye opens." : MapScreen.EnemyNames(encounter) + (encounter.enemies.Length > 1 ? " attack!" : " attacks!"));
            StartCoroutine(Loop());
        }

        Transform Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void AddCombatant(Combatant c, Vector2Int pos)
        {
            var sprite = PixelArt.FromRows(c.unit.sprite);
            var h = new Hud { pos = pos, size = new Vector2Int((int)sprite.rect.width, (int)sprite.rect.height) };
            h.sprite = Ui.Image(transform, sprite, pos.x, pos.y, Ui.World + pos.y / 10);
            huds[c] = h;

            if (c.IsEnemy)
            {
                int cx = pos.x + h.size.x / 2, ly = pos.y + h.size.y + 2;
                var tierColor = c.enemy.tier == Tier.Boss ? Palette.Crimson : c.enemy.tier == Tier.Elite ? Palette.Ember : Palette.Bone;
                h.name = Ui.Label(transform, c.name.ToUpperInvariant(), cx, ly, tierColor, Align.Center);
                h.hpBack = Bar(cx - 15, ly + 9, 30, Palette.Void, 0);
                h.hpBar = Bar(cx - 15, ly + 9, 30, Palette.Crimson, 1);
                h.brkBack = Bar(cx - 15, ly + 12, 30, Palette.Void, 0);
                h.brkBar = Bar(cx - 15, ly + 12, 30, Palette.Gold, 1);
                h.status = Ui.Label(transform, "", cx, ly + 15, Palette.Frost, Align.Center);
                return;
            }
            int y = 117 + c.slot * 15;
            h.name = Ui.Label(transform, PixelFont.FitName(c.name.ToUpperInvariant(), NameWidth), 5, y, Palette.Bone);
            h.hp = Ui.Label(transform, "", 62, y, Palette.Bone);
            h.mp = Ui.Label(transform, "", 106, y, Palette.Lilac);
            h.status = Ui.Label(transform, "", 141, y, Palette.Frost);
            Bar(62, y + 9, 40, Palette.Void, 0);
            h.hpBar = Bar(62, y + 9, 40, Palette.Moss, 1);
            Bar(106, y + 9, 30, Palette.Void, 0);
            h.mpBar = Bar(106, y + 9, 30, Palette.Arcane, 1);
        }

        SpriteRenderer Bar(int x, int y, int w, Color color, int layer)
        {
            var sr = Ui.Image(transform, PixelArt.Pixel, x, y, Ui.Widget + layer, color);
            sr.transform.localScale = new Vector3(w, 2, 1);
            return sr;
        }

        static void SetBar(SpriteRenderer bar, int full, int value, int max) =>
            bar.transform.localScale = new Vector3(max <= 0 ? 0 : Mathf.CeilToInt(full * (float)value / max), 2, 1);

        void Refresh()
        {
            roundLabel.Set($"ROUND {Battle.Round}");
            foreach (var kv in huds)
            {
                var c = kv.Key;
                var h = kv.Value;
                if (c.IsEnemy)
                {
                    h.sprite.enabled = h.hpBack.enabled = h.hpBar.enabled = c.Alive;
                    if (h.brkBack != null) h.brkBack.enabled = c.Alive && c.HasBreakGauge;
                    if (h.brkBar != null)
                    {
                        h.brkBar.enabled = c.Alive && c.HasBreakGauge;
                        SetBar(h.brkBar, 30, c.CurrentBreakGauge, c.MaxBreakGauge);
                        h.brkBar.color = c.IsBroken ? Palette.Ash : Palette.Gold;
                    }
                    h.name.gameObject.SetActive(c.Alive);
                    h.status.gameObject.SetActive(c.Alive);
                    SetBar(h.hpBar, 30, c.unit.hp, c.Stats.maxHp);
                    h.status.Set((c.enemy.phase2At > 0 ? $"PHASE {c.phase} " : "") + c.StatusTags);
                    h.status.Color = c.phase > 1 ? Palette.Crimson : c.HasDebuff ? Palette.Ember : Palette.Frost;
                    if (c.phase != h.phase)
                    {
                        h.phase = c.phase;
                        Hint($"{c.name.ToUpperInvariant()} ENTERS PHASE {c.phase}!");
                        StartCoroutine(Flash(h, c));
                    }
                    continue;
                }
                h.sprite.color = c.Alive ? Color.white : Palette.Shade;
                h.name.Color = !c.Alive ? Palette.Crimson : c == Battle.Active ? Palette.Gold : Palette.Bone;
                h.hp.Set($"{c.unit.hp}/{c.Stats.maxHp}");
                h.mp.Set(c.Alive ? $"MP {c.unit.mp}" : "DOWN");
                h.mp.Color = c.Alive ? Palette.Lilac : Palette.Crimson;
                h.status.Set(c.Alive ? PixelFont.Fit(c.StatusTags, 17) : "");
                h.status.Color = c.HasDebuff ? Palette.Crimson : Palette.Frost;
                SetBar(h.hpBar, 40, c.unit.hp, c.Stats.maxHp);
                SetBar(h.mpBar, 30, c.unit.mp, c.Stats.maxMp);
            }
            marker.enabled = Battle.Active != null;
            BuildTurnBar();
        }

        // Active combatant boxed, then the rest of this round, then a preview of the next round.
        void BuildTurnBar()
        {
            foreach (Transform child in turnBar) Destroy(child.gameObject);
            int x = 52;
            void Chip(Combatant c, Color color, bool active = false)
            {
                int w = PixelFont.Measure(c.tag);
                if (x + w > 314) return;
                if (active) Ui.Image(turnBar, PixelArt.Box(w + 6, 12, Palette.Dusk, Palette.Gold), x - 3, 1, Ui.Widget);
                Ui.Label(turnBar, c.tag, x, 4, color, order: Ui.Widget + 1);
                x += w + 8;
            }
            if (Battle.Active != null) Chip(Battle.Active, Palette.Gold, true);
            foreach (var c in Battle.Upcoming) Chip(c, c.IsEnemy ? Palette.Crimson : Palette.Bone);
            if (x < 300) { Ui.Label(turnBar, "|", x - 3, 4, Palette.Ash, order: Ui.Widget + 1); x += 5; }
            foreach (var c in Battle.NextRoundOrder) Chip(c, Palette.Ash);
        }

        void Update()
        {
            var a = Battle?.Active;
            if (a == null || !marker.enabled) return;
            var h = huds[a];
            int bob = Mathf.Sin(Time.time * 6f) > 0 ? 1 : 0;
            marker.transform.localPosition = new Vector3(h.pos.x + h.size.x / 2 - 2, -(h.pos.y - 6 + bob), 0);
        }

        void Hint(string text) => hint.Set(PixelFont.Fit(text ?? "", Ui.Width - 8));

        void AddLog(string line)
        {
            logLines.AddRange(PixelFont.Wrap(line.ToUpperInvariant(), (int)LogWidth).Split('\n'));
            while (logLines.Count > LogLines) logLines.RemoveAt(0);
            for (int i = 0; i < LogLines; i++) logLabels[i].Set(i < logLines.Count ? logLines[i] : "");
        }

        IEnumerator Loop()
        {
            Refresh();
            yield return new WaitForSeconds(0.4f);
            while (Battle.Outcome == Outcome.None)
            {
                var c = Battle.NextTurn();
                Refresh();
                if (c == null) { yield return null; continue; }

                if (c.IsEnemy)
                {
                    Hint($"{c.name.ToUpperInvariant()} ACTS");
                    yield return new WaitForSeconds(EnemyThink);
                    var act = EnemyAI.Choose(Battle, c);
                    var qte = QteFactory.Defense(act, Assist);
                    if (qte == null)
                    {
                        Telegraph(act);
                        yield return new WaitForSeconds(EnemyThink);
                        yield return Resolve(act);
                        continue;
                    }
                    yield return RunQte(qte, Telegraph(act), act);
                    if (qte.Cancelled) break;
                    yield return Resolve(act, 1f, qte is DefenseQte d ? d.Results : ((AutoQte)qte).Results);
                }
                else
                {
                    chosen = null;
                    ShowActions(c);
                    while (chosen == null) yield return null;
                    // Validated already (Choose), so a QTE never starts for an illegal action.
                    var qte = QteFactory.Offense(chosen, Battle.Tier, Assist, qteRng);
                    float power = 1f;
                    if (qte != null)
                    {
                        yield return RunQte(qte, OffenseHeading(chosen), null);
                        if (qte.Cancelled) break;
                        power = Battle.QtePower(chosen, qte.Result);
                    }
                    yield return Resolve(chosen, power);
                }
            }
            Refresh();
            ShowOutcome();
        }

        IEnumerator Resolve(BattleAction action, float power = 1f, IReadOnlyList<QteResult> defense = null)
        {
            ClearPhase();
            Hint("");
            foreach (var h in Battle.Perform(action, power, defense)) Feedback(h);
            Refresh();
            yield return new WaitForSeconds(AfterAction);
        }

        // ---- QTEs ----

        static QteAssist Assist => Game.I != null ? Game.I.Settings.Assist : QteAssist.Standard;

        static string OffenseHeading(BattleAction a) =>
            a.kind == ActionKind.Attack ? $"STRIKE {a.target.name.ToUpperInvariant()}!"
            : a.skill.kind == SkillKind.Physical ? $"{a.skill.name.ToUpperInvariant()}!"
            : $"CAST {a.skill.name.ToUpperInvariant()}";

        string Telegraph(BattleAction a)
        {
            var move = a.attack;
            if (move != null && move.self)
            {
                Hint($"{move.name.ToUpperInvariant()}: " + string.Join(", ", move.apply.Select(ap => StatusInfo.Name(ap.id).ToUpperInvariant())));
                return null;
            }
            string who = move != null && move.allTargets ? "ALL" : a.target.name.ToUpperInvariant();
            string element = move != null && move.element != Element.Physical ? " " + move.element.ToString().ToUpperInvariant() : "";
            // Telegraph: move, hit count, target, severity and element all spelled out.
            Hint(move == null ? $"{a.actor.name.ToUpperInvariant()} ATTACKS {who}"
                : $"{move.name.ToUpperInvariant()}{(move.hits > 1 ? $" X{move.hits}" : "")} > {who}  {EnemyAI.Severity(move)}{element}");
            return $"GUARD {who}!";
        }

        IEnumerator RunQte(Qte qte, string heading, BattleAction enemyAction)
        {
            ClearPhase();
            ShowLog(false);
            if (enemyAction != null)
                foreach (var t in enemyAction.attack != null && enemyAction.attack.allTargets ? Battle.Party.Where(p => p.Alive) : new[] { enemyAction.target })
                {
                    var h = huds[t];
                    Ui.Image(phase, PixelArt.FromRows(Art.TargetMarker), h.pos.x + h.size.x / 2 - 2, h.pos.y - 6, Ui.Widget + 6);
                }
            var panel = phase.gameObject.AddComponent<QtePanel>();
            panel.Init(qte, heading);
            activeQte = qte;
            if (qte is AutoQte) yield return new WaitForSeconds(enemyAction != null ? EnemyThink : 0.15f); // still show the telegraph
            while (!qte.Done)
            {
                if (Battle.Outcome != Outcome.None) qte.Cancel();
                yield return null;
            }
            activeQte = null;
            if (!qte.Cancelled && !(qte is AutoQte)) yield return new WaitForSecondsRealtime(0.45f); // let the result read
        }

        void OnDestroy() => activeQte?.Cancel(); // leaving combat mid-QTE (e.g. run abandoned)

        void Feedback(Hit hit)
        {
            var h = huds[hit.target];
            int cx = h.pos.x + h.size.x / 2;
            bool immune = hit.effect == Effectiveness.Immune;
            var popup = Ui.Label(transform, hit.blocked ? "PARRY" : immune ? "IMMUNE" : hit.amount.ToString(), cx, h.pos.y - 4,
                hit.blocked ? Palette.Frost : immune ? Palette.Ash : hit.heal ? Palette.Moss : Palette.Crimson, Align.Center, order: Ui.Widget + 10);
            popup.gameObject.AddComponent<Popup>();
            if (hit.brokeTarget)
                Ui.Label(transform, "BROKEN!", cx, h.pos.y - 21, Palette.Gold, Align.Center, order: Ui.Widget + 10).gameObject.AddComponent<Popup>();
            if (hit.effect == Effectiveness.Weak || hit.effect == Effectiveness.Resist)
                Ui.Label(transform, hit.effect == Effectiveness.Weak ? "WEAK!" : "RESIST", cx, h.pos.y - 13,
                    hit.effect == Effectiveness.Weak ? Palette.Gold : Palette.Ash, Align.Center, order: Ui.Widget + 10).gameObject.AddComponent<Popup>();
            if (!hit.heal && hit.amount > 0) StartCoroutine(Flash(h, hit.target));
        }

        IEnumerator Flash(Hud h, Combatant c)
        {
            h.sprite.color = Palette.Crimson;
            yield return new WaitForSeconds(0.12f);
            h.sprite.color = c.Alive ? Color.white : Palette.Shade;
        }

        // ---- Player input phases ----

        void ClearPhase()
        {
            if (phase != null) Destroy(phase.gameObject);
            phase = Child("Phase");
            ShowLog(true);
        }

        void ShowLog(bool on)
        {
            foreach (var l in logLabels) l.gameObject.SetActive(on);
        }

        // Command menus take over the log panel, classic-RPG style.
        Menu NewMenu()
        {
            ClearPhase();
            ShowLog(false);
            var m = phase.gameObject.AddComponent<Menu>();
            m.onFocus = s => Hint(s.tooltip ?? turnText);
            return m;
        }

        void Option(Menu m, string text, Action onClick, bool enabled = true, string tooltip = null)
        {
            var s = Ui.Button(phase, PixelFont.Fit(text, 128), LogX + 14, 116 + m.items.Count * 14, Ui.Width - LogX - 22, onClick, 12, enabled);
            s.tooltip = tooltip;
            m.Add(s);
        }

        void ShowActions(Combatant c)
        {
            turnText = $"{c.name.ToUpperInvariant()}'S TURN - {c.unit.role.ToUpperInvariant()}";
            var m = NewMenu();
            Option(m, "ATTACK", () => ChooseTarget(Battle.Opponents(c), t => BattleAction.Attack(c, t), () => ShowActions(c)));
            Option(m, "SKILL", () => ShowSkills(c));
            Option(m, "ITEM", () => ShowItems(c), Battle.Items.Count > 0, Battle.Items.Count > 0 ? null : "NO ITEMS LEFT");
            Option(m, "GUARD", () => Choose(BattleAction.Guard(c)), tooltip: "HALVE DAMAGE UNTIL NEXT TURN");
        }

        void ShowSkills(Combatant c)
        {
            var skills = c.unit.skills.Select(id => Database.Skills[id]).ToList();
            var m = NewMenu();
            foreach (var s in skills)
            {
                int cd = c.Cooldown(s.id);
                string element = s.kind == SkillKind.Magic || s.kind == SkillKind.Heal ? s.element.ToString().ToUpperInvariant() + " " : "";
                Option(m, $"{s.name.ToUpperInvariant()} {(cd > 0 ? $"CD{cd}" : $"{s.mpCost}MP")}", () => UseSkill(c, s),
                    cd == 0 && c.unit.mp >= s.mpCost, $"{s.mpCost} MP {element}- {s.description.ToUpperInvariant()}");
            }
            Option(m, "BACK", () => ShowActions(c));
            m.onBack = () => ShowActions(c);
        }

        void UseSkill(Combatant c, SkillDef s)
        {
            switch (s.target)
            {
                case TargetType.Enemy: ChooseTarget(Battle.Opponents(c), t => BattleAction.Skill(c, s, t), () => ShowSkills(c)); break;
                case TargetType.Ally: ChooseTarget(Battle.Allies(c), t => BattleAction.Skill(c, s, t), () => ShowSkills(c)); break;
                default: Choose(BattleAction.Skill(c, s, s.target == TargetType.Self ? c : null)); break;
            }
        }

        void ShowItems(Combatant c)
        {
            var stacks = Battle.Items.GroupBy(id => id).ToList();
            var m = NewMenu();
            foreach (var stack in stacks)
            {
                var item = Database.Items[stack.Key];
                Option(m, $"{item.name.ToUpperInvariant()} {stack.Count()}",
                    () => ChooseTarget(Battle.Allies(c), t => BattleAction.Item(c, item, t), () => ShowItems(c)),
                    tooltip: item.description.ToUpperInvariant());
            }
            Option(m, "BACK", () => ShowActions(c));
            m.onBack = () => ShowActions(c);
        }

        // Targets are selected directly on the battlefield sprites (keys or mouse).
        void ChooseTarget(IEnumerable<Combatant> candidates, Func<Combatant, BattleAction> make, Action back)
        {
            ClearPhase();
            var m = phase.gameObject.AddComponent<Menu>();
            m.onFocus = s => Hint(s.tooltip);
            m.onBack = back;
            foreach (var t in candidates.Where(c => c.Alive))
            {
                var h = huds[t];
                var go = new GameObject("Target:" + t.name);
                go.transform.SetParent(phase, false);
                go.transform.localPosition = new Vector3(h.pos.x, -h.pos.y, 0);
                var arrow = Ui.Image(go.transform, PixelArt.FromRows(Art.TargetMarker), h.size.x / 2 - 2, -6, Ui.Widget + 6);
                var sel = go.AddComponent<Selectable>();
                sel.size = h.size;
                string brk = t.IsEnemy && t.HasBreakGauge ? $" BRK:{t.CurrentBreakGauge}/{t.MaxBreakGauge}" : "";
                sel.tooltip = $"{t.name.ToUpperInvariant()} {t.unit.hp}/{t.Stats.maxHp}{brk} " + (t.IsEnemy ? t.Affinities.Describe() : t.StatusTags);
                sel.visual = on => arrow.enabled = on;
                sel.visual(false);
                sel.onClick = () => Choose(make(t));
                m.Add(sel);
            }
        }

        void Choose(BattleAction action)
        {
            var error = Battle.Validate(action);
            if (error != null)
            {
                Sfx.Play(Sound.Deny);
                Hint(error.ToUpperInvariant());
                return;
            }
            ClearPhase();
            chosen = action;
        }

        void ShowOutcome()
        {
            bool won = Battle.Outcome == Outcome.Victory;
            ClearPhase();
            foreach (Transform child in turnBar) Destroy(child.gameObject);
            Hint("");
            Ui.Panel(phase, 100, 40, 120, 52, Palette.Night, won ? Palette.Gold : Palette.Crimson).sortingOrder = Ui.Widget - 1;
            Ui.Label(phase, won ? "VICTORY" : "DEFEAT", 160, 48, won ? Palette.Gold : Palette.Crimson, Align.Center, 2, Ui.Widget + 1);
            var m = phase.gameObject.AddComponent<Menu>();
            m.Add(Ui.Button(phase, "CONTINUE", 125, 72, 70, () =>
            {
                if (won)
                {
                    Game.I.Run.WinCombat();
                    Game.I.Go(GameState.Reward);
                }
                else
                {
                    Game.I.Run.LoseCombat();
                    Game.I.Go(GameState.GameOver);
                }
            }));
        }
    }

    // Floating damage/heal number.
    public class Popup : MonoBehaviour
    {
        float age;

        void Update()
        {
            age += Time.deltaTime;
            transform.localPosition += Vector3.up * (12f * Time.deltaTime);
            if (age > 0.7f) Destroy(gameObject);
        }
    }
}
