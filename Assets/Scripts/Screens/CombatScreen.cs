using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 2DObscure — Battle UI (Priority 1)
// Conforming to UI/UX Visual Direction Specification (Octopath Traveler II Inspired Staging).
// Reference resolution: 640x360.
namespace Obscure
{
    public class CombatScreen : GameScreen
    {
        protected override void Build()
        {
            var enc = Run?.CurrentEncounter ?? Database.Encounters.Values.First();
            Backdrop.Create(Root, true, 16, Palette.Void, Palette.DeepCharcoal, Palette.MidnightNavy);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 80, Palette.DeepCharcoal, 5), 0, 150, Ui.Back + 5);
            Ui.Image(Root, PixelArt.Gradient(Ui.Width, 140, Palette.DeepCharcoal, Palette.Void), 0, 190, Ui.Back + 6);

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            var items = Run?.Items ?? new List<string>(Database.StartingItems);
            var rng = Run?.Rng ?? new System.Random(1);
            var battle = new Battle(party, enc.enemies.Select(id => Database.Enemies[id]), items, rng);
            if (Run != null)
            {
                foreach (var buff in Run.TakeBattleBuffs())
                    foreach (var p in battle.Party.Where(p => p.Alive)) p.AddStatus(buff.id, buff.turns, buff.magnitude, p);
            }
            Root.gameObject.AddComponent<CombatView>().Init(battle, enc);
        }
    }

    public class CombatView : MonoBehaviour
    {
        const float EnemyThink = 0.5f, AfterAction = 0.6f;
        const int ConsoleY = 240, ConsoleH = 120;
        const int PartyConsoleW = 330, CommandMenuW = 122;
        const int DossierX = 332 + CommandMenuW + 4, DossierW = Ui.Width - DossierX - 4;
        const int LogLines = 6;

        // Side-view party hero staging coordinates (640x360)
        static readonly Vector2Int[] PartySlots =
        {
            new Vector2Int(130, 85),
            new Vector2Int(155, 145),
            new Vector2Int(65, 105),
            new Vector2Int(90, 165)
        };

        public Battle Battle { get; private set; }

        class Hud
        {
            public Vector2Int pos, size;
            public SpriteRenderer sprite, cardBox, hpBar, hpBack, mpBar, mpBack, brkBar, brkBack;
            public Label name, role, hp, mp, status, shieldBadge, weaknessLabel;
            public int phase = 1;
        }

        readonly Dictionary<Combatant, Hud> huds = new Dictionary<Combatant, Hud>();
        readonly List<Label> logLabels = new List<Label>();
        readonly List<string> logLines = new List<string>();
        Transform turnBar, phase, dossierPanel;
        Label roundLabel, hint, actBanner;
        Label dossierTitle, dossierCostType, dossierTarget, dossierDesc;
        SpriteRenderer marker;
        string turnText = "";
        BattleAction chosen;
        Qte activeQte;
        readonly System.Random qteRng = new System.Random();

        static Vector2Int[] EnemySlots(int count) => count switch
        {
            1 => new[] { new Vector2Int(480, 110) },
            2 => new[] { new Vector2Int(420, 95), new Vector2Int(520, 145) },
            _ => new[] { new Vector2Int(380, 90), new Vector2Int(465, 135), new Vector2Int(550, 90) },
        };

        public void Init(Battle battle, EncounterDef encounter)
        {
            Battle = battle;
            var t = transform;

            // 1. Top 24px Header: Act banner, Round indicator, and Timeline ribbon (Spec Section 4.1)
            Ui.Panel(t, 0, 0, Ui.Width, 24, Palette.DeepCharcoal, Palette.AntiqueGold);
            string actName = encounter.tier == Tier.Boss ? "ACT 1: THE INNER SANCTUM" : "ACT 1: ASHEN RUINS";
            actBanner = Ui.Label(t, actName, 10, 6, Palette.AntiqueGoldBright, font: FontKind.Serif);
            roundLabel = Ui.Label(t, "", 185, 7, Palette.WarmIvory);
            turnBar = Child("TurnBar");

            // 2. 18px Telegraph banner under the header
            Ui.Panel(t, 0, 23, Ui.Width, 18, Palette.OverlayCharcoal, Palette.DarkBorder);
            hint = Ui.Label(t, "", Ui.Width / 2, 27, Palette.WarmIvory, Align.Center, order: Ui.Widget + 2);

            // 3. Side-view combatants
            for (int i = 0; i < battle.Party.Count; i++) AddCombatant(battle.Party[i], PartySlots[i]);
            var slots = EnemySlots(battle.Enemies.Count);
            for (int i = 0; i < battle.Enemies.Count; i++) AddCombatant(battle.Enemies[i], slots[i]);
            marker = Ui.Image(t, PixelArt.FromRows(Art.Marker), 0, 0, Ui.Widget + 5);

            // 4. Bottom 120px Console: Left 4-Hero Vitality Cards (330px), Right Command & Dossier (310px)
            Ui.Panel(t, 0, ConsoleY, PartyConsoleW, ConsoleH, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Panel(t, PartyConsoleW + 2, ConsoleY, Ui.Width - PartyConsoleW - 2, ConsoleH, Palette.DeepCharcoal, Palette.AntiqueGold);

            // Action Dossier & Combat Log feed panel (Right side of console)
            dossierPanel = Child("DossierPanel");
            Ui.Card(dossierPanel, DossierX, ConsoleY + 4, DossierW, ConsoleH - 8);
            dossierTitle = Ui.Label(dossierPanel, "", DossierX + 6, ConsoleY + 8, Palette.AntiqueGoldBright, font: FontKind.Serif);
            dossierCostType = Ui.Label(dossierPanel, "", DossierX + 6, ConsoleY + 22, Palette.WarmIvory);
            dossierTarget = Ui.Label(dossierPanel, "", DossierX + 6, ConsoleY + 34, Palette.MutedGray);
            dossierDesc = Ui.Label(dossierPanel, "", DossierX + 6, ConsoleY + 46, Palette.WarmIvory);

            for (int i = 0; i < LogLines; i++)
                logLabels.Add(Ui.Label(dossierPanel, "", DossierX + 6, ConsoleY + 8 + i * 17, Palette.WarmIvory));

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
                int cx = pos.x + h.size.x / 2, ly = pos.y + h.size.y + 4;
                var tierColor = c.enemy.tier == Tier.Boss ? Palette.SpecCrimson : c.enemy.tier == Tier.Elite ? Palette.Ember : Palette.WarmIvory;
                h.name = Ui.Label(transform, c.name.ToUpperInvariant(), cx, ly, tierColor, Align.Center, font: c.enemy.tier == Tier.Boss ? FontKind.Serif : FontKind.Interface);

                // Shield count badge & weaknesses (Spec Section 4.1)
                h.shieldBadge = Ui.Label(transform, "", cx, ly + 11, Palette.AntiqueGoldBright, Align.Center);
                h.weaknessLabel = Ui.Label(transform, "", cx, ly + 21, Palette.MutedGray, Align.Center);

                // Framed health & break bars
                h.hpBack = Bar(cx - 30, ly + 31, 60, Palette.OverlayCharcoal, 0, 4);
                h.hpBar = Bar(cx - 30, ly + 31, 60, Palette.SpecCrimson, 1, 4);
                h.brkBack = Bar(cx - 30, ly + 36, 60, Palette.OverlayCharcoal, 0, 3);
                h.brkBar = Bar(cx - 30, ly + 36, 60, Palette.AntiqueGold, 1, 3);
                h.status = Ui.Label(transform, "", cx, ly + 41, Palette.Frost, Align.Center);
                return;
            }

            // 4-Hero Vitality Cards (Spec Section 4.1): 4 spacious stacked rows inside 330px width
            int cy = ConsoleY + 5 + c.slot * 28;
            h.cardBox = Ui.Image(transform, PixelArt.Box(PartyConsoleW - 10, 26, Palette.DeepCharcoal, Palette.DarkBorder), 5, cy, Ui.Panels + 2);

            h.name = Ui.Label(transform, c.name.ToUpperInvariant(), 12, cy + 4, Palette.WarmIvory, font: FontKind.Serif);
            string roleStr = c.unit.role != null ? c.unit.role.ToUpperInvariant() : "HERO";
            h.role = Ui.Label(transform, roleStr, 12, cy + 15, Palette.AntiqueGoldDim);

            // HP readout & Emerald GaugeBar
            Ui.Label(transform, "HP", 90, cy + 4, Palette.Emerald);
            h.hp = Ui.Label(transform, "", 110, cy + 4, Palette.WarmIvory);
            h.hpBack = Bar(90, cy + 15, 95, Palette.OverlayCharcoal, 0, 5);
            h.hpBar = Bar(90, cy + 15, 95, Palette.Emerald, 1, 5);

            // MP readout & Arcane Violet GaugeBar
            Ui.Label(transform, "MP", 195, cy + 4, Palette.ArcaneViolet);
            h.mp = Ui.Label(transform, "", 215, cy + 4, Palette.WarmIvory);
            h.mpBack = Bar(195, cy + 15, 75, Palette.OverlayCharcoal, 0, 5);
            h.mpBar = Bar(195, cy + 15, 75, Palette.ArcaneViolet, 1, 5);

            h.status = Ui.Label(transform, "", 275, cy + 8, Palette.Frost);
        }

        SpriteRenderer Bar(int x, int y, int w, Color color, int layer, int h = 4)
        {
            var sr = Ui.Image(transform, PixelArt.Pixel, x, y, Ui.Widget + layer, color);
            sr.transform.localScale = new Vector3(w, h, 1);
            return sr;
        }

        static void SetBar(SpriteRenderer bar, int full, int value, int max, int h = 4) =>
            bar.transform.localScale = new Vector3(max <= 0 ? 0 : Mathf.CeilToInt(full * (float)value / max), h, 1);

        void Refresh()
        {
            roundLabel.Set($"[R{Battle.Round}]");
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
                        SetBar(h.brkBar, 60, c.CurrentBreakGauge, c.MaxBreakGauge, 3);
                        h.brkBar.color = c.IsBroken ? Palette.MutedGray : Palette.AntiqueGoldBright;
                    }
                    h.name.gameObject.SetActive(c.Alive);
                    h.status.gameObject.SetActive(c.Alive);
                    if (h.shieldBadge != null)
                    {
                        h.shieldBadge.gameObject.SetActive(c.Alive);
                        if (c.IsBroken)
                        {
                            h.shieldBadge.Set("[BREAK!] STAGGERED (150% DMG)");
                            h.shieldBadge.Color = Palette.AntiqueGoldBright;
                        }
                        else if (c.HasBreakGauge)
                        {
                            h.shieldBadge.Set($"# SHIELD: ({c.CurrentBreakGauge})");
                            h.shieldBadge.Color = Palette.AntiqueGold;
                        }
                        else
                        {
                            h.shieldBadge.Set("");
                        }
                    }
                    if (h.weaknessLabel != null)
                    {
                        h.weaknessLabel.gameObject.SetActive(c.Alive);
                        var weaks = c.Affinities?.weak;
                        if (weaks != null && weaks.Length > 0)
                        {
                            string weakStr = string.Join(" ", weaks.Select(w => $"[{ElementConfig.Short(w)}]"));
                            h.weaknessLabel.Set($"WEAK: {weakStr}");
                        }
                        else
                        {
                            h.weaknessLabel.Set("");
                        }
                    }
                    SetBar(h.hpBar, 60, c.unit.hp, c.Stats.maxHp, 4);
                    h.status.Set((c.enemy.phase2At > 0 ? $"PHASE {c.phase} " : "") + c.StatusTags);
                    h.status.Color = c.phase > 1 ? Palette.CrimsonBright : c.HasDebuff ? Palette.Ember : Palette.Frost;
                    if (c.phase != h.phase)
                    {
                        h.phase = c.phase;
                        Hint($"{c.name.ToUpperInvariant()} ENTERS PHASE {c.phase}!");
                        StartCoroutine(Flash(h, c));
                    }
                    continue;
                }

                h.sprite.color = c.Alive ? Color.white : Palette.Shade;
                bool isActive = c == Battle.Active;
                if (h.cardBox != null)
                {
                    h.cardBox.sprite = PixelArt.Box(PartyConsoleW - 10, 26,
                        isActive ? Palette.MidnightNavy : Palette.DeepCharcoal,
                        isActive ? Palette.AntiqueGold : Palette.DarkBorder);
                }
                h.name.Color = !c.Alive ? Palette.SpecCrimson : isActive ? Palette.AntiqueGoldBright : Palette.WarmIvory;
                h.hp.Set($"{c.unit.hp}/{c.Stats.maxHp}");
                h.mp.Set(c.Alive ? $"{c.unit.mp}/{c.Stats.maxMp}" : "DOWN");
                h.mp.Color = c.Alive ? Palette.WarmIvory : Palette.SpecCrimson;
                h.status.Set(c.Alive ? PixelFont.Fit(c.StatusTags, 40) : "");
                h.status.Color = c.HasDebuff ? Palette.SpecCrimson : Palette.Frost;

                bool lowHp = c.unit.hp * 4 <= c.Stats.maxHp;
                h.hpBar.color = lowHp ? Palette.SpecCrimson : Palette.Emerald;
                SetBar(h.hpBar, 95, c.unit.hp, c.Stats.maxHp, 5);
                SetBar(h.mpBar, 75, c.unit.mp, c.Stats.maxMp, 5);
            }
            marker.enabled = Battle.Active != null;
            BuildTurnBar();
        }

        // Timeline ribbon with active combatant highlighted and future turns chained
        void BuildTurnBar()
        {
            foreach (Transform child in turnBar) Destroy(child.gameObject);
            int x = 230;
            Ui.Label(turnBar, "TIMELINE:", x - 72, 7, Palette.AntiqueGoldDim);

            void Chip(Combatant c, Color color, bool active = false)
            {
                int w = PixelFont.Measure(c.tag);
                if (x + w > Ui.Width - 14) return;
                if (active)
                {
                    Ui.Image(turnBar, PixelArt.ButtonBox(w + 12, 16, true, true), x - 4, 4, Ui.Widget);
                    Ui.Label(turnBar, ">" + c.tag, x - 1, 6, Palette.AntiqueGoldBright, order: Ui.Widget + 1);
                    x += w + 16;
                }
                else
                {
                    Ui.Image(turnBar, PixelArt.Box(w + 8, 16, c.IsEnemy ? Palette.DeepCharcoal : Palette.MidnightNavy, c.IsEnemy ? Palette.Blood : Palette.DarkBorder), x - 3, 4, Ui.Widget);
                    Ui.Label(turnBar, c.tag, x + 1, 6, color, order: Ui.Widget + 1);
                    x += w + 12;
                }
            }

            if (Battle.Active != null) Chip(Battle.Active, Palette.AntiqueGoldBright, true);
            foreach (var c in Battle.Upcoming) Chip(c, c.IsEnemy ? Palette.SpecCrimson : Palette.WarmIvory);
            if (x < Ui.Width - 30) { Ui.Label(turnBar, "||", x - 2, 7, Palette.MutedGray, order: Ui.Widget + 1); x += 12; }
            foreach (var c in Battle.NextRoundOrder) Chip(c, Palette.MutedGray);
        }

        void Update()
        {
            var a = Battle?.Active;
            if (a == null || !marker.enabled) return;
            var h = huds[a];
            int bob = Mathf.Sin(Time.time * 6f) > 0 ? 1 : 0;
            marker.transform.localPosition = new Vector3(h.pos.x + h.size.x / 2 - 2, -(h.pos.y - 8 + bob), 0);
        }

        void Hint(string text) => hint.Set(PixelFont.Fit(text ?? "", Ui.Width - 16));

        void AddLog(string line)
        {
            logLines.AddRange(PixelFont.Wrap(line.ToUpperInvariant(), DossierW - 12).Split('\n'));
            while (logLines.Count > LogLines) logLines.RemoveAt(0);
            for (int i = 0; i < LogLines; i++) logLabels[i].Set(i < logLines.Count ? logLines[i] : "");
        }

        void ShowDossier(string title, string costType, string target, string desc)
        {
            foreach (var l in logLabels) l.gameObject.SetActive(false);
            dossierTitle.gameObject.SetActive(true);
            dossierCostType.gameObject.SetActive(true);
            dossierTarget.gameObject.SetActive(true);
            dossierDesc.gameObject.SetActive(true);

            dossierTitle.Set(title);
            dossierCostType.Set(costType);
            dossierTarget.Set(target);
            dossierDesc.Set(PixelFont.Wrap(desc, DossierW - 12));
        }

        void HideDossier()
        {
            dossierTitle.gameObject.SetActive(false);
            dossierCostType.gameObject.SetActive(false);
            dossierTarget.gameObject.SetActive(false);
            dossierDesc.gameObject.SetActive(false);
            foreach (var l in logLabels) l.gameObject.SetActive(true);
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
            HideDossier();
            foreach (var h in Battle.Perform(action, power, defense)) Feedback(h);
            Refresh();
            yield return new WaitForSeconds(AfterAction);
        }

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
            Hint(move == null ? $"{a.actor.name.ToUpperInvariant()} ATTACKS {who}"
                : $"{move.name.ToUpperInvariant()}{(move.hits > 1 ? $" X{move.hits}" : "")} > {who}  {EnemyAI.Severity(move)}{element}");
            return $"GUARD {who}!";
        }

        IEnumerator RunQte(Qte qte, string heading, BattleAction enemyAction)
        {
            ClearPhase();
            HideDossier();
            if (enemyAction != null)
                foreach (var t in enemyAction.attack != null && enemyAction.attack.allTargets ? Battle.Party.Where(p => p.Alive) : new[] { enemyAction.target })
                {
                    var h = huds[t];
                    Ui.Image(phase, PixelArt.FromRows(Art.TargetMarker), h.pos.x + h.size.x / 2 - 2, h.pos.y - 8, Ui.Widget + 6);
                }
            var panel = phase.gameObject.AddComponent<QtePanel>();
            panel.Init(qte, heading);
            activeQte = qte;
            if (qte is AutoQte) yield return new WaitForSeconds(enemyAction != null ? EnemyThink : 0.15f);
            while (!qte.Done)
            {
                if (Battle.Outcome != Outcome.None) qte.Cancel();
                yield return null;
            }
            activeQte = null;
            if (!qte.Cancelled && !(qte is AutoQte)) yield return new WaitForSecondsRealtime(0.45f);
        }

        void OnDestroy() => activeQte?.Cancel();

        void Feedback(Hit hit)
        {
            var h = huds[hit.target];
            int cx = h.pos.x + h.size.x / 2;
            bool immune = hit.effect == Effectiveness.Immune;
            var popup = Ui.Label(transform, hit.blocked ? "PARRY" : immune ? "IMMUNE" : hit.amount.ToString(), cx, h.pos.y - 6,
                hit.blocked ? Palette.FrostBright : immune ? Palette.MutedGray : hit.heal ? Palette.Emerald : Palette.CrimsonBright, Align.Center, order: Ui.Widget + 10);
            popup.gameObject.AddComponent<Popup>();
            if (hit.brokeTarget)
                Ui.Label(transform, "@ BREAK! @", cx, h.pos.y - 25, Palette.AntiqueGoldBright, Align.Center, order: Ui.Widget + 10, font: FontKind.Serif).gameObject.AddComponent<Popup>();
            if (hit.effect == Effectiveness.Weak || hit.effect == Effectiveness.Resist)
                Ui.Label(transform, hit.effect == Effectiveness.Weak ? "* WEAK! *" : "RESIST", cx, h.pos.y - 16,
                    hit.effect == Effectiveness.Weak ? Palette.AntiqueGoldBright : Palette.MutedGray, Align.Center, order: Ui.Widget + 10).gameObject.AddComponent<Popup>();
            if (!hit.heal && hit.amount > 0) StartCoroutine(Flash(h, hit.target));
        }

        IEnumerator Flash(Hud h, Combatant c)
        {
            h.sprite.color = Palette.SpecCrimson;
            yield return new WaitForSeconds(0.12f);
            h.sprite.color = c.Alive ? Color.white : Palette.Shade;
        }

        void ClearPhase()
        {
            if (phase != null) Destroy(phase.gameObject);
            phase = Child("Phase");
            HideDossier();
        }

        Menu NewMenu()
        {
            ClearPhase();
            var m = phase.gameObject.AddComponent<Menu>();
            m.onFocus = s =>
            {
                Hint(s.tooltip ?? turnText);
                if (s.tooltip != null)
                {
                    ShowDossier(s.label != null ? s.label.Text : "ACTION", "", "", s.tooltip);
                }
                else
                {
                    HideDossier();
                }
            };
            return m;
        }

        void Option(Menu m, string text, Action onClick, bool enabled = true, string tooltip = null, string costType = null, string target = null)
        {
            var s = Ui.Button(phase, PixelFont.Fit(text, CommandMenuW - 16), PartyConsoleW + 6, ConsoleY + 6 + m.items.Count * 22, CommandMenuW, onClick, 20, enabled);
            s.tooltip = tooltip;
            m.Add(s);
        }

        void ShowActions(Combatant c)
        {
            turnText = $"{c.name.ToUpperInvariant()}'S TURN — {c.unit.role.ToUpperInvariant()}";
            var m = NewMenu();
            Option(m, "ATTACK", () => ChooseTarget(Battle.Opponents(c), t => BattleAction.Attack(c, t), () => ShowActions(c)), tooltip: "STRIKE ENEMY WITH WEAPON");
            Option(m, "SKILL", () => ShowSkills(c), tooltip: "EXECUTE CLASS SKILLS & SPELLS");
            Option(m, "ITEM", () => ShowItems(c), Battle.Items.Count > 0, Battle.Items.Count > 0 ? "USE A BATTLE INVENTORY ITEM" : "NO ITEMS REMAINING");
            Option(m, "GUARD", () => Choose(BattleAction.Guard(c)), tooltip: "HALVE INCOMING DAMAGE UNTIL NEXT TURN");
        }

        void ShowSkills(Combatant c)
        {
            var skills = c.unit.skills.Select(id => Database.Skills[id]).ToList();
            var m = NewMenu();
            foreach (var s in skills)
            {
                int cd = c.Cooldown(s.id);
                string element = s.kind == SkillKind.Magic || s.kind == SkillKind.Heal ? s.element.ToString().ToUpperInvariant() + " " : "";
                string costText = cd > 0 ? $"CD {cd}" : $"{s.mpCost} MP";
                string typeText = $"{element}{s.kind.ToString().ToUpperInvariant()}";
                string targetText = $"TARGET: {s.target.ToString().ToUpperInvariant()}";
                Option(m, $"{s.name.ToUpperInvariant()} {costText}", () => UseSkill(c, s),
                    cd == 0 && c.unit.mp >= s.mpCost, $"{s.description.ToUpperInvariant()}",
                    $"COST: {costText}  TYPE: {typeText}", targetText);
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
                Option(m, $"{item.name.ToUpperInvariant()} x{stack.Count()}",
                    () => ChooseTarget(Battle.Allies(c), t => BattleAction.Item(c, item, t), () => ShowItems(c)),
                    tooltip: item.description.ToUpperInvariant());
            }
            Option(m, "BACK", () => ShowActions(c));
            m.onBack = () => ShowActions(c);
        }

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
                var arrow = Ui.Image(go.transform, PixelArt.FromRows(Art.TargetMarker), h.size.x / 2 - 2, -8, Ui.Widget + 6);
                var sel = go.AddComponent<Selectable>();
                sel.size = h.size;
                string brk = t.IsEnemy && t.HasBreakGauge ? $" SHIELD:{t.CurrentBreakGauge}/{t.MaxBreakGauge}" : "";
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
            Ui.Panel(phase, 180, 100, 280, 120, Palette.DeepCharcoal, won ? Palette.AntiqueGold : Palette.SpecCrimson).sortingOrder = Ui.Widget - 1;
            Ui.Divider(phase, 190, 106, 260, won ? Palette.AntiqueGoldDim : Palette.Blood, won ? Palette.AntiqueGoldBright : Palette.SpecCrimson);
            Ui.Label(phase, won ? "VICTORY" : "DEFEAT", 320, 120, won ? Palette.AntiqueGoldBright : Palette.SpecCrimson, Align.Center, 2, Ui.Widget + 1, font: FontKind.Serif);
            var m = phase.gameObject.AddComponent<Menu>();
            m.Add(Ui.Button(phase, "CONTINUE", 250, 170, 140, () =>
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
            }, 22));
        }
    }

    public class Popup : MonoBehaviour
    {
        float age;
        Vector3 startPos;
        bool init;

        void Update()
        {
            if (!init) { startPos = transform.localPosition; init = true; }
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / 0.75f);
            float yOff = Mathf.Sin(t * Mathf.PI * 0.5f) * 18f;
            transform.localPosition = startPos + new Vector3(0, yOff, 0);
            if (age > 0.75f) Destroy(gameObject);
        }
    }
}
